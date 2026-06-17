using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Services;
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
        Assert.Equal(8, config.VoteConfig.MaxMenuElements);
        Assert.False(config.VoteConfig.ShouldPrintVoteToChat);
        Assert.False(config.VoteConfig.ShouldPrintVoteRemainingTime);
        Assert.Equal(McsCountdownUiType.Hint, config.VoteConfig.CurrentCountdownUiType);

        // VoteSound
        Assert.Equal("soundevents/soundevents_mapchooser.vsndevts", config.VoteConfig.VoteSoundConfig.VSndEvtsSoundFilePath);
        Assert.Equal("countdown_start", config.VoteConfig.VoteSoundConfig.InitialVoteSounds.VoteCountdownStartSound);
        Assert.Equal("vote_start", config.VoteConfig.VoteSoundConfig.InitialVoteSounds.VoteStartSound);
        Assert.Equal("vote_finish", config.VoteConfig.VoteSoundConfig.InitialVoteSounds.VoteFinishSound);
        Assert.Equal("runoff_countdown_start", config.VoteConfig.VoteSoundConfig.RunoffVoteSounds.VoteCountdownStartSound);

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
        Assert.Equal(5, config.VoteConfig.MaxMenuElements);
        Assert.True(config.VoteConfig.ShouldPrintVoteToChat);
        Assert.True(config.VoteConfig.ShouldPrintVoteRemainingTime);
        Assert.Equal(McsCountdownUiType.Center, config.VoteConfig.CurrentCountdownUiType);

        // VoteSound defaults
        Assert.Equal("", config.VoteConfig.VoteSoundConfig.VSndEvtsSoundFilePath);
        Assert.Equal("", config.VoteConfig.VoteSoundConfig.InitialVoteSounds.VoteCountdownStartSound);
        Assert.Equal("", config.VoteConfig.VoteSoundConfig.RunoffVoteSounds.VoteCountdownStartSound);
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

    #endregion

    #region CountdownUiType

    [Theory]
    [InlineData("None", McsCountdownUiType.None)]
    [InlineData("Hint", McsCountdownUiType.Hint)]
    [InlineData("Center", McsCountdownUiType.Center)]
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
    public void ParseConfigFromDocument_InvalidCountdownUiType_FallsBackToCenter()
    {
        var doc = TomlTestHelper.LoadToml("PluginConfig/07_invalid_countdown_ui.toml");
        var config = _service.ParseConfigFromDocument(doc);
        Assert.Equal(McsCountdownUiType.Center, config.VoteConfig.CurrentCountdownUiType);
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
