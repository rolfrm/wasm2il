using System.Diagnostics;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Wasm;
using Wasm2IL.Utils;
using Wasm2IL.Dwarf;
using Wasm2IL.Optimization;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using Instruction = Mono.Cecil.Cil.Instruction;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using OpCode = Mono.Cecil.Cil.OpCode;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using TypeReference = Mono.Cecil.TypeReference;

namespace Wasm2IL
{
    using instr = Wasm.Instruction;
    using IlInstr = OpCodes;

    public class ResolveImportEventArgs : EventArgs
    {
        public TypeDefinition TypeBuilder { get; set; }
        public string ModuleName { get; set; }
        public string Name { get; set; }
        public object Result { get; set; }
        public bool Handled { get; set; }
        public ModuleDefinition ModuleDefinition { get; set; }
    }

    public class Transformer
    {
        readonly Dictionary<string, List<Type>> importModules = new();
        private List<Type> overrideModules = [];

        /// <summary>
        /// Enable IL optimizations like constant folding. Default is true.
        /// </summary>
        public bool EnableOptimizations { get; set; } = true;

        public void LoadImportModule(string moduleName, Type type)
        {
            if (!importModules.TryGetValue(moduleName, out var typeList))
                importModules[moduleName] = typeList = [];
            typeList.Add(type);
        }

        public event EventHandler<ResolveImportEventArgs> OnResolveImport;

        public object ResolveImportedMethod(string moduleName, string name)
        {
            if (OnResolveImport != null)
            {
                ResolveImportEventArgs resolveEventArgs = new ResolveImportEventArgs()
                {
                    Name = name,
                    ModuleName = moduleName,
                    
                    TypeBuilder = cls,
                    ModuleDefinition = def.MainModule
                };
                OnResolveImport?.Invoke(this, resolveEventArgs);
                if (resolveEventArgs.Handled)
                {
                    return resolveEventArgs.Result;
                }
            }


            if (importModules.TryGetValue(moduleName, out var t))
            {
                foreach (var type in t)
                {
                    if (type.GetMethod(name) is MethodInfo m)
                    {
                        if (m.IsStatic == false)
                            throw new TransformException($"Imported methods must be static: {m}");
                        return m;
                    }
                }
            }

            return null;
        }

        public void LoadOverrideModule(Type type)
        {
            overrideModules.Add(type);
        }

        public WasmAssembly LoadWasmAssembly(Stream stream, string name, string outDll = "tmp.dll", string version = "1.0.0")
        {
            var path = outDll;
            if (File.Exists(path))
                File.Delete(path);
            var mem = new MemoryStream();
            Transform(stream, name, mem, version);
            var asm = Assembly.Load(mem.ToArray());
            if (outDll != null)
                File.WriteAllBytes(outDll, mem.ToArray());
            return new WasmAssembly(asm);
        }

        public WasmAssembly LoadWasmAssembly(string filePath, string name, string outDll = "tmp.dll")
        {
            using var file = File.OpenRead(filePath);
            var path = outDll;
            if (File.Exists(path))
                File.Delete(path);
            using (var mem = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                Transform(file, name, mem);
                mem.Position = 0;
            }

            var asm = Assembly.Load(File.ReadAllBytes(path));
            return new WasmAssembly(asm);
        }

        const string magicHeader = "\0asm";
        const uint page_size = 1 << 16;
        Dictionary<uint, Global> globals = new Dictionary<uint, Global>();
        Dictionary<uint, ImportFunc> ExportFunc = new Dictionary<uint, ImportFunc>();
        private Dictionary<uint, ImportFunc> ImportFuncs = new();
        private Dictionary<uint, ImportFunc> OverrideFuncs = null;

        Dictionary<uint, ExportTable> ExportTables = new Dictionary<uint, ExportTable>();

        Dictionary<uint, TypeId> Types = new Dictionary<uint, TypeId>();

        // function declaration to function type
        Dictionary<uint, FuncDeclType> FuncDecl = new Dictionary<uint, FuncDeclType>();
        AssemblyDefinition def;
        TypeDefinition cls;
        FieldDefinition memoryField;
        private FieldDefinition memoryFieldSize;
        FieldDefinition functionTable;

        TypeReference f32Type, f64Type, i64Type, i16Type, i32Type, voidType, byteType, intPtrType, voidPtrType;
        private TypeReference v128Type;

        MethodReference resolveTypeConstructor(Type t, params Type[] argTypes)
        {
            return def.MainModule.ImportReference(
                t.GetConstructors().FirstOrDefault(x =>
                    x.GetParameters().Select(y => y.ParameterType).SequenceEqual(argTypes)));
        }

        // note there are also globals which are added dynamically depending on need.

        void Init( string asmName, Version version)
        {
            var asmName2 = new AssemblyNameDefinition(asmName, version);
            var asm = AssemblyDefinition.CreateAssembly(asmName2, "Test", ModuleKind.Dll);

            f32Type = asm.MainModule.TypeSystem.Single;
            f64Type = asm.MainModule.TypeSystem.Double;
            i64Type = asm.MainModule.TypeSystem.Int64;
            i32Type = asm.MainModule.TypeSystem.Int32;
            i16Type = asm.MainModule.TypeSystem.Int16;
            voidType = asm.MainModule.TypeSystem.Void;
            byteType = asm.MainModule.TypeSystem.Byte;
            intPtrType = asm.MainModule.TypeSystem.IntPtr;
            voidPtrType = asm.MainModule.TypeSystem.Void.MakePointerType();
            v128Type = asm.MainModule.ImportReference(typeof(Vector128<byte>));
            cls = new TypeDefinition(asmName, "C",
                TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class |
                TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public,
                asm.MainModule.TypeSystem.Object);

            asm.MainModule.Types.Add(cls);
            def = asm;

            memoryField = new FieldDefinition("Memory", FieldAttributes.Static | FieldAttributes.Public,
                asm.MainModule.TypeSystem.Byte.MakePointerType());
            memoryField.IsStatic = true;
            // todo: Figure out how to init based on data.
            cls.Fields.Add(memoryField);

            memoryFieldSize = new FieldDefinition("MemorySize", FieldAttributes.Static | FieldAttributes.Public,
                asm.MainModule.TypeSystem.Int32);
            memoryFieldSize.IsStatic = true;

            cls.Fields.Add(memoryFieldSize);

            functionTable = new FieldDefinition("FunctionTable", FieldAttributes.Static | FieldAttributes.Public,
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

        public void Transform(Stream str, string asmName, string filePath)
        {
            using var outFile = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            Transform(str, asmName, outFile);
        }

        public void Transform(Stream str2, string asmName, Stream outStream, string versionStr = "1.0.0")
        {
            var version = Version.Parse(versionStr);
            var reader = new BinReader(str2);
            var header = reader.ReadStrl(4);
            if (magicHeader != header)
                throw new Exception("invalid header");
            var wasmVersion = new byte[4];
            reader.Read(wasmVersion);
            if (!wasmVersion.SequenceEqual(new byte[] {1, 0, 0, 0}))
                throw new Exception("Unsupported wasm version");
            Log.WriteLine("Wasm Version: {0}", string.Join(" ", wasmVersion));

            Init(asmName, version);
            long codeLoc = 0;
            long elementLoc = 0;
            while (!reader.ReadToEnd())
            {
                var section = (Section) reader.ReadU8();
                uint length = reader.ReadU32Leb();
                Log.WriteLine("Reading section {0}: {1}bytes", section, length);
                var next = reader.Position + length;

                switch (section)
                {
                    case Section.CUSTOM:
                    {
                        var sec = reader.ReadBytes((int)length);
                        ReadCustomSection(new BinReader(sec, 0));
                        reader.Position = next;
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
                        reader.Position = next;
                        break;
                }

                // check that section was properly read.
                Assert.AreEqual(next, reader.Position);
            }


            foreach (var kv in ImportFuncs
                         .Where(x => x.Value.Method == null)
                         .ToArray())
            {
                var imp = kv.Value;

                var method = ResolveImportedMethod(imp.Module, imp.Name);


                if (method != null)
                {
                    continue;
                }

                Log.WriteLine($"Warning: Import not defined {imp.Name} by module {imp.Module}.");
                var type = Types[(uint) imp.TypeId];
                var m = new MethodDefinition(imp.Name, MethodAttributes.Public | MethodAttributes.Static,
                    type.ReturnType);
                foreach (var p in type.ParamTypes)
                {
                    m.Parameters.Add(new ParameterDefinition(p));
                }

                // throw exception
                m.Body.InitLocals = true;

                var il = m.Body.GetILProcessor();

                // let's implemented 

                il.Emit(IlInstr.Nop);
                il.Emit(IlInstr.Ldstr, imp.Name + " not Implemented");
                il.Emit(IlInstr.Newobj, resolveTypeConstructor(typeof(NotImplementedException), typeof(string)));
                il.Emit(IlInstr.Throw);
                cls.Methods.Add(m);
                imp.Method = m;
                ImportFuncs[kv.Key] = imp;
            }

            reader.Position = elementLoc;
            ReadElementSection(reader);

            OverrideFuncs = new();
            Dictionary<string, uint> declaredFunctions = new();
            foreach (var item in FuncDecl)
            {
                if (item.Value?.ImportName is string name)
                {
                    declaredFunctions[name] = item.Key;
                }
            }

            foreach (var type in overrideModules)
            {
                foreach (var method in type.GetMethods())
                {
                    if (declaredFunctions.TryGetValue(method.Name, out var id))
                    {
                        OverrideFuncs[id] = new ImportFunc()
                        {
                            Index = id,
                            CustomName = method.Name,
                            Method = def.MainModule.ImportReference(method),
                            Name = method.Name,
                            Module = "??",
                            TypeId = FuncDecl[id].TypeId
                        };
                    }
                }
            }


            reader.Position = codeLoc;
            ReadCodeSection(reader);

            // Run IL optimizations if enabled
            if (EnableOptimizations)
            {
                ILOptimizer.OptimizeType(cls);
                foreach (var method in cls.Methods)
                {
                    method.Body.Optimize();
                }
            }

            def.Write(outStream);


            Log.WriteLine($"Output written to {outStream}");
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

                        Log.WriteLine("Table: {0}.{1} {2}-{3}", moduleName, itemName, min, max);
                        break;
                    case ImportType.GLOBAL:
                        var valType = reader.ReadU8();
                        bool mut = reader.ReadU8() > 0;

                        Log.WriteLine("Global: {0}.{1} {2}-{3}", moduleName, itemName, valType, mut);
                        break;
                    case ImportType.MEM:
                        //elemType = reader.ReadU8();
                        //Assert.AreEqual(elemType, 0x70);
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

                        Log.WriteLine("Memory: {0}.{1} {2}-{3}", moduleName, itemName, min, max);

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
                if (instr2 == instr.VECTOR_INSTRUCTION)
                {
                    var ext2 = reader.ReadU8();
                    instr2 = (instr) (0xFD00 | ext2);
                }

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
                    if (funcId < ImportFuncs.Count)
                    {
                        var imp = ImportFuncs[funcId];
                        var t = Types[(uint) imp.TypeId];
                        if (imp.Method == null)
                        {
                            var method = ResolveImportedMethod(imp.Module, imp.Name);
                            if (method != null)
                            {
                                var reference = method is MethodReference mr ? mr :def.MainModule.ImportReference((MethodInfo)method);
                                reference = MaybeWrap(reference);
                                imp.Method = reference;
                            }
                        }

                        if (imp.Method == null)
                        {
                            throw new InvalidOperationException("!");
                        }

                        {
                            il.Emit(OpCodes.Ldftn, imp.Method);
                            var ftype = typeToFunc(t);
                            var constr = ftype.GetConstructors().First();
                            var cref = def.MainModule.ImportReference(constr);
                            il.Emit(OpCodes.Newobj, cref);
                            il.Emit(OpCodes.Stelem_Any, def.MainModule.TypeSystem.Object);
                        }
                    }
                    else
                    {
                        var importFunc = FuncDecl[(uint) (funcId - ImportFuncs.Count)];
                        var t = Types[importFunc.TypeId];
                        il.Emit(OpCodes.Ldftn, FuncDecl[(uint) (funcId - ImportFuncs.Count)].Method);
                        var ftype = typeToFunc(t);
                        var constr = ftype.GetConstructors().First();
                        var cref = def.MainModule.ImportReference(constr);
                        il.Emit(OpCodes.Newobj, cref);
                        il.Emit(OpCodes.Stelem_Any, def.MainModule.TypeSystem.Object);
                    }
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
                        case 8:
                            baseType = typeof(Action<,,,,,,,>);
                            break;
                        case 9:
                            baseType = typeof(Action<,,,,,,,,>);
                            break;
                        case 10:
                            baseType = typeof(Action<,,,,,,,,,>);
                            break;
                        default:
                            throw new NotSupportedException();
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
                        case 7:
                            baseType = typeof(Func<,,,,,,,>);
                            break;
                        case 8:
                            baseType = typeof(Func<,,,,,,,,>);
                            break;
                        case 9:
                            baseType = typeof(Func<,,,,,,,,,>);
                            break;
                        case 10:
                            baseType = typeof(Func<,,,,,,,,,,>);
                            break;
                        default:
                            throw new NotSupportedException();
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

                il.Emit(IlInstr.Ldsfld, memoryField);
                il.Emit(IlInstr.Ldc_I4, offset);
                il.Emit(IlInstr.Add);
                for (int i2 = 0; i2 < byteCount; i2++)
                {
                    /*if (bc[i2] != 0)
                    {
                        il.Emit(IlInstr.Dup);
                        il.Emit(IlInstr.Ldc_I4, (int) i2 + offset);
                        il.Emit(IlInstr.Add);
                        il.Emit(IlInstr.Ldc_I4, (int) bc[i2]);
                        il.Emit(IlInstr.Stind_I1);
                    }*/
                    
                    if (byteCount - i2 >= 8)
                    {
                        var v = BitConverter.ToInt64(bc.AsSpan(i2, 8));
                        if (v != 0)
                        {
                            il.Emit(IlInstr.Dup);
                            il.Emit(IlInstr.Ldc_I8, v);
                            il.Emit(IlInstr.Stind_I8);
                        }
                        il.Emit(IlInstr.Ldc_I4_8);
                        il.Emit(IlInstr.Add);
                        
                        i2 += 7;
                    }
                    else if (bc[i2] != 0)
                    {
                        il.Emit(IlInstr.Dup);
                        il.Emit(IlInstr.Ldc_I4, (int) bc[i2]);
                        il.Emit(IlInstr.Stind_I1);
                        il.Emit(IlInstr.Ldc_I4_1);
                        il.Emit(IlInstr.Add);
                    }
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

        MethodReference? methodFromName(string name)
        {
            return null;
        }

        MethodReference? resolveMethod(uint func)
        {
            if (func < ImportFuncs.Count)
            {
                var importFun = ImportFuncs[func];
                if (importFun.Method == null)
                {
                    var m3 = ResolveImportedMethod(importFun.Module, importFun.Name);


                    if (m3 != null)
                    {
                        var type2 = Types[(uint) importFun.TypeId];
                        
                        var m2 =  m3 is MethodReference mr ? mr :def.MainModule.ImportReference((MethodInfo)m3);

                        m2 = MaybeWrap(m2);
                        
                        if (m2.ReturnType.FullName == (voidType.FullName) && (type2.ReturnCount != 0))
                        {
                            throw new Exception($"Type signature of {importFun.Name} does not match declared type. (return arguments)");
                        }
                        if (m2.ReturnType.FullName != (voidType.FullName) && (type2.ReturnCount == 0))
                        {
                            throw new Exception($"Type signature of {importFun.Name} does not match declared type. (return arguments)");
                        }

                        if (type2.ParamCount != m2.Parameters.Count)
                        {
                            throw new Exception($"Type signature of {importFun.Name} does not match declared type. (parameter count)");
                        }

                        importFun.Method = m2;
                        return m2;
                    }


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

                    il.Emit(IlInstr.Ldstr, importFun.Name + " not Implemented");
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

            if (OverrideFuncs.TryGetValue(func - (uint) ImportFuncs.Count, out var reference))
            {
                return reference.Method;
            }

            var decl = FuncDecl[func - (uint) ImportFuncs.Count];
            var declFun = decl.Method;
            return declFun;
        }

        unsafe void ReadCodeSection(BinReader reader)
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
                bool useName = this.parameterNames.TryGetValue(name, out var paramNames);
                for (uint i2 = 0; i2 < ftype.ParamCount; i2++)
                {
                    var parameter = new ParameterDefinition(ftype.ParamTypes[i2]);
                    parameter.Name = "param" + i2;
                    if (useName && paramNames.ElementAtOrDefault((int)i2) is string name2)
                    {
                        parameter.Name = name2;
                    }
                    m1.Parameters.Add(parameter);
                }
            }

            HashSet<instr> usedInstructions = [];
            HashSet<ExtendedInstruction> usedExtendedInstructions = [];
            HashSet<VectorInstructions> usedVectorInstructions = [];
            Dictionary<object, MethodInfo> callMethods = new();
            foreach(var method in typeof(Lib).GetMethods().Where(x => x.IsStatic && x.GetCustomAttribute<WasmOpcodeAttribute>() is {} i))
            {
                var key = method.GetCustomAttribute<WasmOpcodeAttribute>().Key;
                
                callMethods.Add(key, method);
            }
            
            for (uint i = 0; i < funcCount; i++)
            {
                var funcId = FuncDecl[i];
                var ftype = Types[funcId.TypeId];
                var m1 = funcId.Method;
                
                cls.Methods.Add(m1);
                m1.Body.InitLocals = true;
                var il = m1.Body.GetILProcessor();
                //il.Emit(IlInstr.Nop);

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
                        var lv_y_4 = new VariableDefinition(tp)
                        {

                        };
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
                
                var heapvar = new VariableDefinition(def.MainModule.TypeSystem.Byte.MakePointerType());
                m1.Body.Variables.Add(heapvar);

                m1.Body.InitLocals = true;
                var labelStack = new List<LabelType>();
                labelStack.Add(new LabelType()); // base label

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

                void emitLdc(int cint)
                {
                    var opcode =  cint switch
                    {
                        -1 => IlInstr.Ldc_I4_M1,
                        0 => IlInstr.Ldc_I4_0,
                        1 => IlInstr.Ldc_I4_1,
                        2 => IlInstr.Ldc_I4_2,
                        3 => IlInstr.Ldc_I4_3,
                        4 => IlInstr.Ldc_I4_4,
                        5 => IlInstr.Ldc_I4_5,
                        6 => IlInstr.Ldc_I4_6,
                        7 => IlInstr.Ldc_I4_7,
                        8 => IlInstr.Ldc_I4_8,
                        _ => IlInstr.Ldc_I4
                    };
                    if (opcode != IlInstr.Ldc_I4)
                    {
                        il.Emit(opcode);
                    }
                    else
                    {
                        if (cint is < 126 and > -126)
                            il.Emit(IlInstr.Ldc_I4_S, (sbyte)cint);    
                        else
                            il.Emit(IlInstr.Ldc_I4, cint);
                    }
                }


                bool heapInited = false;
                void loadMemory()
                {
                    if (!heapInited)
                    {
                        heapInited = true;
                        il.InsertAfter(0, il.Create(OpCodes.Ldsfld, memoryField));
                        il.InsertAfter(1, il.Create(OpCodes.Stloc, heapvar));
                    }
                    il.Emit(IlInstr.Ldloc, heapvar);
                }

                while (next > reader.Position)
                {
                    var instr = (instr) reader.ReadU8();
                    usedInstructions.Add(instr);
                    
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
                 
                    OpCode? jmpInstr = null;
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

                            otherFun = MaybeWrap(otherFun);

                            il.Emit(IlInstr.Call, otherFun);
                            
                            pop(otherFun.Parameters.Count);
                            
                            push(otherFun.ReturnType);
                            break;
                        case instr.CALL_INDIRECT:
                            var typeidx = reader.ReadU32Leb();
                            var table = reader.ReadU32Leb();
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
                        case instr.IF:
                            blockType = reader.ReadU8();
                        {
                            endLabel = il.Create(OpCodes.Nop);
                            il.Emit(IlInstr.Brfalse, endLabel);
                            blk = new LabelType {Type = blockType, EndLabel = endLabel, StartLabel = null};
                            labelStack.Add(blk);
                        }
                            break;
                        case instr.ELSE:
                            endLabel = il.Create(OpCodes.Nop);
                            il.Emit(OpCodes.Br, endLabel);
                            blk = labelStack.Last();
                            labelStack.Remove(blk);
                            il.Append(blk.EndLabel);
                            blk = new LabelType {Type = blk.Type, EndLabel = endLabel, StartLabel = null};
                            labelStack.Add(blk);
                            break;

                        case instr.BR:
                        case instr.BR_IF:

                            var brindex = reader.ReadU32Leb();
                            if (instr == instr.BR_IF)
                            {
                                pop();
                                il.Emit(jmpInstr ?? OpCodes.Brtrue,
                                    labelStack[(int) (labelStack.Count - brindex - 1)].StartLabel);
                            }
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
                            uint localIndex = reader.ReadU32Leb();
                            bool isArg = true;
                            if (localIndex >= ftype.ParamCount)
                            {
                                isArg = false;
                                localIndex -= ftype.ParamCount;
                                var = m1.Body.Variables[(int) localIndex];
                            }
                            else
                            {
                                param = m1.Parameters[(int) localIndex];
                            }

                            switch (instr)
                            {
                                case instr.LOCAL_GET:
                                    if (!isArg && localIndex < 4)
                                    {
                                        il.Emit(new []{IlInstr.Ldloc_0, IlInstr.Ldloc_1, IlInstr.Ldloc_2, IlInstr.Ldloc_3}[localIndex]);
                                    }
                                    else if (isArg && localIndex < 4)
                                    {
                                        il.Emit(new []{IlInstr.Ldarg_0, IlInstr.Ldarg_1, IlInstr.Ldarg_2, IlInstr.Ldarg_3}[localIndex]);
                                    }
                                    else
                                    {
                                        if (localIndex < 256)
                                        {
                                            il.Emit(isArg ? IlInstr.Ldarg_S : IlInstr.Ldloc_S, (byte) localIndex);
                                        }
                                        else
                                        {
                                            il.Emit(isArg ? IlInstr.Ldarg : IlInstr.Ldloc, (int) localIndex);
                                        }
                                    }

                                    push(param?.ParameterType ?? var?.VariableType);
                                    break;
                                case instr.LOCAL_SET:
                                    if (!isArg && localIndex < 4)
                                    {
                                        il.Emit(new []{IlInstr.Stloc_0, IlInstr.Stloc_1, IlInstr.Stloc_2, IlInstr.Stloc_3}[localIndex]);
                                    }
                                    else
                                    {
                                        if (localIndex < 256)
                                        {
                                            il.Emit(isArg ? IlInstr.Starg_S : IlInstr.Stloc_S, (byte) localIndex);    
                                        }
                                        else
                                        {
                                            il.Emit(isArg ? IlInstr.Starg : IlInstr.Stloc, (int) localIndex);
                                        }
                                    }
                                    
                                    pop();
                                    break;
                                case instr.LOCAL_TEE:
                                    il.Emit(IlInstr.Dup);
                                    il.Emit(isArg ? IlInstr.Starg : IlInstr.Stloc, (int) localIndex);
                                    break;
                            }

                            break;
                        case instr.I32_CONST:
                        {
                            var cint = (int) reader.ReadI64Leb();
                            emitLdc(cint);
                            
                            push(i32Type);
                            break;
                        }
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
                            loadMemory();
                            il.Emit(IlInstr.Ldlen);
                            il.Emit(IlInstr.Ldc_I4, (int) page_size);
                            il.Emit(IlInstr.Div);
                            il.Emit(IlInstr.Conv_I4);
                            break;
                        case instr.MEMORY_GROW:
                            /*
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
                            il.Emit(IlInstr.Ldloc, getVariable(i32Type));*/
                            throw new NotSupportedException();
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

                            loadMemory();
                            il.Emit(IlInstr.Add);

                            // adjust according to the offset 
                            if (offset != 0)
                            {
                                emitLdc((int) offset);
                                il.Emit(IlInstr.Add);
                            }

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
                                    il.Emit(IlInstr.Ldind_I1);
                                    push(i32Type);
                                    break;
                                case instr.I32_LOAD8_U:
                                    il.Emit(IlInstr.Ldind_U1);
                                    push(i32Type);
                                    break;
                                case instr.I32_LOAD16_U:
                                    il.Emit(IlInstr.Ldind_U2);
                                    push(i32Type);
                                    break;
                                case instr.I32_LOAD16_S:
                                    il.Emit(IlInstr.Ldind_I2);
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
                                    il.Emit(IlInstr.Conv_U8);
                                    break;
                                case instr.I64_LOAD16_S:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_I2);
                                    il.Emit(IlInstr.Conv_I8);
                                    break;
                                case instr.I64_LOAD16_U:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_U2);
                                    il.Emit(IlInstr.Conv_U8);
                                    break;
                                case instr.I64_LOAD32_S:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_I4);
                                    il.Emit(IlInstr.Conv_I8);
                                    break;
                                case instr.I64_LOAD32_U:
                                    push(i64Type);
                                    il.Emit(IlInstr.Ldind_U4);
                                    il.Emit(IlInstr.Conv_U8);
                                    break;
                                default:
                                    throw new Exception("Unexpected opcode");
                            }

                            break;
                        case instr.I64_EXTEND_I32_U:
                            il.Emit(IlInstr.Conv_U8);
                            pop();
                            push(i64Type);
                            break;
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

                        case instr.F32_REINTERPRET_I32:
                            m = typeof(BitConverter).GetMethod(nameof(BitConverter.Int32BitsToSingle));
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            pop(1);
                            push(f32Type);
                            break;
                        case instr.F64_REINTERPRET_I64:
                            m = typeof(BitConverter).GetMethod(nameof(BitConverter.Int64BitsToDouble));
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            pop();
                            push(f64Type);
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
                            if ((instr) reader.Clone().ReadU8() == instr.BR_IF)
                            {
                                instr = (instr) reader.ReadU8();
                                pop();
                                jmpInstr = IlInstr.Blt_Un;
                                goto case instr.BR_IF;
                            }
                            il.Emit(IlInstr.Clt_Un);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_LT_S:
                        case instr.I64_LT_S:
                        case instr.F64_LT:
                        case instr.F32_LT:
                            if ((instr) reader.Clone().ReadU8() == instr.BR_IF)
                            {
                                instr = (instr) reader.ReadU8();
                                pop();
                                jmpInstr = IlInstr.Blt;
                                goto case instr.BR_IF;
                            }
                            il.Emit(IlInstr.Clt);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_GT_U:
                        case instr.I64_GT_U:
                            if ((instr) reader.Clone().ReadU8() == instr.BR_IF)
                            {
                                instr = (instr) reader.ReadU8();
                                pop();
                                jmpInstr = IlInstr.Bgt_Un;
                                goto case instr.BR_IF;
                            }
                            il.Emit(IlInstr.Cgt_Un);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_GT_S:
                        case instr.I64_GT_S:
                        case instr.F64_GT:
                        case instr.F32_GT:
                            
                            if ((instr) reader.Clone().ReadU8() == instr.BR_IF)
                            {
                                instr = (instr) reader.ReadU8();
                                pop();
                                jmpInstr = IlInstr.Bgt;
                                goto case instr.BR_IF;
                            }
                            
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
                            
                            // invert the logic
                            var unsigned = instr.ToString().Contains("_U");
                            var le = instr.ToString().Contains("LE");
                            
                            if ((instr) reader.Clone().ReadU8() == instr.BR_IF)
                            {
                                instr = (instr) reader.ReadU8();
                                pop();
                                if (unsigned)
                                {
                                    if (le)
                                        jmpInstr = IlInstr.Ble_Un;
                                    else
                                        jmpInstr = IlInstr.Bge_Un;
                                }
                                else
                                {
                                    if (le)
                                        jmpInstr = IlInstr.Ble;
                                    else
                                        jmpInstr = IlInstr.Bge;
                                }
                                
                                goto case instr.BR_IF;
                            }
                            
                            OpCode cmp = le ? (unsigned ? IlInstr.Cgt_Un : IlInstr.Cgt) 
                                : (unsigned ? IlInstr.Clt_Un : IlInstr.Clt);

                            
                            
                            il.Emit(cmp);
                            il.Emit(IlInstr.Ldc_I4_0);
                            il.Emit(IlInstr.Ceq);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_EQ:
                        case instr.I64_EQ:
                        case instr.F64_EQ:
                        case instr.F32_EQ:
                            if ((instr) reader.Clone().ReadU8() == instr.BR_IF)
                            {
                                instr = (instr) reader.ReadU8();
                                pop();
                                jmpInstr = IlInstr.Beq;
                                goto case instr.BR_IF;
                            }
                            il.Emit(IlInstr.Ceq);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_NE:
                        case instr.I64_NE:
                        case instr.F64_NE:
                        case instr.F32_NE:
                            if ((instr) reader.Clone().ReadU8() == instr.BR_IF)
                            {
                                instr = (instr) reader.ReadU8();
                                pop();
                                jmpInstr = IlInstr.Bne_Un;
                                
                                goto case instr.BR_IF;
                            }

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
                            var nextI = (instr) reader.Clone().ReadU8();
                            if (nextI == instr.BR_IF)
                            {
                                instr = (instr)reader.ReadU8();
                                
                                jmpInstr = IlInstr.Brfalse;
                                goto case instr.BR_IF;
                            }
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
                                [is64 ? typeof(ulong) : typeof(uint)]);
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            if (is64)
                                il.Emit(IlInstr.Conv_I8);
                            break;
                        case instr.I32_CLZ:
                        case instr.I64_CLZ:
                            m = typeof(BitOperations).GetMethod(nameof(BitOperations.LeadingZeroCount),
                                [is64 ? typeof(ulong) : typeof(uint)]);
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            if (is64)
                                il.Emit(IlInstr.Conv_I8);
                            break;
                        case instr.I32_POPCNT:
                        case instr.I64_POPCNT:
                            m = typeof(BitOperations).GetMethod(nameof(BitOperations.PopCount),
                                [is64 ? typeof(ulong) : typeof(uint)]);
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
                                if (il.Body.Instructions.LastOrDefault()?.OpCode != IlInstr.Ret)
                                    il.Emit(IlInstr.Ret);
                                goto next;
                            }

                            break;
                        case instr.I32_EXTEND8_S:
                            il.Emit(IlInstr.Conv_I1);
                            break;
                        case instr.I32_EXTEND16_S:
                            il.Emit(IlInstr.Conv_I2);
                            break;
                        case instr.I64_EXTEND8_S:
                            il.Emit(IlInstr.Conv_I1);
                            il.Emit(IlInstr.Conv_I8);
                            break;
                        case instr.I64_EXTEND16_S:
                            il.Emit(IlInstr.Conv_I2);
                            il.Emit(IlInstr.Conv_I8);
                            break;
                        case instr.I64_EXTEND32_S:
                            il.Emit(IlInstr.Conv_I4);
                            il.Emit(IlInstr.Conv_I8);
                            break;
                        case instr.F32_TRUNC:
                            m = typeof(MathF).GetMethod(nameof(MathF.Truncate),
                                [typeof(float)]);
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            break;
                        case instr.F32_NEAREST:
                            m = typeof(MathF).GetMethod(nameof(MathF.Round),
                                [typeof(float)]);
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            break;
                        case instr.F64_TRUNC:
                            m = typeof(Math).GetMethod(nameof(Math.Truncate),
                                [typeof(double)]);
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            break;
                        
                        case instr.I64_TRUNC_F32_S:
                            EmitCall(il, () => Lib.I64_TRUNC_F32_S);
                            pop();
                            push(i64Type);
                            break;
                        case instr.I64_TRUNC_F32_U:
                            EmitCall(il, () => Lib.I64_TRUNC_F32_U);
                            pop();
                            push(i64Type);
                            break;
                            
                        case instr.F64_NEAREST:
                            m = typeof(Math).GetMethod(nameof(Math.Round),
                                [typeof(double)]);
                            il.Emit(IlInstr.Call, def.MainModule.ImportReference(m));
                            break;
                        case instr.EXTENDED_INSTRUCTION:
                            var einstr = (ExtendedInstruction) reader.ReadU32Leb();
                            usedExtendedInstructions.Add(einstr);
                            switch (einstr)
                            {
                                case ExtendedInstruction.MEMORY_FILL:
                                    Assert.AreEqual(0, reader.ReadU8());
                                    EmitCall(il, () => Lib.MemoryFill);
                                    break;
                                case ExtendedInstruction.MEMORY_COPY:
                                    Assert.AreEqual(0, reader.ReadU8());
                                    Assert.AreEqual(0, reader.ReadU8());
                                    EmitCall(il, () => Lib.MemoryCopy);
                                    break;
                                default:
                                    if (callMethods.TryGetValue(einstr, out var method))
                                    {
                                        il.Emit(OpCodes.Call, cls.Module.ImportReference(method));
                                        break;
                                    }
                                    throw new NotImplementedException();
                            }

                            break;


                        case instr.VECTOR_INSTRUCTION:
                            var instr2 = (VectorInstructions) reader.ReadU32Leb();
                            usedVectorInstructions.Add(instr2);
                            switch (instr2)
                            {
                                case VectorInstructions.V128_STORE:
                                case VectorInstructions.V128_LOAD:
                                {
                                    if (instr2 == VectorInstructions.V128_STORE)
                                    {
                                        stvar = getVariable(v128Type);
                                        il.Emit(IlInstr.Stloc, stvar);
                                        pop();
                                    }

                                    loadMemory();
                                    il.Emit(IlInstr.Add);
                                    var align2 = reader.ReadU32Leb(); // align
                                    var offset3 = reader.ReadU32Leb();

                                    if (offset3 != 0)
                                    {
                                        il.Emit(IlInstr.Ldc_I4, (int) offset3);
                                        il.Emit(IlInstr.Add);
                                    }

                                    var stvar2 = getVariable(v128Type);
                                    if (instr2 == VectorInstructions.V128_STORE)
                                    {
                                        il.Emit(IlInstr.Ldloc, stvar2);
                                        il.Emit(IlInstr.Stobj, v128Type);
                                    }
                                    else
                                    {
                                        il.Emit(IlInstr.Ldobj, v128Type);
                                        push(v128Type);
                                    }

                                    break;
                                }
                                case VectorInstructions.V128_CONST:
                                {
                                    void LoadV128ConstCode()
                                    {
                                        Span<byte> buffer = stackalloc byte[16];
                                        reader.Read(buffer);
                                        foreach (var elem in MemoryMarshal.Cast<byte, long>(buffer))
                                        {
                                            il.Emit(IlInstr.Ldc_I8, elem);
                                        }

                                        il.EmitCall(() => Lib.v128_create);
                                    }

                                    LoadV128ConstCode();
                                    push(v128Type);
                                }

                                    break;


                                case VectorInstructions.I8X16_SHUFFLE:

                                    void LoadV128ShuffleCode()
                                    {
                                        Span<byte> buffer = stackalloc byte[16];
                                        reader.Read(buffer);
                                        foreach (var elem in MemoryMarshal.Cast<byte, long>(buffer))
                                        {
                                            il.Emit(IlInstr.Ldc_I8, elem);
                                        }

                                        il.EmitCall(() => Lib.v128_create);
                                        il.EmitCall(() => Lib.v128_shuffle_vectors);
                                        pop();
                                    }

                                    LoadV128ShuffleCode();
                                    break;
                                
                                case VectorInstructions.I8X16_REPLACE_LANE:
                                case VectorInstructions.I16X8_REPLACE_LANE:
                                case VectorInstructions.I32X4_REPLACE_LANE:
                                case VectorInstructions.I64X2_REPLACE_LANE:
                                case VectorInstructions.F32X4_REPLACE_LANE:
                                case VectorInstructions.F64X2_REPLACE_LANE:
                                {
                                    var lane = reader.ReadU8();
                                    il.Emit(OpCodes.Ldc_I4, (int) lane);
                                    switch (instr2)
                                    {
                                        case VectorInstructions.I8X16_REPLACE_LANE:
                                            il.EmitCall(() => Lib.i8x16_replace_lane);
                                            break;
                                        case VectorInstructions.I16X8_REPLACE_LANE:
                                            il.EmitCall(() => Lib.i16x8_replace_lane);
                                            break;
                                        case VectorInstructions.I32X4_REPLACE_LANE:
                                            il.EmitCall(() => Lib.i32x4_replace_lane);
                                            break;
                                        case VectorInstructions.I64X2_REPLACE_LANE:
                                            il.EmitCall(() => Lib.i64x2_replace_lane);
                                            break;
                                        case VectorInstructions.F32X4_REPLACE_LANE:
                                            il.EmitCall(() => Lib.f32x4_replace_lane);
                                            break;
                                        case VectorInstructions.F64X2_REPLACE_LANE:
                                            il.EmitCall(() => Lib.f64x2_replace_lane);
                                            break;
                                        default:
                                            throw new UnreachableException();
                                    }
                                }
                                    break;

                                case VectorInstructions.I8X16_EXTRACT_LANE_S:
                                case VectorInstructions.I8X16_EXTRACT_LANE_U:
                                case VectorInstructions.I16X8_EXTRACT_LANE_S:
                                case VectorInstructions.I16X8_EXTRACT_LANE_U:
                                case VectorInstructions.I32X4_EXTRACT_LANE:
                                case VectorInstructions.I64X2_EXTRACT_LANE:
                                case VectorInstructions.F32X4_EXTRACT_LANE:
                                case VectorInstructions.F64X2_EXTRACT_LANE:
                                {
                                    var idx = reader.ReadU8();
                                    il.Emit(OpCodes.Ldc_I4, (int) idx);
                                    switch (instr2)
                                    {
                                        case VectorInstructions.I8X16_EXTRACT_LANE_S:
                                            il.EmitCall(() => Lib.i8x16_extract_lane_s);
                                            push(i32Type);
                                            break;
                                        case VectorInstructions.I8X16_EXTRACT_LANE_U:
                                            il.EmitCall(() => Lib.i8x16_extract_lane_u);
                                            push(this.byteType);
                                            break;
                                        case VectorInstructions.I16X8_EXTRACT_LANE_S:
                                            il.EmitCall(() => Lib.i16x8_extract_lane_s);
                                            push(this.i16Type);
                                            break;
                                        case VectorInstructions.I16X8_EXTRACT_LANE_U:
                                            il.EmitCall(() => Lib.i16x8_extract_lane_u);
                                            push(this.i16Type);
                                            break;
                                        case VectorInstructions.I32X4_EXTRACT_LANE:
                                            il.EmitCall(() => Lib.i32x4_extract_lane);
                                            push(this.i32Type);
                                            break;
                                        case VectorInstructions.I64X2_EXTRACT_LANE:
                                            il.EmitCall(() => Lib.i64x2_extract_lane);
                                            push(this.i64Type);
                                            break;
                                        case VectorInstructions.F32X4_EXTRACT_LANE:
                                            il.EmitCall(() => Lib.f32x4_extract_lane);
                                            break;
                                        case VectorInstructions.F64X2_EXTRACT_LANE:
                                            il.EmitCall(() => Lib.f64x2_extract_lane);
                                            break;

                                        default:
                                            throw new UnreachableException();
                                    }
                                }
                                    break;
                                
                                case VectorInstructions.V128_LOAD64_ZERO:
                                case VectorInstructions.V128_LOAD32_ZERO:
                                {
                                    pop();
                                    var align2 = reader.ReadU32Leb();
                                    var offset3 = reader.ReadU32Leb();
                                    loadMemory();
                                    il.Emit(IlInstr.Add);
                                    if (offset3 != 0)
                                    {
                                        il.Emit(OpCodes.Ldc_I4, (int) offset3);
                                        il.Emit(OpCodes.Add);
                                    }

                                    switch (instr2)
                                    {
                                        case VectorInstructions.V128_LOAD64_ZERO:
                                            il.EmitCall(() => Lib.vec128_load64_zero);
                                            break;
                                        case VectorInstructions.V128_LOAD32_ZERO:
                                            il.EmitCall(() => Lib.v128_load32_zero);
                                            break;
                                        default:
                                            throw new NotSupportedException();
                                    }

                                    push(v128Type);
                                    break;
                                }
                                
                                case VectorInstructions.I8X16_ADD_SAT_S:
                                case VectorInstructions.I8X16_ADD_SAT_U:
                                    throw new Exception("not implemented");//il.EmitCall(() => Lib.i8x16_add);
                                    break;
                                case VectorInstructions.I8X16_SUB_SAT_S:
                                case VectorInstructions.I8X16_SUB_SAT_U:
                                    throw new Exception("not implemented");//;il.EmitCall(() => Lib.i8x16_sub);
                                    break;
                                
                                case VectorInstructions.V128_LOAD8_SPLAT:
                                case VectorInstructions.V128_LOAD16_SPLAT:
                                case VectorInstructions.V128_LOAD32_SPLAT:
                                case VectorInstructions.V128_LOAD64_SPLAT:
                                {
                                    var __t = pop();
                                    var align2 = reader.ReadU32Leb();
                                    var offset3 = reader.ReadU32Leb();
                                    loadMemory();
                                    il.Emit(IlInstr.Add);
                                    if (offset3 != 0)
                                    {
                                        il.Emit(OpCodes.Ldc_I4, (int) offset3);
                                        il.Emit(OpCodes.Add);
                                    }

                                    switch (instr2)
                                    {
                                        case VectorInstructions.V128_LOAD8_SPLAT:
                                            il.EmitCall(() => Lib.v128_load8_splat);
                                            break;
                                        case VectorInstructions.V128_LOAD16_SPLAT:
                                            il.EmitCall(() => Lib.v128_load16_splat);
                                            break;
                                        case VectorInstructions.V128_LOAD32_SPLAT:
                                            il.EmitCall(() => Lib.v128_load32_splat);
                                            break;
                                        case VectorInstructions.V128_LOAD64_SPLAT:
                                            il.EmitCall(() => Lib.v128_load64_splat);
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }

                                    push(v128Type);
                                }
                                    break;

                                case VectorInstructions.V128_LOAD8_LANE:
                                case VectorInstructions.V128_LOAD16_LANE:
                                case VectorInstructions.V128_LOAD32_LANE:
                                case VectorInstructions.V128_LOAD64_LANE:
                                case VectorInstructions.V128_STORE8_LANE:
                                case VectorInstructions.V128_STORE16_LANE:
                                case VectorInstructions.V128_STORE32_LANE:
                                case VectorInstructions.V128_STORE64_LANE:
                                {
                                    stvar = getVariable(v128Type);
                                    il.Emit(IlInstr.Stloc, stvar);
                                    pop();

                                    var align2 = reader.ReadU32Leb();
                                    var offset3 = reader.ReadU32Leb();
                                    var lane = reader.ReadU8();
                                    loadMemory();
                                    il.Emit(IlInstr.Add);
                                    if (offset3 != 0)
                                    {
                                        il.Emit(OpCodes.Ldc_I4, (int) offset3);
                                        il.Emit(OpCodes.Add);
                                    }

                                    il.Emit(IlInstr.Ldc_I4, (int) lane);
                                    il.Emit(IlInstr.Ldloc, stvar);

                                    switch (instr2)
                                    {
                                        case VectorInstructions.V128_LOAD8_LANE:
                                            il.EmitCall(() => Lib.v128_load8_lane);
                                            push(byteType);
                                            break;
                                        case VectorInstructions.V128_LOAD16_LANE:
                                            il.EmitCall(() => Lib.v128_load16_lane);
                                            push(this.i16Type);
                                            break;
                                        case VectorInstructions.V128_LOAD32_LANE:
                                            il.EmitCall(() => Lib.v128_load32_lane);
                                            push(this.i32Type);
                                            break;
                                        case VectorInstructions.V128_LOAD64_LANE:
                                            il.EmitCall(() => Lib.v128_load64_lane);
                                            push(this.i64Type);
                                            break;

                                        case VectorInstructions.V128_STORE8_LANE:
                                            il.EmitCall(() => Lib.v128_store8_lane);
                                            break;
                                        case VectorInstructions.V128_STORE16_LANE:
                                            il.EmitCall(() => Lib.v128_store16_lane);
                                            break;
                                        case VectorInstructions.V128_STORE32_LANE:
                                            il.EmitCall(() => Lib.v128_store32_lane);
                                            break;
                                        case VectorInstructions.V128_STORE64_LANE:
                                            il.EmitCall(() => Lib.v128_store64_lane);
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }
                                    break;
                                default:
                                    if (callMethods.TryGetValue(instr2, out var method))
                                    {
                                        il.Emit(OpCodes.Call, cls.Module.ImportReference(method));
                                        break;
                                    }else
                                        throw new Exception("Unsupported opcode: " + instr2 + "   " + instr2.ToString("X"));
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

            

            List<object> allOpcodes = [];
            allOpcodes.AddRange(usedInstructions);
            allOpcodes.AddRange(usedVectorInstructions);
            allOpcodes.AddRange(usedExtendedInstructions);
            
            return;
        }

        private Dictionary<MethodReference, MethodReference> wrappedMethods = new();

        private MethodReference MaybeWrap(MethodReference fcn)
        {
            if (wrappedMethods.TryGetValue(fcn, out var fcn2))
                return fcn2;
            fcn2 = fcn;
            foreach (var param in fcn.Parameters)
            {
                if (param.ParameterType.Name == "CString"
                    || param.ParameterType.IsPointer
                    || param.ParameterType.Name == "HeapContext")
                {
                    fcn2 = WrapMethod(fcn2);
                    break;
                }
            }

            wrappedMethods[fcn] = fcn2;
            return fcn2;
        }

        unsafe MethodReference WrapMethod(MethodReference m)
        {
            if (cls.Methods.FirstOrDefault(x => x.Name == m.Name + "__wrap") is { } ext)
            {
                return ext;
            }

            var m2 = new MethodDefinition(m.Name + "__wrap",
                MethodAttributes.Static | MethodAttributes.Public,
                m.ReturnType);
            ConstructorInfo methodImplConstructor =
                typeof(MethodImplAttribute).GetConstructor([typeof(MethodImplOptions)]);
            // Create a CustomAttributeBuilder with the MethodImplOptions value
            var attr = new CustomAttribute(def.MainModule.ImportReference(methodImplConstructor));
            attr.ConstructorArguments.Add(new CustomAttributeArgument(
                def.MainModule.ImportReference(typeof(MethodImplOptions)), MethodImplOptions.AggressiveInlining));
            m2.CustomAttributes.Add(attr);

            cls.Methods.Add(m2);
            var il2 = m2.Body.GetILProcessor();
            int argidx = 0;
            foreach (var p in m.Parameters)
            {
                var p2 = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType);
                m2.Parameters.Add(p2);
                if (p.ParameterType.Name == "HeapContext")
                {
                    p2.ParameterType = def.MainModule.ImportReference(typeof(Type));
                    il2.Emit(OpCodes.Ldtoken, cls);
                    il2.EmitCall(() => HeapContext.Create);
                    argidx--;
                    m2.Parameters.Remove(p2);
                }
                else if (p.ParameterType.Name == "CString")
                {
                    p2.ParameterType = i32Type;

                    il2.Emit(OpCodes.Ldsfld, memoryField);
                    il2.Emit(OpCodes.Ldarg, argidx);
                    il2.EmitCall(() => CString.New2);
                }
                else if (p.ParameterType.IsPointer)
                {
                    p2.ParameterType = i32Type;

                    il2.Emit(OpCodes.Ldsfld, memoryField);
                    il2.Emit(OpCodes.Ldarg, argidx);
                    il2.Emit(OpCodes.Add);
                    //il2.Emit(OpCodes.Conv_U);
                }
                else
                {
                    il2.Emit(OpCodes.Ldarg, argidx);
                }

                argidx += 1;
            }


            il2.Emit(OpCodes.Call, m);
            if (m.ReturnType.IsPointer)
            {
                il2.Emit(OpCodes.Ldsfld, memoryField);
                il2.Emit(OpCodes.Sub);
                il2.Emit(OpCodes.Conv_I4);
                m2.ReturnType = this.i32Type;
            }

            il2.Emit(OpCodes.Ret);
            return m2;
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
                            Log.WriteLine("Export already defined: {0} {1} - {2}", index, name,
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
                        Log.WriteLine("Memory: {0}", memIndex);
                        break;
                    case ImportType.GLOBAL:
                        var idx = reader.ReadU32Leb();
                        if (globals.TryGetValue(idx, out var glob))
                        {
                            glob.Field.Name = name;
                        }

                        Log.WriteLine("Global import: {0}   {1}", idx, name);
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
                case 123: return v128Type;
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
            Log.WriteLine("Globals: {0}", globals.Count);
        }


        unsafe void ReadMemorySection(BinReader reader)
        {
            var memCount = reader.ReadU32Leb();
            Assert.AreEqual<uint>(1, memCount);
            for (uint i = 0; i < memCount; i++)
            {
                var type = reader.ReadU8();
                var min = reader.ReadU32Leb();
                if (type == 0)
                {
                    Log.WriteLine("Memory: {0} pages", min);
                    var cctoril = cls.GetStaticConstructor().Body.GetILProcessor();
                    cctoril.Body.Instructions.RemoveAt(cctoril.Body.Instructions.Count - 1);
                    cctoril.Emit(OpCodes.Ldc_I4, (int) (min * page_size));
                    // Allocate 4GB of virtual memory so we'll never have to move the heap pointer.
                    cctoril.Emit(OpCodes.Ldc_I8, 4L * 1024L * 1024L * 1024L);
                    cctoril.EmitCall(() => MemoryAllocator.AllocateMemory);
                    cctoril.Emit(OpCodes.Stsfld, memoryField);
                    cctoril.Emit(OpCodes.Stsfld, memoryFieldSize);
                    cctoril.Emit(OpCodes.Ret);
                }
                else if (type == 1)
                {
                    var max = reader.ReadU32Leb();

                    Log.WriteLine("Memory of {0}-{1} pages ({2} - {3})", min, max, min * page_size,
                        max * page_size);
                    var cctoril = cls.GetStaticConstructor().Body.GetILProcessor();
                    cctoril.Body.Instructions.RemoveAt(cctoril.Body.Instructions.Count - 1);
                    
                    // Allocate 4GB of virtual memory so we'll never have to move the heap pointer.
                    cctoril.Emit(OpCodes.Ldc_I8, 4L * 1024L * 1024L * 1024L);
                    cctoril.EmitCall(() => MemoryAllocator.AllocateMemory);
                    cctoril.Emit(OpCodes.Stsfld, memoryField);
                    cctoril.Emit(OpCodes.Ldc_I4, (int) (min * page_size));
                    cctoril.Emit(OpCodes.Stsfld, memoryFieldSize);

                    cctoril.Emit(OpCodes.Ret);
                }
            }
        }

        unsafe void EmitCall(ILProcessor gen, Expression expr)
        {
            var f =
                (((expr as LambdaExpression).Body as UnaryExpression).Operand as MethodCallExpression).Object as
                ConstantExpression;
            var method = (MethodInfo) f.Value;
            var declType = gen.Body.Method.DeclaringType;
            var reference = declType.Module.ImportReference(method);
            reference = MaybeWrap(reference);
            gen.Emit(OpCodes.Call, reference);
        }


        void ReadFunctionSection(BinReader reader)
        {
            uint funcCount = reader.ReadU32Leb();
            Log.WriteLine("Func count: {0}", funcCount);
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

        Dwarf.Parser dwarfparser = new Dwarf.Parser();
        private Dwarf.DwarfCompilationUnit cu = null;
        private Dictionary<string, string[]> parameterNames = new();
        void ReadCustomSection(BinReader reader)
        {
            var name = reader.ReadStrN();
            Log.WriteLine("Custom section name: {0}", name);
            if (name == "name")
            {
                for (int i = 0; i < 3; i++)
                {
                    var id = reader.ReadU8();
                    var len = reader.ReadU32Leb();
                    var next = reader.Position + len;
                    Log.WriteLine("Name section {0} ({1} bytes)", id, len);
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
            if(name == ".debug_abbrev")
            {
                dwarfparser.ParseAbbrev(reader);   
            }

            if (name == ".debug_info")
            {
                var debugInfo = dwarfparser.ParseDebugInfo(reader);
                cu = debugInfo.First();
            }

            if (name == ".debug_str")
            {
                var strTable = new DwarfStringTable(reader.ReadAllBytes());
                var root = cu.RootDIE;
                foreach (var thing in root.Children)
                {
                    if (thing.Tag == DwarfTag.DW_TAG_subprogram)
                    {
                        if (thing.Attributes.TryGetValue(AttributeEncoding.DW_AT_name, out var subProgramNameId)
                            && strTable.TryGetString((uint)subProgramNameId.Value, out var subProgramName))
                        {
                            parameterNames[subProgramName] =
                                thing.Children.Where(die => die.Tag == DwarfTag.DW_TAG_formal_parameter)
                                    .Select(param =>
                                        param.Attributes.FirstOrDefault(attr => attr.Key == AttributeEncoding.DW_AT_name).Value)
                                    
                                    .Select(attrValue => attrValue?.Form ==DwarfForm.DW_FORM_strp ? strTable.GetString((uint) attrValue.Value) : null)
                                    .ToArray();
                            
                        }
                    }
                }
            }

            if (name == ".debug_types")
            {
                // parse the types section.
            }
        }

        void ReadTypeSection(BinReader reader)
        {
            var typeCount = reader.ReadU32Leb();
            for (uint i = 0; i < typeCount; i++)
            {
                var header = reader.ReadU8();
                Assert.AreEqual(0x60, header);
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

    
    public enum DwarfAttibute : byte
    {
        DW_AT_discr_list = 0x3d
    }

    public class TransformException : Exception
    {
        public TransformException(string s) : base(s)
        {
            
        }
    }
}