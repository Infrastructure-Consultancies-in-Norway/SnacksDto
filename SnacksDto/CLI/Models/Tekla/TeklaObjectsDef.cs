namespace CLI.Models.Tekla;

public sealed record class TeklaObjectsDef
{
    public required string ObjectType { get; init; }
    public required string ObjectName { get; init; }
    public required TeklaTabPage TabPage { get; init; }
}
