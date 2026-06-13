using System.Collections.Generic;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Services;
using MapChooserSharpMS.Modules.Ui.Menu;
using MapChooserSharpMS.Tests.Helpers;
using Xunit;

namespace MapChooserSharpMS.Tests.PluginConfig;

public class PluginConfigParsingServiceTests
{
    private readonly PluginConfigParsingService _service = new();

    #region Full Config Parse

    [Fact]
    public void ParseConfigFromDocument_AllSections_ParsesCorrectly()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/01_full.toml");
        var config = _service.ParseConfigFromDocument(doc);

        // General
        Assert.False(config.GeneralConfig.ShouldUseAliasMapNameIfAvailable);
        Assert.False(config.GeneralConfig.VerboseCooldownPrint);
        Assert.True(config.GeneralConfig.ShouldAutoFixMapName);
        Assert.Equal(RtvMapChangeBehaviourType.Cs2EndMatchScreen, config.GeneralConfig.RtvMapChangeBehaviour);
        Assert.Equal(["3070257939", "1234567890"], config.GeneralConfig.WorkshopCollectionIds);

        // SQL
        Assert.Equal(McsSupportedSqlType.MySQL, config.GeneralConfig.SqlConfig.DataBaseType);
        Assert.Equal("MapChooserSharpTest.db", config.GeneralConfig.SqlConfig.DatabaseName);
        Assert.Equal("localhost", config.GeneralConfig.SqlConfig.Host);
        Assert.Equal("3306", config.GeneralConfig.SqlConfig.Port);
        Assert.Equal("root", config.GeneralConfig.SqlConfig.UserName);

        // MapCycle
        Assert.Equal(5, config.MapCycleConfig.FallbackDefaultMaxExtends);
        Assert.Equal(2, config.MapCycleConfig.FallbackMaxExtCommandUses);
        Assert.Equal(20, config.MapCycleConfig.FallbackExtendTimePerExtends);
        Assert.Equal(10, config.MapCycleConfig.FallbackExtendRoundsPerExtends);
        Assert.True(config.MapCycleConfig.ShouldStopSourceTvRecording);
        Assert.Equal(McsMapConfigExecutionType.StartWithMatch, config.MapCycleConfig.MapConfigExecutionType);
        Assert.Equal("Custom/maps/", config.MapCycleConfig.MapConfigDirectoryPath);
        Assert.Equal("Custom/groups/", config.MapCycleConfig.GroupConfigDirectoryPath);

        // MapVote
        Assert.Equal(McsSupportedMenuType.Default, config.VoteConfig.CurrentMenuType);
        Assert.Equal(8, config.VoteConfig.MaxMenuElements);
        Assert.False(config.VoteConfig.ShouldPrintVoteToChat);
        Assert.False(config.VoteConfig.ShouldPrintVoteRemainingTime);
        Assert.Equal(McsCountdownUiType.CenterHud, config.VoteConfig.CurrentCountdownUiType);

        // VoteSound
        Assert.Equal("soundevents/soundevents_mapchooser.vsndevts", config.VoteConfig.VoteSoundConfig.VSndEvtsSoundFilePath);
        Assert.Equal("countdown_start", config.VoteConfig.VoteSoundConfig.InitialVoteSounds.VoteCountdownStartSound);
        Assert.Equal("vote_start", config.VoteConfig.VoteSoundConfig.InitialVoteSounds.VoteStartSound);
        Assert.Equal("vote_finish", config.VoteConfig.VoteSoundConfig.InitialVoteSounds.VoteFinishSound);
        Assert.Equal("runoff_countdown_start", config.VoteConfig.VoteSoundConfig.RunoffVoteSounds.VoteCountdownStartSound);

        // Nomination
        Assert.Equal(McsSupportedMenuType.Default, config.NominationConfig.CurrentMenuType);
    }

    #endregion

    #region Empty / Default Config

    [Fact]
    public void ParseConfigFromDocument_EmptyToml_ReturnsDefaults()
    {
        var doc = TomlTestHelper.ParseToml("");
        var config = _service.ParseConfigFromDocument(doc);

        // General defaults
        Assert.True(config.GeneralConfig.ShouldUseAliasMapNameIfAvailable);
        Assert.True(config.GeneralConfig.VerboseCooldownPrint);
        Assert.Empty(config.GeneralConfig.WorkshopCollectionIds);
        Assert.True(config.GeneralConfig.ShouldAutoFixMapName);
        Assert.Equal(RtvMapChangeBehaviourType.ImmediatelyWithTime, config.GeneralConfig.RtvMapChangeBehaviour);

        // SQL defaults
        Assert.Equal(McsSupportedSqlType.Sqlite, config.GeneralConfig.SqlConfig.DataBaseType);
        Assert.Equal("MapChooserSharp.db", config.GeneralConfig.SqlConfig.DatabaseName);
        Assert.Equal("", config.GeneralConfig.SqlConfig.Host);
        Assert.Equal("", config.GeneralConfig.SqlConfig.Port);
        Assert.Equal("", config.GeneralConfig.SqlConfig.UserName);

        // MapCycle defaults
        Assert.Equal(3, config.MapCycleConfig.FallbackDefaultMaxExtends);
        Assert.Equal(1, config.MapCycleConfig.FallbackMaxExtCommandUses);
        Assert.Equal(15, config.MapCycleConfig.FallbackExtendTimePerExtends);
        Assert.Equal(5, config.MapCycleConfig.FallbackExtendRoundsPerExtends);
        Assert.False(config.MapCycleConfig.ShouldStopSourceTvRecording);
        Assert.Equal(McsMapConfigExecutionType.ExactMatch, config.MapCycleConfig.MapConfigExecutionType);
        Assert.Equal("maps/", config.MapCycleConfig.MapConfigDirectoryPath);
        Assert.Equal("groups/", config.MapCycleConfig.GroupConfigDirectoryPath);

        // MapVote defaults
        Assert.Equal(McsSupportedMenuType.Default, config.VoteConfig.CurrentMenuType);
        Assert.Equal(5, config.VoteConfig.MaxMenuElements);
        Assert.True(config.VoteConfig.ShouldPrintVoteToChat);
        Assert.True(config.VoteConfig.ShouldPrintVoteRemainingTime);
        Assert.Equal(McsCountdownUiType.CenterHtml, config.VoteConfig.CurrentCountdownUiType);

        // VoteSound defaults
        Assert.Equal("", config.VoteConfig.VoteSoundConfig.VSndEvtsSoundFilePath);
        Assert.Equal("", config.VoteConfig.VoteSoundConfig.InitialVoteSounds.VoteCountdownStartSound);
        Assert.Equal("", config.VoteConfig.VoteSoundConfig.RunoffVoteSounds.VoteCountdownStartSound);

        // Nomination defaults
        Assert.Equal(McsSupportedMenuType.Default, config.NominationConfig.CurrentMenuType);
    }

    #endregion

    #region Enum Parsing

    [Fact]
    public void ParseConfigFromDocument_RtvMapChangeBehaviour_ParsesCorrectly()
    {
        var cases = new (string Value, RtvMapChangeBehaviourType Expected)[]
        {
            ("ImmediatelyWithTime", RtvMapChangeBehaviourType.ImmediatelyWithTime),
            ("Cs2EndMatchScreen", RtvMapChangeBehaviourType.Cs2EndMatchScreen),
            ("immediatelywithtime", RtvMapChangeBehaviourType.ImmediatelyWithTime),
        };

        foreach (var (value, expected) in cases)
        {
            var toml = $"""
                [General]
                RtvMapChangeBehaviour = "{value}"
                """;
            var config = _service.ParseConfigFromDocument(TomlTestHelper.ParseToml(toml));
            Assert.Equal(expected, config.GeneralConfig.RtvMapChangeBehaviour);
        }
    }

    [Fact]
    public void ParseConfigFromDocument_InvalidRtvBehaviour_FallsBackToDefault()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/02_invalid_rtv_behaviour.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(RtvMapChangeBehaviourType.ImmediatelyWithTime, config.GeneralConfig.RtvMapChangeBehaviour);
    }

    [Fact]
    public void ParseConfigFromDocument_MapConfigExecutionType_ParsesCorrectly()
    {
        var cases = new (string Value, McsMapConfigExecutionType Expected)[]
        {
            ("ExactMatch", McsMapConfigExecutionType.ExactMatch),
            ("StartWithMatch", McsMapConfigExecutionType.StartWithMatch),
            ("PartialMatch", McsMapConfigExecutionType.PartialMatch),
        };

        foreach (var (value, expected) in cases)
        {
            var toml = $"""
                [MapCycle]
                MapConfigExecutionType = "{value}"
                """;
            var config = _service.ParseConfigFromDocument(TomlTestHelper.ParseToml(toml));
            Assert.Equal(expected, config.MapCycleConfig.MapConfigExecutionType);
        }
    }

    [Fact]
    public void ParseConfigFromDocument_InvalidExecutionType_FallsBackToDefault()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/03_invalid_execution_type.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(McsMapConfigExecutionType.ExactMatch, config.MapCycleConfig.MapConfigExecutionType);
    }

    [Fact]
    public void ParseConfigFromDocument_SqlType_ParsesCorrectly()
    {
        var cases = new (string Value, McsSupportedSqlType Expected)[]
        {
            ("Sqlite", McsSupportedSqlType.Sqlite),
            ("sqlite", McsSupportedSqlType.Sqlite),
            ("MySQL", McsSupportedSqlType.MySQL),
            ("PostgreSQL", McsSupportedSqlType.PostgreSQL),
        };

        foreach (var (value, expected) in cases)
        {
            var toml = $"""
                [General.Sql]
                Type = "{value}"
                """;
            var config = _service.ParseConfigFromDocument(TomlTestHelper.ParseToml(toml));
            Assert.Equal(expected, config.GeneralConfig.SqlConfig.DataBaseType);
        }
    }

    [Fact]
    public void ParseConfigFromDocument_InvalidSqlType_FallsBackToSqlite()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/04_invalid_sql_type.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(McsSupportedSqlType.Sqlite, config.GeneralConfig.SqlConfig.DataBaseType);
    }

    #endregion

    #region MenuType / CountdownUiType

    [Fact]
    public void ParseConfigFromDocument_MenuType_Default_Parsed()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/05_menu_type_default.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(McsSupportedMenuType.Default, config.VoteConfig.CurrentMenuType);
    }

    [Fact]
    public void ParseConfigFromDocument_InvalidMenuType_FallsBackToDefault()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/06_invalid_menu_type.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(McsSupportedMenuType.Default, config.VoteConfig.CurrentMenuType);
    }

    [Theory]
    [InlineData("None", McsCountdownUiType.None)]
    [InlineData("CenterHud", McsCountdownUiType.CenterHud)]
    [InlineData("CenterAlert", McsCountdownUiType.CenterAlert)]
    [InlineData("CenterHtml", McsCountdownUiType.CenterHtml)]
    [InlineData("Chat", McsCountdownUiType.Chat)]
    public void ParseConfigFromDocument_CountdownUiType_ParsesCorrectly(string value, McsCountdownUiType expected)
    {
        var toml = $"""
            [MapVote]
            CountdownUiType = "{value}"
            """;
        var config = _service.ParseConfigFromDocument(TomlTestHelper.ParseToml(toml));
        Assert.Equal(expected, config.VoteConfig.CurrentCountdownUiType);
    }

    [Fact]
    public void ParseConfigFromDocument_InvalidCountdownUiType_FallsBackToCenterHtml()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/07_invalid_countdown_ui.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(McsCountdownUiType.CenterHtml, config.VoteConfig.CurrentCountdownUiType);
    }

    [Fact]
    public void ParseConfigFromDocument_NominationMenuType_Default()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/08_nomination_menu_type.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(McsSupportedMenuType.Default, config.NominationConfig.CurrentMenuType);
    }

    #endregion

    #region VoteSound

    [Fact]
    public void ParseConfigFromDocument_VoteSound_InitialAndRunoff_Parsed()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/09_vote_sound.toml");
        var config = _service.ParseConfigFromDocument(doc);
        var soundConfig = config.VoteConfig.VoteSoundConfig;

        Assert.Equal("soundevents/test.vsndevts", soundConfig.VSndEvtsSoundFilePath);

        // Initial sounds
        Assert.Equal("initial_countdown_start", soundConfig.InitialVoteSounds.VoteCountdownStartSound);
        Assert.Equal("initial_start", soundConfig.InitialVoteSounds.VoteStartSound);
        Assert.Equal("initial_finish", soundConfig.InitialVoteSounds.VoteFinishSound);
        Assert.Equal(10, soundConfig.InitialVoteSounds.VoteCountdownSounds.Count);
        Assert.Equal("tick1", soundConfig.InitialVoteSounds.VoteCountdownSounds[0]);
        Assert.Equal("tick2", soundConfig.InitialVoteSounds.VoteCountdownSounds[1]);
        Assert.Equal("tick3", soundConfig.InitialVoteSounds.VoteCountdownSounds[2]);
        Assert.Equal("tick10", soundConfig.InitialVoteSounds.VoteCountdownSounds[9]);

        // Runoff sounds
        Assert.Equal("runoff_countdown_start", soundConfig.RunoffVoteSounds.VoteCountdownStartSound);
        Assert.Equal("runoff_start", soundConfig.RunoffVoteSounds.VoteStartSound);
        Assert.Equal("runoff_finish", soundConfig.RunoffVoteSounds.VoteFinishSound);
        Assert.Equal(10, soundConfig.RunoffVoteSounds.VoteCountdownSounds.Count);
        Assert.Equal("rtick1", soundConfig.RunoffVoteSounds.VoteCountdownSounds[0]);
        Assert.Equal("rtick10", soundConfig.RunoffVoteSounds.VoteCountdownSounds[9]);
    }

    [Fact]
    public void ParseConfigFromDocument_VoteSound_EmptySounds_AllDefaultToEmpty()
    {
        var doc = TomlTestHelper.ParseToml("");
        var config = _service.ParseConfigFromDocument(doc);
        var soundConfig = config.VoteConfig.VoteSoundConfig;

        Assert.Equal("", soundConfig.VSndEvtsSoundFilePath);
        Assert.Equal("", soundConfig.InitialVoteSounds.VoteCountdownStartSound);
        Assert.Equal("", soundConfig.InitialVoteSounds.VoteStartSound);
        Assert.Equal("", soundConfig.InitialVoteSounds.VoteFinishSound);
        Assert.Equal(10, soundConfig.InitialVoteSounds.VoteCountdownSounds.Count);
        Assert.All(soundConfig.InitialVoteSounds.VoteCountdownSounds, s => Assert.Equal("", s));

        Assert.Equal("", soundConfig.RunoffVoteSounds.VoteCountdownStartSound);
        Assert.Equal(10, soundConfig.RunoffVoteSounds.VoteCountdownSounds.Count);
        Assert.All(soundConfig.RunoffVoteSounds.VoteCountdownSounds, s => Assert.Equal("", s));
    }

    #endregion

    #region WorkshopCollectionIds

    [Fact]
    public void ParseConfigFromDocument_WorkshopCollectionIds_EmptyArray()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/10_workshop_ids_empty.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Empty(config.GeneralConfig.WorkshopCollectionIds);
    }

    [Fact]
    public void ParseConfigFromDocument_WorkshopCollectionIds_MultipleIds()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/11_workshop_ids_multiple.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(3, config.GeneralConfig.WorkshopCollectionIds.Length);
        Assert.Equal("111", config.GeneralConfig.WorkshopCollectionIds[0]);
        Assert.Equal("222", config.GeneralConfig.WorkshopCollectionIds[1]);
        Assert.Equal("333", config.GeneralConfig.WorkshopCollectionIds[2]);
    }

    #endregion

    #region SQL Config

    [Fact]
    public void ParseConfigFromDocument_SqlConfig_FullyParsed()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/12_sql_full.toml");
        var config = _service.ParseConfigFromDocument(doc);
        var sql = config.GeneralConfig.SqlConfig;

        Assert.Equal(McsSupportedSqlType.PostgreSQL, sql.DataBaseType);
        Assert.Equal("my_db", sql.DatabaseName);
        Assert.Equal("192.168.1.1", sql.Host);
        Assert.Equal("5432", sql.Port);
        Assert.Equal("admin", sql.UserName);
        // Password is stored as SecureString, verify it's not null
        Assert.NotNull(sql.Password);
    }

    #endregion

    #region VoteSoundConfig .vsndevts Validation

    [Fact]
    public void ParseConfigFromDocument_SoundFile_ValidVsndevts_Kept()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/13_sound_valid.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal("soundevents/test.vsndevts", config.VoteConfig.VoteSoundConfig.VSndEvtsSoundFilePath);
    }

    [Fact]
    public void ParseConfigFromDocument_SoundFile_InvalidExtension_ClearedToEmpty()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/14_sound_invalid_ext.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal("", config.VoteConfig.VoteSoundConfig.VSndEvtsSoundFilePath);
    }

    [Fact]
    public void ParseConfigFromDocument_SoundFile_Empty_StaysEmpty()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/15_sound_empty.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal("", config.VoteConfig.VoteSoundConfig.VSndEvtsSoundFilePath);
    }

    #endregion

    #region AvailableMenuTypes

    [Fact]
    public void ParseConfigFromDocument_AvailableMenuTypes_ContainsDefault()
    {
        var doc = TomlTestHelper.ParseToml("");
        var config = _service.ParseConfigFromDocument(doc);

        Assert.Single(config.VoteConfig.AvailableMenuTypes);
        Assert.Equal(McsSupportedMenuType.Default, config.VoteConfig.AvailableMenuTypes[0]);

        Assert.Single(config.NominationConfig.AvailableMenuTypes);
        Assert.Equal(McsSupportedMenuType.Default, config.NominationConfig.AvailableMenuTypes[0]);
    }

    #endregion

    #region Partial Config

    [Fact]
    public void ParseConfigFromDocument_OnlyGeneralSection_OtherSectionsGetDefaults()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/16_only_general.toml");
        var config = _service.ParseConfigFromDocument(doc);

        Assert.False(config.GeneralConfig.ShouldUseAliasMapNameIfAvailable);

        // Other sections should still have defaults
        Assert.Equal(3, config.MapCycleConfig.FallbackDefaultMaxExtends);
        Assert.Equal(5, config.VoteConfig.MaxMenuElements);
        Assert.Equal(McsSupportedMenuType.Default, config.NominationConfig.CurrentMenuType);
    }

    [Fact]
    public void ParseConfigFromDocument_OnlyMapCycleSection_OtherSectionsGetDefaults()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/17_only_mapcycle.toml");
        var config = _service.ParseConfigFromDocument(doc);

        Assert.Equal(10, config.MapCycleConfig.FallbackDefaultMaxExtends);
        Assert.True(config.MapCycleConfig.ShouldStopSourceTvRecording);

        // Other sections should still have defaults
        Assert.True(config.GeneralConfig.ShouldUseAliasMapNameIfAvailable);
        Assert.Equal(5, config.VoteConfig.MaxMenuElements);
    }

    #endregion
}
