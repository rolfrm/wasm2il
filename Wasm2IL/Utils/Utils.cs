using System.Reflection;

namespace Wasm2IL.Utils;

public static class Utils
{
    public static MethodInfo? GetMethod(this List<Type> types, string name)
    {
        foreach (var t in types)
        {
            if (t.GetMethod(name) is { } m)
                return m;
        }

        return null;
    }

}