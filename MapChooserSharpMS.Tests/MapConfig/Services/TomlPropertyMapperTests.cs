using System;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.MapConfig.Services;

public class TomlPropertyMapperTests
{
    [Fact]
    public void ExtractProperties_BasicValues_Extracted()
    {
        var toml = """
            [map]
            MapNameAlias = "Test Map"
            MapDescription = "A test map"
            WorkshopId = 12345
            IsDisabled = true
            MaxExtends = 5
            MaxExtCommandUses = 2
            MapTime = 30
            ExtendTimePerExtends = 10
            MapRounds = 8
            ExtendRoundsPerExtends = 3
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.Equal("Test Map", props.MapNameAlias);
        Assert.Equal("A test map", props.MapDescription);
        Assert.Equal(12345L, props.WorkshopId);
        Assert.True(props.IsDisabled);
        Assert.Equal(5, props.MaxExtends);
        Assert.Equal(2, props.MaxExtCommandUses);
        Assert.Equal(30, props.MapTime);
        Assert.Equal(10, props.ExtendTimePerExtends);
        Assert.Equal(8, props.MapRounds);
        Assert.Equal(3, props.ExtendRoundsPerExtends);
    }

    [Fact]
    public void ExtractProperties_NominationSettings_Extracted()
    {
        var toml = """
            [map]
            OnlyNomination = true
            RequiredPermissions = ["css/generic", "css/root"]
            RestrictToAllowedUsersOnly = true
            AllowedSteamIds = [123456789]
            DisallowedSteamIds = [987654321]
            MaxPlayers = 64
            MinPlayers = 10
            ProhibitAdminNomination = true
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.True(props.OnlyNomination);
        Assert.Equal(["css/generic", "css/root"], props.RequiredPermissions);
        Assert.True(props.RestrictToAllowedUsersOnly);
        Assert.NotNull(props.AllowedSteamIds);
        Assert.Single(props.AllowedSteamIds!);
        Assert.Equal(123456789u, props.AllowedSteamIds![0]);
        Assert.NotNull(props.DisallowedSteamIds);
        Assert.Single(props.DisallowedSteamIds!);
        Assert.Equal(987654321u, props.DisallowedSteamIds![0]);
        Assert.Equal(64, props.MaxPlayers);
        Assert.Equal(10, props.MinPlayers);
        Assert.True(props.ProhibitAdminNomination);
    }

    [Fact]
    public void ExtractProperties_DaysAndTimeRanges_Extracted()
    {
        var toml = """
            [map]
            DaysAllowed = ["monday", "wednesday", "friday"]
            AllowedTimeRanges = ["10:00-12:00", "18:00-22:00"]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.NotNull(props.DaysAllowed);
        Assert.Equal(3, props.DaysAllowed!.Count);
        Assert.Equal(DayOfWeek.Monday, props.DaysAllowed[0]);
        Assert.Equal(DayOfWeek.Wednesday, props.DaysAllowed[1]);
        Assert.Equal(DayOfWeek.Friday, props.DaysAllowed[2]);

        Assert.NotNull(props.AllowedTimeRanges);
        Assert.Equal(2, props.AllowedTimeRanges!.Count);
        Assert.Equal(new TimeOnly(10, 0), props.AllowedTimeRanges[0].StartTime);
        Assert.Equal(new TimeOnly(12, 0), props.AllowedTimeRanges[0].EndTime);
    }

    [Fact]
    public void ExtractProperties_CooldownSettings_Extracted()
    {
        var toml = """
            [map]
            Cooldown = 60
            CooldownDateTime = "2d"
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.Equal(60, props.Cooldown);
        Assert.Equal("2d", props.CooldownDateTime);
    }

    [Fact]
    public void ExtractProperties_OverrideSettings_Extracted()
    {
        var toml = """
            [override]
            Enabled = true
            ForceOverride = false
            OverridePriority = 1
            TargetDays = ["saturday", "sunday"]
            TargetTimeRanges = ["18:00-03:00"]
            MaxExtends = 5
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var node = doc.RootNode["override"u8];

        var props = TomlPropertyMapper.ExtractProperties(node);

        Assert.True(props.Enabled);
        Assert.False(props.ForceOverride);
        Assert.Equal(1, props.OverridePriority);
        Assert.NotNull(props.TargetDays);
        Assert.Equal(2, props.TargetDays!.Count);
        Assert.Equal(DayOfWeek.Saturday, props.TargetDays[0]);
        Assert.Equal(DayOfWeek.Sunday, props.TargetDays[1]);
        Assert.NotNull(props.TargetTimeRanges);
        Assert.Single(props.TargetTimeRanges!);
        Assert.Equal(5, props.MaxExtends);
    }

    [Fact]
    public void ExtractProperties_GroupSettings_Extracted()
    {
        var toml = """
            [map]
            GroupSettings = ["Group1", "Group2"]
            CooldownOverride = 60
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.NotNull(props.GroupSettingNames);
        Assert.Equal(["Group1", "Group2"], props.GroupSettingNames);
        Assert.Equal(60, props.CooldownOverride);
    }

    [Fact]
    public void ExtractProperties_SkipsExtraAndDaySettings()
    {
        var toml = """
            [map]
            MaxExtends = 5

            [map.extra.shop]
            cost = 100

            [map.DaySettings.Weekend]
            MaxExtends = 10
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        // Should extract MaxExtends from map level, not from DaySettings
        Assert.Equal(5, props.MaxExtends);
    }

    [Fact]
    public void ExtractProperties_EmptyNode_ReturnsAllNull()
    {
        var toml = """
            [map]
            """;
        var doc = TomlTestHelper.ParseToml(toml);
        var mapNode = doc.RootNode["map"u8];

        var props = TomlPropertyMapper.ExtractProperties(mapNode);

        Assert.Null(props.MapNameAlias);
        Assert.Null(props.MaxExtends);
        Assert.Null(props.IsDisabled);
    }

    [Theory]
    [InlineData("2d", 2)]
    [InlineData("7d", 7)]
    [InlineData("30d", 30)]
    public void ParseCooldownDateTime_Days_ReturnsCorrectTimeSpan(string input, int expectedDays)
    {
        var result = TomlPropertyMapper.ParseCooldownDateTime(input);
        Assert.Equal(TimeSpan.FromDays(expectedDays), result);
    }

    [Theory]
    [InlineData("1m", 30)]
    [InlineData("2m", 60)]
    public void ParseCooldownDateTime_Months_ReturnsCorrectTimeSpan(string input, int expectedDays)
    {
        var result = TomlPropertyMapper.ParseCooldownDateTime(input);
        Assert.Equal(TimeSpan.FromDays(expectedDays), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("x")]
    public void ParseCooldownDateTime_Invalid_ReturnsZero(string? input)
    {
        var result = TomlPropertyMapper.ParseCooldownDateTime(input);
        Assert.Equal(TimeSpan.Zero, result);
    }
}
