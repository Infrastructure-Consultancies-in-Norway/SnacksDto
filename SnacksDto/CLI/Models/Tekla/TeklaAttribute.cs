namespace CLI.Models.Tekla;

public sealed record class TeklaAttribute
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public required string ValueType { get; init; }
    public required string FieldFormat { get; init; }
    public required string SpecialFlag { get; init; }
    public string CheckSwitch { get; init; } = "none";
    public string AttributeValueMax { get; init; } = "0.0";
    public string AttributeValueMin { get; init; } = "0.0";
    public string? Description { get; init; }
    public bool IsUnique { get; init; } = false;
    
    // Optional positioning for multi-column layout
    public int? X { get; init; }
    public int? Y { get; init; }
    public int? Width { get; init; }
}
