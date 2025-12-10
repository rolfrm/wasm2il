using Mono.Cecil;

namespace Wasm2IL.Utils
{
    /// <summary>
    /// Handles type mapping between WASM and .NET types, including delegate type creation.
    /// </summary>
    public static class TypeMapper
    {
        /// <summary>
        /// Converts a TypeId to a Func/Action delegate type.
        /// </summary>
        public static Type TypeIdToFuncType(TypeId typeId, TransformerContext context)
        {
            if (typeId.ParamCount == 0)
            {
                if (typeId.ReturnCount == 0) return typeof(Action);
                return typeof(Func<>).MakeGenericType(context.TypeReferenceToType(typeId.ReturnType));
            }

            Type baseType;
            if (typeId.ReturnCount == 0)
            {
                baseType = typeId.ParamCount switch
                {
                    1 => typeof(Action<>),
                    2 => typeof(Action<,>),
                    3 => typeof(Action<,,>),
                    4 => typeof(Action<,,,>),
                    5 => typeof(Action<,,,,>),
                    6 => typeof(Action<,,,,,>),
                    7 => typeof(Action<,,,,,,>),
                    8 => typeof(Action<,,,,,,,>),
                    9 => typeof(Action<,,,,,,,,>),
                    10 => typeof(Action<,,,,,,,,,>),
                    _ => throw new NotSupportedException($"Action with {typeId.ParamCount} parameters not supported")
                };
            }
            else
            {
                baseType = typeId.ParamCount switch
                {
                    0 => typeof(Func<>),
                    1 => typeof(Func<,>),
                    2 => typeof(Func<,,>),
                    3 => typeof(Func<,,,>),
                    4 => typeof(Func<,,,,>),
                    5 => typeof(Func<,,,,,>),
                    6 => typeof(Func<,,,,,,>),
                    7 => typeof(Func<,,,,,,,>),
                    8 => typeof(Func<,,,,,,,,>),
                    9 => typeof(Func<,,,,,,,,,>),
                    10 => typeof(Func<,,,,,,,,,,>),
                    _ => throw new NotSupportedException($"Func with {typeId.ParamCount} parameters not supported")
                };
            }

            if (typeId.ReturnCount == 0)
            {
                return baseType.MakeGenericType(
                    typeId.ParamTypes.Select(context.TypeReferenceToType).ToArray());
            }

            return baseType.MakeGenericType(
                typeId.ParamTypes.Select(context.TypeReferenceToType)
                    .Append(context.TypeReferenceToType(typeId.ReturnType))
                    .ToArray());
        }
    }
}
