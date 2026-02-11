using System.Text.Json;

namespace CLI.Services;

public sealed class GuidManager
{
    private readonly Dictionary<string, Guid> _guidMappings = new();
    private readonly string _mappingFilePath;

    public GuidManager(string mappingFilePath)
    {
        _mappingFilePath = mappingFilePath;
        LoadGuidMappings();
    }

    private void LoadGuidMappings()
    {
        if (!File.Exists(_mappingFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_mappingFilePath);
            var mappings = JsonSerializer.Deserialize<Dictionary<string, Guid>>(json);
            if (mappings != null)
            {
                foreach (var kvp in mappings)
                {
                    _guidMappings[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load GUID mappings from {_mappingFilePath}: {ex.Message}");
        }
    }

    public Guid GetOrCreateGuid(string parameterName)
    {
        if (_guidMappings.TryGetValue(parameterName, out var existingGuid))
        {
            return existingGuid;
        }

        var newGuid = Guid.NewGuid();
        _guidMappings[parameterName] = newGuid;
        return newGuid;
    }

    public void SaveGuidMappings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_mappingFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_guidMappings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_mappingFilePath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: Could not save GUID mappings to {_mappingFilePath}: {ex.Message}");
        }
    }
}
