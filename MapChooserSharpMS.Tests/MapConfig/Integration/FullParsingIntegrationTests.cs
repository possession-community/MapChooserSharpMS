using System;
using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Integration;

public class FullParsingIntegrationTests
{
    private readonly MapConfigParsingService _service = new();

    /// <summary>
    /// Integration test using config_example.toml equivalent content.
    /// Tests the full parsing pipeline with all feature combinations.
    /// </summary>
    [Fact]
    public void ParseConfigs_FullConfigExample_AllSectionsCorrect()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MapNameAlias = ""
            MapDescription = ""
            IsDisabled = false
            WorkshopId = 0
            OnlyNomination = false
            Cooldown = 0
            MaxExtends = 3
            MaxExtCommandUses = 1
            ExtendTimePerExtends = 15
            MapTime = 20
            ExtendRoundsPerExtends = 5
            MapRounds = 10
            RequiredPermissions = []
            RestrictToAllowedUsersOnly = false
            AllowedSteamIds = []
            DisallowedSteamIds = []
            MaxPlayers = 0
            MinPlayers = 0
            ProhibitAdminNomination = false
            DaysAllowed = []
            AllowedTimeRanges = []

            [ze_example_abc]
            MapNameAlias = "ze example a b c"
            MapDescription = "Let's play ze_example_abc!"
            IsDisabled = false
            WorkshopId = 1234567891234
            OnlyNomination = false
            Cooldown = 60
            CooldownDateTime = "2d"
            MaxExtends = 3
            MaxExtCommandUses = 1
            ExtendTimePerExtends = 15
            RequiredPermissions = ["css/generic"]
            RestrictToAllowedUsersOnly = false
            AllowedSteamIds = [987654321]
            DisallowedSteamIds = [123456789]
            MaxPlayers = 64
            MinPlayers = 10
            ProhibitAdminNomination = false
            DaysAllowed = ["wednesday", "monday"]
            AllowedTimeRanges = ["10:00-12:00", "20:00-22:00", "22:00-03:00"]

            [ze_example_abc.extra.shop]
            cost = 100

            [ze_example_xyz]

            [MapChooserSharpSettings.Groups.HardZeMap]
            Cooldown = 30
            OnlyNomination = true
            DaysAllowed = ["saturday", "sunday"]
            AllowedTimeRanges = ["18:00-00:00"]

            [MapChooserSharpSettings.Groups.HardZeMap.extra.shop]
            cost = 100

            [ze_example_123]
            GroupSettings = ["HardZeMap"]
            OnlyNomination = false
            Cooldown = 60

            [MapChooserSharpSettings.Groups.Group1]
            RequiredPermissions = ["css/root"]
            AllowedTimeRanges = ["18:00-00:00"]
            MaxPlayers = 1000
            AllowedSteamIds = [987654321]
            DisallowedSteamIds = [987654321]

            [MapChooserSharpSettings.Groups.Group2]
            RequiredPermissions = ["css/generic"]
            DaysAllowed = ["saturday", "sunday"]
            MinPlayers = 300
            AllowedSteamIds = [123456789]
            DisallowedSteamIds = [123456789]

            [ze_example_789]
            GroupSettings = ["Group1", "Group2"]

            [ze_example_abc.DaySettings.WeekendNight]
            Enabled = true
            ForceOverride = false
            OverridePriority = 1
            TargetDays = ["saturday", "sunday"]
            TargetTimeRanges = ["18:00-03:00"]
            MaxExtends = 5
            MinPlayers = 20
            OnlyNomination = false

            [ze_example_abc.DaySettings.WeekendNight.extra.shop]
            cost = 50

            [ze_example_abc.DaySettings.Weekday]
            Enabled = true
            ForceOverride = false
            OverridePriority = 0
            TargetDays = ["monday", "tuesday", "wednesday", "thursday", "friday"]
            MaxPlayers = 32
            MinPlayers = 5

            [MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon]
            Enabled = true
            ForceOverride = false
            OverridePriority = 1
            TargetDays = ["saturday", "sunday"]
            TargetTimeRanges = ["14:00-18:00"]
            OnlyNomination = false
            MinPlayers = 10

            [MapChooserSharpSettings.Groups.HardZeMap.DaySettings.WeekendAfternoon.extra.shop]
            cost = 50
            """;

        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        // ---- ze_example_abc ----
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_example_abc"));
        var abcOverrides = result.MapConfigsNameMapping["ze_example_abc"];
        Assert.Equal(3, abcOverrides.Count); // base + WeekendNight + Weekday

        var abcBase = abcOverrides.First(o => o.OverrideConfigName == "").MapConfig;
        Assert.Equal("ze example a b c", abcBase.MapNameAlias);
        Assert.Equal("Let's play ze_example_abc!", abcBase.MapDescription);
        Assert.Equal(1234567891234L, abcBase.WorkshopId);
        Assert.Equal(60, abcBase.CooldownConfig.ConfigCooldown);
        Assert.Equal(TimeSpan.FromDays(2), abcBase.CooldownConfig.TimedCooldown);
        Assert.Equal(["css/generic"], abcBase.NominationConfig.RequiredPermissions);
        Assert.Contains(987654321u, abcBase.NominationConfig.AllowedSteamIds);
        Assert.Contains(123456789u, abcBase.NominationConfig.DisallowedSteamIds);
        Assert.Equal(64, abcBase.NominationConfig.MaxPlayers);
        Assert.Equal(10, abcBase.NominationConfig.MinPlayers);
        Assert.Contains(DayOfWeek.Wednesday, abcBase.NominationConfig.DaysAllowed);
        Assert.Contains(DayOfWeek.Monday, abcBase.NominationConfig.DaysAllowed);
        Assert.Equal(3, abcBase.NominationConfig.AllowedTimeRanges.Count);
        Assert.Equal(100, abcBase.ExtraConfiguration.GetValue<int>("shop", "cost", 0));

        // WorkshopId mapping
        Assert.True(result.MapConfigsWorkshopIdMapping.ContainsKey(1234567891234L));

        // WeekendNight override
        var weekendNight = abcOverrides.First(o => o.OverrideConfigName == "WeekendNight");
        Assert.True(weekendNight.Enabled);
        Assert.False(weekendNight.ForceOverride);
        Assert.Equal(1, weekendNight.OverridePriority);
        Assert.Equal(5, weekendNight.MapConfig.MaxExtends);
        Assert.Equal(20, weekendNight.MapConfig.NominationConfig.MinPlayers);
        Assert.Equal(50, weekendNight.MapConfig.ExtraConfiguration.GetValue<int>("shop", "cost", 0));

        // Weekday override
        var weekday = abcOverrides.First(o => o.OverrideConfigName == "Weekday");
        Assert.True(weekday.Enabled);
        Assert.Equal(0, weekday.OverridePriority);
        Assert.Equal(32, weekday.MapConfig.NominationConfig.MaxPlayers);
        Assert.Equal(5, weekday.MapConfig.NominationConfig.MinPlayers);

        // ---- ze_example_xyz (minimal) ----
        Assert.True(result.MapConfigsNameMapping.ContainsKey("ze_example_xyz"));
        var xyzBase = result.MapConfigsNameMapping["ze_example_xyz"].First().MapConfig;
        Assert.Equal(3, xyzBase.MaxExtends);
        Assert.Equal(20, xyzBase.MapTime);
        Assert.Equal("", xyzBase.MapNameAlias);

        // ---- ze_example_123 (with HardZeMap group) ----
        Assert.True(result.MapConfigsNameMapping.ContainsKey("ze_example_123"));
        var map123Base = result.MapConfigsNameMapping["ze_example_123"].First().MapConfig;
        Assert.Single(map123Base.GroupSettings);
        Assert.Equal("HardZeMap", map123Base.GroupSettings[0].GroupName);
        // Map overrides OnlyNomination from group
        Assert.True(map123Base.RandomPickConfig.IsPickable); // OnlyNomination=false
        // Group extra merged into map
        Assert.Equal(100, map123Base.ExtraConfiguration.GetValue<int>("shop", "cost", 0));

        // ---- ze_example_789 (with Group1 + Group2) ----
        Assert.True(result.MapConfigsNameMapping.ContainsKey("ze_example_789"));
        var map789Base = result.MapConfigsNameMapping["ze_example_789"].First().MapConfig;
        Assert.Equal(2, map789Base.GroupSettings.Count);
        // Group1 first priority for RequiredPermissions
        Assert.Equal(["css/root"], map789Base.NominationConfig.RequiredPermissions);
        Assert.Equal(1000, map789Base.NominationConfig.MaxPlayers);
        // AllowedSteamIds merged
        Assert.Contains(987654321u, map789Base.NominationConfig.AllowedSteamIds);
        Assert.Contains(123456789u, map789Base.NominationConfig.AllowedSteamIds);
        // DisallowedSteamIds merged
        Assert.Contains(987654321u, map789Base.NominationConfig.DisallowedSteamIds);
        Assert.Contains(123456789u, map789Base.NominationConfig.DisallowedSteamIds);

        // ---- Groups ----
        Assert.True(result.MapGroupSettings.ContainsKey("HardZeMap"));
        var hardZeOverrides = result.MapGroupSettings["HardZeMap"];
        Assert.Equal(2, hardZeOverrides.Count); // base + WeekendAfternoon

        var hardZeBase = hardZeOverrides.First(o => o.OverrideConfigName == "").GroupConfig;
        Assert.Equal(30, hardZeBase.CooldownConfig.ConfigCooldown);
        Assert.False(hardZeBase.RandomPickConfig.IsPickable); // OnlyNomination=true

        var weekendAfternoon = hardZeOverrides.First(o => o.OverrideConfigName == "WeekendAfternoon");
        Assert.True(weekendAfternoon.Enabled);
        Assert.Equal(1, weekendAfternoon.OverridePriority);
        Assert.True(weekendAfternoon.GroupConfig.RandomPickConfig.IsPickable); // OnlyNomination=false
        Assert.Equal(10, weekendAfternoon.GroupConfig.NominationConfig.MinPlayers);
        Assert.Equal(50, weekendAfternoon.GroupConfig.ExtraConfiguration.GetValue<int>("shop", "cost", 0));

        Assert.True(result.MapGroupSettings.ContainsKey("Group1"));
        Assert.True(result.MapGroupSettings.ContainsKey("Group2"));
    }
}
