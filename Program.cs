
using Microsoft.Build.Logging.StructuredLogger;

if (args.Length != 1)
{
    return;
}

var logFilePath = args[0];
if (!File.Exists(logFilePath))
{
    return;
}

var build = Serialization.Read(logFilePath);

var testProjectPaths = build
    .FindChildrenRecursive<ProjectEvaluation>()
    .Where(
        project => project.FindChildrenRecursive<Property>(
            property => property.Name == "IsTestProject" &&
                        bool.TryParse(property.Value, out var value) && value).Any())
    .SelectMany(project =>
    {
        var outputPath = project.FindChildrenRecursive<Property>(property => property.Name == "OutputPath").Single().Value;
        var assemblyName = project.FindChildrenRecursive<Property>(property => property.Name == "AssemblyName").Single().Value;
        var targetFrameworks = project.TargetFramework.Split(';');
        return targetFrameworks.Length > 1
            ? targetFrameworks.Select(tfm => Path.Combine(outputPath, tfm, assemblyName + ".dll"))
            : (new[] { Path.Combine(outputPath, assemblyName + ".dll") });
    })
    .Distinct()
    .ToArray();

Console.WriteLine($"Found {testProjectPaths.Length} test projects");
Console.WriteLine();
Console.WriteLine($"{testProjectPaths.Where(p => p.Contains("net472")).Count()} .NET Framework Tests");
foreach (var testProjectPath in testProjectPaths.Where(p => p.Contains("net472")))
{
    Console.WriteLine("\t" + testProjectPath);
}
Console.WriteLine();
Console.WriteLine($"{testProjectPaths.Where(p => !p.Contains("net472")).Count()} .NET Core Tests");
foreach (var testProjectPath in testProjectPaths.Where(p => !p.Contains("net472")))
{
    Console.WriteLine("\t" + testProjectPath);
}