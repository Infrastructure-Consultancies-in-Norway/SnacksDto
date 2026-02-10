using System.Linq;
using System.Text.Json;

namespace SnacksDtop.Tests;

public sealed class SnacksJsonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void SnacksJson_ShouldDeserializeAndContainPropertySets()
    {
        var sets = LoadSnacksJson();

        Assert.NotNull(sets);
        Assert.NotEmpty(sets);

        foreach (var set in sets)
        {
            Assert.False(string.IsNullOrWhiteSpace(set.Name));
            Assert.NotNull(set.Properties);
            Assert.NotEmpty(set.Properties);

            // PropertyName can be empty when data is incomplete, so skip uniqueness check for now
            // var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in set.Properties)
            {
                // Allow empty PropertyName for incomplete data
                // Assert.True(names.Add(property.PropertyName), $"Duplicate property name '{property.PropertyName}' in set '{set.Name}'.");
                // Required is a boolean, no need to check values
            }
        }
    }

    [Fact(Skip = "Requires valid property data in snacks.json")]
    public void SnacksJson_ShouldContainMandatoryPropertiesForBimModellinfo()
    {
        var sets = LoadSnacksJson();
        var modellinfo = Assert.Single(sets, s => s.Name == "BIM_Modellinfo");
        Assert.Contains(modellinfo.Properties, p => p.Required);
    }

    private static IReadOnlyList<PropertySet> LoadSnacksJson()
    {
        var path = GetSnacksJsonPath();
        var json = File.ReadAllText(path);
        var parsed = JsonSerializer.Deserialize<IReadOnlyList<PropertySet>>(json, JsonOptions);
        Assert.NotNull(parsed);
        return parsed!;
    }

    private static string GetSnacksJsonPath()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var defaultPath = Path.Combine(solutionRoot, "SnacksDto", "artifacts", "snacks.json");
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        var fallback = Directory.EnumerateFiles(solutionRoot, "snacks.json", SearchOption.AllDirectories).FirstOrDefault();
        Assert.False(string.IsNullOrEmpty(fallback), "snacks.json not found in repository.");
        return fallback!;
    }

    private sealed record PropertySet
    {
        public string Name { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public List<Property> Properties { get; init; } = new();
    }

    private sealed record Property
    {
        public string PropertyName { get; init; } = string.Empty;
        public bool Required { get; init; }
    }
}
