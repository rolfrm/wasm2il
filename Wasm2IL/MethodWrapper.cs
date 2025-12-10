using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wasm2IL
{
    /// <summary>
    /// Wraps methods that use pointer types or CString to convert between WASM and .NET representations.
    /// </summary>
    public class MethodWrapper
    {
        private readonly TransformerContext _context;

        public MethodWrapper(TransformerContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Wraps a method if it has pointer or CString parameters.
        /// </summary>
        public MethodReference MaybeWrap(MethodReference fcn)
        {
            if (_context.WrappedMethods.TryGetValue(fcn, out var fcn2))
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

            _context.WrappedMethods[fcn] = fcn2;
            return fcn2;
        }

        private unsafe MethodReference WrapMethod(MethodReference m)
        {
            if (_context.MainClass.Methods.FirstOrDefault(x => x.Name == m.Name + "__wrap") is { } ext)
            {
                return ext;
            }

            var m2 = new MethodDefinition(m.Name + "__wrap",
                MethodAttributes.Static | MethodAttributes.Public,
                m.ReturnType);

            ConstructorInfo methodImplConstructor =
                typeof(MethodImplAttribute).GetConstructor([typeof(MethodImplOptions)]);
            var attr = new CustomAttribute(_context.Assembly.MainModule.ImportReference(methodImplConstructor));
            attr.ConstructorArguments.Add(new CustomAttributeArgument(
                _context.Assembly.MainModule.ImportReference(typeof(MethodImplOptions)),
                MethodImplOptions.AggressiveInlining));
            m2.CustomAttributes.Add(attr);

            _context.MainClass.Methods.Add(m2);
            var il2 = m2.Body.GetILProcessor();
            int argidx = 0;

            foreach (var p in m.Parameters)
            {
                var p2 = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType);
                m2.Parameters.Add(p2);

                if (p.ParameterType.Name == "HeapContext")
                {
                    p2.ParameterType = _context.Assembly.MainModule.ImportReference(typeof(Type));
                    il2.Emit(OpCodes.Ldtoken, _context.MainClass);
                    il2.EmitCall(() => HeapContext.Create);
                    argidx--;
                    m2.Parameters.Remove(p2);
                }
                else if (p.ParameterType.Name == "CString")
                {
                    p2.ParameterType = _context.I32Type;
                    il2.Emit(OpCodes.Ldsfld, _context.MemoryField);
                    il2.Emit(OpCodes.Ldarg, argidx);
                    il2.EmitCall(() => CString.New2);
                }
                else if (p.ParameterType.IsPointer)
                {
                    p2.ParameterType = _context.I32Type;
                    il2.Emit(OpCodes.Ldsfld, _context.MemoryField);
                    il2.Emit(OpCodes.Ldarg, argidx);
                    il2.Emit(OpCodes.Add);
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
                il2.Emit(OpCodes.Ldsfld, _context.MemoryField);
                il2.Emit(OpCodes.Sub);
                il2.Emit(OpCodes.Conv_I4);
                m2.ReturnType = _context.I32Type;
            }

            il2.Emit(OpCodes.Ret);
            return m2;
        }
    }
}
