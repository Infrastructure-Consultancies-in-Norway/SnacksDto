using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CLI;
using CLI.Services;

try
{
    var options = CliOptions.Parse(args);

    if (options.ShowHelp)
    {
        PrintUsage();
        return 0;
    }

    var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    var workbookPath = ResolveWorkbookPath(solutionRoot, options.WorkbookPath);

    if (!File.Exists(workbookPath))
    {
        Console.Error.WriteLine($"Workbook not found at '{workbookPath}'.");
        return 1;
    }

    var outputPath = ResolveOutputPath(solutionRoot, options.OutputPath);

    var extractor = new WorkbookExtractor();
    var sheetFilters = options.SheetFilters.Count == 0 ? null : options.SheetFilters;
    var propertySets = extractor.Extract(workbookPath, sheetFilters);

    if (propertySets.Count == 0)
    {
        Console.Error.WriteLine("No property sets were extracted.");
        return 2;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    var json = JsonSerializer.Serialize(propertySets, new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    });

    File.WriteAllText(outputPath, json);

    var propertyCount = propertySets.Sum(set => set.Properties.Count);
    Console.WriteLine($"Extracted {propertySets.Count} property sets ({propertyCount} properties) → {outputPath}");
    return 0;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    PrintUsage();
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex.Message}");
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("SnacksDto workbook extractor");
    Console.WriteLine("Usage: dotnet run -- [-w <workbookPath>] [-o <outputPath>] [--sheet <name>]");
    Console.WriteLine("Defaults: workbook=SnacksDto/SnacksDto/data/EgenskapsstrukturV10.xlsx, output=SnacksDto/artifacts/psets.json");
}

static string ResolveWorkbookPath(string solutionRoot, string? requestedPath)
{
    if (!string.IsNullOrWhiteSpace(requestedPath))
    {
        return Path.GetFullPath(requestedPath);
    }

    var candidates = new[]
    {
        Path.Combine(solutionRoot, "SnacksDto", "data", "EgenskapsstrukturV10.xlsx"),
        Path.Combine(solutionRoot, "data", "EgenskapsstrukturV10.xlsx"),
        Path.Combine(solutionRoot, "EgenskapsstrukturV10.xlsx")
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }

    throw new FileNotFoundException("Could not locate EgenskapsstrukturV10.xlsx. Use --workbook to specify the path.");
}

static string ResolveOutputPath(string solutionRoot, string? requestedPath)
{
    var defaultPath = Path.Combine(solutionRoot, "SnacksDto", "artifacts", "snacks.json");
    if (string.IsNullOrWhiteSpace(requestedPath))
    {
        return defaultPath;
    }

    return Path.GetFullPath(requestedPath);
}
