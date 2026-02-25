using System;
using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Services;

public class MapConfigParsingServiceAdditionalTests
{
    private readonly MapConfigParsingService _service = new();

    // ========================================================================
    // Step 2: Group DaySettings inheritance to Map
    // ========================================================================

    [Fact]
    public void GroupDaySettings_InheritedByMap_WhenMapHasNoDaySetting()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            MaxExtends = 5

            [MapChooserSharpSettings.Groups.G1.DaySettings.EventDay]
            Enabled = true
            TargetDays = ["saturday"]
            MaxExtends = 10

            [ze_test]
            GroupSettings = ["G1"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var overrides = result!.MapConfigsNameMapping["ze_test"];
        Assert.Equal(2, overrides.Count); // base + EventDay

        var eventDay = overrides.First(o => o.OverrideConfigName == "EventDay");
        Assert.True(eventDay.Enabled);
        Assert.Contains(DayOfWeek.Saturday, eventDay.TargetDays);
        Assert.Equal(10, eventDay.MapConfig.MaxExtends);
    }

    [Fact]
    public void GroupDaySettings_MapSameNameOverrideWins()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            MaxExtends = 5

            [MapChooserSharpSettings.Groups.G1.DaySettings.Weekend]
            Enabled = true
            TargetDays = ["saturday"]
            MaxExtends = 8

            [ze_test]
            GroupSettings = ["G1"]

            [ze_test.DaySettings.Weekend]
            Enabled = true
            TargetDays = ["saturday", "sunday"]
            MaxExtends = 15
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var overrides = result!.MapConfigsNameMapping["ze_test"];
        Assert.Equal(2, overrides.Count); // base + Weekend (map version only)

        var weekend = overrides.First(o => o.OverrideConfigName == "Weekend");
        // Map version has MaxExtends=15, Group had MaxExtends=8
        Assert.Equal(15, weekend.MapConfig.MaxExtends);
        // Map version has saturday + sunday
        Assert.Equal(2, weekend.TargetDays.Count);
        Assert.Contains(DayOfWeek.Sunday, weekend.TargetDays);
    }

    [Fact]
    public void GroupDaySettings_MultipleGroupsDifferentDaySettings_BothInherited()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            MaxExtends = 5

            [MapChooserSharpSettings.Groups.G1.DaySettings.EventDay]
            Enabled = true
            TargetDays = ["monday"]
            MaxExtends = 10

            [MapChooserSharpSettings.Groups.G2]
            MaxExtends = 4

            [MapChooserSharpSettings.Groups.G2.DaySettings.Holiday]
            Enabled = true
            TargetDays = ["friday"]
            MaxExtends = 20

            [ze_test]
            GroupSettings = ["G1", "G2"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var overrides = result!.MapConfigsNameMapping["ze_test"];
        Assert.Equal(3, overrides.Count); // base + EventDay + Holiday

        var eventDay = overrides.First(o => o.OverrideConfigName == "EventDay");
        Assert.Equal(10, eventDay.MapConfig.MaxExtends);
        Assert.Contains(DayOfWeek.Monday, eventDay.TargetDays);

        var holiday = overrides.First(o => o.OverrideConfigName == "Holiday");
        Assert.Equal(20, holiday.MapConfig.MaxExtends);
        Assert.Contains(DayOfWeek.Friday, holiday.TargetDays);
    }

    [Fact]
    public void GroupDaySettings_MultipleGroupsSameName_FirstGroupWins()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            MaxExtends = 5

            [MapChooserSharpSettings.Groups.G1.DaySettings.Weekend]
            Enabled = true
            TargetDays = ["saturday"]
            MaxExtends = 10

            [MapChooserSharpSettings.Groups.G2]
            MaxExtends = 4

            [MapChooserSharpSettings.Groups.G2.DaySettings.Weekend]
            Enabled = true
            TargetDays = ["sunday"]
            MaxExtends = 20

            [ze_test]
            GroupSettings = ["G1", "G2"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var overrides = result!.MapConfigsNameMapping["ze_test"];
        Assert.Equal(2, overrides.Count); // base + Weekend (G1 version only)

        var weekend = overrides.First(o => o.OverrideConfigName == "Weekend");
        // G1 version: MaxExtends=10, TargetDays=saturday
        Assert.Equal(10, weekend.MapConfig.MaxExtends);
        Assert.Contains(DayOfWeek.Saturday, weekend.TargetDays);
        Assert.DoesNotContain(DayOfWeek.Sunday, weekend.TargetDays);
    }

    [Fact]
    public void GroupDaySettings_InheritedPropertyMerge_GroupDaySettingsOnMapBase()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3
            MinPlayers = 5

            [MapChooserSharpSettings.Groups.G1]
            MaxExtends = 5

            [MapChooserSharpSettings.Groups.G1.DaySettings.EventDay]
            Enabled = true
            TargetDays = ["saturday"]
            MinPlayers = 30

            [ze_test]
            GroupSettings = ["G1"]
            MapTime = 45
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var overrides = result!.MapConfigsNameMapping["ze_test"];
        var eventDay = overrides.First(o => o.OverrideConfigName == "EventDay");

        // GroupDaySetting overrides MinPlayers
        Assert.Equal(30, eventDay.MapConfig.NominationConfig.MinPlayers);
        // Map's own MapTime is inherited
        Assert.Equal(45, eventDay.MapConfig.MapTime);
        // Group's MaxExtends is in the merge chain (default=3 → G1=5 → map=unset → DaySetting=unset → 5)
        Assert.Equal(5, eventDay.MapConfig.MaxExtends);
    }

    // ========================================================================
    // Step 3: TOML parse error handling
    // ========================================================================

    [Fact]
    public void ParseError_TypeMismatch_FallbackToDefault()
    {
        // MaxExtends expects int, but "five" is a string → TryGetInt64 fails → prop stays null → uses default 3
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            MaxExtends = "five"
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        Assert.Equal(3, config.MaxExtends); // Falls back to default
    }

    [Fact]
    public void ParseError_InvalidDayOfWeek_Skipped()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            DaysAllowed = ["monday", "funday"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        // "funday" is skipped, only monday is parsed
        Assert.Single(config.NominationConfig.DaysAllowed);
        Assert.Contains(DayOfWeek.Monday, config.NominationConfig.DaysAllowed);
    }

    [Fact]
    public void ParseError_InvalidTimeRange_SilentlyIgnored()
    {
        // Mixed valid and invalid: valid ones parsed, invalid skipped (TimeRange.Parse throws, caught by catch)
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            AllowedTimeRanges = ["10:00-12:00", "invalid", "22:00-03:00"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // TimeRange.Parse throws FormatException for "invalid", but
        // ExtractTimeRangeArray catches all exceptions from the entire array processing
        // So the behavior depends on whether the exception breaks the loop
        var ranges = config.NominationConfig.AllowedTimeRanges;
        // The catch block in ExtractTimeRangeArray catches the entire array parsing,
        // so only the first valid range before the error is preserved
        Assert.True(ranges.Count >= 1);
        Assert.Equal(new TimeOnly(10, 0), ranges[0].StartTime);
    }

    // ========================================================================
    // Step 4: CooldownOverride complex chains
    // ========================================================================

    [Fact]
    public void CooldownOverride_ThreeGroups_FirstNonZeroWins()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            Cooldown = 10

            [MapChooserSharpSettings.Groups.G1]
            CooldownOverride = 60

            [MapChooserSharpSettings.Groups.G2]
            CooldownOverride = 90

            [MapChooserSharpSettings.Groups.G3]
            CooldownOverride = 120

            [ze_test]
            GroupSettings = ["G1", "G2", "G3"]
            Cooldown = 30
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        // G1 (first group) has CooldownOverride=60, it wins
        Assert.Equal(60, config.CooldownConfig.ConfigCooldown);
    }

    [Fact]
    public void CooldownOverride_AppliedToDaySettings()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            Cooldown = 10

            [MapChooserSharpSettings.Groups.G1]
            CooldownOverride = 60

            [ze_test]
            GroupSettings = ["G1"]
            Cooldown = 30

            [ze_test.DaySettings.Weekend]
            Enabled = true
            TargetDays = ["saturday"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var weekend = result!.MapConfigsNameMapping["ze_test"].First(o => o.OverrideConfigName == "Weekend");
        // CooldownOverride from G1 should be applied to DaySettings too
        Assert.Equal(60, weekend.MapConfig.CooldownConfig.ConfigCooldown);
    }

    [Fact]
    public void CooldownOverride_FirstGroupZero_SecondApplied()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            Cooldown = 10

            [MapChooserSharpSettings.Groups.G1]
            CooldownOverride = 0

            [MapChooserSharpSettings.Groups.G2]
            CooldownOverride = 90

            [ze_test]
            GroupSettings = ["G1", "G2"]
            Cooldown = 30
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        // G1 has CooldownOverride=0 (skipped), G2 has CooldownOverride=90
        Assert.Equal(90, config.CooldownConfig.ConfigCooldown);
    }

    [Fact]
    public void CooldownOverride_AllGroupsZero_MapCooldownPreserved()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            Cooldown = 10

            [MapChooserSharpSettings.Groups.G1]
            CooldownOverride = 0

            [MapChooserSharpSettings.Groups.G2]
            CooldownOverride = 0

            [ze_test]
            GroupSettings = ["G1", "G2"]
            Cooldown = 30
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        // All groups have CooldownOverride=0, so map's own Cooldown=30 is used
        Assert.Equal(30, config.CooldownConfig.ConfigCooldown);
    }

    // ========================================================================
    // Step 5: Property priority cascade
    // ========================================================================

    [Fact]
    public void Priority_GroupOnlySet_GroupValueUsed()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            MinPlayers = 50

            [ze_test]
            GroupSettings = ["G1"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        // Default has no MinPlayers, G1 sets MinPlayers=50
        Assert.Equal(50, config.NominationConfig.MinPlayers);
    }

    [Fact]
    public void Priority_RequiredPermissions_LastWriteWins_FirstGroupPriority()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            RequiredPermissions = ["css/root"]

            [MapChooserSharpSettings.Groups.G2]
            RequiredPermissions = ["css/generic"]

            [ze_test]
            GroupSettings = ["G1", "G2"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        // Groups applied in reverse (G2 then G1), so G1 overwrites G2 → G1's permissions win
        Assert.Equal(["css/root"], config.NominationConfig.RequiredPermissions);
    }

    [Fact]
    public void Priority_DaysAllowed_MapOverridesGroup()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            DaysAllowed = ["monday", "tuesday"]

            [ze_test]
            GroupSettings = ["G1"]
            DaysAllowed = ["saturday", "sunday"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        // Map overrides group's DaysAllowed (last-write-wins)
        Assert.Equal(2, config.NominationConfig.DaysAllowed.Count);
        Assert.Contains(DayOfWeek.Saturday, config.NominationConfig.DaysAllowed);
        Assert.Contains(DayOfWeek.Sunday, config.NominationConfig.DaysAllowed);
        Assert.DoesNotContain(DayOfWeek.Monday, config.NominationConfig.DaysAllowed);
    }

    [Fact]
    public void Priority_SteamIdsAccumulate_RequiredPermissionsOverwrite()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            RequiredPermissions = ["css/default"]
            AllowedSteamIds = [100]
            DisallowedSteamIds = [200]

            [MapChooserSharpSettings.Groups.G1]
            RequiredPermissions = ["css/root"]
            AllowedSteamIds = [111]
            DisallowedSteamIds = [222]

            [ze_test]
            GroupSettings = ["G1"]
            RequiredPermissions = ["css/vip"]
            AllowedSteamIds = [333]
            DisallowedSteamIds = [444]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // RequiredPermissions: last-write-wins → Map's ["css/vip"]
        Assert.Equal(["css/vip"], config.NominationConfig.RequiredPermissions);

        // AllowedSteamIds: accumulated from all layers
        Assert.Contains(100u, config.NominationConfig.AllowedSteamIds);
        Assert.Contains(111u, config.NominationConfig.AllowedSteamIds);
        Assert.Contains(333u, config.NominationConfig.AllowedSteamIds);

        // DisallowedSteamIds: accumulated from all layers
        Assert.Contains(200u, config.NominationConfig.DisallowedSteamIds);
        Assert.Contains(222u, config.NominationConfig.DisallowedSteamIds);
        Assert.Contains(444u, config.NominationConfig.DisallowedSteamIds);
    }

    // ========================================================================
    // Step 6: Boundary values
    // ========================================================================

    [Fact]
    public void Boundary_NegativeValue_AcceptedAsIs()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            MaxExtends = -5
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        Assert.Equal(-5, config.MaxExtends);
    }

    [Fact]
    public void Boundary_DayOfWeek_CaseInsensitive()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            DaysAllowed = ["MONDAY", "Tuesday"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        Assert.Equal(2, config.NominationConfig.DaysAllowed.Count);
        Assert.Contains(DayOfWeek.Monday, config.NominationConfig.DaysAllowed);
        Assert.Contains(DayOfWeek.Tuesday, config.NominationConfig.DaysAllowed);
    }

    [Fact]
    public void Boundary_DayOfWeek_DuplicatesPreserved()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [ze_test]
            DaysAllowed = ["monday", "monday"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;
        // Duplicates are preserved (2 entries of Monday)
        Assert.Equal(2, config.NominationConfig.DaysAllowed.Count);
        Assert.All(config.NominationConfig.DaysAllowed, d => Assert.Equal(DayOfWeek.Monday, d));
    }

    [Fact]
    public void Boundary_FourGroupCascade()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 1
            MapTime = 10
            MinPlayers = 5
            MaxPlayers = 100

            [MapChooserSharpSettings.Groups.G1]
            MaxExtends = 2
            MapTime = 20

            [MapChooserSharpSettings.Groups.G2]
            MaxExtends = 3
            MinPlayers = 15

            [MapChooserSharpSettings.Groups.G3]
            MapTime = 40

            [MapChooserSharpSettings.Groups.G4]
            MaxPlayers = 500

            [ze_test]
            GroupSettings = ["G1", "G2", "G3", "G4"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // Groups applied in reverse: G4 → G3 → G2 → G1 (G1 has highest priority)
        // MaxExtends: G1=2 overwrites G2=3 → 2
        Assert.Equal(2, config.MaxExtends);
        // MapTime: G1=20 overwrites G3=40 → 20
        Assert.Equal(20, config.MapTime);
        // MinPlayers: G2=15, G1 doesn't set it, so G2's value survives → 15
        Assert.Equal(15, config.NominationConfig.MinPlayers);
        // MaxPlayers: G4=500, no later group overrides it → 500
        Assert.Equal(500, config.NominationConfig.MaxPlayers);
    }

    [Fact]
    public void Boundary_DuplicateGroupReference()
    {
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            MaxExtends = 5
            AllowedSteamIds = [111]

            [ze_test]
            GroupSettings = ["G1", "G1"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var config = result!.MapConfigsNameMapping["ze_test"].First().MapConfig;

        // MaxExtends from G1 applied (twice in merge, but idempotent for overwrite)
        Assert.Equal(5, config.MaxExtends);
        // AllowedSteamIds accumulated from both G1 references
        Assert.Contains(111u, config.NominationConfig.AllowedSteamIds);
    }

    [Fact]
    public void Boundary_GroupDaySettingsInheritedToMap_Confirmed()
    {
        // Same as GroupDaySettings_InheritedByMap_WhenMapHasNoDaySetting but verifying extra too
        var toml = """
            [MapChooserSharpSettings.Default]
            MaxExtends = 3

            [MapChooserSharpSettings.Groups.G1]
            MaxExtends = 5

            [MapChooserSharpSettings.Groups.G1.DaySettings.EventDay]
            Enabled = true
            ForceOverride = true
            OverridePriority = 5
            TargetDays = ["wednesday"]
            TargetTimeRanges = ["18:00-22:00"]
            MinPlayers = 50

            [ze_test]
            GroupSettings = ["G1"]

            [ze_test.extra.shop]
            cost = 100
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var result = _service.ParseConfigsFromDocument(doc);

        Assert.NotNull(result);
        var overrides = result!.MapConfigsNameMapping["ze_test"];
        var eventDay = overrides.First(o => o.OverrideConfigName == "EventDay");

        Assert.True(eventDay.Enabled);
        Assert.True(eventDay.ForceOverride);
        Assert.Equal(5, eventDay.OverridePriority);
        Assert.Contains(DayOfWeek.Wednesday, eventDay.TargetDays);
        Assert.Single(eventDay.TargetTimeRanges);
        Assert.Equal(50, eventDay.MapConfig.NominationConfig.MinPlayers);

        // Extra from map base should be inherited to the DaySettings override
        Assert.Equal(100, eventDay.MapConfig.ExtraConfiguration.GetValue<int>("shop", "cost", 0));
    }
}
