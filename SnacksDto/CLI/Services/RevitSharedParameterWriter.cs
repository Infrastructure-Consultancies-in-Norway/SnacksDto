using System.Text;
using CLI.Models.Revit;

namespace CLI.Services;

public sealed class RevitSharedParameterWriter
{
    public void Write(RevitSharedParameterFile file, string outputPath)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# This is a Revit shared parameter file.");
        sb.AppendLine("# Do not edit manually.");

        // Meta section
        sb.AppendLine("*META\tVERSION\tMINVERSION");
        sb.AppendLine("META\t2\t1");

        // Groups section
        sb.AppendLine("*GROUP\tID\tNAME");
        foreach (var group in file.Groups)
        {
            sb.AppendLine($"GROUP\t{group.Id}\t{group.Name}");
        }

        // Parameters section
        sb.AppendLine("*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\tHIDEWHENNOVALUE");
        foreach (var param in file.Parameters)
        {
            var dataTypeName = GetRevitDataTypeName(param.DataType);
            var description = SanitizeDescription(param.Description);
            var visible = param.Visible ? "1" : "0";
            var userModifiable = param.UserModifiable ? "1" : "0";
            var hideWhenNoValue = param.HideWhenNoValue ? "1" : "0";

            sb.AppendLine($"PARAM\t{param.Guid:D}\t{param.Name}\t{dataTypeName}\t{param.DataCategory}\t{param.GroupId}\t{visible}\t{description}\t{userModifiable}\t{hideWhenNoValue}");
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static string GetRevitDataTypeName(RevitDataType dataType)
    {
        return dataType switch
        {
            RevitDataType.Text => "TEXT",
            RevitDataType.Integer => "INTEGER",
            RevitDataType.Number => "NUMBER",
            RevitDataType.Length => "LENGTH",
            RevitDataType.Area => "AREA",
            RevitDataType.Volume => "VOLUME",
            RevitDataType.Angle => "ANGLE",
            RevitDataType.Slope => "SLOPE",
            RevitDataType.Currency => "CURRENCY",
            RevitDataType.Url => "URL",
            RevitDataType.Material => "MATERIAL",
            RevitDataType.YesNo => "YESNO",
            RevitDataType.FamilyType => "<Family Type>",
            _ => "TEXT"
        };
    }

    private static string SanitizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // Replace tabs and newlines with spaces, trim
        return description
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
    }
}
