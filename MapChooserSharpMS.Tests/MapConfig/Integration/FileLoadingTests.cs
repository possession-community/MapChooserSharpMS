using System;
using System.IO;
using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Services;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Integration;

public class FileLoadingTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MapConfigParsingService _service = new();

    public FileLoadingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"mcs_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ParseConfigs_SingleMapsToml_LoadsCorrectly()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3
            MapTime = 20

            [ze_file_test]
            MaxExtends = 5
            """;
        File.WriteAllText(Path.Combine(_tempDir, "maps.toml"), toml);

        var result = _service.ParseConfigs(_tempDir);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_file_test"));
        Assert.Equal(5, result.MapConfigsNameMapping["ze_file_test"].First().MapConfig.MaxExtends);
    }

    [Fact]
    public void ParseConfigs_MultipleTomlFiles_AllLoaded()
    {
        var defaults = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3
            """;
        var maps = """
            [ze_map_a]
            MaxExtends = 5

            [ze_map_b]
            MaxExtends = 7
            """;

        File.WriteAllText(Path.Combine(_tempDir, "defaults.toml"), defaults);
        File.WriteAllText(Path.Combine(_tempDir, "maps_config.toml"), maps);

        var result = _service.ParseConfigs(_tempDir);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_map_a"));
        Assert.True(result.MapConfigsNameMapping.ContainsKey("ze_map_b"));
    }

    [Fact]
    public void ParseConfigs_EmptyDirectory_ReturnsNull()
    {
        var result = _service.ParseConfigs(_tempDir);

        Assert.Null(result);
    }

    [Fact]
    public void ParseConfigs_NonexistentPath_ReturnsNull()
    {
        var result = _service.ParseConfigs(Path.Combine(_tempDir, "nonexistent"));

        Assert.Null(result);
    }

    [Fact]
    public void ParseConfigs_SubdirectoryTomlFiles_LoadedRecursively()
    {
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);

        var defaults = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3
            """;
        var maps = """
            [ze_sub_map]
            MaxExtends = 10
            """;

        File.WriteAllText(Path.Combine(_tempDir, "defaults.toml"), defaults);
        File.WriteAllText(Path.Combine(subDir, "sub_maps.toml"), maps);

        var result = _service.ParseConfigs(_tempDir);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_sub_map"));
        Assert.Equal(10, result.MapConfigsNameMapping["ze_sub_map"].First().MapConfig.MaxExtends);
    }

    [Fact]
    public void ParseConfigs_MapsTomlTakesPriority_OtherFilesIgnored()
    {
        var mapsToml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_maps_toml]
            MaxExtends = 5
            """;
        var otherToml = """
            [ze_other]
            MaxExtends = 10
            """;

        File.WriteAllText(Path.Combine(_tempDir, "maps.toml"), mapsToml);
        File.WriteAllText(Path.Combine(_tempDir, "other.toml"), otherToml);

        var result = _service.ParseConfigs(_tempDir);

        Assert.NotNull(result);
        // Only maps.toml should be loaded
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_maps_toml"));
        Assert.False(result.MapConfigsNameMapping.ContainsKey("ze_other"));
    }
}
