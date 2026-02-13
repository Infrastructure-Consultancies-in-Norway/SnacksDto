namespace CLI.Models.Tekla;

public sealed record class TeklaTabPage
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public IReadOnlyList<TeklaAttribute> Attributes { get; init; } = Array.Empty<TeklaAttribute>();
}
