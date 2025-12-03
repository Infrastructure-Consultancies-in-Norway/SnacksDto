using System.Linq;

namespace SnacksDto;

/// <summary>
/// Represents a single property definition extracted from snacks.json.
/// </summary>
public sealed record class PropertyDto
{
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Datatype { get; init; }
    public IReadOnlyList<string> ApplicableEntities { get; init; } = Array.Empty<string>();
    public string? SampleValue { get; init; }
    public IReadOnlyList<string> RecommendedValues { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AllowedValues { get; init; } = Array.Empty<string>();
    public string? AllowedPattern { get; init; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public string? StatusColor { get; init; }
    public RequirementLevel Requirement { get; init; } = RequirementLevel.Unknown;
    public bool AllowAnyValue { get; init; }

    internal bool AppliesToEntity(string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName) || ApplicableEntities.Count == 0)
        {
            return true;
        }

        return ApplicableEntities.Any(entity => string.Equals(entity, entityName, StringComparison.OrdinalIgnoreCase));
    }
}
