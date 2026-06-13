using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Services;

public class MapSectionValidationTests
{
    private readonly MapConfigParsingService _service = new();

    [Fact]
    public void ParseConfigs_SectionWithUnknownKeys_IsSkippedWithWarning()
    {
        var doc = TomlTestHelper.ParseToml("""
            [MapChooserSharpSettings.Default]
            MapTime = 20

            [General]
            ShouldUseAliasMapNameIfAvailable = true
            VerboseCooldownPrint = true

            [ze_valid_map]
            MapTime = 30
            """);

        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_valid_map"));
        Assert.False(result.MapConfigsNameMapping.ContainsKey("General"));
        Assert.Contains(_service.Warnings, w => w.Contains("[General]") && w.Contains("ShouldUseAliasMapNameIfAvailable"));
    }

    [Fact]
    public void ParseConfigs_SectionWithUnknownSubTable_IsSkippedEntirely()
    {
        var doc = TomlTestHelper.ParseToml("""
            [MapVote]
            MaxVoteElements = 5

            [MapVote.Sound]
            SoundFile = ""

            [ze_valid_map]
            MapTime = 30
            """);

        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_valid_map"));
        Assert.False(result.MapConfigsNameMapping.ContainsKey("MapVote"));
        Assert.Single(_service.Warnings);
    }

    [Fact]
    public void ParseConfigs_EmptySection_IsStillValidMinimalMapConfig()
    {
        var doc = TomlTestHelper.ParseToml("""
            [MapChooserSharpSettings.Default]
            MapTime = 20

            [ze_minimal]
            """);

        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_minimal"));
        Assert.Empty(_service.Warnings);

        var baseConfig = result.MapConfigsNameMapping["ze_minimal"].First().MapConfig;
        Assert.Equal(20, baseConfig.MapTime);
    }

    [Fact]
    public void ParseConfigs_MapWithExtraAndDaySettings_IsNotRejected()
    {
        var doc = TomlTestHelper.ParseToml("""
            [ze_full]
            MapTime = 30
            Cooldown = 5

            [ze_full.extra.shop]
            cost = 100

            [ze_full.DaySettings.Weekend]
            TargetDays = ["saturday", "sunday"]
            MapTime = 60
            """);

        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_full"));
        Assert.Empty(_service.Warnings);
        Assert.Equal(2, result.MapConfigsNameMapping["ze_full"].Count);
    }

    [Fact]
    public void ParseConfigs_RejectedSection_DoesNotLeakDaySettingsOrExtra()
    {
        var doc = TomlTestHelper.ParseToml("""
            [NotAMap]
            UnknownKey = 1

            [NotAMap.extra.shop]
            cost = 100

            [NotAMap.DaySettings.Weekend]
            MapTime = 60
            """);

        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.False(result!.MapConfigsNameMapping.ContainsKey("NotAMap"));
    }
}
