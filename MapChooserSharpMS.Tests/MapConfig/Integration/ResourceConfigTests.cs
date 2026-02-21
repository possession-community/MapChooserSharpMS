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
        Assert.Equal(0, a.CooldownConfig.ConfigCooldown);
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
        Assert.Equal(10, config.CooldownConfig.ConfigCooldown);
    }

    // ================================================================
    // 05: Multiple Groups — first group priority + SteamId accumulation
    // ================================================================

    [Fact]
    public void MultipleGroups_FirstGroupPriority_SteamIdsAccumulated()
    {
        var result = LoadAndParse("05_multiple_groups.toml");

        var config = result.MapConfigsNameMapping["ze_multi_group"].First().MapConfig;

        // G1 first priority for RequiredPermissions
        Assert.Equal(["css/root"], config.NominationConfig.RequiredPermissions);
        // G1 first priority for MaxPlayers
        Assert.Equal(1000, config.NominationConfig.MaxPlayers);
        // G2 for MinPlayers (G1 doesn't set it)
        Assert.Equal(300, config.NominationConfig.MinPlayers);
        // G3 for MapTime (G1, G2 don't set it)
        Assert.Equal(40, config.MapTime);

        // AllowedSteamIds accumulated from all groups
        Assert.Contains(111u, config.NominationConfig.AllowedSteamIds);
        Assert.Contains(222u, config.NominationConfig.AllowedSteamIds);
        Assert.Contains(333u, config.NominationConfig.AllowedSteamIds);
        Assert.Equal(3, config.NominationConfig.AllowedSteamIds.Count);

        // DisallowedSteamIds accumulated from all groups
        Assert.Contains(11u, config.NominationConfig.DisallowedSteamIds);
        Assert.Contains(22u, config.NominationConfig.DisallowedSteamIds);
        Assert.Contains(33u, config.NominationConfig.DisallowedSteamIds);
        Assert.Equal(3, config.NominationConfig.DisallowedSteamIds.Count);

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
        Assert.Equal(60, result.MapConfigsNameMapping["map_a"].First().MapConfig.CooldownConfig.ConfigCooldown);

        // map_b: Group_None has no CooldownOverride, map's Cooldown=30 used
        Assert.Equal(30, result.MapConfigsNameMapping["map_b"].First().MapConfig.CooldownConfig.ConfigCooldown);

        // map_c: Group_Zero CooldownOverride=0 (not >0, so not applied), map's Cooldown=30
        Assert.Equal(30, result.MapConfigsNameMapping["map_c"].First().MapConfig.CooldownConfig.ConfigCooldown);

        // map_d: Group_A first (CooldownOverride=60), Group_B second (90) — first wins
        Assert.Equal(60, result.MapConfigsNameMapping["map_d"].First().MapConfig.CooldownConfig.ConfigCooldown);

        // map_e: Group_None skipped (no override), Group_B applies (CooldownOverride=90)
        Assert.Equal(90, result.MapConfigsNameMapping["map_e"].First().MapConfig.CooldownConfig.ConfigCooldown);
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
        var baseOverride = overrides.First(o => o.OverrideConfigName == "");
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
        var baseOverride = overrides.First(o => o.OverrideConfigName == "");
        Assert.Equal(5, baseOverride.GroupConfig.MaxExtends);
        Assert.False(baseOverride.GroupConfig.RandomPickConfig.IsPickable); // OnlyNomination=true
        Assert.Equal(30, baseOverride.GroupConfig.CooldownConfig.ConfigCooldown);

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
        var baseConfig = overrides.First(o => o.OverrideConfigName == "").MapConfig;
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
            result.MapConfigsNameMapping["ze_cd_2d"].First().MapConfig.CooldownConfig.TimedCooldown);

        Assert.Equal(TimeSpan.FromDays(30),
            result.MapConfigsNameMapping["ze_cd_1m"].First().MapConfig.CooldownConfig.TimedCooldown);

        Assert.Equal(TimeSpan.FromDays(7),
            result.MapConfigsNameMapping["ze_cd_7d"].First().MapConfig.CooldownConfig.TimedCooldown);

        Assert.Equal(TimeSpan.Zero,
            result.MapConfigsNameMapping["ze_cd_empty"].First().MapConfig.CooldownConfig.TimedCooldown);

        Assert.Equal(TimeSpan.Zero,
            result.MapConfigsNameMapping["ze_cd_none"].First().MapConfig.CooldownConfig.TimedCooldown);

        Assert.Equal(TimeSpan.Zero,
            result.MapConfigsNameMapping["ze_cd_invalid"].First().MapConfig.CooldownConfig.TimedCooldown);
    }

    // ================================================================
    // 15: SteamId accumulation across all layers
    // ================================================================

    [Fact]
    public void SteamIdAccumulation_AllLayersAccumulated()
    {
        var result = LoadAndParse("15_steamid_accumulation.toml");

        var config = result.MapConfigsNameMapping["ze_steamid_all"].First().MapConfig;

        // AllowedSteamIds: Default[100] + SG1[111,222] + SG2[333] + Map[444]
        Assert.Contains(100u, config.NominationConfig.AllowedSteamIds);
        Assert.Contains(111u, config.NominationConfig.AllowedSteamIds);
        Assert.Contains(222u, config.NominationConfig.AllowedSteamIds);
        Assert.Contains(333u, config.NominationConfig.AllowedSteamIds);
        Assert.Contains(444u, config.NominationConfig.AllowedSteamIds);
        Assert.Equal(5, config.NominationConfig.AllowedSteamIds.Count);

        // DisallowedSteamIds: Default[1000] + SG1[1111,2222] + SG2[3333] + Map[4444]
        Assert.Contains(1000u, config.NominationConfig.DisallowedSteamIds);
        Assert.Contains(1111u, config.NominationConfig.DisallowedSteamIds);
        Assert.Contains(2222u, config.NominationConfig.DisallowedSteamIds);
        Assert.Contains(3333u, config.NominationConfig.DisallowedSteamIds);
        Assert.Contains(4444u, config.NominationConfig.DisallowedSteamIds);
        Assert.Equal(5, config.NominationConfig.DisallowedSteamIds.Count);
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
        Assert.Equal(2, baseOverrides.Count); // base + ComplexWeekend

        var baseConfig = baseOverrides.First(o => o.OverrideConfigName == "").MapConfig;

        // Properties: Map overrides Group overrides Default
        Assert.Equal(7, baseConfig.MaxExtends);       // Map (overrides CG1's 5)
        Assert.Equal(30, baseConfig.MapTime);          // CG2 (CG1 doesn't set, default=20)
        Assert.Equal(10, baseConfig.NominationConfig.MinPlayers); // CG2
        Assert.False(baseConfig.RandomPickConfig.IsPickable);     // CG1 OnlyNomination=true

        // CooldownOverride from CG1 (60) overrides default cooldown
        Assert.Equal(60, baseConfig.CooldownConfig.ConfigCooldown);

        // SteamIds accumulated: CG1[111] + CG2[222] + Map[333]
        Assert.Contains(111u, baseConfig.NominationConfig.AllowedSteamIds);
        Assert.Contains(222u, baseConfig.NominationConfig.AllowedSteamIds);
        Assert.Contains(333u, baseConfig.NominationConfig.AllowedSteamIds);
        Assert.Equal(3, baseConfig.NominationConfig.AllowedSteamIds.Count);

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
        Assert.Equal(60, weekend.MapConfig.CooldownConfig.ConfigCooldown); // CooldownOverride still applies

        // Override extra: base map extra + override shop.cost=30
        Assert.Equal(30L, weekend.MapConfig.ExtraConfiguration.GetValue<long>("shop", "cost", 0));
        Assert.Equal(20L, weekend.MapConfig.ExtraConfiguration.GetValue<long>("shop", "discount", 0)); // Preserved

        // --- Group CG1 DaySettings ---
        var cg1Overrides = result.MapGroupSettings["CG1"];
        Assert.Equal(2, cg1Overrides.Count); // base + CG1Weekend

        var cg1Base = cg1Overrides.First(o => o.OverrideConfigName == "");
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
    public void EdgeCases_UnknownPropsIgnored()
    {
        var result = LoadAndParse("17_edge_cases.toml");

        // Unknown property silently ignored, MaxExtends=5 still parsed
        var unknown = result.MapConfigsNameMapping["ze_unknown_prop"].First().MapConfig;
        Assert.Equal(5, unknown.MaxExtends);
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
    public void EdgeCases_BadSteamIdsIgnored()
    {
        var result = LoadAndParse("17_edge_cases.toml");

        // AllowedSteamIds=["abc"] — non-integer elements skipped → empty list
        var badSteam = result.MapConfigsNameMapping["ze_bad_steam_ids"].First().MapConfig;
        Assert.Empty(badSteam.NominationConfig.AllowedSteamIds);
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
        Assert.Empty(empty.NominationConfig.RequiredPermissions);
        Assert.Empty(empty.NominationConfig.AllowedSteamIds);
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
}
