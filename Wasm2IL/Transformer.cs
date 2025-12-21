using System.Diagnostics;
using System.Reflection;
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

namespace Wasm2IL;

using WOp = Wasm.Instruction;
using IlOp = OpCodes;

public class Transformer
{
    readonly Dictionary<string, List<Type>> importModules = new();
    readonly List<Type> overrideModules = [];
    CodeGenModuleContext moduleContext;

    /// <summary>
    /// Enable IL optimizations like constant folding. Default is true.
    /// </summary>
    public bool EnableOptimizations { get; set; } = true;

    /// <summary>
    /// Adds code to check that loads are in range. 
    /// </summary>
    public bool CheckLoads { get; set; } = false;

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
            var resolveEventArgs = new ResolveImportEventArgs
            {
                Name = name,
                ModuleName = moduleName,
                TypeBuilder = cls,
                ModuleDefinition = def.MainModule
            };
            OnResolveImport.Invoke(this, resolveEventArgs);
            if (resolveEventArgs.Handled)
                return resolveEventArgs.Result;
        }

        if (importModules.TryGetValue(moduleName, out var t))
        {
            foreach (var type in t)
            {
                if (type.GetMethod(name) is { } m)
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

    public WasmAssembly LoadWasmAssembly(Stream stream, string name, string outDll = "tmp.dll",
        string version = "1.0.0")
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
        using (var mem = new FileStream(path ?? throw new InvalidOperationException(), FileMode.Create,
                   FileAccess.ReadWrite))
        {
            Transform(file, name, mem);
            mem.Position = 0;
        }

        var asm = Assembly.Load(File.ReadAllBytes(path));
        return new WasmAssembly(asm);
    }

    const string MagicHeader = "\0asm";
    const uint PageSize = 1 << 16;
    readonly Dictionary<uint, Global> globals = new();
    readonly Dictionary<uint, ImportFunc> exportFunc = new();
    readonly Dictionary<uint, ImportFunc> importFuncs = new();
    Dictionary<uint, ImportFunc> overrideFuncs;

    readonly Dictionary<uint, ExportTable> exportTables = new();

    readonly Dictionary<uint, TypeId> types = new();

    readonly Dictionary<uint, FuncDeclType> funcDecl = new();
    AssemblyDefinition def;
    TypeDefinition cls;
    FieldDefinition memoryField;
    FieldDefinition memoryFieldSize;
    FieldDefinition functionTable;

    TypeReference f32Type, f64Type, i64Type, i16Type, i32Type, voidType, byteType;
    TypeReference v128Type;

    MethodReference ResolveTypeConstructor(Type t, params Type[] argTypes)
    {
        return def.MainModule.ImportReference(
            t.GetConstructors().FirstOrDefault(x =>
                x.GetParameters().Select(y => y.ParameterType).SequenceEqual(argTypes)));
    }

    // note there are also globals which are added dynamically depending on need.

    void Init(string asmName, Version version)
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
        v128Type = asm.MainModule.ImportReference(typeof(Vector128<byte>));
        cls = new TypeDefinition(asmName, "C",
            TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class |
            TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public,
            asm.MainModule.TypeSystem.Object);

        asm.MainModule.Types.Add(cls);
        def = asm;

        memoryField = new FieldDefinition("Memory", FieldAttributes.Static | FieldAttributes.Public,
            asm.MainModule.TypeSystem.Byte.MakePointerType());
        cls.Fields.Add(memoryField);

        memoryFieldSize = new FieldDefinition("MemorySize", FieldAttributes.Static | FieldAttributes.Public,
            asm.MainModule.TypeSystem.Int32);
        cls.Fields.Add(memoryFieldSize);

        functionTable = new FieldDefinition("FunctionTable", FieldAttributes.Static | FieldAttributes.Public,
            asm.MainModule.TypeSystem.IntPtr.MakeArrayType());
        cls.Fields.Add(functionTable);
        var cctor = new MethodDefinition(".cctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static |
            MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, asm.MainModule.TypeSystem.Void);
        cls.Methods.Add(cctor);
        var cctoril = cctor.Body.GetILProcessor();
        cctoril.Emit(OpCodes.Nop);
        cctoril.Emit(OpCodes.Ret);

        moduleContext = new(voidType, memoryField, asm.MainModule, cls);
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
        if (MagicHeader != header)
            throw new Exception("invalid header");
        var wasmVersion = new byte[4];
        reader.Read(wasmVersion);
        if (!wasmVersion.SequenceEqual((byte[]) [1, 0, 0, 0]))
            throw new NotSupportedException($"Unsupported wasm version: {BitConverter.ToString(wasmVersion)}");
        Log.WriteLine("Wasm Version: {0}", string.Join(" ", wasmVersion));

        Init(asmName, version);
        long codeLoc = 0;
        long elementLoc = 0;
        while (!reader.IsAtEnd)
        {
            var section = (Section) reader.ReadU8();
            uint length = reader.ReadU32Leb();
            Log.WriteLine("Reading section {0}: {1}bytes", section, length);
            var next = reader.Position + length;

            switch (section)
            {
                case Section.CUSTOM:
                {
                    var sec = reader.ReadBytes((int) length);
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

        foreach (var kv in importFuncs.Where(x => x.Value.Method == null).ToArray())
        {
            var imp = kv.Value;
            var method = ResolveImportedMethod(imp.Module, imp.Name);
            if (method != null)
            {
                continue;
            }

            Log.WriteLine($"Warning: Import not defined {imp.Name} by module {imp.Module}.");
            var type = types[(uint) imp.TypeId];
            var m = new MethodDefinition(imp.Name, MethodAttributes.Public | MethodAttributes.Static,
                type.ReturnType);
            foreach (var p in type.ParamTypes)
                m.Parameters.Add(new ParameterDefinition(p));

            // Stub throws at runtime
            m.Body.InitLocals = true;
            var il = m.Body.GetILProcessor();
            il.Emit(IlOp.Ldstr, $"{imp.Name} not implemented");
            il.Emit(IlOp.Newobj, ResolveTypeConstructor(typeof(Exception), typeof(string)));
            il.Emit(IlOp.Throw);
            cls.Methods.Add(m);
            imp.Method = m;
            importFuncs[kv.Key] = imp;
        }

        reader.Position = elementLoc;
        ReadElementSection(reader);

        overrideFuncs = new();
        Dictionary<string, uint> declaredFunctions = new();
        foreach (var item in funcDecl)
        {
            if (item.Value?.ImportName is { } name)
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
                    overrideFuncs[id] = new ImportFunc
                    {
                        Index = id,
                        CustomName = method.Name,
                        Method = def.MainModule.ImportReference(method),
                        Name = method.Name,
                        Module = "override",
                        TypeId = funcDecl[id].TypeId
                    };
                }
            }
        }

        reader.Position = codeLoc;
        ReadCodeSection(reader);

        if (EnableOptimizations)
        {
            ILOptimizer.OptimizeType(cls);
            foreach (var method in cls.Methods)
                method.Body.Optimize();
        }

        def.Write(outStream);
        Log.WriteLine($"Output written to {outStream}");
        def.Dispose();
    }

    void ReadImportSection(BinReader reader)
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
                    var funid = (uint) importFuncs.Count;
                    importFuncs[funid] = new ImportFunc
                    {
                        Name = itemName,
                        TypeId = typeid,
                        Index = funid,
                        Module = moduleName
                    };
                    break;
                case ImportType.TABLE:
                    var elemType = reader.ReadU8();
                    if (elemType != 0x70)
                        throw new NotSupportedException($"Unsupported table element type 0x{elemType:X2}");
                    byte limitt = reader.ReadU8();
                    uint min = reader.ReadU32Leb();
                    uint max = limitt == 0 ? min : reader.ReadU32Leb();
                    Log.WriteLine("Table: {0}.{1} {2}-{3}", moduleName, itemName, min, max);
                    break;
                case ImportType.GLOBAL:
                    var valType = reader.ReadU8();
                    bool mut = reader.ReadU8() > 0;

                    Log.WriteLine("Global: {0}.{1} {2}-{3}", moduleName, itemName, valType, mut);
                    break;
                case ImportType.MEM:
                    limitt = reader.ReadU8();
                    min = reader.ReadU32Leb();
                    max = limitt == 0 ? min : reader.ReadU32Leb();
                    Log.WriteLine("Memory: {0}.{1} {2}-{3}", moduleName, itemName, min, max);
                    break;
            }
        }
    }

    void ReadElementSection(BinReader reader)
    {
        var cnt = reader.ReadU32Leb();
        for (int i = 0; i < cnt; i++)
        {
            var tableIndex = reader.ReadU32Leb();
            if (tableIndex != 0)
                throw new Exception("Multiple tables are not supported");
            var instr2 = reader.ReadInstruction();
            if (instr2 == WOp.VECTOR_INSTRUCTION)
            {
                var ext2 = reader.ReadU8();
                instr2 = (WOp) (0xFD00 | ext2);
            }

            Assert.AreEqual(Wasm.Instruction.I32_CONST, instr2);
            var elementOffset = reader.ReadU32Leb();
            var end = reader.ReadInstruction();
            if (end != WOp.END)
                throw new Exception("Expected END opcode");
            var fncCnt = reader.ReadU32Leb();

            var ctor = cls.GetStaticConstructor();
            ctor.Body.Instructions.RemoveAt(ctor.Body.Instructions.Count - 1);
            var il = ctor.Body.GetILProcessor();
            il.Emit(OpCodes.Ldc_I4, (int) fncCnt + 1);
            il.Emit(OpCodes.Newarr, def.MainModule.TypeSystem.IntPtr);
            il.Emit(OpCodes.Stsfld, functionTable);

            for (var i2 = 0; i2 < fncCnt; i2++)
            {
                il.Emit(OpCodes.Ldsfld, functionTable);
                il.Emit(OpCodes.Ldc_I4, (int) (i2 + elementOffset));

                var funcId = reader.ReadU32Leb();
                if (funcId < importFuncs.Count)
                {
                    var imp = importFuncs[funcId];
                    if (imp.Method == null)
                    {
                        var method = ResolveImportedMethod(imp.Module, imp.Name);
                        if (method != null)
                        {
                            var reference = method is MethodReference mr
                                ? mr
                                : def.MainModule.ImportReference((MethodInfo) method);
                            reference = moduleContext.MaybeWrap(reference);
                            imp.Method = reference;
                        }
                    }

                    if (imp.Method == null)
                        throw new InvalidOperationException(
                            $"Failed to resolve imported function: {imp.Module}.{imp.Name}");

                    il.Emit(OpCodes.Ldftn, imp.Method);
                    il.Emit(OpCodes.Stelem_I);
                }
                else
                {
                    il.Emit(OpCodes.Ldftn, funcDecl[(uint) (funcId - importFuncs.Count)].Method);
                    il.Emit(OpCodes.Stelem_I);
                }
            }

            il.Emit(IlOp.Ret);
        }
    }

    void ReadDataSection(BinReader reader)
    {
        uint dataCount = reader.ReadU32Leb();
        for (int i = 0; i < dataCount; i++)
        {
            uint memidx = reader.ReadU32Leb();
            if (memidx != 0)
                throw new Exception("Multipe memories are not supported");

            int offset = 0;
            while (true)
            {
                var instr = reader.ReadInstruction();
                switch (instr)
                {
                    case WOp.I32_CONST:
                        var memOffset = (int) reader.ReadI64Leb();
                        offset = memOffset;
                        break;
                    case WOp.GLOBAL_GET:
                        throw new NotSupportedException(
                            "GLOBAL_GET in data section offset expression not supported");
                    case WOp.END:
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

            il.Emit(IlOp.Ldsfld, memoryField);
            il.Emit(IlOp.Ldc_I4, offset);
            il.Emit(IlOp.Add);
            for (int i2 = 0; i2 < byteCount; i2++)
            {
                if (byteCount - i2 >= 8)
                {
                    var v = BitConverter.ToInt64(bc.AsSpan(i2, 8));
                    if (v != 0)
                    {
                        il.Emit(IlOp.Dup);
                        il.Emit(IlOp.Ldc_I8, v);
                        il.Emit(IlOp.Stind_I8);
                    }

                    il.Emit(IlOp.Ldc_I4_8);
                    il.Emit(IlOp.Add);

                    i2 += 7;
                }
                else if (bc[i2] != 0)
                {
                    il.Emit(IlOp.Dup);
                    il.Emit(IlOp.Ldc_I4, (int) bc[i2]);
                    il.Emit(IlOp.Stind_I1);
                    il.Emit(IlOp.Ldc_I4_1);
                    il.Emit(IlOp.Add);
                }
            }

            il.Emit(IlOp.Pop);
            il.Emit(IlOp.Ret);
        }
    }

    class LabelType
    {
        public byte Type;
        public Instruction EndLabel;
        public Instruction StartLabel;
    }

    MethodReference ResolveMethod(CodeGenModuleContext moduleCtx, uint func)
    {
        if (func < importFuncs.Count)
        {
            var importFun = importFuncs[func];
            if (importFun.Method == null)
            {
                var m3 = ResolveImportedMethod(importFun.Module, importFun.Name);
                if (m3 != null)
                {
                    var type2 = types[importFun.TypeId.Value];
                    var m2 = m3 is MethodReference mr ? mr : def.MainModule.ImportReference((MethodInfo) m3);

                    m2 = moduleCtx.MaybeWrap(m2);

                    if (m2.ReturnType.FullName == voidType.FullName && type2.ReturnCount != 0)
                    {
                        throw new Exception(
                            $"Type signature of {importFun.Name} does not match declared type. (return arguments)");
                    }

                    if (m2.ReturnType.FullName != voidType.FullName && type2.ReturnCount == 0)
                    {
                        throw new Exception(
                            $"Type signature of {importFun.Name} does not match declared type. (return arguments)");
                    }

                    if (type2.ParamCount != m2.Parameters.Count)
                    {
                        throw new Exception(
                            $"Type signature of {importFun.Name} does not match declared type. (parameter count)");
                    }

                    importFun.Method = m2;
                    return m2;
                }

                var type = types[(uint) importFun.TypeId];
                var m = new MethodDefinition(importFun.Name.Replace(":", "_"),
                    MethodAttributes.Static | MethodAttributes.Public, type.ReturnType);
                foreach (var param in type.ParamTypes)
                    m.Parameters.Add(new ParameterDefinition(param));
                var il = m.Body.GetILProcessor();
                il.Emit(IlOp.Ldstr, $"{importFun.Name} not implemented");
                il.Emit(IlOp.Newobj, ResolveTypeConstructor(typeof(Exception), typeof(string)));
                il.Emit(IlOp.Throw);
                importFun.Method = m;
                cls.Methods.Add(m);
            }

            return importFun.Method;
        }

        if (overrideFuncs.TryGetValue(func - (uint) importFuncs.Count, out var reference))
            return reference.Method;

        return funcDecl[func - (uint) importFuncs.Count].Method;
    }

    unsafe void ReadCodeSection(BinReader reader)
    {
        uint funcCount = reader.ReadU32Leb();
        for (uint i = 0; i < funcCount; i++)
        {
            var funcId = funcDecl[i];
            var ftype = types[funcId.TypeId];
            string name = funcId.ImportName;
            if (exportFunc.TryGetValue((uint) (i + importFuncs.Count), out var exp))
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
            bool useName = parameterNames.TryGetValue(name, out var paramNames);
            for (uint i2 = 0; i2 < ftype.ParamCount; i2++)
            {
                var parameter = new ParameterDefinition(ftype.ParamTypes[i2])
                {
                    Name = "param" + i2
                };
                if (useName && paramNames.ElementAtOrDefault((int) i2) is { } name2)
                {
                    parameter.Name = name2;
                }

                m1.Parameters.Add(parameter);
            }
        }

        for (uint i = 0; i < funcCount; i++)
        {
            var funcId = funcDecl[i];
            var ftype = types[funcId.TypeId];
            var m1 = funcId.Method;

            cls.Methods.Add(m1);
            var il = m1.Body.GetILProcessor();

            var codeSize = reader.ReadU32Leb();

            var next = reader.Position + codeSize;

            var localCount = reader.ReadU32Leb();
            for (uint i2 = 0; i2 < localCount; i2++)
            {
                uint n = reader.ReadU32Leb();
                var t = reader.ReadU8();
                for (uint i3 = 0; i3 < n; i3++)
                {
                    var tp = ByteToTypeReference(t);
                    m1.Body.Variables.Add(new VariableDefinition(tp));
                }
            }

            var heapVar = new VariableDefinition(def.MainModule.TypeSystem.Byte.MakePointerType());
            var ctx = new CodeGenContext(moduleContext, il, m1, heapVar, reader);

            m1.Body.Variables.Add(new VariableDefinition(def.MainModule.TypeSystem.Int32)); // heapaddr
            m1.Body.Variables.Add(heapVar);

            var labelStack = new List<LabelType> {new()};

            while (next > reader.Position)
            {
                var instr = reader.ReadInstruction();

                OpCode? jmpInstr = null;
                switch (instr)
                {
                    case WOp.NOP:
                        il.Emit(IlOp.Nop);
                        break;
                    case WOp.RETURN_CALL:
                    case WOp.CALL:
                    {
                        var isTailCall = instr == WOp.RETURN_CALL;
                        var fcn = reader.ReadU32Leb();
                        var otherFun = ResolveMethod(moduleContext, fcn);
                        if (otherFun == null)
                            throw new InvalidOperationException($"Cannot resolve function at index {fcn}");
                        otherFun = moduleContext.MaybeWrap(otherFun);

                        if (isTailCall)
                            il.Emit(IlOp.Tail);
                        il.Emit(IlOp.Call, otherFun);
                        if (isTailCall)
                            il.Emit(IlOp.Ret);

                        ctx.PopType(otherFun.Parameters.Count);
                        ctx.PushType(otherFun.ReturnType);
                        break;
                    }
                    case WOp.RETURN_CALL_INDIRECT:
                    case WOp.CALL_INDIRECT:
                    {
                        var isTailCall = instr == WOp.RETURN_CALL_INDIRECT;
                        var typeidx = reader.ReadU32Leb();
                        var table = reader.ReadU32Leb();
                        if (table != 0)
                            throw new NotSupportedException("Multiple tables not supported");
                        var ftp = types[typeidx];

                        // Stack: [params..., tableIndex] -> need [params..., funcPtr]
                        // Store table index, load function pointer, params stay in place
                        il.Emit(IlOp.Stloc, ctx.GetHelperVariable(i32Type));
                        il.Emit(IlOp.Ldsfld, functionTable);
                        il.Emit(IlOp.Ldloc, ctx.GetHelperVariable(i32Type));
                        il.Emit(IlOp.Ldelem_I);

                        // Create CallSite for calli instruction
                        var callSite = new CallSite(ftp.ReturnType);
                        foreach (var paramType in ftp.ParamTypes)
                            callSite.Parameters.Add(new ParameterDefinition(paramType));

                        //if (isTailCall)
                        //    il.Emit(IlOp.Tail);
                        il.Emit(IlOp.Calli, callSite);
                        if (isTailCall)
                            il.Emit(IlOp.Ret);
                        ctx.PopType((int) ftp.ParamCount);
                        ctx.PushType(ftp.ReturnType);
                        break;
                    }
                    case WOp.BLOCK:
                        var blockType = reader.ReadU8();
                        var endLabel = il.Create(OpCodes.Nop);
                        var blk = new LabelType {Type = blockType, EndLabel = endLabel, StartLabel = endLabel};
                        labelStack.Add(blk);
                        break;
                    case WOp.LOOP:
                        blockType = reader.ReadU8();
                        var startLabel = il.Create(OpCodes.Nop);
                        il.Append(startLabel);
                        blk = new LabelType {Type = blockType, EndLabel = null, StartLabel = startLabel};
                        labelStack.Add(blk);
                        break;
                    case WOp.IF:
                        blockType = reader.ReadU8();
                    {
                        endLabel = il.Create(OpCodes.Nop);
                        il.Emit(IlOp.Brfalse, endLabel);
                        blk = new LabelType {Type = blockType, EndLabel = endLabel, StartLabel = null};
                        labelStack.Add(blk);
                    }
                        break;
                    case WOp.ELSE:
                        endLabel = il.Create(OpCodes.Nop);
                        il.Emit(OpCodes.Br, endLabel);
                        blk = labelStack.Last();
                        labelStack.Remove(blk);
                        il.Append(blk.EndLabel);
                        blk = new LabelType {Type = blk.Type, EndLabel = endLabel, StartLabel = null};
                        labelStack.Add(blk);
                        break;

                    case WOp.BR:
                    case WOp.BR_IF:

                        var brindex = reader.ReadU32Leb();
                        if (instr == WOp.BR_IF)
                        {
                            ctx.PopType();
                            il.Emit(jmpInstr ?? OpCodes.Brtrue,
                                labelStack[(int) (labelStack.Count - brindex - 1)].StartLabel);
                        }
                        else
                            il.Emit(OpCodes.Br, labelStack[(int) (labelStack.Count - brindex - 1)].StartLabel);

                        break;
                    case WOp.BR_TABLE:
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

                        ctx.PopType();
                        break;
                    case WOp.SELECT:
                        // select(a,b,c) = a ? b : c
                        // we have to keep track of the type on top of the stack.
                        var t = ctx.PopType(2);
                        var nextLabel = il.Create(IlOp.Stloc, ctx.GetHelperVariable(t));
                        endLabel = il.Create(IlOp.Nop);
                        il.Emit(IlOp.Brfalse, nextLabel);
                        il.Emit(IlOp.Pop);
                        il.Emit(IlOp.Br, endLabel);
                        il.Append(nextLabel);
                        il.Emit(IlOp.Pop);
                        il.Emit(IlOp.Ldloc, ctx.GetHelperVariable(t));
                        il.Append(endLabel);
                        break;
                    case WOp.GLOBAL_GET:
                        var offset2 = reader.ReadU32Leb();
                        var glob = globals[offset2];
                        il.Emit(IlOp.Ldsfld, glob.Field);
                        ctx.PushType(glob.Field.FieldType);
                        break;
                    case WOp.GLOBAL_SET:
                        offset2 = reader.ReadU32Leb();
                        glob = globals[offset2];
                        il.Emit(IlOp.Stsfld, glob.Field);
                        ctx.PopType();
                        break;
                    case WOp.LOCAL_SET:
                    case WOp.LOCAL_GET:
                    case WOp.LOCAL_TEE:
                        VariableDefinition localVar = null;
                        ParameterDefinition param = null;
                        uint localIndex = reader.ReadU32Leb();
                        bool isArg = localIndex < ftype.ParamCount;
                        if (!isArg)
                        {
                            localIndex -= ftype.ParamCount;
                            localVar = m1.Body.Variables[(int) localIndex];
                        }
                        else
                        {
                            param = m1.Parameters[(int) localIndex];
                        }

                        switch (instr)
                        {
                            case WOp.LOCAL_GET:
                                if (!isArg && localIndex < 4)
                                {
                                    il.Emit(new[]
                                        {IlOp.Ldloc_0, IlOp.Ldloc_1, IlOp.Ldloc_2, IlOp.Ldloc_3}[
                                        localIndex]);
                                }
                                else if (isArg && localIndex < 4)
                                {
                                    il.Emit(new[]
                                        {IlOp.Ldarg_0, IlOp.Ldarg_1, IlOp.Ldarg_2, IlOp.Ldarg_3}[
                                        localIndex]);
                                }
                                else if (localIndex < 256)
                                {
                                    il.Emit(isArg ? IlOp.Ldarg_S : IlOp.Ldloc_S, (byte) localIndex);
                                }
                                else
                                {
                                    il.Emit(isArg ? IlOp.Ldarg : IlOp.Ldloc, (int) localIndex);
                                }

                                ctx.PushType(param?.ParameterType ?? localVar?.VariableType);
                                break;
                            case WOp.LOCAL_SET:
                                if (!isArg && localIndex < 4)
                                {
                                    il.Emit(new[]
                                        {IlOp.Stloc_0, IlOp.Stloc_1, IlOp.Stloc_2, IlOp.Stloc_3}[
                                        localIndex]);
                                }
                                else
                                {
                                    if (localIndex < 256)
                                    {
                                        il.Emit(isArg ? IlOp.Starg_S : IlOp.Stloc_S, (byte) localIndex);
                                    }
                                    else
                                    {
                                        il.Emit(isArg ? IlOp.Starg : IlOp.Stloc, (int) localIndex);
                                    }
                                }

                                ctx.PopType();
                                break;
                            case WOp.LOCAL_TEE:
                                il.Emit(IlOp.Dup);
                                il.Emit(isArg ? IlOp.Starg : IlOp.Stloc, (int) localIndex);
                                break;
                        }

                        break;
                    case WOp.I32_CONST:
                    {
                        var cint = (int) reader.ReadI64Leb();
                        il.Emit(IlOp.Ldc_I4, cint);

                        ctx.PushType(i32Type);
                        break;
                    }
                    case WOp.I64_CONST:
                        ctx.PushType(i64Type);
                        il.Emit(IlOp.Ldc_I8, reader.ReadI64Leb());
                        break;
                    case WOp.F32_CONST:
                        ctx.PushType(f32Type);
                        il.Emit(IlOp.Ldc_R4, reader.ReadF32());
                        break;
                    case WOp.F64_CONST:
                        ctx.PushType(f64Type);
                        il.Emit(IlOp.Ldc_R8, reader.ReadF64());
                        break;
                    case WOp.MEMORY_SIZE:
                    {
                        var memIdx = reader.ReadU8();
                        if (memIdx != 0)
                            throw new NotSupportedException("Multiple memories not supported");
                        ctx.LoadMemory();
                        il.Emit(IlOp.Ldlen);
                        il.Emit(IlOp.Ldc_I4, (int) PageSize);
                        il.Emit(IlOp.Div);
                        il.Emit(IlOp.Conv_I4);
                        ctx.PushType(i32Type);
                        break;
                    }
                    case WOp.MEMORY_GROW:
                    {
                        var memIdx = reader.ReadU8();
                        if (memIdx != 0)
                            throw new NotSupportedException("Multiple memories not supported");
                        throw new NotSupportedException("memory.grow is not supported");
                    }
                    case WOp.I32_LOAD:
                    case WOp.I32_LOAD8_S:
                    case WOp.I32_LOAD8_U:
                    case WOp.I32_LOAD16_U:
                    case WOp.I32_LOAD16_S:
                    case WOp.I64_LOAD8_S:
                    case WOp.I64_LOAD8_U:
                    case WOp.I64_LOAD16_S:
                    case WOp.I64_LOAD16_U:
                    case WOp.I64_LOAD32_S:
                    case WOp.I64_LOAD32_U:
                    case WOp.I64_LOAD:
                    case WOp.F32_LOAD:
                    case WOp.F64_LOAD:
                    case WOp.I32_STORE:
                    case WOp.I32_STORE_8:
                    case WOp.I32_STORE_16:
                    case WOp.I64_STORE:
                    case WOp.I64_STORE_32:
                    case WOp.I64_STORE_8:
                    case WOp.I64_STORE_16:
                    case WOp.F32_STORE:
                    case WOp.F64_STORE:
                        reader.ReadU32Leb(); // align hint (ignored)
                        var offset = (int) reader.ReadU32Leb();

                        Instruction loadValue = null;
                        if (instr.ToString().Contains("STORE"))
                        {
                            var valueSource = OptimizerHelper.GetValueSource(il.Body, 1).FirstOrDefault();
                            if (valueSource != null &&
                                (valueSource.OpCode.Name.StartsWith("ldc")
                                 || valueSource.OpCode.Name.StartsWith("ldloc")
                                 || valueSource.OpCode.Name.StartsWith("ldarg")
                                ))
                            {
                                loadValue = valueSource;
                                il.Remove(valueSource);
                            }
                            else
                            {
                                VariableDefinition stvar;
                                if (instr.ToString().Contains("F32"))
                                    stvar = ctx.GetHelperVariable(f32Type);
                                else if (instr.ToString().Contains("F64"))
                                    stvar = ctx.GetHelperVariable(f64Type);
                                else if (instr.ToString().Contains("I64"))
                                    stvar = ctx.GetHelperVariable(i64Type);
                                else if (instr.ToString().Contains("I32"))
                                    stvar = ctx.GetHelperVariable(i32Type);
                                else throw new Exception("Unknown type");
                                loadValue = il.Create(IlOp.Ldloc, stvar);
                                il.Emit(IlOp.Stloc, stvar);
                            }
                        }

                        // check that the memory location is ok.
                       
                        bool later = false;
                        bool add = true;
                        Instruction prevInstr = null;
                        var offsetSource = OptimizerHelper.GetValueSource(il.Body, 1).FirstOrDefault();
                        if (offsetSource != null)
                        {
                            if (offsetSource.OpCode == IlOp.Ldc_I4)
                            {
                                offset += (int) offsetSource.Operand;
                                add = false;
                                il.Remove(offsetSource);
                            }
                            else if (offsetSource.OpCode.Name.StartsWith("ldloc")
                                     || offsetSource.OpCode.Name.StartsWith("ldarg"))
                            {
                                prevInstr = offsetSource;
                                later = true;
                                il.Remove(offsetSource);
                            }
                            else if (offsetSource.OpCode.Name.StartsWith("ldc"))
                            {
                            }
                        }

                        
                        ctx.LoadMemory();
                        if (later)
                            il.InsertAfter(il.Body.Instructions.LastOrDefault(), prevInstr);

                        // adjust according to the offset 
                        if (offset != 0)
                        {
                            il.Emit(IlOp.Ldc_I4, offset);
                            il.Emit(IlOp.Add);
                        }

                        if (add)
                            il.Emit(IlOp.Add);

                        if (CheckLoads && instr.ToString().Contains("LOAD"))
                        {
                            il.Emit(IlOp.Dup);
                            il.Emit(IlOp.Ldsfld, memoryFieldSize);
                            il.Emit(IlOp.Ldsfld, memoryField);
                            ctx.EmitCall(typeof(Lib).GetMethod(nameof(Lib.CheckMemory)));
                        }

                        if (loadValue != null)
                            il.Append(loadValue);
                        switch (instr)
                        {
                            // pop address, value. store value in address according to size.
                            case WOp.I32_STORE_8:
                            case WOp.I64_STORE_8:

                                il.Emit(IlOp.Stind_I1);
                                break;
                            case WOp.I32_STORE_16:
                            case WOp.I64_STORE_16:
                                il.Emit(IlOp.Stind_I2);
                                break;
                            case WOp.I32_STORE:
                            case WOp.I64_STORE_32:
                                il.Emit(IlOp.Stind_I4);
                                break;
                            case WOp.I64_STORE:
                                il.Emit(IlOp.Stind_I8);
                                break;
                            case WOp.F32_STORE:
                                il.Emit(IlOp.Stind_R4);
                                break;
                            case WOp.F64_STORE:
                                il.Emit(IlOp.Stind_R8);
                                break;
                            case WOp.I32_LOAD:
                                il.Emit(IlOp.Ldind_I4);
                                ctx.PushType(i32Type);
                                break;
                            case WOp.I32_LOAD8_S:
                                il.Emit(IlOp.Ldind_I1);
                                ctx.PushType(i32Type);
                                break;
                            case WOp.I32_LOAD8_U:
                                il.Emit(IlOp.Ldind_U1);
                                ctx.PushType(i32Type);
                                break;
                            case WOp.I32_LOAD16_U:
                                il.Emit(IlOp.Ldind_U2);
                                ctx.PushType(i32Type);
                                break;
                            case WOp.I32_LOAD16_S:
                                il.Emit(IlOp.Ldind_I2);
                                ctx.PushType(i32Type);
                                break;
                            case WOp.I64_LOAD:
                                ctx.PushType(i64Type);
                                il.Emit(IlOp.Ldind_I8);
                                break;
                            case WOp.F32_LOAD:
                                ctx.PushType(f32Type);
                                il.Emit(IlOp.Ldind_R4);
                                break;
                            case WOp.F64_LOAD:
                                ctx.PushType(f64Type);
                                il.Emit(IlOp.Ldind_R8);
                                break;
                            case WOp.I64_LOAD8_S:
                                ctx.PushType(i64Type);
                                il.Emit(IlOp.Ldind_I1);
                                il.Emit(IlOp.Conv_I8);
                                break;
                            case WOp.I64_LOAD8_U:
                                ctx.PushType(i64Type);
                                il.Emit(IlOp.Ldind_U1);
                                il.Emit(IlOp.Conv_U8);
                                break;
                            case WOp.I64_LOAD16_S:
                                ctx.PushType(i64Type);
                                il.Emit(IlOp.Ldind_I2);
                                il.Emit(IlOp.Conv_I8);
                                break;
                            case WOp.I64_LOAD16_U:
                                ctx.PushType(i64Type);
                                il.Emit(IlOp.Ldind_U2);
                                il.Emit(IlOp.Conv_U8);
                                break;
                            case WOp.I64_LOAD32_S:
                                ctx.PushType(i64Type);
                                il.Emit(IlOp.Ldind_I4);
                                il.Emit(IlOp.Conv_I8);
                                break;
                            case WOp.I64_LOAD32_U:
                                ctx.PushType(i64Type);
                                il.Emit(IlOp.Ldind_U4);
                                il.Emit(IlOp.Conv_U8);
                                break;
                            default:
                                throw new Exception("Unexpected opcode");
                        }

                        break;
                    case WOp.I64_EXTEND_I32_U:
                        il.Emit(IlOp.Conv_U8);
                        ctx.PopType();
                        ctx.PushType(i64Type);
                        break;
                    case WOp.I64_EXTEND_I32_S:
                        il.Emit(IlOp.Conv_I8);
                        ctx.PopType();
                        ctx.PushType(i64Type);
                        break;
                    case WOp.I32_WRAP_I64:
                        il.Emit(IlOp.Conv_I4);
                        ctx.PopType();
                        ctx.PushType(i32Type);
                        break;

                    case WOp.F64_PROMOTE_F32:
                        il.Emit(IlOp.Conv_R8);
                        ctx.PopType();
                        ctx.PushType(f64Type);
                        break;
                    case WOp.F32_DEMOTE_F64:
                        il.Emit(IlOp.Conv_R4);
                        ctx.PopType();
                        ctx.PushType(f32Type);
                        break;

                    case WOp.I32_TRUNC_F32_S:
                    case WOp.I32_TRUNC_F32_U:
                        il.Emit(IlOp.Conv_I4);
                        ctx.PopType();
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_TRUNC_F64_U:
                        il.Emit(IlOp.Conv_U4);
                        ctx.PopType();
                        ctx.PushType(i32Type);
                        goto case WOp.I32_TRUNC_F64_S;
                    case WOp.I32_TRUNC_F64_S:
                        il.Emit(IlOp.Conv_I4);
                        ctx.PopType();
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I64_TRUNC_F64_U:
                        il.Emit(IlOp.Conv_U8);
                        ctx.PopType();
                        ctx.PushType(i64Type);
                        goto case WOp.I64_TRUNC_F64_S;
                    case WOp.I64_TRUNC_F64_S:
                        il.Emit(IlOp.Conv_I8);
                        ctx.PopType();
                        ctx.PushType(i64Type);
                        break;
                    case WOp.F32_CONVERT_I32_S:
                    case WOp.F32_CONVERT_I32_U:
                    case WOp.F32_CONVERT_I64_S:
                    case WOp.F32_CONVERT_I64_U:
                        if (instr.ToString().EndsWith("_U"))
                            il.Emit(IlOp.Conv_U8);
                        il.Emit(IlOp.Conv_R4);
                        ctx.PopType();
                        ctx.PushType(f32Type);
                        break;
                    case WOp.F64_CONVERT_I32_S:
                    case WOp.F64_CONVERT_I32_U:
                    case WOp.F64_CONVERT_I64_S:
                    case WOp.F64_CONVERT_I64_U:
                        if (instr.ToString().EndsWith("_U"))
                            il.Emit(IlOp.Conv_U8);
                        il.Emit(IlOp.Conv_R8);
                        ctx.PopType();
                        ctx.PushType(f64Type);
                        break;

                    case WOp.F32_ADD:
                    case WOp.F64_ADD:
                    case WOp.I32_ADD:
                    case WOp.I64_ADD:
                        il.Emit(IlOp.Add);
                        ctx.PopType();
                        break;
                    case WOp.F32_SUB:
                    case WOp.F64_SUB:
                    case WOp.I32_SUB:
                    case WOp.I64_SUB:
                        il.Emit(IlOp.Sub);
                        ctx.PopType();
                        break;
                    case WOp.F32_MUL:
                    case WOp.F64_MUL:
                    case WOp.I32_MUL:
                    case WOp.I64_MUL:
                        il.Emit(IlOp.Mul);
                        ctx.PopType();
                        break;
                    case WOp.F32_DIV:
                    case WOp.F64_DIV:
                    case WOp.I32_DIV_S:
                    case WOp.I64_DIV_S:
                        il.Emit(IlOp.Div);
                        ctx.PopType();
                        break;
                    case WOp.I32_DIV_U:
                    case WOp.I64_DIV_U:
                        il.Emit(IlOp.Div_Un);
                        ctx.PopType();
                        break;
                    case WOp.I32_REM_S:
                    case WOp.I64_REM_S:
                        il.Emit(IlOp.Rem);
                        ctx.PopType();
                        break;
                    case WOp.I32_REM_U:
                    case WOp.I64_REM_U:
                        il.Emit(IlOp.Rem_Un);
                        ctx.PopType();
                        break;

                    case WOp.I32_LT_U:
                    case WOp.I64_LT_U:
                        if (reader.PeekInstruction() == WOp.I32_EQZ || reader.PeekInstruction() == WOp.I64_EQZ)
                        {
                            reader.ReadInstruction();
                            goto case WOp.I32_GE_U;
                        }

                        // optimize by peeking if the next instruction is BR_IF.
                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                            // Fuse comparison + br_if into a single conditional branch
                            instr = reader.ReadInstruction();
                            ctx.PopType();
                            jmpInstr = IlOp.Blt_Un;
                            goto case WOp.BR_IF;
                        }

                        il.Emit(IlOp.Clt_Un);
                        ctx.PopType(2);
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_LT_S:
                    case WOp.I64_LT_S:
                    case WOp.F64_LT:
                    case WOp.F32_LT:
                        if (reader.PeekInstruction() == WOp.I32_EQZ || reader.PeekInstruction() == WOp.I64_EQZ)
                        {
                            reader.ReadInstruction();
                            goto case WOp.I32_GE_S;
                        }

                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                            instr = reader.ReadInstruction();
                            ctx.PopType();
                            jmpInstr = IlOp.Blt;
                            goto case WOp.BR_IF;
                        }

                        il.Emit(IlOp.Clt);
                        ctx.PopType(2);
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_GT_U:
                    case WOp.I64_GT_U:
                        if (reader.PeekInstruction() == WOp.I32_EQZ || reader.PeekInstruction() == WOp.I64_EQZ)
                        {
                            reader.ReadInstruction();
                            goto case WOp.I32_LE_U;
                        }

                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                            instr = reader.ReadInstruction();
                            ctx.PopType();
                            jmpInstr = IlOp.Bgt_Un;
                            goto case WOp.BR_IF;
                        }

                        il.Emit(IlOp.Cgt_Un);
                        ctx.PopType(2);
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_GT_S:
                    case WOp.I64_GT_S:
                    case WOp.F64_GT:
                    case WOp.F32_GT:

                        if (reader.PeekInstruction() == WOp.I32_EQZ || reader.PeekInstruction() == WOp.I64_EQZ)
                        {
                            reader.ReadInstruction();
                            goto case WOp.I32_LE_S;
                        }

                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                            instr = reader.ReadInstruction();
                            ctx.PopType();
                            jmpInstr = IlOp.Bgt;
                            goto case WOp.BR_IF;
                        }

                        il.Emit(IlOp.Cgt);
                        ctx.PopType(2);
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_GE_S:
                    case WOp.I32_GE_U:
                    case WOp.I64_GE_S:
                    case WOp.I64_GE_U:
                    case WOp.F64_GE:
                    case WOp.F32_GE:
                    case WOp.I32_LE_S:
                    case WOp.I32_LE_U:
                    case WOp.I64_LE_S:
                    case WOp.I64_LE_U:
                    case WOp.F64_LE:
                    case WOp.F32_LE:

                        // invert the logic
                        var unsigned = instr.ToString().Contains("_U");
                        var le = instr.ToString().Contains("LE");
                        bool invert = false;
                        if (reader.PeekInstruction() == WOp.I32_EQZ || reader.PeekInstruction() == WOp.I64_EQZ)
                        {
                            reader.ReadInstruction();
                            if (le)
                            {
                                if (unsigned)
                                    goto case WOp.I32_GT_U;
                                goto case WOp.I32_GT_S;
                            }

                            if (unsigned)
                                goto case WOp.I32_LT_U;

                            goto case WOp.I32_LT_S;
                        }

                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                            instr = reader.ReadInstruction();
                            ctx.PopType();
                            jmpInstr = unsigned ? le ? IlOp.Ble_Un : IlOp.Bge_Un :
                                le ? IlOp.Ble : IlOp.Bge;
                            goto case WOp.BR_IF;
                        }

                        OpCode cmp = le
                            ? unsigned ? IlOp.Cgt_Un : IlOp.Cgt
                            : unsigned
                                ? IlOp.Clt_Un
                                : IlOp.Clt;

                        il.Emit(cmp);
                        il.Emit(IlOp.Ldc_I4_0);
                        il.Emit(IlOp.Ceq);
                        ctx.PopType(2);
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_EQ:
                    case WOp.I64_EQ:
                    case WOp.F64_EQ:
                    case WOp.F32_EQ:
                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                            instr = reader.ReadInstruction();
                            ctx.PopType();
                            jmpInstr = IlOp.Beq;
                            goto case WOp.BR_IF;
                        }

                        il.Emit(IlOp.Ceq);
                        ctx.PopType(2);
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_NE:
                    case WOp.I64_NE:
                    case WOp.F64_NE:
                    case WOp.F32_NE:
                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                            instr = reader.ReadInstruction();
                            ctx.PopType();
                            jmpInstr = IlOp.Bne_Un;

                            goto case WOp.BR_IF;
                        }

                        il.Emit(IlOp.Ceq);
                        il.Emit(IlOp.Ldc_I4_0);
                        il.Emit(IlOp.Ceq);
                        ctx.PopType(2);
                        ctx.PushType(i32Type);
                        break;
                    case WOp.F32_NEG:
                    case WOp.F64_NEG:
                        il.Emit(IlOp.Neg);
                        break;
                    case WOp.I32_EQZ:
                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                            instr = reader.ReadInstruction();
                            jmpInstr = IlOp.Brfalse;
                            goto case WOp.BR_IF;
                        }

                        il.Emit(IlOp.Ldc_I4_0);
                        il.Emit(IlOp.Ceq);
                        ctx.PopType();
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I64_EQZ:
                        if (reader.PeekInstruction() == WOp.BR_IF)
                        {
                        }

                        il.Emit(IlOp.Ldc_I8, 0L);
                        il.Emit(IlOp.Ceq);
                        ctx.PopType();
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_AND:
                    case WOp.I64_AND:
                        il.Emit(IlOp.And);
                        ctx.PopType();
                        break;
                    case WOp.I32_OR:
                    case WOp.I64_OR:
                        il.Emit(IlOp.Or);
                        ctx.PopType();
                        break;
                    case WOp.I32_XOR:
                    case WOp.I64_XOR:
                        il.Emit(IlOp.Xor);
                        ctx.PopType();
                        break;
                    case WOp.I32_SHL:
                    case WOp.I64_SHL:
                        il.Emit(IlOp.Shl);
                        ctx.PopType();
                        break;
                    case WOp.I32_SHR_S:
                    case WOp.I64_SHR_S:
                        il.Emit(IlOp.Shr);
                        ctx.PopType();
                        break;
                    case WOp.I32_SHR_U:
                    case WOp.I64_SHR_U:
                        il.Emit(IlOp.Shr_Un);
                        ctx.PopType();
                        break;
                    case WOp.UNREACHABLE:
                        il.Emit(IlOp.Ldstr, "Unreachable code");
                        il.Emit(IlOp.Newobj, ResolveTypeConstructor(typeof(Exception), typeof(string)));
                        il.Emit(IlOp.Throw);
                        break;

                    case WOp.RETURN:
                        il.Emit(IlOp.Ret);
                        break;
                    case WOp.DROP:
                        il.Emit(IlOp.Pop);
                        ctx.PopType();
                        break;
                    case WOp.END:
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
                            if (il.Body.Instructions.LastOrDefault()?.OpCode != IlOp.Ret)
                                il.Emit(IlOp.Ret);
                            goto next;
                        }

                        break;
                    case WOp.I32_EXTEND8_S:
                        il.Emit(IlOp.Conv_I1);
                        ctx.PopType();
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I32_EXTEND16_S:
                        il.Emit(IlOp.Conv_I2);
                        ctx.PopType();
                        ctx.PushType(i32Type);
                        break;
                    case WOp.I64_EXTEND8_S:
                        il.Emit(IlOp.Conv_I1);
                        il.Emit(IlOp.Conv_I8);
                        ctx.PopType();
                        ctx.PushType(i64Type);
                        break;
                    case WOp.I64_EXTEND16_S:
                        il.Emit(IlOp.Conv_I2);
                        il.Emit(IlOp.Conv_I8);
                        ctx.PopType();
                        ctx.PushType(i64Type);
                        break;
                    case WOp.I64_EXTEND32_S:
                        il.Emit(IlOp.Conv_I4);
                        il.Emit(IlOp.Conv_I8);
                        ctx.PopType();
                        ctx.PushType(i64Type);
                        break;
                    case WOp.EXTENDED_INSTRUCTION:
                        var einstr = (ExtendedInstruction) reader.ReadU32Leb();
                        ctx.EmitCallForOpcode(einstr);
                        break;

                    case WOp.VECTOR_INSTRUCTION:
                        var instr2 = (VectorInstructions) reader.ReadU32Leb();
                        if (instr2.ToString().Contains("RELAXED"))
                        {
                        }

                        switch (instr2)
                        {
                            case VectorInstructions.V128_STORE:
                            case VectorInstructions.V128_LOAD:
                            {
                                if (instr2 == VectorInstructions.V128_STORE)
                                {
                                    var stvar = ctx.GetHelperVariable(v128Type);
                                    il.Emit(IlOp.Stloc, stvar);
                                    ctx.PopType();
                                }

                                reader.ReadU32Leb(); // align hint (ignored)
                                var offset3 = reader.ReadU32Leb();

                                if (offset3 != 0)
                                {
                                    il.Emit(IlOp.Ldc_I4, (int) offset3);
                                    il.Emit(IlOp.Add);
                                }

                                ctx.LoadMemory();
                                il.Emit(IlOp.Add);

                                var stvar2 = ctx.GetHelperVariable(v128Type);
                                if (instr2 == VectorInstructions.V128_STORE)
                                {
                                    il.Emit(IlOp.Ldloc, stvar2);
                                    il.Emit(IlOp.Stobj, v128Type);
                                }
                                else
                                {
                                    il.Emit(IlOp.Ldobj, v128Type);
                                    ctx.PushType(v128Type);
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
                                        il.Emit(IlOp.Ldc_I8, elem);
                                    }

                                    il.EmitCall(() => Lib.v128_create);
                                }

                                LoadV128ConstCode();
                                ctx.PushType(v128Type);
                            }

                                break;

                            case VectorInstructions.I8X16_SHUFFLE:

                                void LoadV128ShuffleCode()
                                {
                                    Span<byte> buffer = stackalloc byte[16];
                                    reader.Read(buffer);
                                    foreach (var elem in MemoryMarshal.Cast<byte, long>(buffer))
                                    {
                                        il.Emit(IlOp.Ldc_I8, elem);
                                    }

                                    il.EmitCall(() => Lib.v128_create);
                                    il.EmitCall(() => Lib.v128_shuffle_vectors);
                                    ctx.PopType();
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
                                        ctx.PushType(i32Type);
                                        break;
                                    case VectorInstructions.I8X16_EXTRACT_LANE_U:
                                        il.EmitCall(() => Lib.i8x16_extract_lane_u);
                                        ctx.PushType(byteType);
                                        break;
                                    case VectorInstructions.I16X8_EXTRACT_LANE_S:
                                        il.EmitCall(() => Lib.i16x8_extract_lane_s);
                                        ctx.PushType(i16Type);
                                        break;
                                    case VectorInstructions.I16X8_EXTRACT_LANE_U:
                                        il.EmitCall(() => Lib.i16x8_extract_lane_u);
                                        ctx.PushType(i16Type);
                                        break;
                                    case VectorInstructions.I32X4_EXTRACT_LANE:
                                        il.EmitCall(() => Lib.i32x4_extract_lane);
                                        ctx.PushType(i32Type);
                                        break;
                                    case VectorInstructions.I64X2_EXTRACT_LANE:
                                        il.EmitCall(() => Lib.i64x2_extract_lane);
                                        ctx.PushType(i64Type);
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
                                ctx.PopType();
                                reader.ReadU32Leb(); // align hint (ignored)
                                var offset3 = reader.ReadU32Leb();
                                ctx.LoadMemory();
                                il.Emit(IlOp.Add);
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

                                ctx.PushType(v128Type);
                                break;
                            }

                            case VectorInstructions.I8X16_ADD_SAT_S:
                            case VectorInstructions.I8X16_ADD_SAT_U:
                                throw new NotImplementedException("I8X16_ADD_SAT not implemented");
                            case VectorInstructions.I8X16_SUB_SAT_S:
                            case VectorInstructions.I8X16_SUB_SAT_U:
                                throw new NotImplementedException("I8X16_SUB_SAT not implemented");

                            case VectorInstructions.V128_LOAD8_SPLAT:
                            case VectorInstructions.V128_LOAD16_SPLAT:
                            case VectorInstructions.V128_LOAD32_SPLAT:
                            case VectorInstructions.V128_LOAD64_SPLAT:
                            {
                                ctx.PopType();
                                reader.ReadU32Leb(); // align hint (ignored)
                                var offset3 = reader.ReadU32Leb();
                                ctx.LoadMemory();
                                il.Emit(IlOp.Add);
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

                                ctx.PushType(v128Type);
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
                                var stvar = ctx.GetHelperVariable(v128Type);
                                il.Emit(IlOp.Stloc, stvar);
                                ctx.PopType();
                                reader.ReadU32Leb(); // align hint (ignored)
                                var offset3 = reader.ReadU32Leb();
                                var lane = reader.ReadU8();
                                ctx.LoadMemory();
                                il.Emit(IlOp.Add);
                                if (offset3 != 0)
                                {
                                    il.Emit(OpCodes.Ldc_I4, (int) offset3);
                                    il.Emit(OpCodes.Add);
                                }

                                il.Emit(IlOp.Ldc_I4, (int) lane);
                                il.Emit(IlOp.Ldloc, stvar);

                                switch (instr2)
                                {
                                    case VectorInstructions.V128_LOAD8_LANE:
                                        il.EmitCall(() => Lib.v128_load8_lane);
                                        ctx.PushType(byteType);
                                        break;
                                    case VectorInstructions.V128_LOAD16_LANE:
                                        il.EmitCall(() => Lib.v128_load16_lane);
                                        ctx.PushType(i16Type);
                                        break;
                                    case VectorInstructions.V128_LOAD32_LANE:
                                        il.EmitCall(() => Lib.v128_load32_lane);
                                        ctx.PushType(i32Type);
                                        break;
                                    case VectorInstructions.V128_LOAD64_LANE:
                                        il.EmitCall(() => Lib.v128_load64_lane);
                                        ctx.PushType(i64Type);
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
                                ctx.EmitCallForOpcode(instr2);
                                break;
                        }

                        break;

                    default:
                        ctx.EmitCallForOpcode(instr);
                        break;
                }
            }

            if (labelStack.Count > 0)
            {
                Assert.IsTrue(labelStack.Count == 1);
                if (il.Body.Instructions.Last().OpCode != IlOp.Ret)
                    il.Emit(IlOp.Ret);
            }

            next: ;
        }
    }

    readonly Dictionary<MethodReference, MethodReference> wrappedMethods = new();


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
                    if (exportFunc.TryGetValue(index, out var value))
                    {
                        Log.WriteLine("Export already defined: {0} {1} - {2}", index, name,
                            value.Name);
                    }
                    else
                        exportFunc[index] = new ImportFunc {Name = name, Index = index};

                    break;
                case ImportType.TABLE:
                    index = reader.ReadU32Leb();
                    exportTables[index] = new ExportTable() {Name = name, Index = index};
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

    // WASM valtype encoding
    TypeReference ByteToTypeReference(byte b) => b switch
    {
        0x7F => def.MainModule.TypeSystem.Int32, // i32
        0x7E => def.MainModule.TypeSystem.Int64, // i64
        0x7D => def.MainModule.TypeSystem.Single, // f32
        0x7C => def.MainModule.TypeSystem.Double, // f64
        0x7B => v128Type, // v128
        _ => throw new Exception($"Invalid valtype 0x{b:X2}")
    };

    void ReadGlobalSection(BinReader reader)
    {
        uint globalCount = reader.ReadU32Leb();
        for (uint i = 0; i < globalCount; i++)
        {
            var valType = reader.ReadU8();
            var mut = reader.ReadU8();
            var instr = reader.ReadInstruction();
            var glob = new Global();
            glob.Const = mut == 0;
            glob.Type = valType;

            switch (instr)
            {
                case WOp.I32_CONST:
                    glob.Value = (int) reader.ReadI64Leb();
                    break;
                case WOp.I64_CONST:
                    glob.Value = reader.ReadI64Leb();
                    break;
                case WOp.F64_CONST:
                    glob.Value = reader.ReadF64();
                    break;
                case WOp.F32_CONST:
                    glob.Value = reader.ReadF32();
                    break;
                default:
                    throw new Exception("Unsupported constant " + instr);
            }

            var end = reader.ReadInstruction();
            Assert.AreEqual(WOp.END, end);
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
                    il.Emit(IlOp.Ldc_I4, i4);
                    break;
                case long i8:
                    il.Emit(IlOp.Ldc_I8, i8);
                    break;
                case float r4:
                    il.Emit(IlOp.Ldc_R4, r4);
                    break;
                case double r8:
                    il.Emit(IlOp.Ldc_R8, r8);
                    break;
                default: throw new Exception("Unsupported type");
            }

            il.Emit(IlOp.Stsfld, fld);
        }

        il.Emit(IlOp.Ret);
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
                cctoril.Emit(OpCodes.Ldc_I4, (int) (min * PageSize));
                // Allocate 4GB of virtual memory so we'll never have to move the heap pointer.
                cctoril.Emit(OpCodes.Ldc_I8, 200L * 1024L * 1024L);
                cctoril.EmitCall(() => MemoryAllocator.AllocateMemory);
                cctoril.Emit(OpCodes.Stsfld, memoryField);
                cctoril.Emit(OpCodes.Stsfld, memoryFieldSize);
                cctoril.Emit(OpCodes.Ret);
            }
            else if (type == 1)
            {
                var max = reader.ReadU32Leb();

                Log.WriteLine("Memory of {0}-{1} pages ({2} - {3})", min, max, min * PageSize,
                    max * PageSize);
                var cctoril = cls.GetStaticConstructor().Body.GetILProcessor();
                cctoril.Body.Instructions.RemoveAt(cctoril.Body.Instructions.Count - 1);

                // Allocate 4GB of virtual memory so we'll never have to move the heap pointer.
                cctoril.Emit(OpCodes.Ldc_I8, 200L * 1024L * 1024L * 1024L);
                cctoril.EmitCall(() => MemoryAllocator.AllocateMemory);
                cctoril.Emit(OpCodes.Stsfld, memoryField);
                cctoril.Emit(OpCodes.Ldc_I4, (int) (min * PageSize));
                cctoril.Emit(OpCodes.Stsfld, memoryFieldSize);

                cctoril.Emit(OpCodes.Ret);
            }
        }
    }


    void ReadFunctionSection(BinReader reader)
    {
        uint funcCount = reader.ReadU32Leb();
        Log.WriteLine("Func count: {0}", funcCount);
        for (uint i = 0; i < funcCount; i++)
        {
            uint typeid = reader.ReadU32Leb();

            funcDecl[i] = new FuncDeclType
            {
                TypeId = typeid,
                Method = new MethodDefinition("func" + i, MethodAttributes.Static | MethodAttributes.Public,
                    def.MainModule.TypeSystem.Void)
            };
        }
    }

    readonly Parser dwarfparser = new();
    DwarfCompilationUnit cu;
    readonly Dictionary<string, string[]> parameterNames = new();

    void ReadCustomSection(BinReader reader)
    {
        var name = reader.ReadStrN();
        Log.WriteLine(name.Contains((char) 0) ? "Custom section name: ???" : "Custom section name: {0}", name);

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
                        if (idx < importFuncs.Count)
                        {
                            var imp = importFuncs[idx];
                            imp.CustomName = fname;
                        }
                        else
                        {
                            var f = funcDecl[(uint) (idx - importFuncs.Count)];
                            f.ImportName = fname;
                            if (f.IsDefaultName)
                                f.Method.Name = fname;
                        }
                    }
                }

                reader.Position = next;
            }
        }

        if (name == ".debug_abbrev")
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
                        && strTable.TryGetString((uint) subProgramNameId.Value, out var subProgramName))
                    {
                        parameterNames[subProgramName] =
                            thing.Children.Where(die => die.Tag == DwarfTag.DW_TAG_formal_parameter)
                                .Select(param =>
                                    param.Attributes
                                        .FirstOrDefault(attr => attr.Key == AttributeEncoding.DW_AT_name).Value)
                                .Select(attrValue =>
                                    attrValue?.Form == DwarfForm.DW_FORM_strp
                                        ? strTable.GetString((uint) attrValue.Value)
                                        : null)
                                .ToArray();
                    }
                }
            }
        }

        if (name == ".debug_types")
        {
            // Not used yet
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
            types[i] = new TypeId
            {
                ReturnCount = returnCount, ParamCount = paramCount, ParamTypes = paramTypes, ReturnType = returnType
            };
        }
    }
}