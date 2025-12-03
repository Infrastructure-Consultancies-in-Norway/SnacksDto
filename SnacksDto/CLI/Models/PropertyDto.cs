namespace CLI.Models;

public sealed record class PropertyDto
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
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
}

public enum RequirementLevel
{
    Unknown = 0,
    Optional,
    Mandatory
}
