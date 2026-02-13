using CLI.Models.Tekla;
using SnacksDto;

namespace CLI.Services;

public sealed class TeklaObjectsGenerator
{
    private static readonly string[] ReinforcementSheets = { "KON_Felles", "KON_FDV", "KON_Armering" };
    
    private static readonly string[] StandardObjectTypes =
    {
        "part", "beam", "column", "beamortho", "twinprofile",
        "contourplate", "foldedplate", "concrete_beam", "concrete_column",
        "pad_footing", "strip_footing", "concrete_panel", "concrete_slab",
        "concrete_item", "item", "pour_object", "surfacing",
        "steelassembly", "precastassembly", "insituassembly"
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
        const int multiColumnThreshold = 40;
        const int column1X = 30;
        const int column2X = 240;
        const int startY = 30;
        const int yIncrement = 30;
        const int fieldWidth = 150;

        var useMultiColumn = propertySet.Properties.Count > multiColumnThreshold;
        var splitIndex = useMultiColumn ? (propertySet.Properties.Count + 1) / 2 : 0; // Split in half, rounding up for column 1
        
        var index = 0;
        foreach (var property in propertySet.Properties)
        {
            var attributeName = SanitizeAttributeName(property.PropertyName);
            var (valueType, fieldFormat) = MapIfcToTeklaType(property.Datatype);
            var isUnique = IsUniqueAttribute(property.PropertyName);

            int? x = null, y = null, width = null;
            
            if (useMultiColumn)
            {
                var isColumn1 = index < splitIndex;
                if (!isColumn1)
                {
                    x = column2X;
                    y = startY + (index - splitIndex) * yIncrement;
                }
            }

            var attribute = new TeklaAttribute
            {
                Name = attributeName,
                Label = property.PropertyName,
                ValueType = valueType,
                FieldFormat = fieldFormat,
                SpecialFlag = "no",
                Description = property.Description,
                IsUnique = isUnique,
                X = x,
                Y = y,
                Width = width
            };

            attributes.Add(attribute);
            index++;
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
            .Replace(".", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace(" - ", "_")
            .Replace(" ", "_")
            .Replace("-", "_");

        // Collapse multiple consecutive underscores into a single underscore
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }

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
            "beam" => "Beam",
            "column" => "Column",
            "beamortho" => "Beam/orthogonal",
            "twinprofile" => "Twin profile",
            "contourplate" => "Contour plate",
            "foldedplate" => "Folded plate",
            "concrete_beam" => "Concrete beam",
            "concrete_column" => "Concrete column",
            "pad_footing" => "Pad footing",
            "strip_footing" => "Strip footing",
            "concrete_panel" => "Panel",
            "concrete_slab" => "Slab",
            "concrete_item" => "Item",
            "item" => "j_Item",
            "pour_object" => "p_Item",
            "surfacing" => "Surfacing",
            "steelassembly" => "jd_SteelAssembly",
            "precastassembly" => "jd_PrecastCastUnit",
            "insituassembly" => "jd_CastInPlaceCastUnit",
            "rebar" => "Reinforcing bar",
            "rebar_group" => "Rebar Group",
            "rebar_mesh" => "Rebar Mesh",
            "rebar_strand" => "Rebar Strand",
            "reinforcement" => "Reinforcement",
            "reinforcing_bar" => "Reinforcing Bar",
            _ => objectType
        };
    }
}
