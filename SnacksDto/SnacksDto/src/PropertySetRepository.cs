using System.Linq;
using System.Text.Json;

namespace SnacksDto;

/// <summary>
/// Provides access to snacks.json property set definitions.
/// </summary>
public sealed class PropertySetRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyDictionary<string, PropertySetDto> _byName;

    private PropertySetRepository(IReadOnlyList<PropertySetDto> propertySets)
    {
        PropertySets = propertySets;
        _byName = propertySets
            .Where(set => !string.IsNullOrWhiteSpace(set.Name))
            .ToDictionary(set => set.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>All property sets loaded from snacks.json.</summary>
    public IReadOnlyList<PropertySetDto> PropertySets { get; }

    /// <summary>
    /// Loads the property sets from a JSON file. If <paramref name="path"/> is not supplied, the repository tries
    /// to resolve the bundled SnacksDto\artifacts\snacks.json file.
    /// </summary>
    public static PropertySetRepository FromJsonFile(string? path = null)
    {
        var resolvedPath = path ?? ResolveDefaultJsonPath();
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"snacks.json was not found at '{resolvedPath}'.", resolvedPath);
        }

        using var stream = File.OpenRead(resolvedPath);
        return FromJson(stream);
    }

    /// <summary>
    /// Loads the repository from the supplied JSON stream.
    /// </summary>
    public static PropertySetRepository FromJson(Stream jsonStream)
    {
        var propertySets = JsonSerializer.Deserialize<IReadOnlyList<PropertySetDto>>(jsonStream, SerializerOptions)
                           ?? throw new InvalidOperationException("Could not deserialize snacks.json");

        return new PropertySetRepository(propertySets);
    }

    /// <summary>Attempts to retrieve a property set by name (case-insensitive).</summary>
    public bool TryGet(string? name, out PropertySetDto? propertySet)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            propertySet = null;
            return false;
        }

        var found = _byName.TryGetValue(name, out propertySet);
        return found;
    }

    /// <summary>Gets a property set by name or throws if not present.</summary>
    public PropertySetDto GetRequired(string name)
    {
        if (!TryGet(name, out var propertySet) || propertySet is null)
        {
            throw new KeyNotFoundException($"Property set '{name}' was not found in snacks.json");
        }

        return propertySet;
    }

    /// <summary>
    /// Returns the properties for <paramref name="setName"/> filtered by entity.
    /// </summary>
    public IReadOnlyList<PropertyDto> GetProperties(string setName, string? entityName = null)
    {
        var set = GetRequired(setName);

        IEnumerable<PropertyDto> query = set.Properties;

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(property => property.AppliesToEntity(entityName));
        }

        return query.ToArray();
    }

    /// <summary>
    /// Finds every property set that mentions the supplied entity.
    /// </summary>
    public IReadOnlyList<PropertySetDto> FindByEntity(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return Array.Empty<PropertySetDto>();
        }

        return PropertySets
            .Where(set => set.Properties.Any(prop => prop.AppliesToEntity(entityName)))
            .ToArray();
    }

    private static string ResolveDefaultJsonPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var defaultCandidate = Path.Combine(baseDirectory, "artifacts", "snacks.json");
        if (File.Exists(defaultCandidate))
        {
            return defaultCandidate;
        }

        var directory = baseDirectory;
        for (var i = 0; i < 5 && directory is not null; i++)
        {
            var candidate = Path.Combine(directory, "SnacksDto", "artifacts", "snacks.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return defaultCandidate;
    }
}
