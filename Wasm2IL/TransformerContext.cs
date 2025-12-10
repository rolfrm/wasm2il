using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Runtime.Intrinsics;
using Wasm2IL.Dwarf;

namespace Wasm2IL
{
    /// <summary>
    /// Holds the shared state and references used during WASM to IL transformation.
    /// </summary>
    public class TransformerContext
    {
        public const uint PageSize = 1 << 16;

        // Assembly and module
        public AssemblyDefinition Assembly { get; set; }
        public TypeDefinition MainClass { get; set; }

        // Type references
        public TypeReference F32Type { get; set; }
        public TypeReference F64Type { get; set; }
        public TypeReference I64Type { get; set; }
        public TypeReference I16Type { get; set; }
        public TypeReference I32Type { get; set; }
        public TypeReference VoidType { get; set; }
        public TypeReference ByteType { get; set; }
        public TypeReference IntPtrType { get; set; }
        public TypeReference VoidPtrType { get; set; }
        public TypeReference V128Type { get; set; }

        // Fields
        public FieldDefinition MemoryField { get; set; }
        public FieldDefinition MemoryFieldSize { get; set; }
        public FieldDefinition FunctionTable { get; set; }

        // State dictionaries
        public Dictionary<uint, Global> Globals { get; } = new();
        public Dictionary<uint, ImportFunc> ExportFuncs { get; } = new();
        public Dictionary<uint, ImportFunc> ImportFuncs { get; } = new();
        public Dictionary<uint, ImportFunc> OverrideFuncs { get; set; }
        public Dictionary<uint, ExportTable> ExportTables { get; } = new();
        public Dictionary<uint, TypeId> Types { get; } = new();
        public Dictionary<uint, FuncDeclType> FuncDecls { get; } = new();
        public Dictionary<MethodReference, MethodReference> WrappedMethods { get; } = new();

        // DWARF debug info
        public Parser DwarfParser { get; } = new();
        public DwarfCompilationUnit CompilationUnit { get; set; }
        public Dictionary<string, string[]> ParameterNames { get; } = new();

        public TransformerContext(AssemblyDefinition assembly, TypeDefinition mainClass)
        {
            Assembly = assembly;
            MainClass = mainClass;

            var module = assembly.MainModule;
            F32Type = module.TypeSystem.Single;
            F64Type = module.TypeSystem.Double;
            I64Type = module.TypeSystem.Int64;
            I32Type = module.TypeSystem.Int32;
            I16Type = module.TypeSystem.Int16;
            VoidType = module.TypeSystem.Void;
            ByteType = module.TypeSystem.Byte;
            IntPtrType = module.TypeSystem.IntPtr;
            VoidPtrType = module.TypeSystem.Void.MakePointerType();
            V128Type = module.ImportReference(typeof(Vector128<byte>));
        }

        /// <summary>
        /// Converts a WASM byte type code to a TypeReference.
        /// </summary>
        public TypeReference ByteToTypeReference(byte b)
        {
            switch (b)
            {
                case 0x7F: return I32Type;
                case 0x7E: return I64Type;
                case 0x7D: return F32Type;
                case 0x7C: return F64Type;
                case 123: return V128Type;
                default:
                    throw new Exception("Invalid type " + b);
            }
        }

        /// <summary>
        /// Converts a TypeReference to a runtime Type for delegate creation.
        /// </summary>
        public Type TypeReferenceToType(TypeReference r)
        {
            if (r == I32Type) return typeof(int);
            if (r == I64Type) return typeof(long);
            if (r == F32Type) return typeof(float);
            if (r == F64Type) return typeof(double);
            return typeof(void);
        }
    }
}
