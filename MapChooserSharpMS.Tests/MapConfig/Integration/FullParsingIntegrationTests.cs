using System;
using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Shared.MapConfig;
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
        var doc = TomlTestHelper.LoadToml("56_full_parsing_integration.toml");
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);

        // ---- ze_example_abc ----
        Assert.True(result!.MapConfigsNameMapping.ContainsKey("ze_example_abc"));
        var abcOverrides = result.MapConfigsNameMapping["ze_example_abc"];
        Assert.Equal(3, abcOverrides.Count); // base + WeekendNight + Weekday

        var abcBase = abcOverrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).MapConfig;
        Assert.Equal("ze example a b c", abcBase.MapNameAlias);
        Assert.Equal("Let's play ze_example_abc!", abcBase.MapDescription);
        Assert.Equal(1234567891234L, abcBase.WorkshopId);
        Assert.Equal(60, abcBase.CooldownSettings.ConfigCooldown);
        Assert.Equal(TimeSpan.FromDays(2), abcBase.CooldownSettings.TimedCooldown);
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
        Assert.Equal(1000, map789Base.NominationConfig.MaxPlayers);

        // ---- Groups ----
        Assert.True(result.MapGroupSettings.ContainsKey("HardZeMap"));
        var hardZeOverrides = result.MapGroupSettings["HardZeMap"];
        Assert.Equal(2, hardZeOverrides.Count); // base + WeekendAfternoon

        var hardZeBase = hardZeOverrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal(30, hardZeBase.CooldownSettings.ConfigCooldown);
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
