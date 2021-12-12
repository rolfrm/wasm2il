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

        TypeReference f32Type, f64Type, i64Type, i32Type, voidType;

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
            cls = new TypeDefinition(asmName, "Code",
                TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Class |
                TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Public,
                asm.MainModule.TypeSystem.Object);

            asm.MainModule.Types.Add(cls);
            def = asm;
            
            heapField = new FieldDefinition("Heap", FieldAttributes.Static | FieldAttributes.Private,
                asm.MainModule.TypeSystem.Byte.MakeArrayType());
            heapField.IsStatic = true;
            // todo: Figure out how to init based on data.
            cls.Fields.Add(heapField);

            var cctor = new MethodDefinition(".cctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static |
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, asm.MainModule.TypeSystem.Void);
            cls.Methods.Add(cctor);
            var cctoril = cctor.Body.GetILProcessor();
            cctoril.Emit(OpCodes.Nop);
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

            iller.Init(asmName);

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
                    case Section.DATA:
                        iller.ReadDataSection(reader);
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
                            var _offset = (int)reader.ReadI64Leb();
                            offset = _offset;
                            break;
                        case instr.GLOBAL_GET:
                            throw new Exception("Check this!");
                            _offset = (int)reader.ReadI64Leb();
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
                il.Emit(IlInstr.Ldsfld, heapField);
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
            public Instruction? Label;
            public bool Forward;
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

                Dictionary<TypeReference, VariableDefinition> helperVars =
                    new Dictionary<TypeReference, VariableDefinition>();

                VariableDefinition getVariable(TypeReference tr)
                {
                    if (tr == voidType) throw new Exception("void type");
                    if (helperVars.TryGetValue(tr, out var x))
                        return x;
                    var v = new VariableDefinition(tr);
                    m1.Body.Variables.Add(v);
                    helperVars[tr] = v;
                    return v;
                }
                var heapaddr = new VariableDefinition(def.MainModule.TypeSystem.Int32);
                m1.Body.Variables.Add(heapaddr);

                for (uint i2 = 0; i2 < ftype.ParamCount; i2++)
                {
                    var parameter = new ParameterDefinition(ftype.ParamTypes[i2]);
                    parameter.Name = "param" + i2;
                    m1.Parameters.Add(parameter);
                }

                m1.Body.InitLocals = true;
                int codeidx = 0;
                var labelStack = new List<LabelType>();
                labelStack.Add(new LabelType()); // base label

                // to satisfy SELECT.
                Stack<TypeReference> top = new Stack<TypeReference>();

                void push(TypeReference? tr)
                {
                    if (tr == null) throw new Exception("??");
                    if(tr != voidType)
                        top.Push(tr);
                }

                TypeReference pop(int i = 1)
                {
                    while (i > 1)
                    {
                        top.Pop();
                        i--;
                    }
                    return top.Pop();
                }
                
                while (next > reader.Position)
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
                            pop(otherFun.Parameters.Count);
                            push(otherFun.ReturnType);
                            break;
                        case instr.BLOCK:
                            var blockType = reader.ReadU8();
                            var label = il.Create(OpCodes.Nop);
                            var blk = new LabelType() {Type = blockType, Label = label, Forward = true};
                            labelStack.Add(blk);
                            break;
                        case instr.LOOP:
                            blockType = reader.ReadU8();
                            label = il.Create(OpCodes.Nop);
                            il.Append(label);
                            blk = new LabelType() {Type = blockType, Label = label };
                            labelStack.Add(blk);
                            break;
                        //case instr.IF:
                        //    blockType = reader.ReadU8();
                        //    break;
                        case instr.BR:
                        case instr.BR_IF:
                            var brindex = reader.ReadU32Leb();
                            if (instr == instr.BR_IF )
                                il.Emit(OpCodes.Brtrue, labelStack[(int) (labelStack.Count - brindex - 1)].Label);
                            else
                                il.Emit(OpCodes.Br, labelStack[(int) (labelStack.Count - brindex - 1)].Label);
                            break;
                        case instr.BR_TABLE:
                            var cnt = reader.ReadU32Leb();
                            var items = new Instruction[cnt];
                            for (int i2 = 0; i2 < cnt; i2++)
                            {
                                var brindex2 = reader.ReadU32Leb();
                                items[i2] = labelStack[(int) (labelStack.Count - brindex2 - 1)].Label;
                            }
                            var defaultLabelIndex = reader.ReadU32Leb();
                            var defaultLabel = labelStack[(int) (labelStack.Count - defaultLabelIndex - 1)].Label;
                            il.Emit(OpCodes.Switch, items);
                            il.Emit(OpCodes.Br, defaultLabel);
                            pop();
                            break;
                        case instr.SELECT:
                            // we have to keep track of the type on top of the stack.
                            var t = pop(2);
                            var nextLabel = il.Create(IlInstr.Stloc, getVariable(t));
                            var endLabel = il.Create(IlInstr.Nop);
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
                                var = m1.Body.Variables[(int)local_index];
                            }
                            else
                            {
                                param = m1.Parameters[(int)local_index];
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
                            push(i32Type);
                            il.Emit(IlInstr.Ldc_R4, reader.ReadF32());
                            break;
                        case instr.F64_CONST:
                            push(f64Type);
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
                                    il.Emit(IlInstr.Stloc, getVariable(f32Type));
                                else if (instr == instr.F64_STORE)
                                    il.Emit(IlInstr.Stloc, getVariable(f64Type));
                                else
                                    il.Emit(IlInstr.Stloc, getVariable(i64Type));
                                pop();
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
                                    il.Emit(IlInstr.Ldloc, getVariable(i64Type));
                                    il.Emit(IlInstr.Stind_I1);
                                    break;
                                case instr.I32_STORE_16:
                                case instr.I64_STORE_16:
                                    il.Emit(IlInstr.Ldloc, getVariable(i64Type));
                                    il.Emit(IlInstr.Stind_I2);
                                    break;
                                case instr.I32_STORE:
                                case instr.I64_STORE_32:
                                    il.Emit(IlInstr.Ldloc, getVariable(i64Type));
                                    il.Emit(IlInstr.Stind_I4);
                                    break;
                                case instr.I64_STORE:
                                    il.Emit(IlInstr.Ldloc, getVariable(i64Type));
                                    il.Emit(IlInstr.Stind_I8);
                                    break;
                                case instr.F32_STORE:
                                    il.Emit(IlInstr.Ldloc, getVariable(f32Type));
                                    il.Emit(IlInstr.Stind_R4);
                                    break;
                                case instr.F64_STORE:
                                    il.Emit(IlInstr.Ldloc, getVariable(f64Type));
                                    il.Emit(IlInstr.Stind_R8);
                                    break;
                                case instr.I32_LOAD:
                                    il.Emit(IlInstr.Ldind_I4);
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
                            }

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
                        case instr.I32_DIV_U:
                        case instr.I64_DIV_U:
                            il.Emit(IlInstr.Div);
                            pop();
                            break;
                        case instr.I32_LT_S:
                        case instr.I32_LT_U:
                        case instr.I64_LT_S:
                        case instr.I64_LT_U:
                        case instr.F64_LT:
                        case instr.F32_LT:
                            il.Emit(IlInstr.Clt);
                            pop(2);
                            push(i32Type);
                            break;;
                        case instr.I32_GT_S:
                        case instr.I32_GT_U:
                        case instr.I64_GT_S:
                        case instr.I64_GT_U:
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
                            il.Emit(IlInstr.Dup);
                            il.Emit(IlInstr.Ceq);
                            il.Emit(IlInstr.Cgt);
                            il.Emit(IlInstr.Or);
                            pop(2);
                            push(i32Type);
                            break;  
                        case instr.I32_LE_S:
                        case instr.I32_LE_U:
                        case instr.I64_LE_S:
                        case instr.I64_LE_U:
                        case instr.F64_LE:
                        case instr.F32_LE:
                            il.Emit(IlInstr.Dup);
                            il.Emit(IlInstr.Ceq);
                            il.Emit(IlInstr.Clt);
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
                            il.Emit(IlInstr.Not);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_EQZ:
                            il.Emit(IlInstr.Ldc_I4, 0);
                            il.Emit(IlInstr.Ceq);
                            pop(2);
                            push(i32Type);
                            break;  
                        case instr.I64_EQZ:
                            il.Emit(IlInstr.Ldc_I8, 0);
                            il.Emit(IlInstr.Ceq);
                            pop(2);
                            push(i32Type);
                            break;
                        case instr.I32_AND:
                        case instr.I64_AND:
                            il.Emit(IlInstr.And);
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
                        case instr.UNREACHABLE:
                            il.Emit(IlInstr.Ret);
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
                                if (r.Forward)
                                {
                                    il.Append(r.Label);
                                }else if (r.Label != null) {
                                    il.Emit(OpCodes.Br, (Instruction)r.Label);
                                }
                            }
                            else
                            {
                                labelStack.RemoveAt(0);
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
                    cctoril.Emit(OpCodes.Ldc_I8, (long)min * page_size);
                    cctoril.Emit(OpCodes.Newarr, def.MainModule.TypeSystem.Byte);
                    cctoril.Emit(OpCodes.Stsfld, heapField);
                    cctoril.Emit(OpCodes.Ret);
                }
                else if (type == 1)
                {
                    var max = reader.ReadU32Leb();

                    Console.WriteLine("Memory of {0}-{1} pages ({2} - {3})", min, max, min * page_size,
                        max * page_size);
                    var cctoril = cls.GetStaticConstructor().Body.GetILProcessor();
                    cctoril.Body.Instructions.RemoveAt(cctoril.Body.Instructions.Count - 1);
                    cctoril.Emit(OpCodes.Ldc_I8, (long)max * page_size);
                    cctoril.Emit(OpCodes.Newarr, def.MainModule.TypeSystem.Byte);
                    cctoril.Emit(OpCodes.Stsfld, heapField);
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