using System.Linq;

namespace SnacksDto;

/// <summary>
/// Represents a single property definition extracted from snacks.json.
/// </summary>
public sealed record class PropertyDto
{
    public string PropertyName { get; init; } = string.Empty;
    public string? Datatype { get; init; }
    public IReadOnlyList<string> Level { get; init; } = Array.Empty<string>();
    public string? SampleValue { get; init; }
    public IReadOnlyList<string> RecommendedValues { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AllowedValues { get; init; } = Array.Empty<string>();
    public string? AllowedPattern { get; init; }
    public string? Description { get; init; }
    public string? RequirementColor { get; init; }
    public bool Required { get; init; }
    public bool AllowAnyValue { get; init; }

    internal bool AppliesToEntity(string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName) || Level.Count == 0)
        {
            return true;
        }

        return Level.Any(entity => string.Equals(entity, entityName, StringComparison.OrdinalIgnoreCase));
    }
}
