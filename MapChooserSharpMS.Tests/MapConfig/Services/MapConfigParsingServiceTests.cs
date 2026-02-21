using System;
using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Services;

public class MapConfigParsingServiceTests
{
    private readonly MapConfigParsingService _service = new();

    [Fact]
    public void ParseConfigs_DefaultToMap_MapOverridesDefault()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3
            MapTime = 20

            [ze_test]
            MaxExtends = 5
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_test"));

        var overrides = result.MapConfigsNameMapping["ze_test"];
        var baseConfig = overrides.First().MapConfig;

        // Map value overrides default
        Assert.Equal(5, baseConfig.MaxExtends);
        // Default value used when map doesn't specify
        Assert.Equal(20, baseConfig.MapTime);
    }

    [Fact]
    public void ParseConfigs_DefaultToGroupToMap_Priority()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3
            MapTime = 20
            MinPlayers = 5

            [MapChooserSharpSettings.Groups.TestGroup]
            MaxExtends = 4
            MapTime = 30

            [ze_test]
            GroupSettings = ["TestGroup"]
            MaxExtends = 5
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var baseConfig = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // Map > Group > Default
        Assert.Equal(5, baseConfig.MaxExtends);       // Map
        Assert.Equal(30, baseConfig.MapTime);          // Group (map didn't specify)
        Assert.Equal(5, baseConfig.NominationConfig.MinPlayers); // Default
    }

    [Fact]
    public void ParseConfigs_MultipleGroups_FirstGroupPriority()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 1

            [MapChooserSharpSettings.Groups.Group1]
            RequiredPermissions = ["css/root"]
            MaxPlayers = 1000
            AllowedSteamIds = [111]
            DisallowedSteamIds = [222]

            [MapChooserSharpSettings.Groups.Group2]
            RequiredPermissions = ["css/generic"]
            MinPlayers = 300
            AllowedSteamIds = [333]
            DisallowedSteamIds = [444]

            [ze_test]
            GroupSettings = ["Group1", "Group2"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var baseConfig = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // First group (Group1) takes priority for non-merge properties
        Assert.Equal(["css/root"], baseConfig.NominationConfig.RequiredPermissions);
        Assert.Equal(1000, baseConfig.NominationConfig.MaxPlayers);

        // AllowedSteamIds and DisallowedSteamIds are merged from all groups
        Assert.Contains(111u, baseConfig.NominationConfig.AllowedSteamIds);
        Assert.Contains(333u, baseConfig.NominationConfig.AllowedSteamIds);
        Assert.Contains(222u, baseConfig.NominationConfig.DisallowedSteamIds);
        Assert.Contains(444u, baseConfig.NominationConfig.DisallowedSteamIds);
    }

    [Fact]
    public void ParseConfigs_ExtraMerge_DefaultGroupMap()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.TestGroup]
            Cooldown = 30

            [MapChooserSharpSettings.Groups.TestGroup.extra.shop]
            cost = 150

            [ze_test]
            GroupSettings = ["TestGroup"]

            [ze_test.extra.shop]
            discount = 20
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var baseConfig = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // Extra merged: group.shop.cost + map.shop.discount
        Assert.Equal(150, baseConfig.ExtraConfiguration.GetValue<int>("shop", "cost", 0));
        Assert.Equal(20, baseConfig.ExtraConfiguration.GetValue<int>("shop", "discount", 0));
    }

    [Fact]
    public void ParseConfigs_CooldownOverride_GroupOverridesMapCooldown()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            Cooldown = 10

            [MapChooserSharpSettings.Groups.TestGroup]
            CooldownOverride = 60

            [ze_test]
            GroupSettings = ["TestGroup"]
            Cooldown = 30
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var baseConfig = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // CooldownOverride from group overrides map's own cooldown
        Assert.Equal(60, baseConfig.CooldownConfig.ConfigCooldown);
    }

    [Fact]
    public void ParseConfigs_WorkshopIdMapping()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_workshop_map]
            WorkshopId = 1234567891234
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.True(result!.MapConfigsWorkshopIdMapping.ContainsKey(1234567891234L));

        var config = result.MapConfigsWorkshopIdMapping[1234567891234L].First().MapConfig;
        Assert.Equal("ze_workshop_map", config.MapName);
        Assert.Equal(1234567891234L, config.WorkshopId);
    }

    [Fact]
    public void ParseConfigs_DaySettings_MapOverride()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3
            MinPlayers = 5

            [ze_test]
            MaxExtends = 4

            [ze_test.DaySettings.WeekendNight]
            Enabled = true
            ForceOverride = false
            OverridePriority = 1
            TargetDays = ["saturday", "sunday"]
            TargetTimeRanges = ["18:00-03:00"]
            MaxExtends = 5
            MinPlayers = 20
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var overrides = result!.MapConfigsNameMapping["ze_test"];
        Assert.Equal(2, overrides.Count); // base + WeekendNight

        var weekendOverride = overrides.FirstOrDefault(o => o.OverrideConfigName == "WeekendNight");
        Assert.NotNull(weekendOverride);
        Assert.True(weekendOverride!.Enabled);
        Assert.False(weekendOverride.ForceOverride);
        Assert.Equal(1, weekendOverride.OverridePriority);
        Assert.Contains(DayOfWeek.Saturday, weekendOverride.TargetDays);
        Assert.Contains(DayOfWeek.Sunday, weekendOverride.TargetDays);
        Assert.Single(weekendOverride.TargetTimeRanges);

        // Overridden values
        Assert.Equal(5, weekendOverride.MapConfig.MaxExtends);
        Assert.Equal(20, weekendOverride.MapConfig.NominationConfig.MinPlayers);
    }

    [Fact]
    public void ParseConfigs_DaySettings_GroupOverride()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.TestGroup]
            OnlyNomination = true

            [MapChooserSharpSettings.Groups.TestGroup.DaySettings.WeekendAfternoon]
            Enabled = true
            ForceOverride = false
            OverridePriority = 1
            TargetDays = ["saturday", "sunday"]
            TargetTimeRanges = ["14:00-18:00"]
            OnlyNomination = false
            MinPlayers = 10
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.True(result!.MapGroupSettings.ContainsKey("TestGroup"));

        var groupOverrides = result.MapGroupSettings["TestGroup"];
        Assert.Equal(2, groupOverrides.Count); // base + WeekendAfternoon

        var weekendOverride = groupOverrides.FirstOrDefault(o => o.OverrideConfigName == "WeekendAfternoon");
        Assert.NotNull(weekendOverride);
        Assert.True(weekendOverride!.Enabled);
        Assert.False(weekendOverride.GroupConfig.RandomPickConfig.IsPickable == false); // OnlyNomination=false → IsPickable=true
        Assert.Equal(10, weekendOverride.GroupConfig.NominationConfig.MinPlayers);
    }

    [Fact]
    public void ParseConfigs_DaySettingsExtra_OverridesBaseExtra()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            MaxExtends = 4

            [ze_test.extra.shop]
            cost = 100

            [ze_test.DaySettings.WeekendNight]
            Enabled = true
            TargetDays = ["saturday"]

            [ze_test.DaySettings.WeekendNight.extra.shop]
            cost = 50
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var overrides = result!.MapConfigsNameMapping["ze_test"];

        // Base config has cost=100
        var baseOverride = overrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName);
        Assert.Equal(100, baseOverride.MapConfig.ExtraConfiguration.GetValue<int>("shop", "cost", 0));

        // WeekendNight override has cost=50
        var weekendOverride = overrides.First(o => o.OverrideConfigName == "WeekendNight");
        Assert.Equal(50, weekendOverride.MapConfig.ExtraConfiguration.GetValue<int>("shop", "cost", 0));
    }

    [Fact]
    public void ParseConfigs_NonexistentGroupReference_Ignored()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            GroupSettings = ["NonexistentGroup"]
            MaxExtends = 5
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var baseConfig = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // Map still parses correctly
        Assert.Equal(5, baseConfig.MaxExtends);
        // No groups resolved
        Assert.Empty(baseConfig.GroupSettings);
    }

    [Fact]
    public void ParseConfigs_MinimalMap_UsesDefaults()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3
            MapTime = 20
            MapRounds = 10

            [ze_minimal]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var baseConfig = result!.MapConfigsNameMapping["ze_minimal"].First().MapConfig;

        Assert.Equal(3, baseConfig.MaxExtends);
        Assert.Equal(20, baseConfig.MapTime);
        Assert.Equal(10, baseConfig.MapRounds);
        Assert.Equal("ze_minimal", baseConfig.MapName);
        Assert.Equal("", baseConfig.MapNameAlias);
        Assert.Equal(0L, baseConfig.WorkshopId);
    }

    [Fact]
    public void ParseConfigs_OnlyNomination_MapsToIsPickable()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            OnlyNomination = false

            [ze_nom_only]
            OnlyNomination = true
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_nom_only"].First().MapConfig;

        Assert.False(config.RandomPickConfig.IsPickable);
    }

    [Fact]
    public void ParseConfigs_CooldownDateTime_Parsed()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            Cooldown = 0

            [ze_test]
            Cooldown = 60
            CooldownDateTime = "2d"
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        Assert.Equal(60, config.CooldownConfig.ConfigCooldown);
        Assert.Equal(TimeSpan.FromDays(2), config.CooldownConfig.TimedCooldown);
    }

    [Fact]
    public void ParseConfigs_AllowedTimeRanges_Parsed()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            AllowedTimeRanges = ["10:00-12:00", "22:00-03:00"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        var ranges = config.NominationConfig.AllowedTimeRanges;

        Assert.Equal(2, ranges.Count);
        Assert.Equal(new TimeOnly(10, 0), ranges[0].StartTime);
        Assert.Equal(new TimeOnly(12, 0), ranges[0].EndTime);
        Assert.Equal(new TimeOnly(22, 0), ranges[1].StartTime);
        Assert.Equal(new TimeOnly(3, 0), ranges[1].EndTime);
    }

    [Fact]
    public void ParseConfigs_MultipleMaps_AllParsed()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_map_a]
            MaxExtends = 5

            [ze_map_b]
            MaxExtends = 7
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        Assert.Equal(2, result!.MapConfigsNameMapping.Count);
        Assert.Equal(5, result.MapConfigsNameMapping["ze_map_a"].First().MapConfig.MaxExtends);
        Assert.Equal(7, result.MapConfigsNameMapping["ze_map_b"].First().MapConfig.MaxExtends);
    }
}
