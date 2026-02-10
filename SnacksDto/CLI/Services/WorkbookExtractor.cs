using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CLI.Models;

namespace CLI.Services;

internal sealed class WorkbookExtractor
{
    private const int NameColumn = 2;      // B
    private const int DatatypeColumn = 3;  // C
    private const int LevelColumn = 4;     // D
    private const int SampleColumn = 5;    // E
    private const int RecommendedColumn = 6; // F
    private const int AllowedColumn = 7;   // G
    private const int RegexColumn = 8;     // H
    private const int DescriptionColumn = 9; // I
    private const int NotesColumn = 10;       // J
    private const int ColorColumn = 11;    // K

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

            var propertySet = ParseWorksheet(worksheet);
            if (propertySet is not null && propertySet.Properties.Count > 0)
            {
                propertySets.Add(propertySet);
            }
        }

        return propertySets;
    }

    private static PropertySetDto? ParseWorksheet(IXLWorksheet worksheet)
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
        var discipline = ReadCellString(worksheet, 2, NameColumn);

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
            var (code, displayNameProperty) = SplitCodeAndName(propertyNameRaw);

            var datatype = ReadCellString(worksheet, row, DatatypeColumn);
            var levelRaw = ReadCellString(worksheet, row, LevelColumn);
            var sample = ReadCellString(worksheet, row, SampleColumn);
            var recommendedRaw = ReadCellString(worksheet, row, RecommendedColumn);
            var allowedRaw = ReadCellString(worksheet, row, AllowedColumn);
            var regex = ReadCellString(worksheet, row, RegexColumn);
            var description = ReadCellString(worksheet, row, DescriptionColumn);
            var notes = ReadCellString(worksheet, row, NotesColumn);
            var color = ReadCellString(worksheet, row, ColorColumn);

            var allowAny = string.Equals(allowedRaw, "*", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(allowedRaw);

            var property = new PropertyDto
            {
                Code = code,
                DisplayName = displayNameProperty,
                Datatype = datatype,
                ApplicableEntities = SplitLevel(levelRaw),
                SampleValue = sample,
                RecommendedValues = SplitList(recommendedRaw),
                AllowedValues = allowAny ? Array.Empty<string>() : SplitList(allowedRaw),
                AllowedPattern = regex,
                Description = description,
                Notes = notes,
                StatusColor = color,
                Requirement = MapRequirement(color),
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
            Discipline = discipline,
            Scope = scope,
            Properties = properties
        };
    }

    private static string? ReadCellString(IXLWorksheet worksheet, int row, int column)
    {
        var text = worksheet.Cell(row, column).GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static (string Code, string DisplayName) SplitCodeAndName(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return (value, value);
        }

        var parts = trimmed.Split('-', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
            ? (parts[0], parts[1])
            : (trimmed, trimmed);
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

    private static RequirementLevel MapRequirement(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return RequirementLevel.Unknown;
        }

        var normalized = color.Trim();
        return normalized.Equals("Grønn", StringComparison.OrdinalIgnoreCase)
            ? RequirementLevel.Mandatory
            : normalized.Equals("Grå", StringComparison.OrdinalIgnoreCase)
                ? RequirementLevel.Optional
                : RequirementLevel.Unknown;
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
}
