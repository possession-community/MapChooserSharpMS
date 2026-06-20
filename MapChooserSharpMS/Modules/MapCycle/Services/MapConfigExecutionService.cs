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
    private readonly ISharedSystem _sharedSystem;
    private readonly ILogger _logger;
    private readonly string _mapCfgDirectory;
    private readonly string _groupCfgDirectory;
    private readonly Func<McsMapConfigExecutionType> _executionTypeProvider;

    public MapConfigExecutionService(
        ISharedSystem sharedSystem,
        ILogger logger,
        string sharpPath,
        Func<McsMapConfigExecutionType> executionTypeProvider)
    {
        _sharedSystem = sharedSystem;
        _logger = logger;
        _mapCfgDirectory = Path.Combine(sharpPath, "configs", "mcsms", "cfgs", "maps");
        _groupCfgDirectory = Path.Combine(sharpPath, "configs", "mcsms", "cfgs", "groups");
        _executionTypeProvider = executionTypeProvider;
    }

    public void ExecuteConfigsForMap(IMapConfig mapConfig)
    {
        _logger.LogInformation(
            "[MapConfigExec] Starting cfg execution for map={Map}, groups={Groups}, mapDir={MapDir}, groupDir={GroupDir}",
            mapConfig.MapName, mapConfig.GroupSettings.Count, _mapCfgDirectory, _groupCfgDirectory);
        ExecuteGroupConfigs(mapConfig);
        ExecuteMapConfigs(mapConfig.MapName, _executionTypeProvider());
    }

    private void ExecuteGroupConfigs(IMapConfig mapConfig)
    {
        if (!Directory.Exists(_groupCfgDirectory))
        {
            _logger.LogDebug("[MapConfigExec] Group cfg directory does not exist: {Dir}", _groupCfgDirectory);
            return;
        }

        string[] cfgFiles;
        try
        {
            cfgFiles = Directory.GetFiles(_groupCfgDirectory, "*.cfg");
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
                    ExecuteCfgFile(filePath, $"group:{group.GroupName}");
                    break;
                }
            }
        }
    }

    private void ExecuteMapConfigs(string mapName, McsMapConfigExecutionType executionType)
    {
        if (!Directory.Exists(_mapCfgDirectory))
        {
            _logger.LogDebug("[MapConfigExec] Map cfg directory does not exist: {Dir}", _mapCfgDirectory);
            return;
        }

        string[] cfgFiles;
        try
        {
            cfgFiles = Directory.GetFiles(_mapCfgDirectory, "*.cfg");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MapConfigExec] Failed to scan map cfg directory: {Dir}", _mapCfgDirectory);
            return;
        }

        var matched = MatchCfgFiles(cfgFiles, mapName, executionType);
        _logger.LogDebug("[MapConfigExec] Map cfg scan: {Total} files found, {Matched} matched (mode={Mode})",
            cfgFiles.Length, matched.Count, executionType);
        foreach (string cfgPath in matched)
        {
            ExecuteCfgFile(cfgPath, $"map:{mapName}");
        }
    }

    private static List<string> MatchCfgFiles(string[] cfgFiles, string mapName, McsMapConfigExecutionType executionType)
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

        if (executionType == McsMapConfigExecutionType.StartWithMatch)
        {
            results.Sort((a, b) =>
                Path.GetFileNameWithoutExtension(a).Length
                    .CompareTo(Path.GetFileNameWithoutExtension(b).Length));
        }

        return results;
    }

    private void ExecuteCfgFile(string cfgPath, string label, int depth = 0, HashSet<string>? visited = null)
    {
        string fullPath = Path.GetFullPath(cfgPath);
        visited ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!visited.Add(fullPath))
        {
            _logger.LogWarning("[MapConfigExec] Circular reference detected, skipping: {CfgPath}", cfgPath);
            return;
        }

        if (depth > 8)
        {
            _logger.LogWarning("[MapConfigExec] Recursion limit reached for {CfgPath}", cfgPath);
            return;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(cfgPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MapConfigExec] Failed to read {CfgPath}", cfgPath);
            return;
        }

        _logger.LogInformation("[MapConfigExec] Executing {CfgPath} ({Label}, {LineCount} lines)",
            cfgPath, label, lines.Length);

        string? directory = Path.GetDirectoryName(cfgPath);
        var modSharp = _sharedSystem.GetModSharp();
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("//"))
                continue;

            if (trimmed.StartsWith("exec ", StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = trimmed[5..].Trim().Trim('"');
                if (!relativePath.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
                    relativePath += ".cfg";

                string resolvedPath = directory is not null
                    ? Path.Combine(directory, relativePath)
                    : relativePath;

                if (File.Exists(resolvedPath))
                    ExecuteCfgFile(resolvedPath, $"exec:{Path.GetFileName(resolvedPath)}", depth + 1, visited);
                else
                    _logger.LogWarning("[MapConfigExec] exec target not found: {Path}", resolvedPath);

                continue;
            }

            _logger.LogDebug("[MapConfigExec] > {Command}", trimmed);
            modSharp.ServerCommand(trimmed);
        }
    }
}
