using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Services;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.PluginConfig;

public class CooldownScopeConfigParsingTests
{
    private readonly PluginConfigParsingService _service = new();

    [Fact]
    public void ParseConfigFromDocument_CooldownSection_ParsesCorrectly()
    {
        var doc = TomlTestHelper.ParseToml("""
            [Cooldown]
            ScopeMatchMode = "StartsWith"
            ScopePattern = "TokyoAWP"
            """);
        var config = _service.ParseConfigFromDocument(doc);

        Assert.Equal(McsCooldownScopeMatchMode.StartsWith, config.CooldownScopeConfig.ScopeMatchMode);
        Assert.Equal("TokyoAWP", config.CooldownScopeConfig.ScopePattern);
    }

    [Fact]
    public void ParseConfigFromDocument_MissingCooldownSection_ReturnsDefaults()
    {
        var doc = TomlTestHelper.ParseToml("");
        var config = _service.ParseConfigFromDocument(doc);

        Assert.Equal(McsCooldownScopeMatchMode.Exact, config.CooldownScopeConfig.ScopeMatchMode);
        Assert.Equal("", config.CooldownScopeConfig.ScopePattern);
    }

    [Fact]
    public void ParseConfigFromDocument_InvalidScopeMatchMode_FallsBackToExact()
    {
        var doc = TomlTestHelper.ParseToml("""
            [Cooldown]
            ScopeMatchMode = "Partial"
            """);
        var config = _service.ParseConfigFromDocument(doc);

        Assert.Equal(McsCooldownScopeMatchMode.Exact, config.CooldownScopeConfig.ScopeMatchMode);
    }

    [Fact]
    public void ParseConfigFromDocument_ScopeMatchMode_IsCaseInsensitive()
    {
        var doc = TomlTestHelper.ParseToml("""
            [Cooldown]
            ScopeMatchMode = "startswith"
            """);
        var config = _service.ParseConfigFromDocument(doc);

        Assert.Equal(McsCooldownScopeMatchMode.StartsWith, config.CooldownScopeConfig.ScopeMatchMode);
    }
}
