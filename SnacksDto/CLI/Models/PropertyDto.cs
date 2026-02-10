namespace CLI.Models;

public sealed record class PropertyDto
{
    public required string PropertyName { get; init; }
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
}
