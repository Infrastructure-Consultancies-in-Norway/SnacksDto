using System.Linq;
using System.Text.Encodings.Web;
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

    if (!options.SkipUpdate)
    {
        var downloader = new GitHubFileDownloader();
        await downloader.UpdateIfNeededAsync(workbookPath, options.ForceDownload);
    }

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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });

    File.WriteAllText(outputPath, json);

    var propertyCount = propertySets.Sum(set => set.Properties.Count);
    Console.WriteLine($"Extracted {propertySets.Count} property sets ({propertyCount} properties) → {outputPath}");

    if (options.GenerateRevit)
    {
        var revitOutputPath = Path.Combine(solutionRoot, "SnacksDto", "artifacts", "Revit", "snacksSharedParameters.txt");
        var guidMappingPath = Path.Combine(solutionRoot, "CLI", "Data", "revit-guid-mappings.json");

        var guidManager = new GuidManager(guidMappingPath);
        var generator = new RevitSharedParameterGenerator();
        var revitFile = generator.Generate(propertySets, guidManager);

        var writer = new RevitSharedParameterWriter();
        writer.Write(revitFile, revitOutputPath);
        guidManager.SaveGuidMappings();

        Console.WriteLine($"Generated Revit shared parameter file → {revitOutputPath}");
    }

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
    Console.WriteLine("Usage: dotnet run -- [-w <workbookPath>] [-o <outputPath>] [--sheet <name>] [--skip-update] [--force-download]");
    Console.WriteLine("Defaults: workbook=SnacksDto/SnacksDto/data/Egenskapsstruktur.xlsx, output=SnacksDto/artifacts/psets.json");
    Console.WriteLine("");
    Console.WriteLine("Options:");
    Console.WriteLine("  -w, --workbook          Path to Excel workbook");
    Console.WriteLine("  -o, --output            Path to output JSON file");
    Console.WriteLine("  -s, --sheet             Sheet names to extract (comma-separated)");
    Console.WriteLine("  --skip-update           Skip checking for updates on GitHub");
    Console.WriteLine("  --force-download        Force download from GitHub regardless of version");
    Console.WriteLine("  --revit                 Generate Revit shared parameter file");
    Console.WriteLine("  -h, --help              Show this help message");
}

static string ResolveWorkbookPath(string solutionRoot, string? requestedPath)
{
    if (!string.IsNullOrWhiteSpace(requestedPath))
    {
        return Path.GetFullPath(requestedPath);
    }

    var candidates = new[]
    {
        Path.Combine(solutionRoot, "SnacksDto", "data", "Egenskapsstruktur.xlsx"),
        Path.Combine(solutionRoot, "data", "Egenskapsstruktur.xlsx"),
        Path.Combine(solutionRoot, "Egenskapsstruktur.xlsx"),
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

    return Path.Combine(solutionRoot, "SnacksDto", "data", "Egenskapsstruktur.xlsx");
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
