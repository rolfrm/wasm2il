using System.Diagnostics;
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
    public static int Main()
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToHashSet();
        var failures = new List<(string testName, Exception exception)>();
        int passed = 0;
        int failed = 0;

        foreach (var type in Assembly.GetCallingAssembly().ExportedTypes)
        {
            if (type.IsAbstract) continue;
            if (type.GetCustomAttribute<TestFixtureAttribute>() != null)
            {
                object instance;
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"====== Fixture {type.Name} ========");
                    Console.WriteLine($"Failed to create instance: {e.InnerException?.Message ?? e.Message}");
                    failures.Add(($"{type.Name} (constructor)", e.InnerException ?? e));
                    failed++;
                    continue;
                }

                foreach (var method in type.GetMethods())
                {
                    if (args.Count > 0 && !(args.Contains(method.Name) || args.Contains(type.Name)))
                    {
                        continue;
                    }
                    
                    if (method.GetCustomAttribute<TestAttribute>() != null)
                    {
                        var testName = $"{type.Name}.{method.Name}";
                        Console.WriteLine($"====== Test {method} ========");
                        try
                        {
                            var sw = Stopwatch.StartNew();
                            method.Invoke(instance, Array.Empty<object>());
                            Console.WriteLine($"======= Pass ({sw.ElapsedMilliseconds} ms) ========");
                            passed++;
                        }
                        catch (TargetInvocationException e)
                        {
                            var inner = e.InnerException ?? e;
                            Console.WriteLine($"Fail: {inner.Message}");
                            failures.Add((testName, inner));
                            failed++;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Fail: {e.Message}");
                            failures.Add((testName, e));
                            failed++;
                        }
                    }
                }
            }
        }

        // Print summary
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine($"Test Results: {passed} passed, {failed} failed");
        Console.WriteLine("========================================");

        if (failures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Failed tests:");
            Console.WriteLine();
            foreach (var (testName, exception) in failures)
            {
                Console.WriteLine($"  FAIL: {testName}");
                Console.WriteLine($"        {exception.Message}");
                Console.WriteLine($"{exception.StackTrace}");
                Console.WriteLine();
            }
            return 1;
        }

        return 0;
    }
}