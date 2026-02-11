using System.Text.Json;
using CLI.Services;

namespace CLI.Tests.Services;

public class GuidManagerTests
{
    [Fact]
    public void GetOrCreateGuid_CreatesNewGuidForNewParameter()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var manager = new GuidManager(tempFile);
            var guid = manager.GetOrCreateGuid("TestParam");
            
            Assert.NotEqual(Guid.Empty, guid);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetOrCreateGuid_ReturnsExistingGuidForSameParameter()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var manager = new GuidManager(tempFile);
            var guid1 = manager.GetOrCreateGuid("TestParam");
            var guid2 = manager.GetOrCreateGuid("TestParam");
            
            Assert.Equal(guid1, guid2);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAndLoadGuidMappings_PersistsGuidsBetweenInstances()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            Guid originalGuid;
            
            // Create and save
            var manager1 = new GuidManager(tempFile);
            originalGuid = manager1.GetOrCreateGuid("TestParam");
            manager1.SaveGuidMappings();
            
            // Load and verify
            var manager2 = new GuidManager(tempFile);
            var loadedGuid = manager2.GetOrCreateGuid("TestParam");
            
            Assert.Equal(originalGuid, loadedGuid);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadGuidMappings_HandlesNonExistentFile()
    {
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        var manager = new GuidManager(nonExistentFile);
        var guid = manager.GetOrCreateGuid("TestParam");
        
        Assert.NotEqual(Guid.Empty, guid);
    }
}
