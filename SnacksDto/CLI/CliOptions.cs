namespace CLI;

internal sealed record class CliOptions(
    string? WorkbookPath,
    string? OutputPath,
    IReadOnlyList<string> SheetFilters,
    bool ShowHelp,
    bool SkipUpdate,
    bool ForceDownload,
    bool GenerateRevit,
    bool GenerateTekla)
{
    public static CliOptions Parse(string[] args)
    {
        string? workbookPath = null;
        string? outputPath = null;
        var sheets = new List<string>();
        var showHelp = false;
        var skipUpdate = false;
        var forceDownload = false;
        var generateRevit = false;
        var generateTekla = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-w":
                case "--workbook":
                    workbookPath = ReadNextValue(args, ref i, args[i]);
                    break;

                case "-o":
                case "--output":
                    outputPath = ReadNextValue(args, ref i, args[i]);
                    break;

                case "-s":
                case "--sheet":
                case "--sheets":
                    var sheetValue = ReadNextValue(args, ref i, args[i]);
                    sheets.AddRange(sheetValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                    break;

                case "-h":
                case "--help":
                    showHelp = true;
                    break;

                case "--skip-update":
                    skipUpdate = true;
                    break;

                case "--force-download":
                    forceDownload = true;
                    break;

                case "--revit":
                case "--generate-revit":
                    generateRevit = true;
                    break;

                case "--tekla":
                case "--generate-tekla":
                    generateTekla = true;
                    break;

                default:
                    throw new ArgumentException($"Unknown argument '{args[i]}'");
            }
        }

        return new CliOptions(
            workbookPath,
            outputPath,
            sheets,
            showHelp,
            skipUpdate,
            forceDownload,
            generateRevit,
            generateTekla);
    }

    private static string ReadNextValue(IReadOnlyList<string> args, ref int index, string currentArg)
    {
        if (index + 1 >= args.Count)
        {
            throw new ArgumentException($"Missing value for {currentArg}");
        }

        index++;
        return args[index];
    }
}
