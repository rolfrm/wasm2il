using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using TypeReference = Mono.Cecil.TypeReference;

namespace Wasm2Il
{
    using instr = Wasm.Instruction;
    using IlInstr = OpCodes;

    public enum ImportType : byte
    {
        FUNC = 0,
        TABLE = 1,
        MEM = 2,
        GLOBAL = 3
    }

    public enum Section : byte
    {
        CUSTOM = 0,
        TYPE = 1,
        IMPORT = 2,
        FUNCTION = 3,
        TABLE = 4,
        MEMORY = 5,
        GLOBAL = 6,
        EXPORT = 7,
        START = 8,
        ELEMENT = 9,
        CODE = 10,
        DATA = 11
    }

    public class Transformer
    {
        struct TypeId
        {
            public uint ParamCount;
            public uint ReturnCount;
            public TypeReference[] ParamTypes;
            public TypeReference ReturnType;
        }

        struct ImportFunc
        {
            public string Name;
            public string Module;
            public uint Index;
            public uint? TypeId;
            public MethodReference? Method;
            public string CustomName;

            public override string ToString()
            {
                return $"import: {Name}";
            }
        }

        struct ExportTable
        {
            public string Name;
            public uint Index;
        }

        class Global
        {
            public bool Const;
            public byte Type;
            public object? Value;
            public FieldDefinition? Field;
        }

        public class FuncDeclType
        {
            public MethodDefinition? Method;
            public uint TypeId;
            public bool IsDefaultName;
            public string? ImportName { get; set; }
            public override string ToString() => Method?.Name ?? "Func?";
        }

        const string magicHeader = "\0asm";
        const uint page_size = 1 << 16;
        Dictionary<uint, Global> globals = new Dictionary<uint, Global>();
        Dictionary<uint, ImportFunc> ExportFunc = new Dictionary<uint, ImportFunc>();
        private Dictionary<uint, ImportFunc> ImportFuncs = new();
        Dictionary<uint, ExportTable> ExportTables = new Dictionary<uint, ExportTable>();

        Dictionary<uint, TypeId> Types = new Dictionary<uint, TypeId>();

        // function declaration to function type
        Dictionary<uint, FuncDeclType> FuncDecl = new Dictionary<uint, FuncDeclType>();
        AssemblyDefinition def;
        TypeDefinition cls;
        FieldDefinition memoryField;
        FieldDefinition functionTable;

        TypeReference f32Type, f64Type, i64Type, i32Type, voidType, byteType;

        MethodReference resolveTypeConstructor(Type t, params Type[] argTypes)
        {
            return def.MainModule.ImportReference(
                t.GetConstructors().FirstOrDefault(x =>
                    x.GetParameters().Select(y => y.ParameterType).SequenceEqual(argTypes)));
        }

        // note there are also globals which are added dynamically depending on need.

        void Init(string asmName)
        {
            var asmName2 = new AssemblyNameDefinition(asmName, Version.Parse("1.0.0"));
            var asm = AssemblyDefinition.CreateAssembly(asmName2, "Test", ModuleKind.Dll);
            
            f32Type = asm.MainModule.TypeSystem.Single;
            f64Type = asm.MainModule.TypeSystem.Double;
            i64Type = asm.MainModule.TypeSystem.Int64;
            i32Type = asm.MainModule.TypeSystem.Int32;
            voidType = asm.MainModule.TypeSystem.Void;
            byteType = asm.MainModule.TypeSystem.Byte;
            cls = new TypeDefinition(asmName, "Code",
                TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class |
                TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public,
                asm.MainModule.TypeSystem.Object);

            asm.MainModule.Types.Add(cls);
            def = asm;

            memoryField = new FieldDefinition("Memory", FieldAttributes.Static | FieldAttributes.Private,
                asm.MainModule.TypeSystem.Byte.MakeArrayType());
            memoryField.IsStatic = true;
            // todo: Figure out how to init based on data.
            cls.Fields.Add(memoryField);

            functionTable = new FieldDefinition("FunctionTable", FieldAttributes.Static | FieldAttributes.Private,
                asm.MainModule.TypeSystem.Object.MakeArrayType());
            cls.Fields.Add(functionTable);
            var cctor = new MethodDefinition(".cctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static |
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, asm.MainModule.TypeSystem.Void);
            cls.Methods.Add(cctor);
            var cctoril = cctor.Body.GetILProcessor();
            cctoril.Emit(OpCodes.Nop);
            cctoril.Emit(OpCodes.Ret);
            def = asm;
        }

        public void Go(Stream str, string asmName, string outpath)
        {
            var reader = new BinReader(str);
            var header = reader.ReadStrl(4);
            if (magicHeader != header)
                throw new Exception("invalid header");
            var wasmVersion = new byte[4];
            reader.Read(wasmVersion);
            if (!wasmVersion.SequenceEqual(new byte[] {1, 0, 0, 0}))
                throw new Exception("Unsupported wasm version");
            Console.WriteLine("Wasm Version: {0}", string.Join(" ", wasmVersion));

            Init(asmName);
            long codeLoc = 0;
            long elementLoc = 0;
            while (!reader.ReadToEnd())
            {
                var section = (Section) reader.ReadU8();
                uint length = reader.ReadU32Leb();
                Console.WriteLine("Reading section {0}: {1}bytes", section, length);
                var next = str.Position + length;

                switch (section)
                {
                    case Section.CUSTOM:
                    {
                        ReadCustomSection(reader);
                        str.Position = next;
                        break;
                    }
                    case Section.TYPE:
                        ReadTypeSection(reader);
                        break;
                    case Section.FUNCTION:
                        ReadFunctionSection(reader);
                        break;
                    case Section.MEMORY:
                        ReadMemorySection(reader);
                        break;
                    case Section.GLOBAL:
                        ReadGlobalSection(reader);
                        break;
                    case Section.EXPORT:
                        ReadExportSection(reader);
                        break;
                    case Section.IMPORT:
                        ReadImportSection(reader);
                        break;
                    case Section.CODE:
                        codeLoc = reader.Position;
                        goto case default;
                    case Section.DATA:
                        ReadDataSection(reader);
                        break;
                    case Section.ELEMENT:
                        elementLoc = reader.Position;
                        goto case default;
                    default:
                        str.Position = next;
                        break;
                }

                // check that section was properly read.
                Assert.AreEqual(next, str.Position);
            }
            reader.Position = elementLoc;
            ReadElementSection(reader);

            reader.Position = codeLoc;
            ReadCodeSection(reader);

            def.Write(outpath);
            Console.WriteLine("Output written to " + outpath);
            def.Dispose();
        }

        private void ReadImportSection(BinReader reader)
        {
            var importCount = reader.ReadU32Leb();
            for (uint i = 0; i < importCount; i++)
            {
                var moduleName = reader.ReadStrN();
                var itemName = reader.ReadStrN();
                var type = (ImportType) reader.ReadU8();
                switch (type)
                {
                    case ImportType.FUNC:
                        var typeid = reader.ReadU32Leb();
                        var funid = (uint) ImportFuncs.Count;
                        ImportFuncs[funid] = new ImportFunc()
                            {Name = itemName, TypeId = typeid, Index = funid, Module = moduleName};
                        break;
                    case ImportType.TABLE:
                        var elemType = reader.ReadU8();
                        Assert.AreEqual(elemType, 0x70);
                        byte limitt = reader.ReadU8();
                        uint min = 0, max = 0;
                        if (limitt == 0)
                        {
                            min = reader.ReadU32Leb();
                            max = min;
                        }
                        else
                        {
                            min = reader.ReadU32Leb();
                            max = reader.ReadU32Leb();
                        }

                        Console.WriteLine("Table: {0}.{1} {2}-{3}", moduleName, itemName, min, max);
                        break;
                    case ImportType.GLOBAL:
                        var valType = reader.ReadU8();
                        bool mut = reader.ReadU8() > 0;

                        Console.WriteLine("Global: {0}.{1} {2}-{3}", moduleName, itemName, valType, mut);
                        break;
                    case ImportType.MEM:
                        elemType = reader.ReadU8();
                        Assert.AreEqual(elemType, 0x70);
                        limitt = reader.ReadU8();
                        if (limitt == 0)
                        {
                            min = reader.ReadU32Leb();
                            max = min;
                        }
                        else
                        {
                            min = reader.ReadU32Leb();
                            max = reader.ReadU32Leb();
                        }

                        Console.WriteLine("Memory: {0}.{1} {2}-{3}", moduleName, itemName, min, max);

                        break;
                }
            }
        }

        private void ReadElementSection(BinReader reader)
        {
            var cnt = reader.ReadU32Leb();
            for (int i = 0; i < cnt; i++)
            {
                var table_index = reader.ReadU32Leb();
                var instr2 = (instr) reader.ReadU8();
                Assert.AreEqual(Wasm.Instruction.I32_CONST, instr2);
                var offset = reader.ReadU32Leb();
                var end = (instr) reader.ReadU8();
                var fncCnt = reader.ReadU32Leb();

                var ctor = cls.GetStaticConstructor();
                ctor.Body.Instructions.RemoveAt(ctor.Body.Instructions.Count - 1);
                var il = ctor.Body.GetILProcessor();
                il.Emit(OpCodes.Ldc_I4, (int) fncCnt + 1);
                il.Emit(OpCodes.Newarr, def.MainModule.TypeSystem.Object);
                il.Emit(OpCodes.Stsfld, functionTable);

                for (var i2 = 0; i2 < fncCnt; i2++)
                {
                    il.Emit(OpCodes.Ldsfld, functionTable);
                    il.Emit(OpCodes.Ldc_I4, (int) i2 + 1);

                    il.Emit(OpCodes.Ldnull);
                    var funcId = reader.ReadU32Leb();
                    var importFunc = FuncDecl[(uint) (funcId - ImportFuncs.Count)];
                    var t = Types[FuncDecl[(uint) (funcId - ImportFuncs.Count)].TypeId];
                    il.Emit(OpCodes.Ldftn, FuncDecl[(uint) (funcId - ImportFuncs.Count)].Method);
                    var ftype = typeToFunc(t);
                    var constr = ftype.GetConstructors().First();
                    var cref = def.MainModule.ImportReference(constr);
                    il.Emit(OpCodes.Newobj, cref);
                    il.Emit(OpCodes.Stelem_Any, def.MainModule.TypeSystem.Object);
                }

                il.Emit(IlInstr.Ret);
            }
        }

        Type typeToFunc(TypeId id)
        {
            if (id.ParamCount == 0)
            {
                if (id.ReturnCount == 0) return typeof(Action);
                return typeof(Func<>).MakeGenericType(refToType(id.ReturnType));
            }
            else
            {
                Type baseType = null;
                if (id.ReturnCount == 0)
                {
                    switch (id.ParamCount)
                    {
                        case 1:
                            baseType = typeof(Action<>);
                            break;
                        case 2:
                            baseType = typeof(Action<,>);
                            break;
                        case 3:
                            baseType = typeof(Action<,,>);
                            break;
                        case 4:
                            baseType = typeof(Action<,,,>);
                            break;
                        case 5:
                            baseType = typeof(Action<,,,,>);
                            break;
                        case 6:
                            baseType = typeof(Action<,,,,,>);
                            break;
                        case 7:
                            baseType = typeof(Action<,,,,,,>);
                            break;
                    }
                }
                else
                {
                    switch (id.ParamCount)
                    {
                        case 0:
                            baseType = typeof(Func<>);
                            break;
                        case 1:
                            baseType = typeof(Func<,>);
                            break;
                        case 2:
                            baseType = typeof(Func<,,>);
                            break;
                        case 3:
                            baseType = typeof(Func<,,,>);
                            break;
                        case 4:
                            baseType = typeof(Func<,,,,>);
                            break;
                        case 5:
                            baseType = typeof(Func<,,,,,>);
                            break;
                        case 6:
                            baseType = typeof(Func<,,,,,,>);
                            break;
                    }
                }

                if (id.ReturnCount == 0)
                {
                    return baseType.MakeGenericType(id.ParamTypes.Select(refToType).ToArray());
                }

                return baseType.MakeGenericType(id.ParamTypes.Select(refToType).Append(refToType(id.ReturnType))
                    .ToArray());
            }
        }

        Type refToType(TypeReference r)
        {
            if (r == i32Type) return typeof(int);
            if (r == i64Type) return typeof(long);
            if (r == f32Type) return typeof(float);
            if (r == f64Type) return typeof(double);
            return typeof(void);
        }

        void ReadDataSection(BinReader reader)
        {
            uint dataCount = reader.ReadU32Leb();
            for (int i = 0; i < dataCount; i++)
            {
                uint memidx = reader.ReadU32Leb();
                // memory index is normally 0.
                bool isGlobal = false;
                int offset = 0;
                while (true)
                {
                    var instr = (instr) reader.ReadU8();
                    switch (instr)
                    {
                        case instr.I32_CONST:
                            var _offset = (int) reader.ReadI64Leb();
                            offset = _offset;
                            break;
                        case instr.GLOBAL_GET:
                            throw new Exception("Check this!");
                            _offset = (int) reader.ReadI64Leb();
                            offset = _offset;
                            isGlobal = true;
                            break;
                        case instr.END:
                            goto read_end;
                        default:
                            throw new Exception("Unknown instruction");
                    }
                }

                read_end: ;
                // load the data into the heap one byte at a time.
                // consider finding a way to load it based on static data instead.
                uint byteCount = reader.ReadU32Leb();
                byte[] bc = new byte[byteCount];
                reader.Read(bc);
                var cctor = cls.GetStaticConstructor();
                var il = cctor.Body.GetILProcessor();
                il.RemoveAt(cctor.Body.Instructions.Count - 1); // remove RET

                int acc = 0;
                il.Emit(IlInstr.Ldsfld, memoryField);
                for (int i2 = 0; i2 < byteCount; i2++)
                {
                    if (bc[i2] != 0)
                    {
                        il.Emit(IlInstr.Dup);
                        il.Emit(IlInstr.Ldc_I4, (int) i2 + offset);
                        il.Emit(IlInstr.Ldc_I4, (int) bc[i2]);
                        il.Emit(IlInstr.Stelem_I1);
                    }

                    acc += 1;
                }

                il.Emit(IlInstr.Pop);
                il.Emit(IlInstr.Ret);
            }
        }

        class LabelType
        {
            public byte Type;
            public Instruction? EndLabel;
            public bool Forward;
            public Instruction? StartLabel;
        }

        private Dictionary<string, MethodReference?> methodCache = new Dictionary<string, MethodReference?>();

        MethodReference? methodFromName(string name)
        {
            if (name == null) return null;
            if (methodCache.TryGetValue(name, out var m))
            {
                return m;
            }

            var imp = typeof(Wasi);
            var method = imp.GetMethod(name);
            if (method != null)
            {
                m = def.MainModule.ImportReference(method);
                return methodCache[name] = m;
            }

            return methodCache[name] = null;
        }

        MethodReference? resolveMethod(uint func)
        {
            if (func < ImportFuncs.Count)
            {
                var importFun = ImportFuncs[func];
                if (importFun.Method == null)
                {
                    var type = Types[(uint) importFun.TypeId];
                    var method = methodFromName(importFun.Name);
                    if (method != null)
                    {
                        importFun.Method = method;
                        return method;
                    }

                    var m = new MethodDefinition(importFun.Name.Replace(":", "_"),
                        MethodAttributes.Static | MethodAttributes.Public,
                        type.ReturnType);
                    m.Body.InitLocals = true;
                    var il = m.Body.GetILProcessor();
                    il.Emit(IlInstr.Ldstr, "Not Implemented");
                    il.Emit(IlInstr.Newobj, resolveTypeConstructor(typeof(NotImplementedException), typeof(string)));
                    il.Emit(IlInstr.Throw);
                    importFun.Method = m;
                    cls.Methods.Add(m);
                    foreach (var param in type.ParamTypes)
                    {
                        m.Parameters.Add(new ParameterDefinition(param));
                    }
                }

                return importFun.Method;
            }

            var decl = FuncDecl[func - (uint) ImportFuncs.Count];
            var declFun = decl.Method;
            return declFun;
        }

        void ReadCodeSection(BinReader reader)
        {
            uint funcCount = reader.ReadU32Leb();
            for (uint i = 0; i < funcCount; i++)
            {
                var funcId = FuncDecl[i];
                var ftype = Types[funcId.TypeId];
                string name = funcId.ImportName;
                if (ExportFunc.TryGetValue((uint) (i + ImportFuncs.Count), out var exp))
                {
                    name = exp.Name;
                }

                if (string.IsNullOrEmpty(name))
                {
                    name = "Func" + i;
                    funcId.IsDefaultName = true;
                }


                var m1 = funcId.Method;
                m1.ReturnType = ftype.ReturnType;
                m1.Name = name;
                for (uint i2 = 0; i2 < ftype.ParamCount; i2++)
                {
                    var parameter = new ParameterDefinition(ftype.ParamTypes[i2]);
                    parameter.Name = "param" + i2;
                    m1.Parameters.Add(parameter);
                }
            }

            var wasi = typeof(Wasi);

            for (uint i = 0; i < funcCount; i++)
            {
                var funcId = FuncDecl[i];
                var ftype = Types[funcId.TypeId];
                var m1 = funcId.Method;

                var wasiMethod = wasi.GetMethod(m1.Name);
                if (wasiMethod != null)
                {
                    var m2 = new MethodDefinition(wasiMethod.Name + "_pre",
                        MethodAttributes.Static | MethodAttributes.Public,
                        ftype.ReturnType);
                    cls.Methods.Add(m1);
                    var il2 = m1.Body.GetILProcessor();
                    m1.Body.InitLocals = true;
                    var wasiMethod2 = def.MainModule.ImportReference(wasiMethod);
                    if (wasiMethod2.Parameters.Count != m1.Parameters.Count + 1)
                    {
                        throw new Exception("Unmatched paramters");
                    }
                    if(wasiMethod2.ReturnType.FullName != m1.ReturnType.FullName)
                        throw new Exception("Unmatched return type.");
                    for(int i2 = 0; i2 < m1.Parameters.Count; i2++)
                    {
                        var p = m1.Parameters[i2];
                        il2.Emit(IlInstr.Ldarg, p);
                        if (wasiMethod2.Parameters[i2].ParameterType.FullName != p.ParameterType.FullName)
                            throw new Exception("Unmatched parameters types");
                    }

                    il2.Emit(IlInstr.Ldtoken, cls);
                    il2.Emit(IlInstr.Call,
                        def.MainModule.ImportReference(
                            wasi.GetMethod(nameof(Wasi.GetContext))));
                    il2.Emit(IlInstr.Call, wasiMethod2);
                    il2.Emit(IlInstr.Ret);
                    
                   
                    for (uint i2 = 0; i2 < ftype.ParamCount; i2++)
                    {
                        var parameter = new ParameterDefinition(ftype.ParamTypes[i2]);
                        parameter.Name = "param" + i2;
                        m2.Parameters.Add(parameter);
                    }
                    

                    m1 = m2;
                    Console.WriteLine("Override: {0}", wasiMethod);
                }
                cls.Methods.Add(m1);
                m1.Body.InitLocals = true;
                var il = m1.Body.GetILProcessor();
                il.Emit(IlInstr.Nop);

                var codeSize = reader.ReadU32Leb();

                var next = reader.Position + codeSize;

                var localCount = reader.ReadU32Leb();
                uint localTotal = 0;
                for (uint i2 = 0; i2 < localCount; i2++)
                {
                    uint n = reader.ReadU32Leb();
                    var t = reader.ReadU8();
                    localTotal += n;
                    for (uint i3 = 0; i3 < n; i3++)
                    {
                        var tp = ByteToTypeReference(t);
                        var lv_y_4 = new VariableDefinition(tp);
                        m1.Body.Variables.Add(lv_y_4);
                    }
                }

                Dictionary<int, Dictionary<TypeReference, VariableDefinition>> helperVars = new();

                VariableDefinition getVariable(TypeReference tr, int idx = 0)
                {
                    if (helperVars.ContainsKey(idx) == false)
                        helperVars[idx] = new();
                    var dict = helperVars[idx];
                    if (tr == voidType) throw new Exception("void type");
                    if (dict.TryGetValue(tr, out var x))
                        return x;
                    var v = new VariableDefinition(tr);
                    m1.Body.Variables.Add(v);
                    dict[tr] = v;
                    return v;
                }

                var heapaddr = new VariableDefinition(def.MainModule.TypeSystem.Int32);
                m1.Body.Variables.Add(heapaddr);

                m1.Body.InitLocals = true;
                int codeidx = 0;
                var labelStack = new List<LabelType>();
                labelStack.Add(new LabelType()); // base label
                List<instr> instructions = new List<instr>();

                // to satisfy SELECT.
                Stack<TypeReference> top = new Stack<TypeReference>();

                void push(TypeReference? tr)
                {
                    if (tr == null) throw new Exception("??");
                    if (tr != voidType)
                        top.Push(tr);
                }

                TypeReference pop(int i = 1)
                {
                    if (i == 0) return default;
                    while (i > 1)
                    {
                        top.Pop();
                        i--;
                    }

                    return top.Pop();
                }

                var start = reader.Position + 1;
                while (next > reader.Position)
                {
                    var instr = (instr) reader.ReadU8();

                    TypeReference instrType()
                    {
                        var s = instr.ToString();
                        if (s.Contains("F32")) return f32Type;
                        if (s.Contains("F64")) return f64Type;
                        if (s.Contains("I32")) return i32Type;
                        if (s.Contains("I64")) return i64Type;
                        return voidType;
                    }

                    Type instrType2(bool unsigned = false)
                    {
                        var s = instr.ToString();
                        if (s.Contains("F32")) return typeof(float);
                        if (s.Contains("F64")) return typeof(double);
                        if (s.Contains("I32")) return unsigned ? typeof(uint) : typeof(int);
                        if (s.Contains("I64")) return unsigned ? typeof(ulong) : typeof(long);
                        return typeof(void);
                    }

                    MethodReference getMethod(Type classT, string method, params Type[] argTypes)
                    {
                        var csm = classT.GetMethod(method, BindingFlags.Static | BindingFlags.Public, argTypes);
                        return def.MainModule.ImportReference(csm);
                    }

                    bool is64 = instr.ToString().Contains("64");
                    instructions.Add(instr);
                    codeidx++;
                    switch (instr)
                    {
                        case instr.NOP:
                            il.Emit(IlInstr.Nop);
                            break;
                        case instr.CALL:
                            var fcn = reader.ReadU32Leb();
                            var otherFun = resolveMethod(fcn);
                            if (otherFun == null)
                                throw new Exception("");
                            if (otherFun.DeclaringType?.Name == nameof(Wasi))
                            {
                                il.Emit(IlInstr.Ldtoken, cls);
                                il.Emit(IlInstr.Call,
                                    def.MainModule.ImportReference(
                                        typeof(Wasi).GetMethod(nameof(Wasi.GetContext))));
                            }

                            il.Emit(IlInstr.Call, otherFun);
                            if (otherFun.DeclaringType?.Name == nameof(Wasi))
                            {
                                pop(otherFun.Parameters.Count - 1);
                            }
                            else
                            {
                                pop(otherFun.Parameters.Count);
                            }

                            push(otherFun.ReturnType);
                            break;
                        case instr.CALL_INDIRECT:
                            var typeidx = reader.ReadU32Leb();
                            var table = reader.ReadU8();
                            Assert.AreEqual(0, table);
                            var ftp = Types[typeidx];
                            // function ID is top of the stack.
                            // store id

                            il.Emit(IlInstr.Stloc, getVariable(i32Type));
                            for (int _i2 = 0; _i2 < ftp.ParamCount; _i2++)
                            {
                                var i2 = ftp.ParamCount - _i2 - 1;
                                il.Emit(IlInstr.Stloc, getVariable(ftp.ParamTypes[i2], (int) i2 + 1));
                            }

                            // get function from global table
                            il.Emit(IlInstr.Ldsfld, functionTable);
                            il.Emit(IlInstr.Ldloc, getVariable(i32Type));
                            var funct = typeToFunc(ftp);

                            il.Emit(IlInstr.Ldelem_Any, def.MainModule.TypeSystem.Object);
                            il.Emit(IlInstr.Castclass, def.MainModule.ImportReference(funct));
                            for (int i2 = 0; i2 < ftp.ParamCount; i2++)
                                il.Emit(IlInstr.Ldloc, getVariable(ftp.ParamTypes[i2], i2 + 1));
                            var invoke = funct.GetMethod("Invoke");
                            il.Emit(IlInstr.Callvirt, def.MainModule.ImportReference(invoke));
                            pop((int) ftp.ParamCount);
                            push(ftp.ReturnType);
                            break;
                        case instr.BLOCK:
                            var blockType = reader.ReadU8();
                            var endLabel = il.Create(OpCodes.Nop);
                            var blk = new LabelType
                                {Type = blockType, EndLabel = endLabel, StartLabel = endLabel, Forward = true};
                            labelStack.Add(blk);
                            break;
                        case instr.LOOP:
                            blockType = reader.ReadU8();
                            var startLabel = il.Create(OpCodes.Nop);
                            il.Append(startLabel);
                            blk = new LabelType {Type = blockType, EndLabel = null, StartLabel = startLabel};
                            labelStack.Add(blk);
                            break;
                        case instr.BR:
                        case instr.BR_IF:

                            var brindex = reader.ReadU32Leb();
                            if (instr == instr.BR_IF)
                                il.Emit(OpCodes.Brtrue, labelStack[(int) (labelStack.Count - brindex - 1)].StartLabel);
                            else
                                il.Emit(OpCodes.Br, labelStack[(int) (labelStack.Count - brindex - 1)].StartLabel);
                            break;
                        case instr.BR_TABLE:
                            var cnt = reader.ReadU32Leb();
                            var items = new Instruction[cnt];
                            for (int i2 = 0; i2 < cnt; i2++)
                            {
                                var brindex2 = reader.ReadU32Leb();
                                var brindex3 = (int) (labelStack.Count - brindex2 - 1);
                                items[i2] = labelStack[brindex3].StartLabel;
                            }

                            var defaultLabelIndex = reader.ReadU32Leb();
                            var defaultLabel = labelStack[(int) (labelStack.Count - defaultLabelIndex - 1)].StartLabel;
                            il.Emit(OpCodes.Switch, items);
                            if (defaultLabel == null)
                                throw new Exception("Unexpected situation");
                            il.Emit(OpCodes.Br, defaultLabel);

                            pop();
                            break;
                        case instr.SELECT:
                            // select(a,b,c) = a ? b : c
                            // we have to keep track of the type on top of the stack.
                            var t = pop(2);
                            var nextLabel = il.Create(IlInstr.Stloc, getVariable(t));
                            endLabel = il.Create(IlInstr.Nop);
                            il.Emit(IlInstr.Brfalse, nextLabel);
                            il.Emit(IlInstr.Pop);
                            il.Emit(IlInstr.Br, endLabel);
                            il.Append(nextLabel);
                            il.Emit(IlInstr.Pop);
                            il.Emit(IlInstr.Ldloc, getVariable(t));
                            il.Append(endLabel);
                            break;
                        case instr.GLOBAL_GET:
                            var offset2 = reader.ReadU32Leb();
                            var glob = globals[offset2];
                            il.Emit(IlInstr.Ldsfld, glob.Field);
                            push(glob.Field.FieldType);
                            break;
                        case instr.GLOBAL_SET:
                            offset2 = reader.ReadU32Leb();
                            glob = globals[offset2];
                            il.Emit(IlInstr.Stsfld, glob.Field);
                            pop();
                            break;
                        case instr.LOCAL_SET:
                        case instr.LOCAL_GET:
                        case instr.LOCAL_TEE:
                            VariableDefinition var = null;
                            ParameterDefinition param = null;
                            uint local_index = reader.ReadU32Leb();
                            bool isArg = true;
                            if (local_index >= ftype.ParamCount)
                            {
                                isArg = false;
                                local_index -= ftype.ParamCount;
                                var = m1.Body.Variables[(int) local_index];
                            }
                            else
                            {
                                param = m1.Parameters[(int) local_index];
                            }

                            switch (instr)
                            {
                                case instr.LOCAL_GET:
                                    il.Emit(isArg ? IlInstr.Ldarg : IlInstr.Ldloc, (int) local_index);
                                    push(param?.ParameterType ?? var?.VariableType);
                                    break;
                                case instr.LOCAL_SET:
                                    il.Emit(isArg ? IlInstr.Starg : IlInstr.Stloc, (int) local_index);
                                    pop();
                                    break;
                                case instr.LOCAL_TEE:
                                    il.Emit(IlInstr.Dup);
                                    il.Emit(isArg ? IlInstr.Starg : IlInstr.Stloc, (int) local_index);
                                    break;
                            }

                            break;
                        case instr.I32_CONST:
                            il.Emit(IlInstr.Ldc_I4, (int) reader.ReadI64Leb());
                            push(i32Type);
                            break;
                        case instr.I64_CONST:
                            push(i64Type);
                            il.Emit(IlInstr.Ldc_I8, reader.ReadI64Leb());
                            break;
                        case instr.F32_CONST:
                            push(f32Type);
                            il.Emit(IlInstr.Ldc_R4, reader.ReadF32());
                            break;
                        case instr.F64_CONST:
                            push(f64Type);
                            il.Emit(IlInstr.Ldc_R8, reader.ReadF64());
                            break;
                        case instr.MEMORY_SIZE:
                            var x = reader.ReadU8();
                            Assert.AreEqual(0, x);

                            push(i32Type);
                            il.Emit(IlInstr.Ldsfld, memoryField);
                            il.Emit(IlInstr.Ldlen);
                            il.Emit(IlInstr.Ldc_I4, (int) page_size);
                            il.Emit(IlInstr.Div);
                            il.Emit(IlInstr.Conv_I4);
                            break;
                        case instr.MEMORY_GROW:
                            x = reader.ReadU8();
                            Assert.AreEqual(0, x);
                            pop(1);
                            push(i32Type);
                            il.Emit(IlInstr.Ldsfld, memoryField);
                            il.Emit(IlInstr.Ldlen);
                            il.Emit(IlInstr.Ldc_I4, (int) page_size);
                            il.Emit(IlInstr.Div);
                            il.Emit(IlInstr.Dup);
                            il.Emit(IlInstr.Stloc, getVariable(i32Type));

                            il.Emit(IlInstr.Add); // add the argument pages;
                            il.Emit(IlInstr.Ldc_I4, (int) page_size);
                            il.Emit(IlInstr.Mul);
                            // new page size top stack.

                            il.Emit(IlInstr.Newarr, byteType);
                            il.Emit(IlInstr.Dup);
                            il.Emit(IlInstr.Ldc_I8, 0L);
                            il.Emit(IlInstr.Ldelema, byteType);
                            il.Emit(IlInstr.Ldsfld, memoryField);
                            il.Emit(IlInstr.Ldc_I8, 0L);
                            il.Emit(IlInstr.Ldelema, byteType);
                            il.Emit(IlInstr.Ldsfld, memoryField);
                            il.Emit(IlInstr.Ldlen);
                            il.Emit(IlInstr.Conv_U4);
                            var mcpy = getMethod(typeof(Unsafe), nameof(Unsafe.CopyBlock), typeof(byte).MakeByRefType(),
                                typeof(byte).MakeByRefType(),
                                typeof(uint));
                            il.Emit(IlInstr.Call, mcpy);
                            //il.Emit(IlInstr.Cpblk); // copy!
                            il.Emit(IlInstr.Stsfld, memoryField); // store tue duplicate.
                            il.Emit(IlInstr.Ldloc, getVariable(i32Type));
                            break;
                        case instr.I32_LOAD:
                        case instr.I32_LOAD8_S:
                        case instr.I32_LOAD8_U:
                        case instr.I32_LOAD16_U:
                        case instr.I32_LOAD16_S:
                        case instr.I64_LOAD8_S:
                        case instr.I64_LOAD8_U:
                        case instr.I64_LOAD16_S:
                        case instr.I64_LOAD16_U:
                        case instr.I64_LOAD32_S:
                        case instr.I64_LOAD32_U:
                        case instr.I64_LOAD:
                        case instr.F32_LOAD:
                        case instr.F64_LOAD:
                        case instr.I32_STORE:
                        case instr.I32_STORE_8:
                        case instr.I32_STORE_16:
                        case instr.I64_STORE:
                        case instr.I64_STORE_32:
                        case instr.I64_STORE_8:
                        case instr.I64_STORE_16:
                        case instr.F32_STORE:
                        case instr.F64_STORE:
                            // in the code:
                            var align = reader.ReadU32Leb(); // align
                            var offset = reader.ReadU32Leb();
                            //stack:
                            // STORE: [... heap address, value?]
                            // LOAD: [... heap address]

                            // get the heap
                            VariableDefinition stvar = null;
                            if (instr.ToString().Contains("STORE"))
                            {
                                if (instr.ToString().Contains("F32"))
                                    stvar = getVariable(f32Type);
                                else if (instr.ToString().Contains("F64"))
                                    stvar = getVariable(f64Type);
                                else if (instr.ToString().Contains("I64"))
                                    stvar = getVariable(i64Type);
                                else if (instr.ToString().Contains("I32"))
                                    stvar = getVariable(i32Type);
                                else throw new Exception("Unknown type");
                                il.Emit(IlInstr.Stloc, stvar);
                                pop();
                            }

                            il.Emit(IlInstr.Stloc, heapaddr);

                            il.Emit(IlInstr.Ldsfld, memoryField);
                            il.Emit(IlInstr.Ldloc, heapaddr);
                            // adjust according to the offset 
                            if (offset != 0)
                            {
                                il.Emit(IlInstr.Ldc_I8, offset);
                                il.Emit(IlInstr.Add);
                            }

                            // get the address of element N (pop the address from the stack)
                            il.Emit(IlInstr.Ldelema, def.MainModule.TypeSystem.Byte);
                            switch (instr)
                            {
                                // pop address, value. store value in address according to size.
                                case instr.I32_STORE_8:
                                case instr.I64_STORE_8:
                                    il.Emit(IlInstr.Ldloc, stvar);
                                    il.Emit(IlInstr.Stind_I1);
                                    break;
                                case instr.I32_STORE_16:
                                case instr.I64_STORE_16:
                                    il.Emit(IlInstr.Ldloc, stvar);
                                    il.Emit(IlInstr.Stind_I2);
                                    break;
                                case instr.I32_STORE:
                                case instr.I64_STORE_32:
                                    il.Emit(IlInstr.Ldloc, stvar);
                                    il.Emit(IlInstr.Stind_I4);
                                    break;
                                case instr.I64_STORE:
                                    il.Emit(IlInstr.Ldloc, stvar);
                                    il.Emit(IlInstr.Stind_I8);
                                    break;
                                case instr.F32_STORE:
                                    il.Emit(IlInstr.Ldloc, stvar);
                                    il.Emit(IlInstr.Stind_R4);
                                    break;
                                case instr.F64_STORE:
                                    il.Emit(IlInstr.Ldloc, stvar);
                                    il.Emit(IlInstr.Stind_R8);
                                    break;
                                case instr.I32_LOAD:
                                    il.Emit(IlInstr.Ldind_I4);
                                    push(i32Type);
                                    break;
                                case instr.I32_LOAD8_S:
                                case instr.I32_LOAD8_U:
                                    if (instr.I32_LOAD8_S == instr)
                                        il.Emit(IlInstr.Ldind_I1);
                                    else
                                        il.Emit(IlInstr.Ldind_U1);
                                    il.Emit(IlInstr.Conv_I4);
                                    push(i32Type);
                                    break;
                                case instr.I32_LOAD16_U:
                                case instr.I32_LOAD16_S:
                                    if (instr.I32_LOAD16_S == instr)
                                        il.Emit(IlInstr.Ldind_I2);
                                    else
                                        il.Emit(IlInstr.Ldind_U2);
                                    il.Emit(IlInstr.Conv_I4);
                                    push(i32Type);
                                    break;
                                case instr.I64_LOAD:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_I8);
                                    break;
                                case instr.F32_LOAD:
                                    push(f32Type);
                                    il.Emit(IlInstr.Ldind_R4);
                                    break;
                                case instr.F64_LOAD:
                                    push(f64Type);
                                    il.Emit(IlInstr.Ldind_R8);
                                    break;
                                case instr.I64_LOAD8_S:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_I1);
                                    il.Emit(IlInstr.Conv_I8);
                                    break;
                                case instr.I64_LOAD8_U:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_U1);
                                    il.Emit(IlInstr.Conv_I8);
                                    break;
                                case instr.I64_LOAD16_S:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_I2);
                                    il.Emit(IlInstr.Conv_I8);
                                    break;
                                case instr.I64_LOAD16_U:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_U2);
                                    il.Emit(IlInstr.Conv_I8);
                                    break;
                                case instr.I64_LOAD32_S:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_I4);
                                    il.Emit(IlInstr.Conv_I8);
                                    break;
                                case instr.I64_LOAD32_U:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_U4);
                                    il.Emit(IlInstr.Conv_I8);
                                    break;
                                default:
                                    throw new Exception("Unexpected opcode");
                            }

                            break;
                        case instr.I64_EXTEND_I32_U:
                            il.Emit(IlInstr.Conv_U4);
                            goto case instr.I64_EXTEND_I32_S;
                        case instr.I64_EXTEND_I32_S:
                            il.Emit(IlInstr.Conv_I8);
                            pop();
                            push(i64Type);
                            break;
                        case instr.I32_WRAP_I64:
                            il.Emit(IlInstr.Conv_I4);
                            pop();
                            push(i32Type);
                            break;
                        case instr.I64_REINTERPRET_F64:
                            var m = typeof(BitConverter).GetMethod(nameof(BitConverter.DoubleToInt64Bits));
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            pop();
                            push(i64Type);
                            break;
                        case instr.I32_REINTERPRET_F32:
                            m = typeof(BitConverter).GetMethod(nameof(BitConverter.SingleToInt32Bits));
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            pop();
                            push(i32Type);
                            break;
                        case instr.F64_REINTERPRET_I64:
                            m = typeof(BitConverter).GetMethod(nameof(BitConverter.Int64BitsToDouble));
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            pop();
                            push(f64Type);
                            break;
                        case instr.F32_REINTERPRET_I32:
                            m = typeof(BitConverter).GetMethod(nameof(BitConverter.Int32BitsToSingle));
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            pop(1);
                            push(f32Type);
                            break;
                        case instr.F64_PROMOTE_F32:
                            il.Emit(IlInstr.Conv_R8);
                            pop(1);
                            push(f64Type);
                            break;
                        case instr.F32_DEMOTE_F64:
                            il.Emit(IlInstr.Conv_R4);
                            pop(1);
                            push(f32Type);
                            break;

                        case instr.I32_TRUNC_F32_S:
                        case instr.I32_TRUNC_F32_U:
                            il.Emit(IlInstr.Conv_I4);
                            pop(1);
                            push(i32Type);
                            break;
                        case instr.I32_TRUNC_F64_U:
                            il.Emit(IlInstr.Conv_U4);
                            pop(1);
                            push(i32Type);
                            goto case instr.I32_TRUNC_F64_S;
                        case instr.I32_TRUNC_F64_S:
                            il.Emit(IlInstr.Conv_I4);
                            pop(1);
                            push(i32Type);
                            break;
                        case instr.I64_TRUNC_F64_U:
                            il.Emit(IlInstr.Conv_U8);
                            pop(1);
                            push(i64Type);
                            goto case instr.I64_TRUNC_F64_S;
                        case instr.I64_TRUNC_F64_S:
                            il.Emit(IlInstr.Conv_I8);
                            pop(1);
                            push(i64Type);
                            break;
                        case instr.F32_CONVERT_I32_S:
                        case instr.F32_CONVERT_I32_U:
                        case instr.F32_CONVERT_I64_S:
                        case instr.F32_CONVERT_I64_U:
                            if (instr.ToString().EndsWith("_U"))
                                il.Emit(IlInstr.Conv_U8);
                            il.Emit(IlInstr.Conv_R4);
                            pop(1);
                            push(f32Type);
                            break;
                        case instr.F64_CONVERT_I32_S:
                        case instr.F64_CONVERT_I32_U:
                        case instr.F64_CONVERT_I64_S:
                        case instr.F64_CONVERT_I64_U:
                            if (instr.ToString().EndsWith("_U"))
                                il.Emit(IlInstr.Conv_U8);
                            il.Emit(IlInstr.Conv_R8);
                            pop(1);
                            push(f64Type);
                            break;

                        case instr.F32_ADD:
                        case instr.F64_ADD:
                        case instr.I32_ADD:
                        case instr.I64_ADD:
                            il.Emit(IlInstr.Add);
                            pop();
                            break;
                        case instr.F32_SUB:
                        case instr.F64_SUB:
                        case instr.I32_SUB:
                        case instr.I64_SUB:
                            il.Emit(IlInstr.Sub);
                            pop();
                            break;
                        case instr.F32_MUL:
                        case instr.F64_MUL:
                        case instr.I32_MUL:
                        case instr.I64_MUL:
                            il.Emit(IlInstr.Mul);
                            pop();
                            break;
                        case instr.F32_DIV:
                        case instr.F64_DIV:
                        case instr.I32_DIV_S:
                        case instr.I64_DIV_S:
                            il.Emit(IlInstr.Div);
                            pop();
                            break;
                        case instr.I32_DIV_U:
                        case instr.I64_DIV_U:
                            il.Emit(IlInstr.Div_Un);
                            pop();
                            break;
                        case instr.I32_REM_S:
                        case instr.I64_REM_S:
                            il.Emit(IlInstr.Rem);
                            pop();
                            break;
                        case instr.I32_REM_U:
                        case instr.I64_REM_U:
                            il.Emit(IlInstr.Rem_Un);
                            pop();
                            break;

                        case instr.I32_LT_U:
                        case instr.I64_LT_U:
                            il.Emit(IlInstr.Clt_Un);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_LT_S:
                        case instr.I64_LT_S:
                        case instr.F64_LT:
                        case instr.F32_LT:
                            il.Emit(IlInstr.Clt);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_GT_U:
                        case instr.I64_GT_U:
                            il.Emit(IlInstr.Cgt_Un);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_GT_S:
                        case instr.I64_GT_S:
                        case instr.F64_GT:
                        case instr.F32_GT:
                            il.Emit(IlInstr.Cgt);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_GE_S:
                        case instr.I32_GE_U:
                        case instr.I64_GE_S:
                        case instr.I64_GE_U:
                        case instr.F64_GE:
                        case instr.F32_GE:
                        case instr.I32_LE_S:
                        case instr.I32_LE_U:
                        case instr.I64_LE_S:
                        case instr.I64_LE_U:
                        case instr.F64_LE:
                        case instr.F32_LE:
                            var unsigned = instr.ToString().Contains("_U");
                            var le = instr.ToString().Contains("LE");
                            OpCode cmp = le ? IlInstr.Clt : IlInstr.Cgt;
                            if (unsigned)
                                cmp = le ? IlInstr.Clt_Un : IlInstr.Cgt_Un;

                            var v = getVariable(instrType());
                            var v2 = getVariable(instrType(), 1);
                            il.Emit(IlInstr.Stloc, v);
                            il.Emit(IlInstr.Stloc, v2);
                            il.Emit(IlInstr.Ldloc, v2);
                            il.Emit(IlInstr.Ldloc, v);
                            il.Emit(IlInstr.Ceq);
                            il.Emit(IlInstr.Ldloc, v2);
                            il.Emit(IlInstr.Ldloc, v);

                            il.Emit(cmp);
                            il.Emit(IlInstr.Or);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_EQ:
                        case instr.I64_EQ:
                        case instr.F64_EQ:
                        case instr.F32_EQ:
                            il.Emit(IlInstr.Ceq);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_NE:
                        case instr.I64_NE:
                        case instr.F64_NE:
                        case instr.F32_NE:
                            il.Emit(IlInstr.Ceq);
                            il.Emit(IlInstr.Ldc_I4_0);
                            il.Emit(IlInstr.Ceq);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.F32_NEG:
                        case instr.F64_NEG:
                            il.Emit(IlInstr.Neg);
                            break;
                        case instr.F32_ABS:
                        case instr.F64_ABS:
                            il.Emit(IlInstr.Dup);
                            if (is64)
                                il.Emit(IlInstr.Ldc_R8, 0.0);
                            else
                                il.Emit(IlInstr.Ldc_R4, 0.0f);
                            il.Emit(IlInstr.Clt);
                            var label = il.Create(IlInstr.Nop);
                            il.Emit(IlInstr.Brfalse, label);
                            il.Emit(IlInstr.Neg);
                            il.Append(label);
                            break;
                        case instr.F32_MIN:
                        case instr.F64_MIN:
                        case instr.F32_MAX:
                        case instr.F64_MAX:
                            var name = instr.ToString().EndsWith("MAX") ? "Max" : "Min";
                            var m2 = getMethod(typeof(Math), name, instrType2(), instrType2());
                            il.Emit(IlInstr.Call, m2);
                            pop();
                            break;
                        case instr.F32_SQRT:
                        case instr.F64_SQRT:
                            m2 = getMethod(typeof(Math), nameof(Math.Sqrt), instrType2());
                            il.Emit(IlInstr.Call, m2);
                            break;
                        case instr.F64_CEIL:
                        case instr.F32_CEIL:
                            m2 = getMethod(typeof(Math), nameof(Math.Ceiling), instrType2());
                            il.Emit(IlInstr.Call, m2);
                            break;
                        case instr.F64_FLOOR:
                        case instr.F32_FLOOR:
                            m2 = getMethod(typeof(Math), nameof(Math.Floor), instrType2());
                            il.Emit(IlInstr.Call, m2);
                            break;
                        case instr.F64_COPYSIGN:
                        case instr.F32_COPYSIGN:
                            var vtype = is64 ? f64Type : f32Type;
                            il.Emit(IlInstr.Stloc, getVariable(vtype));
                            il.Emit(IlInstr.Stloc, getVariable(vtype, 1));
                            il.Emit(IlInstr.Ldloc, getVariable(vtype));
                            il.Emit(IlInstr.Ldloc, getVariable(vtype, 1));
                            il.Emit(IlInstr.Ldloc, getVariable(vtype));
                            il.Emit(IlInstr.Mul);
                            if (is64)
                                il.Emit(IlInstr.Ldc_R8, 0.0);
                            else
                                il.Emit(IlInstr.Ldc_R4, 0.0f);
                            il.Emit(IlInstr.Clt);
                            label = il.Create(IlInstr.Nop);
                            il.Emit(IlInstr.Brfalse, label);
                            il.Emit(IlInstr.Neg);
                            il.Append(label);
                            break;
                        case instr.I32_EQZ:
                            il.Emit(IlInstr.Ldc_I4_0);
                            il.Emit(IlInstr.Ceq);
                            pop(1);
                            push(i32Type);
                            break;
                        case instr.I64_EQZ:
                            il.Emit(IlInstr.Ldc_I8, (long) 0);
                            il.Emit(IlInstr.Ceq);
                            pop(1);
                            push(i32Type);
                            break;
                        case instr.I32_AND:
                        case instr.I64_AND:
                            il.Emit(IlInstr.And);
                            pop(1);
                            break;
                        case instr.I32_OR:
                        case instr.I64_OR:
                            il.Emit(IlInstr.Or);
                            pop(1);
                            break;
                        case instr.I32_XOR:
                        case instr.I64_XOR:
                            il.Emit(IlInstr.Xor);
                            pop(1);
                            break;
                        case instr.I32_ROTR:
                        case instr.I64_ROTR:
                            il.Emit(IlInstr.Call, getMethod(typeof(BitOperations), nameof(BitOperations.RotateRight),
                                instrType2(true),
                                typeof(int)));
                            pop(1);
                            break;
                        case instr.I32_ROTL:
                        case instr.I64_ROTL:
                            il.Emit(IlInstr.Call, getMethod(typeof(BitOperations), nameof(BitOperations.RotateLeft),
                                instrType2(true),
                                typeof(int)));
                            pop(1);
                            break;
                        case instr.I32_SHL:
                        case instr.I64_SHL:
                            il.Emit(IlInstr.Shl);
                            pop(1);
                            break;
                        case instr.I32_SHR_S:
                        case instr.I64_SHR_S:
                            il.Emit(IlInstr.Shr);
                            pop(1);
                            break;
                        case instr.I32_SHR_U:
                        case instr.I64_SHR_U:
                            il.Emit(IlInstr.Shr_Un);
                            pop(1);
                            break;
                        case instr.I32_CTZ:
                        case instr.I64_CTZ:
                            m = typeof(BitOperations).GetMethod(nameof(BitOperations.TrailingZeroCount),
                                new Type[] {is64 ? typeof(ulong) : typeof(uint)});
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            if (is64)
                                il.Emit(IlInstr.Conv_I8);
                            break;
                        case instr.I32_CLZ:
                        case instr.I64_CLZ:
                            m = typeof(BitOperations).GetMethod(nameof(BitOperations.LeadingZeroCount),
                                new Type[] {is64 ? typeof(ulong) : typeof(uint)});
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            if (is64)
                                il.Emit(IlInstr.Conv_I8);
                            break;
                        case instr.I32_POPCNT:
                        case instr.I64_POPCNT:
                            m = typeof(BitOperations).GetMethod(nameof(BitOperations.PopCount),
                                new Type[] {is64 ? typeof(ulong) : typeof(uint)});
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            if (is64)
                                il.Emit(IlInstr.Conv_I8);
                            break;
                        case instr.UNREACHABLE:
                            il.Emit(IlInstr.Ldstr, "Unreachable code");
                            il.Emit(IlInstr.Newobj, resolveTypeConstructor(typeof(Exception), typeof(string)));
                            il.Emit(IlInstr.Throw);
                            break;

                        case instr.RETURN:
                            il.Emit(IlInstr.Ret);
                            break;
                        case instr.DROP:
                            il.Emit(IlInstr.Pop);
                            pop();
                            break;
                        case instr.END:
                            if (labelStack.Count > 1)
                            {
                                var r = labelStack.Last();
                                labelStack.RemoveAt(labelStack.Count - 1);

                                if (r.EndLabel != null)
                                    il.Append(r.EndLabel);
                            }
                            else
                            {
                                labelStack.RemoveAt(0);
                                if (il.Body.Instructions.Last().OpCode != IlInstr.Ret)
                                    il.Emit(IlInstr.Ret);
                                goto next;
                            }

                            break;
                        default:
                            throw new Exception("Unsupported instruction: " + instr);
                    }
                }

                if (labelStack.Count > 0)
                {
                    Assert.IsTrue(labelStack.Count == 1);
                    if (il.Body.Instructions.Last().OpCode != IlInstr.Ret)
                        il.Emit(IlInstr.Ret);
                }

                next: ;
            }
        }


        void ReadExportSection(BinReader reader)
        {
            var exportCount = reader.ReadU32Leb();
            for (uint i = 0; i < exportCount; i++)
            {
                var name = reader.ReadStrN();
                var type = (ImportType) reader.ReadU8();
                switch (type)
                {
                    case ImportType.FUNC:
                        uint index = reader.ReadU32Leb();
                        if (ExportFunc.ContainsKey(index))
                        {
                            Console.WriteLine("Export already defined: {0} {1} - {2}", index, name,
                                ExportFunc[index].Name);
                        }
                        else
                            ExportFunc[index] = new ImportFunc {Name = name, Index = index};

                        break;
                    case ImportType.TABLE:
                        index = reader.ReadU32Leb();
                        ExportTables[index] = new ExportTable() {Name = name, Index = index};
                        break;
                    case ImportType.MEM:
                        var memIndex = reader.ReadU32Leb();
                        Console.WriteLine("Memory: {0}", memIndex);
                        break;
                    case ImportType.GLOBAL:
                        var idx = reader.ReadU32Leb();
                        if (globals.TryGetValue(idx, out var glob))
                        {
                            glob.Field.Name = name;
                        }

                        Console.WriteLine("Global import: {0}   {1}", idx, name);
                        break;
                }
            }
        }


        TypeReference ByteToTypeReference(byte b)
        {
            switch (b)
            {
                case 0x7F: return def.MainModule.TypeSystem.Int32;
                case 0x7E: return def.MainModule.TypeSystem.Int64;
                case 0x7D: return def.MainModule.TypeSystem.Single;
                case 0x7C: return def.MainModule.TypeSystem.Double;
                default:
                    throw new Exception("Invalid type " + b);
            }
        }

        void ReadGlobalSection(BinReader reader)
        {
            uint global_count = reader.ReadU32Leb();
            for (uint i = 0; i < global_count; i++)
            {
                var valType = reader.ReadU8();
                var mut = reader.ReadU8();
                var instr = (instr) reader.ReadU8();
                var glob = new Global();
                glob.Const = mut == 0;
                glob.Type = valType;

                switch (instr)
                {
                    case instr.I32_CONST:
                        glob.Value = (int) reader.ReadI64Leb();
                        break;
                    case instr.I64_CONST:
                        glob.Value = reader.ReadI64Leb();
                        break;
                    case instr.F64_CONST:
                        glob.Value = reader.ReadF64();
                        break;
                    case instr.F32_CONST:
                        glob.Value = reader.ReadF32();
                        break;
                    default:
                        throw new Exception("Unsupported constant " + instr);
                }

                var end = (instr) reader.ReadU8();
                Assert.AreEqual(instr.END, end);
                globals[i] = glob;
            }

            var ctor = cls.GetStaticConstructor();
            var il = ctor.Body.GetILProcessor();
            ctor.Body.Instructions.RemoveAt(ctor.Body.Instructions.Count - 1);

            foreach (var global in globals)
            {
                var type = global.Value.Type;
                var fld = new FieldDefinition("global" + global.Key, FieldAttributes.Static, ByteToTypeReference(type));

                cls.Fields.Add(fld);
                globals[global.Key].Field = fld;
                switch (global.Value.Value)
                {
                    case int i4:
                        il.Emit(IlInstr.Ldc_I4, i4);
                        break;
                    case long i8:
                        il.Emit(IlInstr.Ldc_I8, i8);
                        break;
                    case float r4:
                        il.Emit(IlInstr.Ldc_R4, r4);
                        break;
                    case double r8:
                        il.Emit(IlInstr.Ldc_R8, r8);
                        break;
                    default: throw new Exception("Unsupported type");
                }

                il.Emit(IlInstr.Stsfld, fld);
            }

            il.Emit(IlInstr.Ret);
            Console.WriteLine("Globals: {0}", globals.Count);
        }


        void ReadMemorySection(BinReader reader)
        {
            var memCount = reader.ReadU32Leb();
            Assert.AreEqual<uint>(1, memCount);
            for (uint i = 0; i < memCount; i++)
            {
                var type = reader.ReadU8();
                var min = reader.ReadU32Leb();
                if (type == 0)
                {
                    Console.WriteLine("Memory: {0} pages", min);
                    var cctoril = cls.GetStaticConstructor().Body.GetILProcessor();
                    cctoril.Body.Instructions.RemoveAt(cctoril.Body.Instructions.Count - 1);
                    cctoril.Emit(OpCodes.Ldc_I8, (long) min * page_size);
                    cctoril.Emit(OpCodes.Newarr, def.MainModule.TypeSystem.Byte);
                    cctoril.Emit(OpCodes.Stsfld, memoryField);
                    cctoril.Emit(OpCodes.Ret);
                }
                else if (type == 1)
                {
                    var max = reader.ReadU32Leb();

                    Console.WriteLine("Memory of {0}-{1} pages ({2} - {3})", min, max, min * page_size,
                        max * page_size);
                    var cctoril = cls.GetStaticConstructor().Body.GetILProcessor();
                    cctoril.Body.Instructions.RemoveAt(cctoril.Body.Instructions.Count - 1);
                    cctoril.Emit(OpCodes.Ldc_I8, (long) max * page_size);
                    cctoril.Emit(OpCodes.Newarr, def.MainModule.TypeSystem.Byte);
                    cctoril.Emit(OpCodes.Stsfld, memoryField);
                    cctoril.Emit(OpCodes.Ret);
                }
            }
        }


        void ReadFunctionSection(BinReader reader)
        {
            uint funcCount = reader.ReadU32Leb();
            Console.WriteLine("Func count: {0}", funcCount);
            for (uint i = 0; i < funcCount; i++)
            {
                uint typeid = reader.ReadU32Leb();
                var type = Types[typeid];
                FuncDecl[i] = new FuncDeclType
                {
                    TypeId = typeid,
                    Method = new MethodDefinition("func" + i, MethodAttributes.Static | MethodAttributes.Public,
                        def.MainModule.TypeSystem.Void)
                };
            }
        }

        void ReadCustomSection(BinReader reader)
        {
            var name = reader.ReadStrN();
            Console.WriteLine("Custom section name: {0}", name);
            if (name == "name")
            {
                for (int i = 0; i < 3; i++)
                {
                    var id = reader.ReadU8();
                    var len = reader.ReadU32Leb();
                    var next = reader.Position + len;
                    Console.WriteLine("Name section {0} ({1} bytes)", id, len);
                    if (id == 1)
                    {
                        var names = reader.ReadU32Leb();
                        for (var nameId = 0; nameId < names; nameId++)
                        {
                            var idx = reader.ReadU32Leb();
                            var fname = reader.ReadStrN().Replace(":", "_");
                            if (idx < ImportFuncs.Count)
                            {
                                var imp = ImportFuncs[idx];
                                imp.CustomName = fname;
                            }
                            else
                            {
                                var f = FuncDecl[(uint) (idx - ImportFuncs.Count)];
                                f.ImportName = fname;
                                if (f.IsDefaultName)
                                    f.Method.Name = fname;
                            }
                        }
                    }

                    reader.Position = next;
                }
            }
        }

        void ReadTypeSection(BinReader reader)
        {
            var typeCount = reader.ReadU32Leb();
            for (uint i = 0; i < typeCount; i++)
            {
                var header = reader.ReadU8();
                Equals(0x60, header);
                var paramCount = reader.ReadU32Leb();
                var paramTypes = new TypeReference[paramCount];
                for (int i2 = 0; i2 < paramCount; i2++)
                {
                    var t = reader.ReadU8();
                    paramTypes[i2] = ByteToTypeReference(t);
                }

                var returnCount = reader.ReadU32Leb();
                Assert.IsTrue(returnCount < 2);
                TypeReference returnType = def.MainModule.TypeSystem.Void;
                for (int i2 = 0; i2 < returnCount; i2++)
                    returnType = ByteToTypeReference(reader.ReadU8());
                Types[i] = new TypeId
                {
                    ReturnCount = returnCount, ParamCount = paramCount, ParamTypes = paramTypes, ReturnType = returnType
                };
            }
        }
    }
}