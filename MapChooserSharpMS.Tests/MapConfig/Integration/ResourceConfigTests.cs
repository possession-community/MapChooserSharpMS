using System;
using System.IO;
using System.Linq;
using CsToml;
using MapChooserSharpMS.Modules.MapConfig.Interfaces;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Shared.MapConfig;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Integration;

public class ResourceConfigTests
{
    private readonly MapConfigParsingService _service = new();

    private IMapConfigParsingResult LoadAndParse(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
        var bytes = File.ReadAllBytes(path);
        var doc = CsTomlSerializer.Deserialize<TomlDocument>(bytes);
        var result = _service.ParseConfigsFromDocument(doc);
        Assert.NotNull(result);
        return result!;
    }

    private IMapConfigParsingResult LoadAndParseDirectory(string dirName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", dirName);
        var result = _service.ParseConfigs(path);
        Assert.NotNull(result);
        return result!;
    }

    // ================================================================
    // 01: No Default — hardcoded fallback values
    // ================================================================

    [Fact]
    public void NoDefault_MapsUseHardcodedDefaults()
    {
        var result = LoadAndParse("01_no_default.toml");

        Assert.Equal(2, result.MapConfigsNameMapping.Count);

        // ze_nodefault_a: explicitly set MaxExtends=5, rest defaults
        var a = result.MapConfigsNameMapping["ze_nodefault_a"].First().MapConfig;
        Assert.Equal(5, a.MaxExtends);
        Assert.Equal(20, a.MapTime);
        Assert.Equal(1, a.MaxExtCommandUses);
        Assert.Equal(15, a.ExtendTimePerExtends);
        Assert.Equal(10, a.MapRounds);
        Assert.Equal(5, a.ExtendRoundsPerExtends);
        Assert.Equal(0, a.NominationConfig.MinPlayers);
        Assert.Equal(0, a.NominationConfig.MaxPlayers);
        Assert.Equal(0, a.CooldownSettings.ConfigCooldown);
        Assert.False(a.IsDisabled);
        Assert.True(a.RandomPickConfig.IsPickable);
        Assert.Equal("", a.MapNameAlias);
        Assert.Equal("", a.MapDescription);
        Assert.Equal(0, a.WorkshopId);

        // ze_nodefault_b: completely empty, all hardcoded defaults
        var b = result.MapConfigsNameMapping["ze_nodefault_b"].First().MapConfig;
        Assert.Equal(3, b.MaxExtends);
        Assert.Equal(20, b.MapTime);
        Assert.Equal(1, b.MaxExtCommandUses);
        Assert.Equal(15, b.ExtendTimePerExtends);
        Assert.Equal(10, b.MapRounds);
        Assert.Equal(5, b.ExtendRoundsPerExtends);
    }

    // ================================================================
    // 02: Default Only — no maps in result
    // ================================================================

    [Fact]
    public void DefaultOnly_NoMapsInResult()
    {
        var result = LoadAndParse("02_default_only.toml");

        Assert.Empty(result.MapConfigsNameMapping);
        Assert.Empty(result.MapConfigsWorkshopIdMapping);
        Assert.Empty(result.MapGroupSettings);
    }

    // ================================================================
    // 03: Minimal Maps — empty sections inherit Default
    // ================================================================

    [Fact]
    public void MinimalMaps_InheritDefaultValues()
    {
        var result = LoadAndParse("03_minimal_maps.toml");

        Assert.Equal(3, result.MapConfigsNameMapping.Count);

        foreach (var mapName in new[] { "ze_a", "ze_b", "ze_c" })
        {
            Assert.True(result.MapConfigsNameMapping.ContainsKey(mapName));
            var config = result.MapConfigsNameMapping[mapName].First().MapConfig;
            Assert.Equal(3, config.MaxExtends);
            Assert.Equal(20, config.MapTime);
        }
    }

    // ================================================================
    // 04: Property Priority — Map > Group > Default
    // ================================================================

    [Fact]
    public void PropertyPriority_MapOverridesGroupOverridesDefault()
    {
        var result = LoadAndParse("04_property_priority.toml");

        var config = result.MapConfigsNameMapping["ze_priority_test"].First().MapConfig;

        // Map overrides Group and Default
        Assert.Equal(5, config.MaxExtends);
        // Group overrides Default
        Assert.Equal(30, config.MapTime);
        Assert.Equal(10, config.NominationConfig.MinPlayers);
        // Default only
        Assert.Equal(100, config.NominationConfig.MaxPlayers);
        Assert.Equal(10, config.CooldownSettings.ConfigCooldown);
    }

    // ================================================================
    // 05: Multiple Groups — first group priority + SteamId accumulation
    // ================================================================

    [Fact]
    public void MultipleGroups_FirstGroupPriority()
    {
        var result = LoadAndParse("05_multiple_groups.toml");

        var config = result.MapConfigsNameMapping["ze_multi_group"].First().MapConfig;

        // G1 first priority for MaxPlayers
        Assert.Equal(1000, config.NominationConfig.MaxPlayers);
        // G2 for MinPlayers (G1 doesn't set it)
        Assert.Equal(300, config.NominationConfig.MinPlayers);
        // G3 for MapTime (G1, G2 don't set it)
        Assert.Equal(40, config.MapTime);

        // 3 groups resolved
        Assert.Equal(3, config.GroupSettings.Count);
    }

    // ================================================================
    // 06: CooldownOverride — group overrides map cooldown
    // ================================================================

    [Fact]
    public void CooldownOverride_GroupOverridesMapCooldown()
    {
        var result = LoadAndParse("06_cooldown_override.toml");

        // map_a: Group_A CooldownOverride=60 overrides map's Cooldown=30
        Assert.Equal(60, result.MapConfigsNameMapping["map_a"].First().MapConfig.CooldownSettings.ConfigCooldown);

        // map_b: Group_None has no CooldownOverride, map's Cooldown=30 used
        Assert.Equal(30, result.MapConfigsNameMapping["map_b"].First().MapConfig.CooldownSettings.ConfigCooldown);

        // map_c: Group_Zero CooldownOverride=0 (not >0, so not applied), map's Cooldown=30
        Assert.Equal(30, result.MapConfigsNameMapping["map_c"].First().MapConfig.CooldownSettings.ConfigCooldown);

        // map_d: Group_A first (CooldownOverride=60), Group_B second (90) — first wins
        Assert.Equal(60, result.MapConfigsNameMapping["map_d"].First().MapConfig.CooldownSettings.ConfigCooldown);

        // map_e: Group_None skipped (no override), Group_B applies (CooldownOverride=90)
        Assert.Equal(90, result.MapConfigsNameMapping["map_e"].First().MapConfig.CooldownSettings.ConfigCooldown);
    }

    // ================================================================
    // 07: Extra Merge — Default → Group → Map layering
    // ================================================================

    [Fact]
    public void ExtraMerge_DefaultGroupMapLayering()
    {
        var result = LoadAndParse("07_extra_merge.toml");

        // map1: GroupA + map extra
        var map1 = result.MapConfigsNameMapping["map1"].First().MapConfig;
        Assert.Equal(100L, map1.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(10L, map1.ExtraConfiguration.GetValue<long>("shop", "bonus", 0));
        Assert.Equal(20L, map1.ExtraConfiguration.GetValue<long>("shop", "discount", 0));
        Assert.Equal(1L, map1.ExtraConfiguration.GetValue<long>("shop", "base_val", 0));

        // map2: Map overrides GroupA's cost
        var map2 = result.MapConfigsNameMapping["map2"].First().MapConfig;
        Assert.Equal(300L, map2.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(10L, map2.ExtraConfiguration.GetValue<long>("shop", "bonus", 0));
        Assert.Equal(1L, map2.ExtraConfiguration.GetValue<long>("shop", "base_val", 0));

        // map3: No group, map has rewards.xp + default base_val
        var map3 = result.MapConfigsNameMapping["map3"].First().MapConfig;
        Assert.Equal(500L, map3.ExtraConfiguration.GetValue<long>("rewards", "xp", 0));
        Assert.Equal(1L, map3.ExtraConfiguration.GetValue<long>("shop", "base_val", 0));
        Assert.Equal(0L, map3.ExtraConfiguration.GetValue<long>("shop", "cost", 0)); // not set
    }

    [Fact]
    public void ExtraMerge_MultipleGroupsForwardOrder()
    {
        var result = LoadAndParse("07_extra_merge.toml");

        // map4: GroupSettings=["GroupA","GroupB"] — Extra forward order: GroupA then GroupB
        // GroupB's cost=200 overwrites GroupA's cost=100
        var map4 = result.MapConfigsNameMapping["map4"].First().MapConfig;
        Assert.Equal(200L, map4.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(10L, map4.ExtraConfiguration.GetValue<long>("shop", "bonus", 0));
        Assert.Equal(1L, map4.ExtraConfiguration.GetValue<long>("shop", "base_val", 0));
        Assert.Equal(50L, map4.ExtraConfiguration.GetValue<long>("rewards", "xp", 0));

        // map5: GroupSettings=["GroupB","GroupA"] — Extra forward order: GroupB then GroupA
        // GroupA's cost=100 overwrites GroupB's cost=200
        var map5 = result.MapConfigsNameMapping["map5"].First().MapConfig;
        Assert.Equal(100L, map5.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(10L, map5.ExtraConfiguration.GetValue<long>("shop", "bonus", 0));
        Assert.Equal(1L, map5.ExtraConfiguration.GetValue<long>("shop", "base_val", 0));
        Assert.Equal(50L, map5.ExtraConfiguration.GetValue<long>("rewards", "xp", 0));
    }

    [Fact]
    public void ExtraMerge_MapOverridesAllGroups()
    {
        var result = LoadAndParse("07_extra_merge.toml");

        // map6: GroupSettings=["GroupA","GroupB"] + Map extra cost=999
        // Map always wins regardless of group order
        var map6 = result.MapConfigsNameMapping["map6"].First().MapConfig;
        Assert.Equal(999L, map6.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(10L, map6.ExtraConfiguration.GetValue<long>("shop", "bonus", 0));
        Assert.Equal(1L, map6.ExtraConfiguration.GetValue<long>("shop", "base_val", 0));
        Assert.Equal(50L, map6.ExtraConfiguration.GetValue<long>("rewards", "xp", 0));
    }

    // ================================================================
    // 08: DaySettings Map — overrides created
    // ================================================================

    [Fact]
    public void DaySettingsMap_OverridesCreated()
    {
        var result = LoadAndParse("08_day_settings_map.toml");

        var overrides = result.MapConfigsNameMapping["ze_ds_map"];
        Assert.Equal(4, overrides.Count); // base + 3 overrides

        // Base
        var baseOverride = overrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName);
        Assert.Equal(4, baseOverride.MapConfig.MaxExtends);
        Assert.Equal(20, baseOverride.MapConfig.MapTime);

        // Override1
        var o1 = overrides.First(o => o.OverrideConfigName == "Override1");
        Assert.True(o1.Enabled);
        Assert.False(o1.ForceOverride);
        Assert.Equal(1, o1.OverridePriority);
        Assert.Equal(6, o1.MapConfig.MaxExtends);
        Assert.Contains(DayOfWeek.Saturday, o1.TargetDays);
        Assert.Contains(DayOfWeek.Sunday, o1.TargetDays);
        Assert.Single(o1.TargetTimeRanges);

        // Override2
        var o2 = overrides.First(o => o.OverrideConfigName == "Override2");
        Assert.False(o2.Enabled);
        Assert.Equal(0, o2.OverridePriority);
        Assert.Contains(DayOfWeek.Monday, o2.TargetDays);

        // Override3
        var o3 = overrides.First(o => o.OverrideConfigName == "Override3");
        Assert.True(o3.Enabled);
        Assert.True(o3.ForceOverride);
        Assert.Equal(5, o3.OverridePriority);
        Assert.Equal(30, o3.MapConfig.NominationConfig.MinPlayers);
        Assert.Contains(DayOfWeek.Friday, o3.TargetDays);
    }

    // ================================================================
    // 09: DaySettings Group — overrides created
    // ================================================================

    [Fact]
    public void DaySettingsGroup_OverridesCreated()
    {
        var result = LoadAndParse("09_day_settings_group.toml");

        var overrides = result.MapGroupSettings["DSGroup"];
        Assert.Equal(3, overrides.Count); // base + 2 overrides

        // Base
        var baseOverride = overrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName);
        Assert.Equal(5, baseOverride.GroupConfig.MaxExtends);
        Assert.False(baseOverride.GroupConfig.RandomPickConfig.IsPickable); // OnlyNomination=true
        Assert.Equal(30, baseOverride.GroupConfig.CooldownSettings.ConfigCooldown);

        // WeekendOverride
        var weekend = overrides.First(o => o.OverrideConfigName == "WeekendOverride");
        Assert.True(weekend.Enabled);
        Assert.Equal(2, weekend.OverridePriority);
        Assert.True(weekend.GroupConfig.RandomPickConfig.IsPickable); // OnlyNomination=false
        Assert.Equal(15, weekend.GroupConfig.NominationConfig.MinPlayers);
        Assert.Contains(DayOfWeek.Saturday, weekend.TargetDays);
        Assert.Contains(DayOfWeek.Sunday, weekend.TargetDays);

        // LateNight
        var lateNight = overrides.First(o => o.OverrideConfigName == "LateNight");
        Assert.True(lateNight.Enabled);
        Assert.Equal(1, lateNight.OverridePriority);
        Assert.Equal(8, lateNight.GroupConfig.MaxExtends);
        Assert.Contains(DayOfWeek.Friday, lateNight.TargetDays);
        Assert.Contains(DayOfWeek.Saturday, lateNight.TargetDays);
        Assert.Single(lateNight.TargetTimeRanges);
    }

    // ================================================================
    // 10: DaySettings Extra — override overwrites base extra
    // ================================================================

    [Fact]
    public void DaySettingsExtra_OverridesBaseExtra()
    {
        var result = LoadAndParse("10_day_settings_extra.toml");

        var overrides = result.MapConfigsNameMapping["ze_ds_extra"];
        Assert.Equal(2, overrides.Count); // base + Sale

        // Base extra
        var baseConfig = overrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).MapConfig;
        Assert.Equal(100L, baseConfig.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(10L, baseConfig.ExtraConfiguration.GetValue<long>("shop", "discount", 0));

        // Sale override: cost overridden, discount preserved from base
        var sale = overrides.First(o => o.OverrideConfigName == "Sale");
        Assert.Equal(50L, sale.MapConfig.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(10L, sale.MapConfig.ExtraConfiguration.GetValue<long>("shop", "discount", 0));
    }

    // ================================================================
    // 11: ForceOverride + OverridePriority
    // ================================================================

    [Fact]
    public void ForceOverride_PriorityValues()
    {
        var result = LoadAndParse("11_force_override.toml");

        var overrides = result.MapConfigsNameMapping["ze_force"];
        Assert.Equal(3, overrides.Count); // base + 2

        var low = overrides.First(o => o.OverrideConfigName == "LowPriority");
        Assert.True(low.ForceOverride);
        Assert.Equal(1, low.OverridePriority);
        Assert.Equal(6, low.MapConfig.MaxExtends);

        var high = overrides.First(o => o.OverrideConfigName == "HighPriority");
        Assert.True(high.ForceOverride);
        Assert.Equal(5, high.OverridePriority);
        Assert.Equal(10, high.MapConfig.MaxExtends);
    }

    // ================================================================
    // 12: IsDisabled / OnlyNomination
    // ================================================================

    [Fact]
    public void DisabledAndNomination_CorrectFlags()
    {
        var result = LoadAndParse("12_disabled_and_nomination.toml");

        // ze_disabled: IsDisabled=true, IsPickable=true (default OnlyNomination=false)
        var disabled = result.MapConfigsNameMapping["ze_disabled"].First().MapConfig;
        Assert.True(disabled.IsDisabled);
        Assert.True(disabled.RandomPickConfig.IsPickable);

        // ze_nomination_only: OnlyNomination=true → IsPickable=false
        var nomOnly = result.MapConfigsNameMapping["ze_nomination_only"].First().MapConfig;
        Assert.False(nomOnly.RandomPickConfig.IsPickable);
        Assert.False(nomOnly.IsDisabled);

        // ze_nomination_from_group: Group OnlyNomination=true → IsPickable=false
        var fromGroup = result.MapConfigsNameMapping["ze_nomination_from_group"].First().MapConfig;
        Assert.False(fromGroup.RandomPickConfig.IsPickable);

        // ze_nomination_group_override: Map OnlyNomination=false overrides group → IsPickable=true
        var groupOverride = result.MapConfigsNameMapping["ze_nomination_group_override"].First().MapConfig;
        Assert.True(groupOverride.RandomPickConfig.IsPickable);
    }

    // ================================================================
    // 13: WorkshopId mapping
    // ================================================================

    [Fact]
    public void WorkshopMaps_MappingCorrect()
    {
        var result = LoadAndParse("13_workshop_maps.toml");

        // WorkshopId=123 should be in mapping
        Assert.True(result.MapConfigsWorkshopIdMapping.ContainsKey(123));
        Assert.Single(result.MapConfigsWorkshopIdMapping);

        // Individual map values
        Assert.Equal(123L, result.MapConfigsNameMapping["ze_ws_valid"].First().MapConfig.WorkshopId);
        Assert.Equal(0L, result.MapConfigsNameMapping["ze_ws_zero"].First().MapConfig.WorkshopId);
        Assert.Equal(0L, result.MapConfigsNameMapping["ze_ws_none"].First().MapConfig.WorkshopId);
    }

    // ================================================================
    // 14: CooldownDateTime variations
    // ================================================================

    [Fact]
    public void CooldownDateTime_VariousFormats()
    {
        var result = LoadAndParse("14_cooldown_datetime.toml");

        Assert.Equal(TimeSpan.FromDays(2),
            result.MapConfigsNameMapping["ze_cd_2d"].First().MapConfig.CooldownSettings.TimedCooldown);

        Assert.Equal(TimeSpan.FromDays(30),
            result.MapConfigsNameMapping["ze_cd_1m"].First().MapConfig.CooldownSettings.TimedCooldown);

        Assert.Equal(TimeSpan.FromDays(7),
            result.MapConfigsNameMapping["ze_cd_7d"].First().MapConfig.CooldownSettings.TimedCooldown);

        Assert.Equal(TimeSpan.Zero,
            result.MapConfigsNameMapping["ze_cd_empty"].First().MapConfig.CooldownSettings.TimedCooldown);

        Assert.Equal(TimeSpan.Zero,
            result.MapConfigsNameMapping["ze_cd_none"].First().MapConfig.CooldownSettings.TimedCooldown);

        Assert.Equal(TimeSpan.Zero,
            result.MapConfigsNameMapping["ze_cd_invalid"].First().MapConfig.CooldownSettings.TimedCooldown);
    }

    // ================================================================
    // 16: Complex Layers — all features integrated
    // ================================================================

    [Fact]
    public void ComplexLayers_AllFeaturesIntegrated()
    {
        var result = LoadAndParse("16_complex_layers.toml");

        // --- Base ze_complex ---
        var baseOverrides = result.MapConfigsNameMapping["ze_complex"];
        Assert.Equal(3, baseOverrides.Count); // base + ComplexWeekend + CG1Weekend (inherited from CG1)

        var baseConfig = baseOverrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).MapConfig;

        // Properties: Map overrides Group overrides Default
        Assert.Equal(7, baseConfig.MaxExtends);       // Map (overrides CG1's 5)
        Assert.Equal(30, baseConfig.MapTime);          // CG2 (CG1 doesn't set, default=20)
        Assert.Equal(10, baseConfig.NominationConfig.MinPlayers); // CG2
        Assert.False(baseConfig.RandomPickConfig.IsPickable);     // CG1 OnlyNomination=true

        // CooldownOverride from CG1 (60) overrides default cooldown
        Assert.Equal(60, baseConfig.CooldownSettings.ConfigCooldown);

        // Extra merge: Default→CG1→CG2→Map
        // Note: Each group accessor already includes default extra merged in.
        // CG2's accessor has mode="default" (inherited), which overwrites CG1's mode="group1".
        Assert.Equal(1L, baseConfig.ExtraConfiguration.GetValue<long>("shop", "base_val", 0));
        Assert.Equal("default", baseConfig.ExtraConfiguration.GetValue<string>("shop", "mode", ""));
        Assert.Equal(200L, baseConfig.ExtraConfiguration.GetValue<long>("shop", "cost", 0)); // CG2 overwrites CG1
        Assert.Equal(5L, baseConfig.ExtraConfiguration.GetValue<long>("shop", "bonus", 0));  // CG2
        Assert.Equal(20L, baseConfig.ExtraConfiguration.GetValue<long>("shop", "discount", 0)); // Map

        // Groups resolved
        Assert.Equal(2, baseConfig.GroupSettings.Count);

        // --- ComplexWeekend override ---
        var weekend = baseOverrides.First(o => o.OverrideConfigName == "ComplexWeekend");
        Assert.True(weekend.Enabled);
        Assert.Equal(2, weekend.OverridePriority);
        Assert.Contains(DayOfWeek.Saturday, weekend.TargetDays);
        Assert.Single(weekend.TargetTimeRanges);

        // Override properties
        Assert.Equal(25, weekend.MapConfig.NominationConfig.MinPlayers); // Override
        Assert.Equal(7, weekend.MapConfig.MaxExtends);                   // Inherited from map
        Assert.Equal(60, weekend.MapConfig.CooldownSettings.ConfigCooldown); // CooldownOverride still applies

        // Override extra: base map extra + override shop.cost=30
        Assert.Equal(30L, weekend.MapConfig.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(20L, weekend.MapConfig.ExtraConfiguration.GetValue<long>("shop", "discount", 0)); // Preserved

        // --- Group CG1 DaySettings ---
        var cg1Overrides = result.MapGroupSettings["CG1"];
        Assert.Equal(2, cg1Overrides.Count); // base + CG1Weekend

        var cg1Base = cg1Overrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName);
        Assert.False(cg1Base.GroupConfig.RandomPickConfig.IsPickable); // OnlyNomination=true

        var cg1Weekend = cg1Overrides.First(o => o.OverrideConfigName == "CG1Weekend");
        Assert.True(cg1Weekend.GroupConfig.RandomPickConfig.IsPickable); // OnlyNomination=false
        Assert.Equal(15, cg1Weekend.GroupConfig.NominationConfig.MinPlayers);
        Assert.Equal(50L, cg1Weekend.GroupConfig.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
    }

    // ================================================================
    // 17: Edge Cases — unknown props, type mismatches
    // ================================================================

    [Fact]
    public void EdgeCases_UnknownPropsRejectSection()
    {
        var result = LoadAndParse("17_edge_cases.toml");

        // A section containing an unknown property is not a valid map config
        // and is skipped entirely; other sections still load.
        Assert.False(result.MapConfigsNameMapping.ContainsKey("ze_unknown_prop"));
        Assert.True(result.MapConfigsNameMapping.ContainsKey("ze_type_mismatch"));
    }

    [Fact]
    public void EdgeCases_TypeMismatchFallsBackToDefault()
    {
        var result = LoadAndParse("17_edge_cases.toml");

        // MaxExtends="five" — TryGetInt64 fails → null → hardcoded default 3
        var mismatch = result.MapConfigsNameMapping["ze_type_mismatch"].First().MapConfig;
        Assert.Equal(3, mismatch.MaxExtends);
        Assert.Equal(20, mismatch.MapTime); // This one parses fine
    }

    [Fact]
    public void EdgeCases_BadGroupSettingsIgnored()
    {
        var result = LoadAndParse("17_edge_cases.toml");

        // GroupSettings=[123] — non-string elements skipped → empty list
        var badGroup = result.MapConfigsNameMapping["ze_bad_group_settings"].First().MapConfig;
        Assert.Empty(badGroup.GroupSettings);
    }

    [Fact]
    public void EdgeCases_BadDaysIgnored()
    {
        var result = LoadAndParse("17_edge_cases.toml");

        // DaysAllowed=["funday"] — invalid enum → empty list
        var badDays = result.MapConfigsNameMapping["ze_bad_days"].First().MapConfig;
        Assert.Empty(badDays.NominationConfig.DaysAllowed);
    }

    [Fact]
    public void EdgeCases_EmptyArraysPreserved()
    {
        var result = LoadAndParse("17_edge_cases.toml");

        var empty = result.MapConfigsNameMapping["ze_empty_arrays"].First().MapConfig;
        Assert.Empty(empty.NominationConfig.DaysAllowed);
        Assert.Empty(empty.NominationConfig.AllowedTimeRanges);
    }

    // ================================================================
    // Multifile — cross-file group reference
    // ================================================================

    [Fact]
    public void Multifile_CrossFileGroupReference()
    {
        var result = LoadAndParseDirectory("multifile");

        Assert.True(result.MapConfigsNameMapping.ContainsKey("ze_multifile_test"));
        var config = result.MapConfigsNameMapping["ze_multifile_test"].First().MapConfig;

        // MaxExtends from MFGroup (cross-file reference)
        Assert.Equal(5, config.MaxExtends);
        // MapTime from map (overrides default)
        Assert.Equal(25, config.MapTime);
        // MinPlayers from MFGroup
        Assert.Equal(10, config.NominationConfig.MinPlayers);
        // Group resolved
        var singleGroup = Assert.Single(config.GroupSettings);
        Assert.Equal("MFGroup", singleGroup.GroupName);
    }

    // ================================================================
    // Multifile with maps.toml — maps.toml takes priority
    // ================================================================

    [Fact]
    public void MultifileWithMapsToml_MapsTomlPriority()
    {
        var result = LoadAndParseDirectory("multifile_with_maps_toml");

        // Only maps.toml loaded
        Assert.True(result.MapConfigsNameMapping.ContainsKey("ze_maps_toml_only"));
        Assert.Equal(5, result.MapConfigsNameMapping["ze_maps_toml_only"].First().MapConfig.MaxExtends);

        // extra_maps.toml should NOT be loaded
        Assert.False(result.MapConfigsNameMapping.ContainsKey("ze_should_not_load"));
    }

    // ================================================================
    // 18: Complex Realistic — 24h ZE Server scenario
    //     3 groups (HardZE, Premium, LongMaps) each with DaySettings
    //     1 map (ze_epic_finale_v3) with 3 groups + map DaySettings
    //     Extra configs at every layer
    // ================================================================

    [Fact]
    public void ComplexRealistic_StructureCounts()
    {
        var result = LoadAndParse("18_complex_realistic.toml");

        // 1 map
        Assert.Single(result.MapConfigsNameMapping);
        Assert.True(result.MapConfigsNameMapping.ContainsKey("ze_epic_finale_v3"));

        // 3 groups
        Assert.Equal(3, result.MapGroupSettings.Count);
        Assert.True(result.MapGroupSettings.ContainsKey("HardZE"));
        Assert.True(result.MapGroupSettings.ContainsKey("Premium"));
        Assert.True(result.MapGroupSettings.ContainsKey("LongMaps"));

        // Map overrides: base + EventMode + WeekendRelax + inherited group DaySettings
        // HardZE: WeekendPrime, WeekdayNight; Premium: FreeWeekend; LongMaps: PeakHours, LateNight = 8 total
        Assert.Equal(8, result.MapConfigsNameMapping["ze_epic_finale_v3"].Count);

        // Group override counts: HardZE(base+2), Premium(base+1), LongMaps(base+2)
        Assert.Equal(3, result.MapGroupSettings["HardZE"].Count);
        Assert.Equal(2, result.MapGroupSettings["Premium"].Count);
        Assert.Equal(3, result.MapGroupSettings["LongMaps"].Count);

        // WorkshopId mapping
        Assert.True(result.MapConfigsWorkshopIdMapping.ContainsKey(987654321));
    }

    [Fact]
    public void ComplexRealistic_BaseMapProperties()
    {
        var result = LoadAndParse("18_complex_realistic.toml");
        var map = result.MapConfigsNameMapping["ze_epic_finale_v3"]
            .First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).MapConfig;

        // --- Identifiers ---
        Assert.Equal("ze_epic_finale_v3", map.MapName);
        Assert.Equal("Epic Finale V3", map.MapNameAlias);
        Assert.Equal("The ultimate zombie escape experience!", map.MapDescription);
        Assert.Equal(987654321L, map.WorkshopId);

        // --- Property priority: Map > HardZE(1st) > Premium(2nd) > LongMaps(3rd) > Default ---

        // Map sets MaxExtends=3 (overrides HardZE's 2 and LongMaps' 4)
        Assert.Equal(3, map.MaxExtends);
        // HardZE(1st group) sets MapTime=45 (overrides LongMaps' 60)
        Assert.Equal(45, map.MapTime);
        // LongMaps sets ExtendTimePerExtends=20 (no other group/map sets it)
        Assert.Equal(20, map.ExtendTimePerExtends);
        // LongMaps sets MaxExtCommandUses=2
        Assert.Equal(2, map.MaxExtCommandUses);
        // Default values retained for unset properties
        Assert.Equal(10, map.MapRounds);
        Assert.Equal(5, map.ExtendRoundsPerExtends);

        // --- CooldownOverride ---
        // HardZE CooldownOverride=48 overrides map's Cooldown=96
        Assert.Equal(48, map.CooldownSettings.ConfigCooldown);
        // Map CooldownDateTime="7d" overrides LongMaps' "3d"
        Assert.Equal(TimeSpan.FromDays(7), map.CooldownSettings.TimedCooldown);

        // --- Nomination: merge from Premium(2nd) and HardZE(1st) ---
        // HardZE OnlyNomination=true (map doesn't override) → IsPickable=false
        Assert.False(map.RandomPickConfig.IsPickable);
        Assert.False(map.IsDisabled);
        // Premium MaxPlayers=48
        Assert.Equal(48, map.NominationConfig.MaxPlayers);
        // HardZE MinPlayers=20
        Assert.Equal(20, map.NominationConfig.MinPlayers);
        // Premium ProhibitAdminNomination=true
        Assert.True(map.NominationConfig.ProhibitAdminNomination);
        // HardZE DaysAllowed
        Assert.Contains(DayOfWeek.Friday, map.NominationConfig.DaysAllowed);
        Assert.Contains(DayOfWeek.Saturday, map.NominationConfig.DaysAllowed);
        Assert.Contains(DayOfWeek.Sunday, map.NominationConfig.DaysAllowed);
        Assert.Equal(3, map.NominationConfig.DaysAllowed.Count);
        // LongMaps AllowedTimeRanges (HardZE/Premium don't set it)
        Assert.Single(map.NominationConfig.AllowedTimeRanges);

        // --- 3 groups resolved ---
        Assert.Equal(3, map.GroupSettings.Count);
    }

    [Fact]
    public void ComplexRealistic_BaseMapExtra()
    {
        var result = LoadAndParse("18_complex_realistic.toml");
        var extra = result.MapConfigsNameMapping["ze_epic_finale_v3"]
            .First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).MapConfig.ExtraConfiguration;

        // Extra merge order: Default(empty) → HardZE → Premium → LongMaps → Map

        // shop: Premium cost=500 → Map cost=1000 (Map overwrites), currency from Premium preserved
        Assert.Equal(1000L, extra.GetValue<long>("shop", "cost", 0));
        Assert.Equal("credits", extra.GetValue<string>("shop", "currency", ""));

        // difficulty: HardZE level=5, mode="hard" + Map boss_hp=50000
        Assert.Equal(5L, extra.GetValue<long>("difficulty", "level", 0));
        Assert.Equal("hard", extra.GetValue<string>("difficulty", "mode", ""));
        Assert.Equal(50000L, extra.GetValue<long>("difficulty", "boss_hp", 0));

        // rewards: HardZE xp_multiplier=2
        Assert.Equal(2L, extra.GetValue<long>("rewards", "xp_multiplier", 0));

        // gameplay: LongMaps speed=1.0, gravity=800
        Assert.Equal(1.0, extra.GetValue<double>("gameplay", "speed", 0.0));
        Assert.Equal(800L, extra.GetValue<long>("gameplay", "gravity", 0));

        // special: Map event_name="finale_challenge"
        Assert.Equal("finale_challenge", extra.GetValue<string>("special", "event_name", ""));

        // Verify all 5 extra sections exist
        var sections = extra.GetSections();
        Assert.Equal(5, sections.Count);
    }

    [Fact]
    public void ComplexRealistic_MapEventModeOverride()
    {
        var result = LoadAndParse("18_complex_realistic.toml");
        var eventMode = result.MapConfigsNameMapping["ze_epic_finale_v3"]
            .First(o => o.OverrideConfigName == "EventMode");

        // --- Override metadata ---
        Assert.True(eventMode.Enabled);
        Assert.True(eventMode.ForceOverride);
        Assert.Equal(10, eventMode.OverridePriority);
        Assert.Contains(DayOfWeek.Saturday, eventMode.TargetDays);
        Assert.Single(eventMode.TargetDays);
        Assert.Single(eventMode.TargetTimeRanges);

        // --- Properties: all layers merged + EventMode override ---
        var map = eventMode.MapConfig;
        Assert.Equal(10, map.MaxExtends);  // EventMode override (over base map's 3)
        Assert.Equal(40, map.NominationConfig.MinPlayers);  // EventMode override
        Assert.True(map.RandomPickConfig.IsPickable);  // EventMode OnlyNomination=false
        Assert.Equal(45, map.MapTime);  // Inherited from HardZE via base chain
        Assert.Equal(48, map.CooldownSettings.ConfigCooldown);  // CooldownOverride still applies

        // --- EventMode Extra: base map extra + override ---
        var extra = map.ExtraConfiguration;

        // special: overridden by EventMode
        Assert.Equal("grand_finale", extra.GetValue<string>("special", "event_name", ""));
        Assert.True(extra.GetValue<bool>("special", "bonus_rewards", false));

        // difficulty.boss_hp overridden by EventMode (50000 → 100000)
        Assert.Equal(100000L, extra.GetValue<long>("difficulty", "boss_hp", 0));
        // difficulty.level/mode preserved from base
        Assert.Equal(5L, extra.GetValue<long>("difficulty", "level", 0));
        Assert.Equal("hard", extra.GetValue<string>("difficulty", "mode", ""));

        // shop, rewards, gameplay all preserved from base map extra
        Assert.Equal(1000L, extra.GetValue<long>("shop", "cost", 0));
        Assert.Equal("credits", extra.GetValue<string>("shop", "currency", ""));
        Assert.Equal(2L, extra.GetValue<long>("rewards", "xp_multiplier", 0));
        Assert.Equal(1.0, extra.GetValue<double>("gameplay", "speed", 0.0));
        Assert.Equal(800L, extra.GetValue<long>("gameplay", "gravity", 0));
    }

    [Fact]
    public void ComplexRealistic_MapWeekendRelaxOverride()
    {
        var result = LoadAndParse("18_complex_realistic.toml");
        var relax = result.MapConfigsNameMapping["ze_epic_finale_v3"]
            .First(o => o.OverrideConfigName == "WeekendRelax");

        // --- Override metadata ---
        Assert.True(relax.Enabled);
        Assert.False(relax.ForceOverride);
        Assert.Equal(2, relax.OverridePriority);
        Assert.Contains(DayOfWeek.Saturday, relax.TargetDays);
        Assert.Contains(DayOfWeek.Sunday, relax.TargetDays);

        // --- Properties ---
        var map = relax.MapConfig;
        Assert.Equal(10, map.NominationConfig.MinPlayers);  // WeekendRelax override
        Assert.Equal(32, map.NominationConfig.MaxPlayers);  // WeekendRelax override (over Premium's 48)
        Assert.Equal(30, map.MapTime);  // WeekendRelax override (over HardZE's 45)
        Assert.Equal(3, map.MaxExtends);  // Inherited from map base
        Assert.Equal(48, map.CooldownSettings.ConfigCooldown);  // CooldownOverride still applies

        // --- Extra: base map extra + WeekendRelax shop override ---
        Assert.Equal(300L, map.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal("credits", map.ExtraConfiguration.GetValue<string>("shop", "currency", ""));
        // Other sections preserved
        Assert.Equal(50000L, map.ExtraConfiguration.GetValue<long>("difficulty", "boss_hp", 0));
        Assert.Equal("finale_challenge", map.ExtraConfiguration.GetValue<string>("special", "event_name", ""));
    }

    [Fact]
    public void ComplexRealistic_HardZEGroupAndOverrides()
    {
        var result = LoadAndParse("18_complex_realistic.toml");
        var overrides = result.MapGroupSettings["HardZE"];

        // --- Base ---
        var baseGroup = overrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal(2, baseGroup.MaxExtends);
        Assert.Equal(45, baseGroup.MapTime);
        Assert.Equal(72, baseGroup.CooldownSettings.ConfigCooldown);
        Assert.Equal(48, baseGroup.MapCooldownOverride);
        Assert.False(baseGroup.RandomPickConfig.IsPickable);  // OnlyNomination=true
        Assert.Equal(20, baseGroup.NominationConfig.MinPlayers);
        Assert.Equal(3, baseGroup.NominationConfig.DaysAllowed.Count);
        // Base extra
        Assert.Equal(5L, baseGroup.ExtraConfiguration.GetValue<long>("difficulty", "level", 0));
        Assert.Equal("hard", baseGroup.ExtraConfiguration.GetValue<string>("difficulty", "mode", ""));
        Assert.Equal(2L, baseGroup.ExtraConfiguration.GetValue<long>("rewards", "xp_multiplier", 0));

        // --- WeekendPrime override ---
        var weekendPrime = overrides.First(o => o.OverrideConfigName == "WeekendPrime");
        Assert.True(weekendPrime.Enabled);
        Assert.False(weekendPrime.ForceOverride);
        Assert.Equal(2, weekendPrime.OverridePriority);
        Assert.Contains(DayOfWeek.Saturday, weekendPrime.TargetDays);
        Assert.Contains(DayOfWeek.Sunday, weekendPrime.TargetDays);
        Assert.Single(weekendPrime.TargetTimeRanges);

        var wpGroup = weekendPrime.GroupConfig;
        Assert.True(wpGroup.RandomPickConfig.IsPickable);  // OnlyNomination=false
        Assert.Equal(15, wpGroup.NominationConfig.MinPlayers);  // Override
        Assert.Equal(45, wpGroup.MapTime);  // Inherited from HardZE base
        Assert.Equal(2, wpGroup.MaxExtends);  // Inherited
        // Extra: difficulty.level overridden to 4, mode preserved
        Assert.Equal(4L, wpGroup.ExtraConfiguration.GetValue<long>("difficulty", "level", 0));
        Assert.Equal("hard", wpGroup.ExtraConfiguration.GetValue<string>("difficulty", "mode", ""));
        Assert.Equal(2L, wpGroup.ExtraConfiguration.GetValue<long>("rewards", "xp_multiplier", 0));

        // --- WeekdayNight override ---
        var weekdayNight = overrides.First(o => o.OverrideConfigName == "WeekdayNight");
        Assert.True(weekdayNight.Enabled);
        Assert.Equal(1, weekdayNight.OverridePriority);
        Assert.Equal(5, weekdayNight.TargetDays.Count);  // Mon-Fri
        Assert.Single(weekdayNight.TargetTimeRanges);

        var wnGroup = weekdayNight.GroupConfig;
        Assert.Equal(12, wnGroup.NominationConfig.MinPlayers);  // Override
        Assert.False(wnGroup.RandomPickConfig.IsPickable);  // Inherited OnlyNomination=true
        // Extra: rewards.xp_multiplier overridden to 3, difficulty preserved
        Assert.Equal(3L, wnGroup.ExtraConfiguration.GetValue<long>("rewards", "xp_multiplier", 0));
        Assert.Equal(5L, wnGroup.ExtraConfiguration.GetValue<long>("difficulty", "level", 0));
    }

    [Fact]
    public void ComplexRealistic_PremiumGroupAndOverrides()
    {
        var result = LoadAndParse("18_complex_realistic.toml");
        var overrides = result.MapGroupSettings["Premium"];

        // --- Base ---
        var baseGroup = overrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal(48, baseGroup.NominationConfig.MaxPlayers);
        Assert.True(baseGroup.NominationConfig.ProhibitAdminNomination);
        // Defaults inherited for properties Premium doesn't set
        Assert.Equal(3, baseGroup.MaxExtends);
        Assert.Equal(20, baseGroup.MapTime);
        // Base extra
        Assert.Equal(500L, baseGroup.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal("credits", baseGroup.ExtraConfiguration.GetValue<string>("shop", "currency", ""));

        // --- FreeWeekend override (ForceOverride!) ---
        var freeWeekend = overrides.First(o => o.OverrideConfigName == "FreeWeekend");
        Assert.True(freeWeekend.Enabled);
        Assert.True(freeWeekend.ForceOverride);
        Assert.Equal(5, freeWeekend.OverridePriority);
        Assert.Contains(DayOfWeek.Saturday, freeWeekend.TargetDays);
        Assert.Contains(DayOfWeek.Sunday, freeWeekend.TargetDays);

        var fwGroup = freeWeekend.GroupConfig;
        Assert.False(fwGroup.NominationConfig.ProhibitAdminNomination);
        // MaxPlayers inherited from base Premium
        Assert.Equal(48, fwGroup.NominationConfig.MaxPlayers);
        // Extra: shop.cost discounted to 200, currency preserved
        Assert.Equal(200L, fwGroup.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal("credits", fwGroup.ExtraConfiguration.GetValue<string>("shop", "currency", ""));
    }

    [Fact]
    public void ComplexRealistic_LongMapsGroupAndOverrides()
    {
        var result = LoadAndParse("18_complex_realistic.toml");
        var overrides = result.MapGroupSettings["LongMaps"];

        // --- Base ---
        var baseGroup = overrides.First(o => o.OverrideConfigName == IBaseOverrideConfig.BaseConfigName).GroupConfig;
        Assert.Equal(60, baseGroup.MapTime);
        Assert.Equal(20, baseGroup.ExtendTimePerExtends);
        Assert.Equal(4, baseGroup.MaxExtends);
        Assert.Equal(2, baseGroup.MaxExtCommandUses);
        Assert.Equal(48, baseGroup.CooldownSettings.ConfigCooldown);
        Assert.Equal(TimeSpan.FromDays(3), baseGroup.CooldownSettings.TimedCooldown);  // "3d" = 3 days
        Assert.Single(baseGroup.NominationConfig.AllowedTimeRanges);
        // Base extra
        Assert.Equal(1.0, baseGroup.ExtraConfiguration.GetValue<double>("gameplay", "speed", 0.0));
        Assert.Equal(800L, baseGroup.ExtraConfiguration.GetValue<long>("gameplay", "gravity", 0));

        // --- PeakHours override ---
        var peakHours = overrides.First(o => o.OverrideConfigName == "PeakHours");
        Assert.True(peakHours.Enabled);
        Assert.Equal(3, peakHours.OverridePriority);
        Assert.Contains(DayOfWeek.Saturday, peakHours.TargetDays);
        Assert.Contains(DayOfWeek.Sunday, peakHours.TargetDays);

        var phGroup = peakHours.GroupConfig;
        Assert.Equal(30, phGroup.NominationConfig.MinPlayers);
        Assert.Equal(64, phGroup.NominationConfig.MaxPlayers);
        Assert.Equal(60, phGroup.MapTime);  // Inherited from LongMaps base
        // Extra: speed overridden to 1.2, gravity preserved
        Assert.Equal(1.2, phGroup.ExtraConfiguration.GetValue<double>("gameplay", "speed", 0.0));
        Assert.Equal(800L, phGroup.ExtraConfiguration.GetValue<long>("gameplay", "gravity", 0));

        // --- LateNight override ---
        var lateNight = overrides.First(o => o.OverrideConfigName == "LateNight");
        Assert.True(lateNight.Enabled);
        Assert.Equal(1, lateNight.OverridePriority);
        Assert.Equal(3, lateNight.TargetDays.Count);  // Fri, Sat, Sun
        Assert.Contains(DayOfWeek.Friday, lateNight.TargetDays);

        var lnGroup = lateNight.GroupConfig;
        Assert.Equal(8, lnGroup.NominationConfig.MinPlayers);
        Assert.Equal(90, lnGroup.MapTime);  // Override
        Assert.Equal(6, lnGroup.MaxExtends);  // Override
        Assert.Equal(20, lnGroup.ExtendTimePerExtends);  // Inherited from LongMaps base
        // Extra: gravity overridden to 600, speed preserved
        Assert.Equal(1.0, lnGroup.ExtraConfiguration.GetValue<double>("gameplay", "speed", 0.0));
        Assert.Equal(600L, lnGroup.ExtraConfiguration.GetValue<long>("gameplay", "gravity", 0));
    }
}
