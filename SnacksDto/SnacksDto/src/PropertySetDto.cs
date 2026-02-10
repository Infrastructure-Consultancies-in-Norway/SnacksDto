namespace SnacksDto;

/// <summary>
/// Represents a property set definition comprised of multiple properties.
/// </summary>
public sealed record class PropertySetDto
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Scope { get; init; }
    public IReadOnlyList<PropertyDto> Properties { get; init; } = Array.Empty<PropertyDto>();
}
