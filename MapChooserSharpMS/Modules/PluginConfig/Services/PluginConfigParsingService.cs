using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CsToml;
using CsToml.Extensions;
using CsToml.Values;
using MapChooserSharp.Modules.MapVote.Countdown;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Models;
using MapChooserSharpMS.Modules.Ui.Menu;

namespace MapChooserSharpMS.Modules.PluginConfig.Services;

internal sealed class PluginConfigParsingService : IPluginConfigParsingService
{
    public IMcsPluginConfig ParseConfig(string configFilePath)
    {
        var doc = CsTomlFileSerializer.Deserialize<TomlDocument>(configFilePath);
        return ParseConfigFromDocument(doc);
    }

    internal IMcsPluginConfig ParseConfigFromDocument(TomlDocument doc)
    {
        var root = doc.RootNode;

        var generalConfig = ParseGeneralConfig(root);
        var mapCycleConfig = ParseMapCycleConfig(root);
        var voteConfig = ParseVoteConfig(root);
        var nominationConfig = ParseNominationConfig(root);

        return new Models.PluginConfig(voteConfig, nominationConfig, mapCycleConfig, generalConfig);
    }

    private GeneralConfig ParseGeneralConfig(TomlDocumentNode root)
    {
        var generalNode = TryGetSection(root, "General"u8);

        bool shouldUseAlias = GetBool(generalNode, "ShouldUseAliasMapNameIfAvailable"u8, true);
        bool verboseCooldown = GetBool(generalNode, "VerboseCooldownPrint"u8, true);
        string[] workshopCollectionIds = GetStringArray(generalNode, "WorkshopCollectionIds"u8);
        bool shouldAutoFix = GetBool(generalNode, "ShouldAutoFixMapName"u8, true);
        var rtvBehaviour = GetEnum(generalNode, "RtvMapChangeBehaviour"u8, RtvMapChangeBehaviourType.ImmediatelyWithTime);

        var sqlConfig = ParseSqlConfig(generalNode);

        return new GeneralConfig(shouldUseAlias, verboseCooldown, workshopCollectionIds, shouldAutoFix, sqlConfig, rtvBehaviour);
    }

    private SqlConfig ParseSqlConfig(TomlDocumentNode generalNode)
    {
        var sqlNode = TryGetSection(generalNode, "Sql"u8);

        var dbType = GetEnum(sqlNode, "Type"u8, McsSupportedSqlType.Sqlite);
        string databaseName = GetString(sqlNode, "DatabaseName"u8, "MapChooserSharp.db");
        string host = GetString(sqlNode, "Address"u8, "");
        string port = GetString(sqlNode, "Port"u8, "");
        string user = GetString(sqlNode, "User"u8, "");
        string password = GetString(sqlNode, "Password"u8, "");

        return new SqlConfig(host, port, databaseName, user, ref password, dbType);
    }

    private McsMapCycleConfig ParseMapCycleConfig(TomlDocumentNode root)
    {
        var cycleNode = TryGetSection(root, "MapCycle"u8);

        int fallbackMaxExtends = GetInt(cycleNode, "FallbackMaxExtends"u8, 3);
        int fallbackMaxExtCommandUses = GetInt(cycleNode, "FallbackMaxExtCommandUses"u8, 1);
        int fallbackExtendTime = GetInt(cycleNode, "FallbackExtendTimePerExtends"u8, 15);
        int fallbackExtendRounds = GetInt(cycleNode, "FallbackExtendRoundsPerExtends"u8, 5);
        bool shouldStopSourceTv = GetBool(cycleNode, "ShouldStopSourceTvRecording"u8, false);
        var executionType = GetEnum(cycleNode, "MapConfigExecutionType"u8, McsMapConfigExecutionType.ExactMatch);
        string mapConfigDir = GetString(cycleNode, "MapConfigDirectoryPath"u8, "MapChooserSharp/maps/");
        string groupConfigDir = GetString(cycleNode, "GroupConfigDirectoryPath"u8, "MapChooserSharp/groups/");

        return new McsMapCycleConfig(
            fallbackMaxExtends, fallbackMaxExtCommandUses,
            fallbackExtendTime, fallbackExtendRounds,
            shouldStopSourceTv, executionType,
            mapConfigDir, groupConfigDir);
    }

    private VoteConfig ParseVoteConfig(TomlDocumentNode root)
    {
        var voteNode = TryGetSection(root, "MapVote"u8);

        var menuType = GetEnum(voteNode, "MenuType"u8, McsSupportedMenuType.Default);
        int maxVoteElements = GetInt(voteNode, "MaxVoteElements"u8, 5);
        bool shouldPrintVote = GetBool(voteNode, "ShouldPrintVoteToChat"u8, true);
        bool shouldPrintRemaining = GetBool(voteNode, "ShouldPrintVoteRemainingTime"u8, true);
        var countdownUiType = GetEnum(voteNode, "CountdownUiType"u8, McsCountdownUiType.CenterHtml);

        var voteSoundConfig = ParseVoteSoundConfig(voteNode);

        // AvailableMenuTypes is hardcoded to [Default]
        var availableMenuTypes = new List<McsSupportedMenuType> { McsSupportedMenuType.Default };

        return new VoteConfig(
            availableMenuTypes, menuType,
            maxVoteElements, shouldPrintVote, shouldPrintRemaining,
            voteSoundConfig, countdownUiType);
    }

    private VoteSoundConfig ParseVoteSoundConfig(TomlDocumentNode voteNode)
    {
        var soundNode = TryGetSection(voteNode, "Sound"u8);

        string soundFile = GetString(soundNode, "SoundFile"u8, "");

        var initialSounds = ParseVoteSound(soundNode, "Initial");
        var runoffSounds = ParseVoteSound(soundNode, "Runoff");

        return new VoteSoundConfig(soundFile, initialSounds, runoffSounds);
    }

    private VoteSound ParseVoteSound(TomlDocumentNode soundNode, string prefix)
    {
        string countdownStartSound = GetString(soundNode, Encoding.UTF8.GetBytes($"{prefix}VoteCountdownStartSound"), "");
        string voteStartSound = GetString(soundNode, Encoding.UTF8.GetBytes($"{prefix}VoteStartSound"), "");
        string voteFinishSound = GetString(soundNode, Encoding.UTF8.GetBytes($"{prefix}VoteFinishSound"), "");

        var countdownSounds = new List<string>(10);
        for (int i = 1; i <= 10; i++)
        {
            string sound = GetString(soundNode, Encoding.UTF8.GetBytes($"{prefix}VoteCountdownSound{i}"), "");
            countdownSounds.Add(sound);
        }

        return new VoteSound(countdownStartSound, voteStartSound, voteFinishSound, countdownSounds);
    }

    private NominationConfig ParseNominationConfig(TomlDocumentNode root)
    {
        var nominationNode = TryGetSection(root, "Nomination"u8);

        var menuType = GetEnum(nominationNode, "MenuType"u8, McsSupportedMenuType.Default);
        var availableMenuTypes = new List<McsSupportedMenuType> { McsSupportedMenuType.Default };

        return new NominationConfig(availableMenuTypes, menuType);
    }

    #region Helpers

    private static TomlDocumentNode TryGetSection(TomlDocumentNode parent, ReadOnlySpan<byte> key)
    {
        try
        {
            var node = parent[key];
            try
            {
                if (node.HasValue)
                    return node;
            }
            catch
            {
                // default struct — HasValue throws
            }
        }
        catch
        {
            // key not found
        }
        return default;
    }

    private static string GetString(TomlDocumentNode node, ReadOnlySpan<byte> key, string defaultValue)
    {
        try
        {
            var child = node[key];
            try
            {
                if (!child.HasValue)
                    return defaultValue;
            }
            catch
            {
                return defaultValue;
            }

            if (child.TryGetString(out var value))
                return value;
        }
        catch
        {
            // default struct or key not found
        }
        return defaultValue;
    }

    private static int GetInt(TomlDocumentNode node, ReadOnlySpan<byte> key, int defaultValue)
    {
        try
        {
            var child = node[key];
            try
            {
                if (!child.HasValue)
                    return defaultValue;
            }
            catch
            {
                return defaultValue;
            }

            if (child.TryGetInt64(out var value))
                return (int)value;
        }
        catch
        {
            // default struct or key not found
        }
        return defaultValue;
    }

    private static bool GetBool(TomlDocumentNode node, ReadOnlySpan<byte> key, bool defaultValue)
    {
        try
        {
            var child = node[key];
            try
            {
                if (!child.HasValue)
                    return defaultValue;
            }
            catch
            {
                return defaultValue;
            }

            if (child.TryGetBool(out var value))
                return value;
        }
        catch
        {
            // default struct or key not found
        }
        return defaultValue;
    }

    private static string[] GetStringArray(TomlDocumentNode node, ReadOnlySpan<byte> key)
    {
        try
        {
            var child = node[key];
            try
            {
                if (!child.HasValue)
                    return [];
            }
            catch
            {
                return [];
            }

            if (child.TryGetArray(out ReadOnlyCollection<TomlValue>? arr) && arr != null)
            {
                var result = new List<string>(arr.Count);
                foreach (var item in arr)
                {
                    if (item.TryGetString(out var str))
                        result.Add(str);
                }
                return result.ToArray();
            }
        }
        catch
        {
            // default struct or key not found
        }
        return [];
    }

    private static T GetEnum<T>(TomlDocumentNode node, ReadOnlySpan<byte> key, T defaultValue) where T : struct, Enum
    {
        string str = GetString(node, key, "");
        if (string.IsNullOrEmpty(str))
            return defaultValue;

        if (Enum.TryParse<T>(str, ignoreCase: true, out var result))
            return result;

        return defaultValue;
    }

    #endregion
}
