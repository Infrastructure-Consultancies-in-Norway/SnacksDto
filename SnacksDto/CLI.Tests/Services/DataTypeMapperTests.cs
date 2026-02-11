using CLI.Models.Revit;
using CLI.Services;

namespace CLI.Tests.Services;

public class DataTypeMapperTests
{
    [Theory]
    [InlineData("IfcText", RevitDataType.Text)]
    [InlineData("IfcLabel", RevitDataType.Text)]
    [InlineData("IfcBoolean", RevitDataType.YesNo)]
    [InlineData("IfcInteger", RevitDataType.Integer)]
    [InlineData("IfcReal", RevitDataType.Number)]
    [InlineData("IfcNumber", RevitDataType.Number)]
    [InlineData("IfcLengthMeasure", RevitDataType.Length)]
    [InlineData("IfcAreaMeasure", RevitDataType.Area)]
    [InlineData("IfcVolumeMeasure", RevitDataType.Volume)]
    [InlineData("IfcPlaneAngleMeasure", RevitDataType.Angle)]
    [InlineData(null, RevitDataType.Text)]
    [InlineData("", RevitDataType.Text)]
    [InlineData("UnknownType", RevitDataType.Text)]
    public void MapIfcToRevitDataType_MapsCorrectly(string? ifcType, RevitDataType expected)
    {
        var result = DataTypeMapper.MapIfcToRevitDataType(ifcType);
        Assert.Equal(expected, result);
    }
}
