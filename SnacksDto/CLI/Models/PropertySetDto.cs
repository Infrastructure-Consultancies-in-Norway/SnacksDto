namespace CLI.Models;

public sealed record class PropertySetDto
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Scope { get; init; }
    public IReadOnlyList<PropertyDto> Properties { get; init; } = Array.Empty<PropertyDto>();
}
