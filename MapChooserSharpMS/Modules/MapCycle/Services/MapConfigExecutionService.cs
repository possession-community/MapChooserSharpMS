using System;
using System.Collections.Generic;
using System.IO;
using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.Logging;
using Sharp.Shared;

namespace MapChooserSharpMS.Modules.MapCycle.Services;

internal sealed class MapConfigExecutionService
{
    private const string CfgBasePath = "mcsms";
    private const string MapCfgSubDir = "maps";
    private const string GroupCfgSubDir = "groups";

    private readonly ISharedSystem _sharedSystem;
    private readonly ILogger _logger;
    private readonly string _mapCfgDirectory;
    private readonly string _groupCfgDirectory;
    private readonly Func<McsMapConfigExecutionType> _executionTypeProvider;

    public MapConfigExecutionService(
        ISharedSystem sharedSystem,
        ILogger logger,
        Func<McsMapConfigExecutionType> executionTypeProvider)
    {
        _sharedSystem = sharedSystem;
        _logger = logger;
        _executionTypeProvider = executionTypeProvider;

        string gamePath = sharedSystem.GetModSharp().GetGamePath();
        _mapCfgDirectory = Path.Combine(gamePath, "cfg", CfgBasePath, MapCfgSubDir);
        _groupCfgDirectory = Path.Combine(gamePath, "cfg", CfgBasePath, GroupCfgSubDir);
    }

    public void ExecuteConfigsForMap(IMapConfig mapConfig)
    {
        _logger.LogInformation(
            "[MapConfigExec] Starting cfg execution for map={Map}, groups={Groups}",
            mapConfig.MapName, mapConfig.GroupSettings.Count);

        ExecuteGroupConfigs(mapConfig);
        ExecuteMapConfigs(mapConfig.MapName, _executionTypeProvider());
    }

    private void ExecuteGroupConfigs(IMapConfig mapConfig)
    {
        if (!Directory.Exists(_groupCfgDirectory))
            return;

        string[] cfgFiles;
        try
        {
            cfgFiles = Directory.GetFiles(_groupCfgDirectory, "*.cfg", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MapConfigExec] Failed to scan group cfg directory: {Dir}", _groupCfgDirectory);
            return;
        }

        foreach (var group in mapConfig.GroupSettings)
        {
            foreach (string filePath in cfgFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                if (string.Equals(fileName, group.GroupName, StringComparison.OrdinalIgnoreCase))
                {
                    Exec(ToExecPath(_groupCfgDirectory, GroupCfgSubDir, filePath), $"group:{group.GroupName}");
                }
            }
        }
    }

    private void ExecuteMapConfigs(string mapName, McsMapConfigExecutionType executionType)
    {
        if (!Directory.Exists(_mapCfgDirectory))
            return;

        string[] cfgFiles;
        try
        {
            cfgFiles = Directory.GetFiles(_mapCfgDirectory, "*.cfg", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MapConfigExec] Failed to scan map cfg directory: {Dir}", _mapCfgDirectory);
            return;
        }

        var matched = MatchCfgFiles(cfgFiles, mapName, executionType);
        foreach (string cfgPath in matched)
        {
            Exec(ToExecPath(_mapCfgDirectory, MapCfgSubDir, cfgPath), $"map:{mapName}");
        }
    }

    private static string ToExecPath(string baseDirectory, string subDir, string filePath)
    {
        string relativePath = Path.GetRelativePath(baseDirectory, filePath).Replace('\\', '/');
        return $"{CfgBasePath}/{subDir}/{relativePath}";
    }

    internal static List<string> MatchCfgFiles(string[] cfgFiles, string mapName, McsMapConfigExecutionType executionType)
    {
        var results = new List<string>();

        foreach (string filePath in cfgFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            bool isMatch = executionType switch
            {
                McsMapConfigExecutionType.ExactMatch =>
                    string.Equals(fileName, mapName, StringComparison.OrdinalIgnoreCase),
                McsMapConfigExecutionType.StartWithMatch =>
                    mapName.StartsWith(fileName, StringComparison.OrdinalIgnoreCase),
                McsMapConfigExecutionType.PartialMatch =>
                    mapName.Contains(fileName, StringComparison.OrdinalIgnoreCase),
                _ => false,
            };

            if (isMatch)
                results.Add(filePath);
        }

        // Generic cfgs first (shorter name = broader match), exact-match cfg
        // always last so its values override every partial/prefix cfg.
        results.Sort((a, b) =>
        {
            string nameA = Path.GetFileNameWithoutExtension(a);
            string nameB = Path.GetFileNameWithoutExtension(b);

            bool exactA = string.Equals(nameA, mapName, StringComparison.OrdinalIgnoreCase);
            bool exactB = string.Equals(nameB, mapName, StringComparison.OrdinalIgnoreCase);
            if (exactA != exactB)
                return exactA ? 1 : -1;

            return nameA.Length.CompareTo(nameB.Length);
        });

        return results;
    }

    private void Exec(string cfgRelativePath, string label)
    {
        _logger.LogInformation("[MapConfigExec] exec {CfgPath} ({Label})", cfgRelativePath, label);
        _sharedSystem.GetModSharp().ServerCommand($"exec {cfgRelativePath}");
    }
}
