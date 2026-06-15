using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Services;

public class GroupConfigPropertyTests
{
    private readonly MapConfigParsingService _service = new();

    // ========================================================================
    // TomlPropertyMapper: NominationLimit / MapSelectionWeight / ShortGroupName
    // ========================================================================

    [Fact]
    public void ExtractProperties_GroupSpecificProperties_Extracted()
    {
        var doc = TomlTestHelper.LoadToml("TomlPropertyMapper/09_group_specific_properties.toml");
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.Equal(5, props.MapSelectionWeight);
        Assert.Equal("HD", props.ShortGroupName);
        Assert.Equal(3, props.NominationLimit);
    }

    [Fact]
    public void ExtractProperties_NominationLimit_NegativeClamped()
    {
        var doc = TomlTestHelper.ParseToml("[map]\nNominationLimit = -5\n");
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.Equal(0, props.NominationLimit);
    }

    [Fact]
    public void ExtractProperties_NominationLimit_NotSpecified_IsNull()
    {
        var doc = TomlTestHelper.ParseToml("[map]\nMaxExtends = 3\n");
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.Null(props.NominationLimit);
    }

    // ========================================================================
    // NominationLimit: group-level config, per-group limit
    // ========================================================================

    [Fact]
    public void NominationLimit_GroupConfigHasCorrectValue()
    {
        var doc = TomlTestHelper.LoadToml("57_nomination_limit_group.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var hardGroup = result!.MapGroupSettings["HardGroup"]
            .First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal(2, hardGroup.NominationLimit);

        var easyGroup = result.MapGroupSettings["EasyGroup"]
            .First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal(5, easyGroup.NominationLimit);
    }

    [Fact]
    public void NominationLimit_DefaultsToZero_WhenNotSpecified()
    {
        var doc = TomlTestHelper.LoadToml("57_nomination_limit_group.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var unlimitedGroup = result!.MapGroupSettings["UnlimitedGroup"]
            .First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal(0, unlimitedGroup.NominationLimit);
    }

    [Fact]
    public void NominationLimit_MapInheritsGroupLimit()
    {
        var doc = TomlTestHelper.LoadToml("57_nomination_limit_group.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var hardMap = result!.MapConfigsNameMapping["ze_hard_map"].First().MapConfig;
        Assert.Single(hardMap.GroupSettings);
        Assert.Equal(2, hardMap.GroupSettings[0].NominationLimit);
    }

    [Fact]
    public void NominationLimit_MultiGroupMap_EachGroupKeepsOwnLimit()
    {
        var doc = TomlTestHelper.LoadToml("57_nomination_limit_group.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var multiMap = result!.MapConfigsNameMapping["ze_multi_group_map"].First().MapConfig;
        Assert.Equal(2, multiMap.GroupSettings.Count);

        var hard = multiMap.GroupSettings.First(g => g.GroupName == "HardGroup");
        Assert.Equal(2, hard.NominationLimit);

        var easy = multiMap.GroupSettings.First(g => g.GroupName == "EasyGroup");
        Assert.Equal(5, easy.NominationLimit);
    }

    // ========================================================================
    // MapSelectionWeight: default → group → map inheritance
    // ========================================================================

    [Fact]
    public void MapSelectionWeight_GroupOverridesDefault()
    {
        var doc = TomlTestHelper.LoadToml("58_map_selection_weight.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var defaultWeightMap = result!.MapConfigsNameMapping["ze_default_weight"].First().MapConfig;
        Assert.Equal(10u, defaultWeightMap.RandomPickConfig.MapSelectionWeight);
    }

    [Fact]
    public void MapSelectionWeight_MapOverridesGroup()
    {
        var doc = TomlTestHelper.LoadToml("58_map_selection_weight.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var overrideMap = result!.MapConfigsNameMapping["ze_override_weight"].First().MapConfig;
        Assert.Equal(50u, overrideMap.RandomPickConfig.MapSelectionWeight);
    }

    [Fact]
    public void MapSelectionWeight_ZeroClampsToZero()
    {
        var doc = TomlTestHelper.LoadToml("58_map_selection_weight.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var noWeightMap = result!.MapConfigsNameMapping["ze_no_weight"].First().MapConfig;
        Assert.Equal(0u, noWeightMap.RandomPickConfig.MapSelectionWeight);
    }

    // ========================================================================
    // ShortGroupName: group-level config
    // ========================================================================

    [Fact]
    public void ShortGroupName_GroupConfigHasCorrectValue()
    {
        var doc = TomlTestHelper.LoadToml("59_short_group_name.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var hardGroup = result!.MapGroupSettings["HardGroup"]
            .First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal("HD", hardGroup.ShortGroupName);
    }

    [Fact]
    public void ShortGroupName_TruncatedToFourChars()
    {
        var doc = TomlTestHelper.LoadToml("59_short_group_name.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var longGroup = result!.MapGroupSettings["LongNameGroup"]
            .First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal("TOOL", longGroup.ShortGroupName);
    }

    [Fact]
    public void ShortGroupName_DefaultsToEmpty_WhenNotSpecified()
    {
        var doc = TomlTestHelper.LoadToml("59_short_group_name.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var noShortGroup = result!.MapGroupSettings["NoShortName"]
            .First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal("", noShortGroup.ShortGroupName);
    }

    [Fact]
    public void ShortGroupName_MapGroupSettingsHasCorrectShortName()
    {
        var doc = TomlTestHelper.LoadToml("59_short_group_name.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        var hardMap = result!.MapConfigsNameMapping["ze_hard"].First().MapConfig;
        Assert.Single(hardMap.GroupSettings);
        Assert.Equal("HD", hardMap.GroupSettings[0].ShortGroupName);
    }

    // ========================================================================
    // MergeProperties: NominationLimit merges correctly
    // ========================================================================

    [Fact]
    public void MergeProperties_NominationLimit_OverrideTakesPrecedence()
    {
        var baseProps = new ParsedProperties { NominationLimit = 3 };
        var overrideProps = new ParsedProperties { NominationLimit = 7 };

        var merged = MapConfigBuilder.MergeProperties(baseProps, overrideProps);

        Assert.Equal(7, merged.NominationLimit);
    }

    [Fact]
    public void MergeProperties_NominationLimit_NullOverrideFallsBackToBase()
    {
        var baseProps = new ParsedProperties { NominationLimit = 3 };
        var overrideProps = new ParsedProperties { NominationLimit = null };

        var merged = MapConfigBuilder.MergeProperties(baseProps, overrideProps);

        Assert.Equal(3, merged.NominationLimit);
    }

    [Fact]
    public void MergeProperties_MapSelectionWeight_OverrideTakesPrecedence()
    {
        var baseProps = new ParsedProperties { MapSelectionWeight = 1 };
        var overrideProps = new ParsedProperties { MapSelectionWeight = 50 };

        var merged = MapConfigBuilder.MergeProperties(baseProps, overrideProps);

        Assert.Equal(50, merged.MapSelectionWeight);
    }

    [Fact]
    public void MergeProperties_ShortGroupName_OverrideTakesPrecedence()
    {
        var baseProps = new ParsedProperties { ShortGroupName = "AA" };
        var overrideProps = new ParsedProperties { ShortGroupName = "BB" };

        var merged = MapConfigBuilder.MergeProperties(baseProps, overrideProps);

        Assert.Equal("BB", merged.ShortGroupName);
    }
}
