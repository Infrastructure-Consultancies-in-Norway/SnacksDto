namespace CLI.Models.Revit;

public sealed record class RevitSharedParameterFile
{
    public IReadOnlyList<RevitParameterGroup> Groups { get; init; } = Array.Empty<RevitParameterGroup>();
    public IReadOnlyList<RevitParameter> Parameters { get; init; } = Array.Empty<RevitParameter>();
}
