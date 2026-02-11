using CLI.Models.Revit;

namespace CLI.Services;

public static class DataTypeMapper
{
    public static RevitDataType MapIfcToRevitDataType(string? ifcDatatype)
    {
        if (string.IsNullOrWhiteSpace(ifcDatatype))
        {
            return RevitDataType.Text;
        }

        return ifcDatatype.ToLowerInvariant() switch
        {
            var dt when dt.Contains("text") => RevitDataType.Text,
            var dt when dt.Contains("boolean") => RevitDataType.YesNo,
            var dt when dt.Contains("integer") => RevitDataType.Integer,
            var dt when dt.Contains("real") || dt.Contains("number") => RevitDataType.Number,
            var dt when dt.Contains("length") => RevitDataType.Length,
            var dt when dt.Contains("area") => RevitDataType.Area,
            var dt when dt.Contains("volume") => RevitDataType.Volume,
            var dt when dt.Contains("angle") => RevitDataType.Angle,
            var dt when dt.Contains("slope") => RevitDataType.Slope,
            var dt when dt.Contains("currency") || dt.Contains("monetary") => RevitDataType.Currency,
            var dt when dt.Contains("url") || dt.Contains("uri") => RevitDataType.Url,
            var dt when dt.Contains("material") => RevitDataType.Material,
            _ => RevitDataType.Text
        };
    }
}
