using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

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
            public uint Index;
        }
        class Global
        {
            public bool Const;
            public byte Type;
            public object Value;
            public FieldDefinition Field;
        }

        public class FuncDeclType
        {
            public MethodDefinition Method;
            public uint TypeId;
        }
        const string magicHeader = "\0asm";
        const uint page_size = 64000;
        Dictionary<uint, Global> globals = new Dictionary<uint, Global>();
        Dictionary<uint, ImportFunc> ExportFunc = new Dictionary<uint, ImportFunc>();
        Dictionary<uint, TypeId> Types = new Dictionary<uint, TypeId>();
        // function declaration to function type
        Dictionary<uint, FuncDeclType> FuncDecl = new Dictionary<uint, FuncDeclType>();
        AssemblyDefinition def;
        TypeDefinition cls;
        FieldDefinition heapField;
        // note there are also globals which are added dynamically depending on need.

        void init(string asmName)
        {
            var asmName2 = new AssemblyNameDefinition(asmName, Version.Parse("1.0.0"));
            var asm = AssemblyDefinition.CreateAssembly(asmName2, "Test", ModuleKind.Dll);

            cls = new TypeDefinition(asmName, "Code",
                TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class |
                TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public,
                asm.MainModule.TypeSystem.Object);

            asm.MainModule.Types.Add(cls);
            def = asm;

            heapField = new FieldDefinition("Heap", FieldAttributes.Static | FieldAttributes.Private,
                asm.MainModule.TypeSystem.Byte.MakeArrayType());
            cls.Fields.Add(heapField);

            var cctor = new MethodDefinition(".cctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static |
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, asm.MainModule.TypeSystem.Void);
            cls.Methods.Add(cctor);
            var cctoril = cctor.Body.GetILProcessor();
            cctoril.Emit(OpCodes.Ldc_I4, 1024 * 1024);
            cctoril.Emit(OpCodes.Newarr, asm.MainModule.TypeSystem.Byte);
            cctoril.Emit(OpCodes.Stsfld, heapField);
            cctoril.Emit(OpCodes.Ret);
            def = asm;
        }

        public static void Go(Stream str, string asmName)
        {
            var iller = new Transformer();
            var reader = new BinReader(str);
            var header = reader.ReadStrl(4);
            if (magicHeader != header)
                throw new Exception("invalid header");
            var wasmVersion = new byte[4];
            reader.Read(wasmVersion);
            if (!wasmVersion.SequenceEqual(new byte[] {1, 0, 0, 0}))
                throw new Exception("Unsupported wasm version");
            Console.WriteLine("Wasm Version: {0}", string.Join(" ", wasmVersion));

            iller.init(asmName);

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
                        iller.ReadCustomSection(reader);
                        str.Position = next;
                        break;
                    }
                    case Section.TYPE:
                        iller.ReadTypeSection(reader);
                        break;
                    case Section.FUNCTION:
                        iller.ReadFunctionSection(reader);
                        break;
                    case Section.MEMORY:
                        iller.ReadMemorySection(reader);
                        break;
                    case Section.GLOBAL:
                        iller.ReadGlobalSection(reader);
                        break;
                    case Section.EXPORT:
                        iller.ReadExportSection(reader);
                        break;
                    case Section.CODE:
                        iller.ReadCodeSection(reader);
                        break;
                    default:
                        str.Position = next;
                        break;
                }
                // check that section was properly read.
                Assert.AreEqual(next, str.Position);
            }

            iller.def.Write(asmName + ".dll");
            iller.def.Dispose();
        }

        void ReadCodeSection(BinReader reader)
        {
            uint funcCount = reader.ReadU32Leb();
            for (uint i = 0; i < funcCount; i++)
            {
                var funcId = FuncDecl[i];
                var ftype = Types[funcId.TypeId];
                string name = "Func" + funcId;
                if (ExportFunc.TryGetValue(i, out var exp))
                {
                    name = exp.Name;
                }

                var m1 = funcId.Method;
                m1.ReturnType = ftype.ReturnType;
                m1.Name = name;
                
                cls.Methods.Add(m1);
                m1.Body.InitLocals = true;
                var il = m1.Body.GetILProcessor();
                il.Emit(IlInstr.Nop);

                var codeSize = reader.ReadU32Leb();
                var next = reader.Position;

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

                var heapaddr = new VariableDefinition(def.MainModule.TypeSystem.Int32);
                m1.Body.Variables.Add(heapaddr);
                var i32Vvar = new VariableDefinition(def.MainModule.TypeSystem.Int32);
                m1.Body.Variables.Add(i32Vvar);
                var i64Vvar = new VariableDefinition(def.MainModule.TypeSystem.Int32);
                m1.Body.Variables.Add(i64Vvar);
                var f32var = new VariableDefinition(def.MainModule.TypeSystem.Single);
                m1.Body.Variables.Add(f32var);
                var f64var = new VariableDefinition(def.MainModule.TypeSystem.Double);
                m1.Body.Variables.Add(f64var);

                for (uint i2 = 0; i2 < ftype.ParamCount; i2++)
                {
                    var parameter = new ParameterDefinition(ftype.ParamTypes[i2]);
                    parameter.Name = "param" + i2;
                    m1.Parameters.Add(parameter);
                }

                m1.Body.InitLocals = true;
                int codeidx = 0;
                while (true)
                {
                    var instr = (instr) reader.ReadU8();
                    codeidx++;
                    switch (instr)
                    {
                        case instr.NOP:
                            il.Emit(IlInstr.Nop);
                            break;
                        case instr.CALL:
                            var fcn = reader.ReadU32Leb();
                            var otherFun = FuncDecl[fcn].Method;
                            il.Emit(IlInstr.Call, otherFun);

                            break;
                        case instr.GLOBAL_GET:
                            var offset2 = reader.ReadU32Leb();
                            var glob = globals[offset2];
                            il.Emit(IlInstr.Ldsfld, glob.Field);
                            break;
                        case instr.GLOBAL_SET:
                            offset2 = reader.ReadU32Leb();
                            glob = globals[offset2];
                            il.Emit(IlInstr.Stsfld, glob.Field);
                            break;
                        case instr.LOCAL_SET:
                        case instr.LOCAL_GET:
                        case instr.LOCAL_TEE:
                            uint local_index = reader.ReadU32Leb();
                            bool isArg = true;
                            if (local_index >= ftype.ParamCount)
                            {
                                isArg = false;
                                local_index -= ftype.ParamCount;
                            }

                            switch (instr)
                            {
                                case instr.LOCAL_GET:
                                    il.Emit(isArg ? IlInstr.Ldarg : IlInstr.Ldloc, (int) local_index);
                                    break;
                                case instr.LOCAL_SET:
                                    il.Emit(isArg ? IlInstr.Starg : IlInstr.Stloc, (int) local_index);
                                    break;
                                case instr.LOCAL_TEE:
                                    il.Emit(IlInstr.Dup);
                                    il.Emit(isArg ? IlInstr.Starg : IlInstr.Stloc, (int) local_index);
                                    break;
                            }

                            break;
                        case instr.I32_CONST:
                            il.Emit(IlInstr.Ldc_I4, (int) reader.ReadI64Leb());
                            break;
                        case instr.I64_CONST:
                            il.Emit(IlInstr.Ldc_I8, reader.ReadI64Leb());
                            break;
                        case instr.F32_CONST:
                            il.Emit(IlInstr.Ldc_R4, reader.ReadF32());
                            break;
                        case instr.F64_CONST:
                            il.Emit(IlInstr.Ldc_R8, reader.ReadF64());
                            break;

                        case instr.I32_LOAD:
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

                            if (instr.ToString().Contains("STORE"))
                            {
                                if (instr == instr.F32_STORE)
                                    il.Emit(IlInstr.Stloc, f32var);
                                else if (instr == instr.F64_STORE)
                                    il.Emit(IlInstr.Stloc, f64var);
                                else
                                    il.Emit(IlInstr.Stloc, i64Vvar);
                            }

                            il.Emit(IlInstr.Stloc, heapaddr);

                            il.Emit(IlInstr.Ldsfld, heapField);
                            il.Emit(IlInstr.Ldloc, heapaddr);
                            // adjust according to the offset 
                            il.Emit(IlInstr.Ldc_I8, offset);
                            il.Emit(IlInstr.Add);
                            // get the address of element N (pop the address from the stack)
                            il.Emit(IlInstr.Ldelema, def.MainModule.TypeSystem.Byte);
                            switch (instr)
                            {
                                // pop address, value. store value in address according to size.
                                case instr.I32_STORE_8:
                                case instr.I64_STORE_8:
                                    il.Emit(IlInstr.Ldloc, i64Vvar);
                                    il.Emit(IlInstr.Stind_I1);
                                    break;
                                case instr.I32_STORE_16:
                                case instr.I64_STORE_16:
                                    il.Emit(IlInstr.Ldloc, i64Vvar);
                                    il.Emit(IlInstr.Stind_I2);
                                    break;
                                case instr.I32_STORE:
                                case instr.I64_STORE_32:
                                    il.Emit(IlInstr.Ldloc, i64Vvar);
                                    il.Emit(IlInstr.Stind_I4);
                                    break;
                                case instr.I64_STORE:
                                    il.Emit(IlInstr.Ldloc, i64Vvar);
                                    il.Emit(IlInstr.Stind_I8);
                                    break;
                                case instr.F32_STORE:
                                    il.Emit(IlInstr.Ldloc, f32var);
                                    il.Emit(IlInstr.Stind_R4);
                                    break;
                                case instr.F64_STORE:
                                    il.Emit(IlInstr.Ldloc, f64var);
                                    il.Emit(IlInstr.Stind_R8);
                                    break;
                                case instr.I32_LOAD:
                                    il.Emit(IlInstr.Ldind_I4);
                                    break;
                                case instr.I64_LOAD:
                                    il.Emit(IlInstr.Ldind_I8);
                                    break;
                                case instr.F32_LOAD:
                                    il.Emit(IlInstr.Ldind_R4);
                                    break;
                                case instr.F64_LOAD:
                                    il.Emit(IlInstr.Ldind_R8);
                                    break;
                            }

                            break;
                        case instr.F32_ADD:
                        case instr.F64_ADD:
                        case instr.I32_ADD:
                        case instr.I64_ADD:
                            il.Emit(IlInstr.Add);
                            break;
                        case instr.F32_SUB:
                        case instr.F64_SUB:
                        case instr.I32_SUB:
                        case instr.I64_SUB:
                            il.Emit(IlInstr.Sub);
                            break;
                        case instr.F32_MUL:
                        case instr.F64_MUL:
                        case instr.I32_MUL:
                        case instr.I64_MUL:
                            il.Emit(IlInstr.Mul);
                            break;
                        case instr.F32_DIV:
                        case instr.F64_DIV:
                        case instr.I32_DIV_S:
                        case instr.I64_DIV_S:
                        case instr.I32_DIV_U:
                        case instr.I64_DIV_U:
                            il.Emit(IlInstr.Div);
                            break;
                        case instr.UNREACHABLE:
                            il.Emit(IlInstr.Ret);
                            break;

                        case instr.RETURN:
                            il.Emit(IlInstr.Ret);
                            break;
                        case instr.END:
                            if (codeidx == 1) // for empty methods.
                                il.Emit(IlInstr.Ret);
                            goto next;
                        default:
                            throw new Exception("Unsupported instruction: " + instr);
                    }
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
                        ExportFunc[index] = new ImportFunc {Name = name, Index = index};
                        break;
                    case ImportType.TABLE:
                        throw new Exception("Import table unsupported");
                    case ImportType.MEM:
                        var memIndex = reader.ReadU32Leb();
                        Console.WriteLine("Memory: {0}", memIndex);
                        break;
                    case ImportType.GLOBAL:
                        var idx = reader.ReadU32Leb();
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
                }
                else if (type == 1)
                {
                    var max = reader.ReadU32Leb();

                    Console.WriteLine("Memory of {0}-{1} pages ({2} - {3})", min, max, min * page_size,
                        max * page_size);
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
                FuncDecl[i] = new FuncDeclType
                {
                    TypeId = typeid,
                    Method = new MethodDefinition("func" + i, MethodAttributes.Static | MethodAttributes.Public, def.MainModule.TypeSystem.Void)
                };
            }
        }

        void ReadCustomSection(BinReader reader)
        {
            var name = reader.ReadStrN();
            Console.WriteLine("Custom section name: {0}", name);
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