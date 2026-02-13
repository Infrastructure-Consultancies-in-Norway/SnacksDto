namespace CLI.Models.Revit;

public sealed record class RevitParameter
{
    public required Guid Guid { get; init; }
    public required string Name { get; init; }
    public required RevitDataType DataType { get; init; }
    public string DataCategory { get; init; } = string.Empty;
    public required int GroupId { get; init; }
    public bool Visible { get; init; } = true;
    public string? Description { get; init; }
    public bool UserModifiable { get; init; } = true;
    public bool HideWhenNoValue { get; init; } = false;
}
