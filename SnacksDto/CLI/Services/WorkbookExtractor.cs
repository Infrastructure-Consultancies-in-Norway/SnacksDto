using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SnacksDto;

namespace CLI.Services;

public sealed class WorkbookExtractor
{
    private const int NameColumn = 1;      // A
    private const int DatatypeColumn = 2;  // B
    private const int LevelColumn = 3;     // C
    private const int SampleColumn = 4;    // D
    private const int RecommendedColumn = 5; // E
    private const int AllowedColumn = 6;   // F
    private const int RegexColumn = 7;     // G
    private const int DescriptionColumn = 8; // H
    private const int ColorColumn = 9;    // I (Farge)

    private static readonly string[] DefaultSheetPrefixes = { "KON", "BIM" };
    private static readonly Regex ScopeRegex = new(@"\(([^)]+)\)");

    public IReadOnlyList<PropertySetDto> Extract(string workbookPath, IReadOnlyCollection<string>? sheetFilters = null)
    {
        if (string.IsNullOrWhiteSpace(workbookPath))
        {
            throw new ArgumentException("Workbook path must be provided", nameof(workbookPath));
        }

        using var workbook = new XLWorkbook(workbookPath);
        
        var explicitFilters = sheetFilters is null || sheetFilters.Count == 0
            ? null
            : new HashSet<string>(sheetFilters.Where(s => !string.IsNullOrWhiteSpace(s)), StringComparer.OrdinalIgnoreCase);

        var propertySets = new List<PropertySetDto>();

        foreach (var worksheet in workbook.Worksheets)
        {
            // If explicit filters provided, use those for exact name matching
            if (explicitFilters is not null)
            {
                if (!explicitFilters.Contains(worksheet.Name))
                {
                    continue;
                }
            }
            // Otherwise, use default prefix filtering for KON and BIM sheets
            else if (!worksheet.Name.StartsWith("KON", StringComparison.Ordinal) && 
                     !worksheet.Name.StartsWith("BIM", StringComparison.Ordinal))
            {
                continue;
            }

            var propertySet = ParseWorksheet(worksheet, workbook);
            if (propertySet is not null && propertySet.Properties.Count > 0)
            {
                propertySets.Add(propertySet);
            }
        }

        return propertySets;
    }

    private PropertySetDto? ParseWorksheet(IXLWorksheet worksheet, XLWorkbook workbook)
    {
        var headerCell = worksheet.CellsUsed(cell => string.Equals(cell.GetString().Trim(), "Egenskapsnavn", StringComparison.OrdinalIgnoreCase))
                                   .OrderBy(cell => cell.Address.RowNumber)
                                   .FirstOrDefault();

        if (headerCell is null)
        {
            return null;
        }

        var headerRow = headerCell.Address.RowNumber;
        var properties = new List<PropertyDto>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var limitRow = DetermineLastPropertyRow(worksheet, headerRow, lastRow);
        var blankRowStreak = 0;

        var titleValue = ReadCellString(worksheet, 1, NameColumn);
        var (displayName, scope) = ParseTitle(titleValue);

        for (var row = headerRow + 1; row <= limitRow && blankRowStreak < 5; row++)
        {
            var propertyNameRaw = ReadCellString(worksheet, row, NameColumn);

            if (string.IsNullOrWhiteSpace(propertyNameRaw))
            {
                if (RowIsEmpty(worksheet, row))
                {
                    blankRowStreak++;
                }
                continue;
            }

            blankRowStreak = 0;

            var datatype = ReadCellString(worksheet, row, DatatypeColumn);
            var levelRaw = ReadCellString(worksheet, row, LevelColumn);
            var sample = ReadCellString(worksheet, row, SampleColumn);
            var recommendedRaw = ReadCellString(worksheet, row, RecommendedColumn);
            var allowedRaw = ReadCellString(worksheet, row, AllowedColumn);
            var regex = ReadCellString(worksheet, row, RegexColumn);
            var description = ReadCellString(worksheet, row, DescriptionColumn);
            var color = ReadCellString(worksheet, row, ColorColumn);

            // Check for hyperlinks in AllowedValues column and resolve them
            var allowedValues = new List<string>();
            var hyperlinkedValues = ResolveHyperlinkValues(worksheet, row, AllowedColumn, workbook);
            if (hyperlinkedValues.Count > 0)
            {
                allowedValues.AddRange(hyperlinkedValues);
            }
            else if (!string.IsNullOrWhiteSpace(allowedRaw) && !string.Equals(allowedRaw, "*", StringComparison.OrdinalIgnoreCase))
            {
                allowedValues.AddRange(SplitList(allowedRaw));
            }

            var allowAny = allowedValues.Count == 0 && (string.Equals(allowedRaw, "*", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(allowedRaw));

            var property = new PropertyDto
            {
                PropertyName = propertyNameRaw,
                Datatype = datatype,
                Level = SplitLevel(levelRaw),
                SampleValue = sample,
                RecommendedValues = SplitList(recommendedRaw),
                AllowedValues = allowedValues,
                AllowedPattern = regex,
                Description = description,
                RequirementColor = color,
                Required = MapRequired(color),
                AllowAnyValue = allowAny
            };

            properties.Add(property);
        }

        if (properties.Count == 0)
        {
            return null;
        }

        return new PropertySetDto
        {
            Name = worksheet.Name,
            DisplayName = displayName,
            Scope = scope,
            Properties = properties
        };
    }

    private static string? ReadCellString(IXLWorksheet worksheet, int row, int column)
    {
        var text = worksheet.Cell(row, column).GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static (string? DisplayName, string? Scope) ParseTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        var trimmed = value.Trim();
        var match = ScopeRegex.Match(trimmed);
        if (!match.Success)
        {
            return (trimmed, null);
        }

        var scope = match.Groups[1].Value.Trim();
        var displayName = trimmed.Remove(match.Index, match.Length).Trim();
        return (string.IsNullOrWhiteSpace(displayName) ? trimmed : displayName, scope);
    }

    private static IReadOnlyList<string> SplitLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        var parts = value.Split(new[] { '/', ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? new[] { value.Trim() } : parts;
    }

    private static IReadOnlyList<string> SplitList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        var parts = value.Split(new[] { ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? new[] { value.Trim() } : parts;
    }

    private static bool MapRequired(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return false;
        }

        var normalized = color.Trim();
        return normalized.Equals("Svart", StringComparison.OrdinalIgnoreCase);
    }

    private static int DetermineLastPropertyRow(IXLWorksheet worksheet, int headerRow, int lastRow)
    {
        for (var row = lastRow; row > headerRow; row--)
        {
            var color = ReadCellString(worksheet, row, ColorColumn);
            var name = ReadCellString(worksheet, row, NameColumn);
            if (!string.IsNullOrWhiteSpace(color) && !string.IsNullOrWhiteSpace(name))
            {
                return row;
            }
        }

        for (var row = lastRow; row > headerRow; row--)
        {
            if (!string.IsNullOrWhiteSpace(ReadCellString(worksheet, row, NameColumn)))
            {
                return row;
            }
        }

        return headerRow;
    }

    private static bool RowIsEmpty(IXLWorksheet worksheet, int row)
    {
        for (var column = NameColumn; column <= ColorColumn; column++)
        {
            if (!string.IsNullOrWhiteSpace(ReadCellString(worksheet, row, column)))
            {
                return false;
            }
        }

        return true;
    }

    private IReadOnlyList<string> ResolveHyperlinkValues(IXLWorksheet worksheet, int row, int column, XLWorkbook workbook)
    {
        try
        {
            var cell = worksheet.Cell(row, column);
            var hyperlink = cell.GetHyperlink();

            if (hyperlink is null)
            {
                return Array.Empty<string>();
            }

            var link = hyperlink.InternalAddress;
            if (string.IsNullOrEmpty(link))
            {
                return Array.Empty<string>();
            }

            // Parse hyperlink format: typically "SheetName!CellRange" or "SheetName!CellAddress"
            // Split sheet name from range: "Oversikt!E24:E65" → ["Oversikt", "E24:E65"]
            var exclamationIndex = link.IndexOf('!');
            if (exclamationIndex <= 0)
            {
                return Array.Empty<string>();
            }

            var sheetName = link.Substring(0, exclamationIndex).Trim('\'');
            var rangeAddress = link.Substring(exclamationIndex + 1);

            var targetSheet = workbook.Worksheet(sheetName);
            if (targetSheet is null)
            {
                return Array.Empty<string>();
            }

            // Parse the range to get starting cell (e.g., "E24" from "E24:E65" or just "E24")
            var rangeStartCell = rangeAddress.Split(':')[0]; // Get "E24" from "E24:E65"

            // Try to determine the actual last row with data
            // For ranges like "E24", find the last row in that column
            var startCell = targetSheet.Cell(rangeStartCell);
            var startRow = startCell.Address.RowNumber;
            var startCol = startCell.Address.ColumnNumber;

            // Find last used row in the sheet
            var lastUsedRow = targetSheet.LastRowUsed()?.RowNumber() ?? startRow;

            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Use HashSet for deduplication

            // Iterate from startRow to lastUsedRow, collecting non-blank, non-header values
            for (var r = startRow; r <= lastUsedRow; r++)
            {
                var cellValue = targetSheet.Cell(r, startCol).GetString();

                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    continue; // Skip blank cells
                }

                var trimmedValue = cellValue.Trim();

                // Skip header-like values (e.g., "Fagkode")
                if (trimmedValue.Equals("Fagkode", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                values.Add(trimmedValue); // HashSet handles deduplication automatically
            }

            return values.ToList();
        }
        catch (Exception)
        {
            // If hyperlink resolution fails for any reason, return empty list
            return Array.Empty<string>();
        }
    }
}
