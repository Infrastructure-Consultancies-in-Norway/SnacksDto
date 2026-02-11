using CLI.Models.Tekla;
using SnacksDto;

namespace CLI.Services;

public sealed class TeklaObjectsGenerator
{
    private static readonly string[] ReinforcementSheets = { "KON_Felles", "KON_FDV", "KON_Armering" };
    
    private static readonly string[] StandardObjectTypes =
    {
        "part", "column", "beam", "plate", "bolt", "weld",
        "beam_splice", "plate_splice", "contour_plate", 
        "polybeam", "steel_joint", "assembly"
    };
    
    private static readonly string[] ReinforcementObjectTypes =
    {
        "rebar", "rebar_group", "rebar_mesh", "rebar_strand",
        "reinforcement", "reinforcing_bar"
    };

    public TeklaObjectsFile Generate(PropertySetDto propertySet)
    {
        var isReinforcementSheet = ReinforcementSheets.Contains(propertySet.Name);
        var objectTypes = isReinforcementSheet ? ReinforcementObjectTypes : StandardObjectTypes;
        
        var tabPage = CreateTabPage(propertySet);
        var objectDefinitions = new List<TeklaObjectsDef>();

        foreach (var objectType in objectTypes)
        {
            var objectDef = new TeklaObjectsDef
            {
                ObjectType = objectType,
                ObjectName = GetObjectDisplayName(objectType),
                TabPage = tabPage
            };
            objectDefinitions.Add(objectDef);
        }

        return new TeklaObjectsFile
        {
            SheetName = propertySet.Name,
            ObjectDefinitions = objectDefinitions
        };
    }

    private static TeklaTabPage CreateTabPage(PropertySetDto propertySet)
    {
        var attributes = new List<TeklaAttribute>();

        foreach (var property in propertySet.Properties)
        {
            var attributeName = SanitizeAttributeName(property.PropertyName);
            var (valueType, fieldFormat) = MapIfcToTeklaType(property.Datatype);
            var isUnique = IsUniqueAttribute(property.PropertyName);

            var attribute = new TeklaAttribute
            {
                Name = attributeName,
                Label = property.PropertyName,
                ValueType = valueType,
                FieldFormat = fieldFormat,
                SpecialFlag = "no",
                Description = property.Description,
                IsUnique = isUnique
            };

            attributes.Add(attribute);
        }

        return new TeklaTabPage
        {
            Name = propertySet.Name,
            Label = propertySet.Name,
            Attributes = attributes
        };
    }

    private static string SanitizeAttributeName(string name)
    {
        var sanitized = name
            .Replace(" ", "_")
            .Replace("-", "_")
            .Replace(".", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "");

        if (sanitized.Length > 19)
        {
            sanitized = sanitized.Substring(0, 19);
        }

        return sanitized;
    }

    private static (string valueType, string fieldFormat) MapIfcToTeklaType(string? ifcDatatype)
    {
        if (string.IsNullOrWhiteSpace(ifcDatatype))
        {
            return ("string", "%s");
        }

        var lowerType = ifcDatatype.ToLowerInvariant();

        if (lowerType.Contains("boolean"))
        {
            return ("option", "%s");
        }
        
        if (lowerType.Contains("integer"))
        {
            return ("integer", "%d");
        }
        
        if (lowerType.Contains("real") || lowerType.Contains("number") || 
            lowerType.Contains("length") || lowerType.Contains("area") || 
            lowerType.Contains("volume") || lowerType.Contains("angle"))
        {
            return ("float", "%f");
        }

        return ("string", "%s");
    }

    private static bool IsUniqueAttribute(string propertyName)
    {
        var lowerName = propertyName.ToLowerInvariant();
        return lowerName.Contains("status") || 
               lowerName.Contains("check") || 
               lowerName.Contains("validation") ||
               lowerName.Contains("approved");
    }

    private static string GetObjectDisplayName(string objectType)
    {
        return objectType switch
        {
            "part" => "Part",
            "column" => "Column",
            "beam" => "Beam",
            "plate" => "Plate",
            "bolt" => "Bolt",
            "weld" => "Weld",
            "beam_splice" => "Beam Splice",
            "plate_splice" => "Plate Splice",
            "contour_plate" => "Contour Plate",
            "polybeam" => "Polybeam",
            "steel_joint" => "Steel Joint",
            "assembly" => "Assembly",
            "rebar" => "Rebar",
            "rebar_group" => "Rebar Group",
            "rebar_mesh" => "Rebar Mesh",
            "rebar_strand" => "Rebar Strand",
            "reinforcement" => "Reinforcement",
            "reinforcing_bar" => "Reinforcing Bar",
            _ => objectType
        };
    }
}
