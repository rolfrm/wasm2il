using System.Reflection;

namespace Wasm2IL.Utils;

public static class Utils
{
    public static MethodInfo GetMethod(this List<Type> types, string name)
    {
        foreach (var t in types)
        {
            if (t.GetMethod(name) is { } m)
                return m;
        }

        return null;
    }

    public static IEnumerable<T> Reversed<T>(this IList<T> col)
    {
        int l = col.Count;
        for (var i = 0; i < l; i++)
            yield return col[l - i - 1];
    }
}