using ClosedXML.Excel;
using CLI.Models;
using CLI.Services;

namespace SnacksDto.CLI.Tests;

public sealed class WorkbookExtractorTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    [Fact]
    public void Extract_ShouldStopAtLastColoredRow_AndSetRequirementLevels()
    {
        var workbookPath = CreateWorkbook(sheet =>
        {
            PopulateHeaders(sheet);
            SetPropertyRow(sheet, 4, "MOD.01 - Version", "IfcText", "IfcBuilding", "v1", "Svart");
            SetPropertyRow(sheet, 5, "MOD.02 - Optional", "IfcText", "IfcBridge", "demo", "Grå");
            sheet.Cell(6, 1).Value = "NOTE - IGNORE";
            sheet.Cell(6, 9).Value = string.Empty;
        });

        var extractor = new WorkbookExtractor();
        var sets = extractor.Extract(workbookPath, new[] { "Sheet" });

        var set = Assert.Single(sets);
        Assert.Equal("Sheet", set.Name);
        Assert.Equal(2, set.Properties.Count);

        var mandatory = Assert.Single(set.Properties, p => p.PropertyName == "MOD.01 - Version");
        Assert.True(mandatory.Required);

        var optional = Assert.Single(set.Properties, p => p.PropertyName == "MOD.02 - Optional");
        Assert.False(optional.Required);
    }

    private string CreateWorkbook(Action<IXLWorksheet> configure)
    {
        var path = Path.Combine(Path.GetTempPath(), $"SnacksDto_{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet");
        sheet.Cell(1, 1).Value = "Sheet (scope)";
        sheet.Cell(2, 1).Value = "Felles";
        configure(sheet);
        workbook.SaveAs(path);
        _tempFiles.Add(path);
        return path;
    }

    private static void PopulateHeaders(IXLWorksheet sheet)
    {
        var headers = new[]
        {
            "Egenskapsnavn",
            "Datatype",
            "Nivå",
            "Eksempelverdi",
            "Anbefalte verdier",
            "Tillatte verdier",
            "Tillatte verdier (Regex)",
            "Forklaring",
            "Farge"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(3, 1 + i).Value = headers[i];
        }
    }

    private static void SetPropertyRow(
        IXLWorksheet sheet,
        int row,
        string name,
        string datatype,
        string level,
        string sample,
        string color)
    {
        sheet.Cell(row, 1).Value = name;
        sheet.Cell(row, 2).Value = datatype;
        sheet.Cell(row, 3).Value = level;
        sheet.Cell(row, 4).Value = sample;
        sheet.Cell(row, 5).Value = sample;
        sheet.Cell(row, 6).Value = "*";
        sheet.Cell(row, 7).Value = "^.*$";
        sheet.Cell(row, 8).Value = "Description";
        sheet.Cell(row, 9).Value = color;
    }

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }
}
