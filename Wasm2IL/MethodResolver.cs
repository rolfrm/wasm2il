using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection;

namespace Wasm2IL
{
    /// <summary>
    /// Resolves method references from function indices.
    /// </summary>
    public class MethodResolver
    {
        private readonly TransformerContext _context;
        private readonly IImportResolver _importResolver;
        private readonly MethodWrapper _methodWrapper;

        public MethodResolver(TransformerContext context,
            IImportResolver importResolver,
            MethodWrapper methodWrapper)
        {
            _context = context;
            _importResolver = importResolver;
            _methodWrapper = methodWrapper;
        }

        /// <summary>
        /// Resolves a method reference from a function index.
        /// </summary>
        public MethodReference ResolveMethod(uint func)
        {
            if (func < _context.ImportFuncs.Count)
            {
                return ResolveImportedFunction(func);
            }

            if (_context.OverrideFuncs != null &&
                _context.OverrideFuncs.TryGetValue(func - (uint)_context.ImportFuncs.Count, out var reference))
            {
                return reference.Method;
            }

            var decl = _context.FuncDecls[func - (uint)_context.ImportFuncs.Count];
            return decl.Method;
        }

        private MethodReference ResolveImportedFunction(uint func)
        {
            var importFun = _context.ImportFuncs[func];
            if (importFun.Method == null)
            {
                var m3 = _importResolver.ResolveImportedMethod(importFun.Module, importFun.Name);

                if (m3 != null)
                {
                    var type2 = _context.Types[(uint)importFun.TypeId];
                    var m2 = m3 is MethodReference mr
                        ? mr
                        : _context.Assembly.MainModule.ImportReference((MethodInfo)m3);

                    m2 = _methodWrapper.MaybeWrap(m2);

                    ValidateMethodSignature(m2, type2, importFun.Name);

                    importFun.Method = m2;
                    return m2;
                }

                var type = _context.Types[(uint)importFun.TypeId];
                var method = CreateStubImportMethod(importFun, type);
                importFun.Method = method;
                _context.MainClass.Methods.Add(method);
            }

            return importFun.Method;
        }

        private void ValidateMethodSignature(MethodReference method, TypeId typeId, string name)
        {
            if (method.ReturnType.FullName == _context.VoidType.FullName && typeId.ReturnCount != 0)
            {
                throw new Exception(
                    $"Type signature of {name} does not match declared type. (return arguments)");
            }

            if (method.ReturnType.FullName != _context.VoidType.FullName && typeId.ReturnCount == 0)
            {
                throw new Exception(
                    $"Type signature of {name} does not match declared type. (return arguments)");
            }

            if (typeId.ParamCount != method.Parameters.Count)
            {
                throw new Exception(
                    $"Type signature of {name} does not match declared type. (parameter count)");
            }
        }

        private MethodDefinition CreateStubImportMethod(ImportFunc importFun, TypeId type)
        {
            var m = new MethodDefinition(importFun.Name.Replace(":", "_"),
                MethodAttributes.Static | MethodAttributes.Public,
                type.ReturnType);
            m.Body.InitLocals = true;

            var il = m.Body.GetILProcessor();
            il.Emit(OpCodes.Ldstr, importFun.Name + " not Implemented");
            il.Emit(OpCodes.Newobj, ResolveTypeConstructor(typeof(Exception), typeof(string)));
            il.Emit(OpCodes.Throw);

            foreach (var param in type.ParamTypes)
            {
                m.Parameters.Add(new ParameterDefinition(param));
            }

            return m;
        }

        private MethodReference ResolveTypeConstructor(Type t, params Type[] argTypes)
        {
            return _context.Assembly.MainModule.ImportReference(
                t.GetConstructors().FirstOrDefault(x =>
                    x.GetParameters().Select(y => y.ParameterType).SequenceEqual(argTypes)));
        }
    }
}
