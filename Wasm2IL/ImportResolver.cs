using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection;

namespace Wasm2IL
{
    /// <summary>
    /// Resolves imported methods from WASM modules.
    /// </summary>
    public class ImportResolver : IImportResolver
    {
        private readonly Dictionary<string, List<Type>> _importModules = new();
        private readonly List<Type> _overrideModules = new();
        private readonly TransformerContext _context;

        public event EventHandler<ResolveImportEventArgs> OnResolveImport;

        public ImportResolver(TransformerContext context)
        {
            _context = context;
        }

        public void LoadImportModule(string moduleName, Type type)
        {
            if (!_importModules.TryGetValue(moduleName, out var typeList))
                _importModules[moduleName] = typeList = [];
            typeList.Add(type);
        }

        public void LoadOverrideModule(Type type)
        {
            _overrideModules.Add(type);
        }

        public object ResolveImportedMethod(string moduleName, string name)
        {
            if (OnResolveImport != null)
            {
                var resolveEventArgs = new ResolveImportEventArgs
                {
                    Name = name,
                    ModuleName = moduleName,
                    TypeBuilder = _context.MainClass,
                    ModuleDefinition = _context.Assembly.MainModule
                };

                OnResolveImport?.Invoke(this, resolveEventArgs);
                if (resolveEventArgs.Handled)
                {
                    return resolveEventArgs.Result;
                }
            }

            if (_importModules.TryGetValue(moduleName, out var t))
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

        /// <summary>
        /// Resolves all unresolved import functions and creates stub implementations.
        /// </summary>
        public void ResolveUnresolvedImports()
        {
            foreach (var kv in _context.ImportFuncs
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
                CreateStubMethod(imp, kv.Key);
            }
        }

        /// <summary>
        /// Sets up override functions from loaded override modules.
        /// </summary>
        public void SetupOverrideFunctions()
        {
            _context.OverrideFuncs = new();
            Dictionary<string, uint> declaredFunctions = new();

            foreach (var item in _context.FuncDecls)
            {
                if (item.Value?.ImportName is { } name)
                {
                    declaredFunctions[name] = item.Key;
                }
            }

            foreach (var type in _overrideModules)
            {
                foreach (var method in type.GetMethods())
                {
                    if (declaredFunctions.TryGetValue(method.Name, out var id))
                    {
                        _context.OverrideFuncs[id] = new ImportFunc
                        {
                            Index = id,
                            CustomName = method.Name,
                            Method = _context.Assembly.MainModule.ImportReference(method),
                            Name = method.Name,
                            Module = "??",
                            TypeId = _context.FuncDecls[id].TypeId
                        };
                    }
                }
            }
        }

        private void CreateStubMethod(ImportFunc imp, uint key)
        {
            var type = _context.Types[(uint)imp.TypeId];
            var m = new MethodDefinition(imp.Name, MethodAttributes.Public | MethodAttributes.Static,
                type.ReturnType);

            foreach (var p in type.ParamTypes)
            {
                m.Parameters.Add(new ParameterDefinition(p));
            }

            m.Body.InitLocals = true;
            var il = m.Body.GetILProcessor();

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldstr, imp.Name + " not Implemented");
            il.Emit(OpCodes.Newobj, ResolveTypeConstructor(typeof(Exception), typeof(string)));
            il.Emit(OpCodes.Throw);

            _context.MainClass.Methods.Add(m);
            imp.Method = m;
            _context.ImportFuncs[key] = imp;
        }

        private MethodReference ResolveTypeConstructor(Type t, params Type[] argTypes)
        {
            return _context.Assembly.MainModule.ImportReference(
                t.GetConstructors().FirstOrDefault(x =>
                    x.GetParameters().Select(y => y.ParameterType).SequenceEqual(argTypes)));
        }
    }
}
