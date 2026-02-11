namespace CLI.Models.Tekla;

public sealed record class TeklaObjectsFile
{
    public required string SheetName { get; init; }
    public IReadOnlyList<TeklaObjectsDef> ObjectDefinitions { get; init; } = Array.Empty<TeklaObjectsDef>();
}
