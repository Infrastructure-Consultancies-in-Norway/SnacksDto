namespace CLI.Models.Revit;

public sealed record class RevitParameterGroup
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}
