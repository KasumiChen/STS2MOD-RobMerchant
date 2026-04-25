using System.Reflection;
using OfflineTestHarness;

var harnessDir = AppContext.BaseDirectory;
var testAssemblyPath = TestAssemblyPathResolver.ResolveFromHarnessDirectory(harnessDir);
if (!File.Exists(testAssemblyPath))
{
    Console.Error.WriteLine($"Test assembly not found: {testAssemblyPath}");
    return 2;
}

var testAssembly = Assembly.LoadFrom(testAssemblyPath);
var factMethods = testAssembly
    .GetTypes()
    .Where(type => type.IsClass && type.IsPublic)
    .SelectMany(
        type => type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes().Any(attribute => attribute.GetType().Name == "FactAttribute"))
            .Select(method => (Type: type, Method: method)))
    .OrderBy(entry => entry.Type.FullName)
    .ThenBy(entry => entry.Method.Name)
    .ToArray();

var failures = new List<string>();

foreach (var factMethod in factMethods)
{
    var instance = Activator.CreateInstance(factMethod.Type);

    try
    {
        var result = factMethod.Method.Invoke(instance, []);
        if (result is Task task)
        {
            await task;
        }

        Console.WriteLine($"PASS {factMethod.Type.Name}.{factMethod.Method.Name}");
    }
    catch (TargetInvocationException ex)
    {
        failures.Add($"{factMethod.Type.Name}.{factMethod.Method.Name}: {ex.InnerException}");
        Console.WriteLine($"FAIL {factMethod.Type.Name}.{factMethod.Method.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{factMethod.Type.Name}.{factMethod.Method.Name}: {ex}");
        Console.WriteLine($"FAIL {factMethod.Type.Name}.{factMethod.Method.Name}");
    }
}

Console.WriteLine($"Executed {factMethods.Length} fact tests.");

if (failures.Count == 0)
{
    return 0;
}

Console.WriteLine("Failures:");
foreach (var failure in failures)
{
    Console.WriteLine(failure);
}

return 1;
