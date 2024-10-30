using System.Reflection;

namespace Wasm2Cil;

public class TestFixtureAttribute : Attribute
{
    
}

public class TestAttribute : Attribute
{
    
}

public class Program
{
    public static void Main()
    {
        foreach (var type in Assembly.GetCallingAssembly().ExportedTypes)
        {
            if (type.IsAbstract) continue;
            if (type.GetCustomAttribute<TestFixtureAttribute>() != null)
            {
                var instance = Activator.CreateInstance(type);
                Console.WriteLine($"TestFixture {type}");
                foreach (var method in type.GetMethods())
                {
                    if (method.GetCustomAttribute<TestAttribute>() != null)
                    {
                        Console.WriteLine($"Test {method}");
                        try
                        {
                            method.Invoke(instance, Array.Empty<object>());
                            Console.WriteLine($"Pass");
                        }
                        catch (TargetInvocationException e)
                        {
                            Console.WriteLine($"Fail: {e.InnerException}");
                            Console.WriteLine($"      {e.InnerException.StackTrace}");
                        }

                    }
                }
            }
        }
    }
}