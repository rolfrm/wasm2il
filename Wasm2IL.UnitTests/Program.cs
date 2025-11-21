using System.Reflection;

namespace Wasm2IL;

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
        var args = Environment.GetCommandLineArgs().Skip(1).ToHashSet();
        
        foreach (var type in Assembly.GetCallingAssembly().ExportedTypes)
        {
            if (type.IsAbstract) continue;
            if (type.GetCustomAttribute<TestFixtureAttribute>() != null)
            {
                var instance = Activator.CreateInstance(type);
                
                foreach (var method in type.GetMethods())
                {
                    if (args.Count > 0 && !(args.Contains(method.Name) || args.Contains(type.Name)))
                    {
                        continue;
                    }
                    if (method.GetCustomAttribute<TestAttribute>() != null)
                    {
                        Console.WriteLine($"====== Test {method} ========");
                        try
                        {
                            method.Invoke(instance, Array.Empty<object>());
                            Console.WriteLine($"======= Pass ========");
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