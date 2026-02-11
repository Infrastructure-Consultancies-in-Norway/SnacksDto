using CLI.Models;
using CLI.Models.Revit;

namespace CLI.Services;

public sealed class RevitSharedParameterGenerator
{
    public RevitSharedParameterFile Generate(IReadOnlyList<PropertySetDto> propertySets, GuidManager guidManager)
    {
        var groups = new List<RevitParameterGroup>();
        var parameters = new List<RevitParameter>();
        var groupId = 1;

        foreach (var propertySet in propertySets)
        {
            var group = new RevitParameterGroup
            {
                Id = groupId,
                Name = propertySet.Name
            };
            groups.Add(group);

            foreach (var property in propertySet.Properties)
            {
                var guid = guidManager.GetOrCreateGuid(property.PropertyName);
                var dataType = DataTypeMapper.MapIfcToRevitDataType(property.Datatype);

                var parameter = new RevitParameter
                {
                    Guid = guid,
                    Name = property.PropertyName,
                    DataType = dataType,
                    DataCategory = string.Empty,
                    GroupId = groupId,
                    Visible = true,
                    Description = property.Description,
                    UserModifiable = true,
                    HideWhenNoValue = false
                };

                parameters.Add(parameter);
            }

            groupId++;
        }

        return new RevitSharedParameterFile
        {
            Groups = groups,
            Parameters = parameters
        };
    }
}
