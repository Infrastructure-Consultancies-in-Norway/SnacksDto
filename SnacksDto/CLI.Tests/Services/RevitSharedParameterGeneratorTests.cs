using CLI.Models;
using CLI.Services;

namespace CLI.Tests.Services;

public class RevitSharedParameterGeneratorTests
{
    [Fact]
    public void Generate_CreatesCorrectNumberOfGroupsAndParameters()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var propertySets = new List<PropertySetDto>
            {
                new()
                {
                    Name = "TestGroup1",
                    Properties = new List<PropertyDto>
                    {
                        new() { PropertyName = "Param1", Datatype = "IfcText" },
                        new() { PropertyName = "Param2", Datatype = "IfcBoolean" }
                    }
                },
                new()
                {
                    Name = "TestGroup2",
                    Properties = new List<PropertyDto>
                    {
                        new() { PropertyName = "Param3", Datatype = "IfcInteger" }
                    }
                }
            };

            var guidManager = new GuidManager(tempFile);
            var generator = new RevitSharedParameterGenerator();
            
            var result = generator.Generate(propertySets, guidManager);
            
            Assert.Equal(2, result.Groups.Count);
            Assert.Equal(3, result.Parameters.Count);
            Assert.Equal("TestGroup1", result.Groups[0].Name);
            Assert.Equal(1, result.Groups[0].Id);
            Assert.Equal("TestGroup2", result.Groups[1].Name);
            Assert.Equal(2, result.Groups[1].Id);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_AssignsCorrectGroupIdsToParameters()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var propertySets = new List<PropertySetDto>
            {
                new()
                {
                    Name = "Group1",
                    Properties = new List<PropertyDto>
                    {
                        new() { PropertyName = "Param1", Datatype = "IfcText" }
                    }
                },
                new()
                {
                    Name = "Group2",
                    Properties = new List<PropertyDto>
                    {
                        new() { PropertyName = "Param2", Datatype = "IfcText" }
                    }
                }
            };

            var guidManager = new GuidManager(tempFile);
            var generator = new RevitSharedParameterGenerator();
            
            var result = generator.Generate(propertySets, guidManager);
            
            Assert.Equal(1, result.Parameters[0].GroupId);
            Assert.Equal(2, result.Parameters[1].GroupId);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_SetsDefaultParameterValues()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var propertySets = new List<PropertySetDto>
            {
                new()
                {
                    Name = "TestGroup",
                    Properties = new List<PropertyDto>
                    {
                        new() 
                        { 
                            PropertyName = "TestParam", 
                            Datatype = "IfcText",
                            Description = "Test description"
                        }
                    }
                }
            };

            var guidManager = new GuidManager(tempFile);
            var generator = new RevitSharedParameterGenerator();
            
            var result = generator.Generate(propertySets, guidManager);
            var param = result.Parameters[0];
            
            Assert.True(param.Visible);
            Assert.True(param.UserModifiable);
            Assert.False(param.HideWhenNoValue);
            Assert.Equal(string.Empty, param.DataCategory);
            Assert.Equal("Test description", param.Description);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
