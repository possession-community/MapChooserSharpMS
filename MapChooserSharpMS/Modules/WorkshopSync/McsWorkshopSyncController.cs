using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.WorkshopSync;

internal sealed class McsWorkshopSyncController(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload), Sharp.Shared.Listeners.IGameListener
{
    public override string PluginModuleName => "McsWorkshopSyncController";
    public override string ModuleChatPrefix => "";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    public int ListenerVersion => 1;
    public int ListenerPriority => 0;

    private readonly SteamWorkshopApiService _apiService = new();
    private CancellationTokenSource? _cts;

    internal WorkshopVisibilityCheckResult? LastVisibilityCheckResult { get; private set; }

    protected override void OnAllModulesLoaded()
    {
        SharedSystem.GetModSharp().InstallGameListener(this);

        var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();

        string apiKey = configProvider.PluginConfig.GeneralConfig.SteamWebApiKey;
        _apiService.SetApiKey(apiKey);

        if (!string.IsNullOrEmpty(apiKey))
            Logger.LogInformation("Steam Web API key configured");

        var collectionIds = configProvider.PluginConfig.GeneralConfig.WorkshopCollectionIds;

        if (collectionIds.Length == 0)
        {
            Logger.LogDebug("No WorkshopCollectionIds configured, skipping collection sync");
            return;
        }

        _cts = new CancellationTokenSource();
        Task.Run(() => RunSyncAsync(collectionIds, _cts.Token));
    }

    public void OnGameActivate()
    {
        if (string.IsNullOrEmpty(_apiService.ApiKey))
            return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        Task.Run(() => RunVisibilityCheckAsync(_cts.Token));
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetModSharp().RemoveGameListener(this);
        _cts?.Cancel();
        _cts?.Dispose();
        _apiService.Dispose();
    }

    private async Task RunSyncAsync(string[] collectionIds, CancellationToken ct)
    {
        try
        {
            var allItemIds = new List<long>();

            foreach (string collectionId in collectionIds)
            {
                string trimmed = collectionId.Trim();
                if (!long.TryParse(trimmed, out _))
                {
                    Logger.LogWarning("Invalid WorkshopCollectionId format: '{Id}'", trimmed);
                    continue;
                }

                try
                {
                    var ids = await _apiService!.GetCollectionItemIds(trimmed, ct);
                    Logger.LogInformation("Collection {Id}: {Count} items found", trimmed, ids.Count);
                    allItemIds.AddRange(ids);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to fetch collection {Id}", trimmed);
                }
            }

            if (allItemIds.Count == 0)
            {
                Logger.LogInformation("No workshop items found across all collections");
                return;
            }

            var distinct = allItemIds.Distinct().ToList();
            var details = await _apiService!.GetPublishedFileDetails(distinct, ct);

            // Marshal back to game thread for file I/O + config reload
            SharedSystem.GetModSharp().PushTimer(() =>
            {
                ProcessResults(details);
            }, 0f, GameTimerFlags.None);
        }
        catch (OperationCanceledException)
        {
            // Module unloaded during sync
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Workshop sync failed");
        }
    }

    private void ProcessResults(List<WorkshopItemInfo> items)
    {
        var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        var mapConfigProvider = ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();

        string configDirectory = ResolveMapConfigDirectory(configProvider);
        string syncDir = Path.Combine(configDirectory, "synced_workshop");
        int created = 0;

        foreach (var item in items)
        {
            if (item.Status is not (WorkshopItemStatus.Public or WorkshopItemStatus.Unlisted or WorkshopItemStatus.FriendsOnly))
                continue;

            if (mapConfigProvider.TryGetMapConfig(item.PublishedFileId, out _))
                continue;

            string mapName = CreateValidMapName(item.Title, item.PublishedFileId.ToString());
            string toml = GenerateMinimalToml(mapName, item.PublishedFileId, item.Title);

            try
            {
                Directory.CreateDirectory(syncDir);
                string filePath = Path.Combine(syncDir, $"{mapName}.toml");
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, toml, Encoding.UTF8);
                    Logger.LogInformation("Created config for workshop map: {Name} (ID: {Id})",
                        mapName, item.PublishedFileId);
                    created++;
                }
            }
            catch (IOException ex)
            {
                Logger.LogWarning(ex, "Failed to write config for {Name}", mapName);
            }
        }

        if (created > 0)
        {
            Logger.LogInformation("Created {Count} new map config(s), reloading...", created);
            mapConfigProvider.ReloadConfigs();
        }
        else
        {
            Logger.LogInformation("Workshop sync complete, no new maps to add");
        }
    }

    private string ResolveMapConfigDirectory(IMcsPluginConfigProvider configProvider)
    {
        string relativePath;
        try
        {
            relativePath = configProvider.PluginConfig.MapCycleConfig.MapConfigDirectoryPath;
        }
        catch (InvalidOperationException)
        {
            relativePath = "maps/";
        }

        return Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(Plugin.BaseCfgDirectoryPath, relativePath);
    }

    private static string GenerateMinimalToml(string mapName, long workshopId, string? title)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{mapName}]");
        sb.AppendLine($"WorkshopId = {workshopId}");
        if (!string.IsNullOrEmpty(title))
            sb.AppendLine($"MapNameAlias = \"{TomlEncode(title)}\"");
        return sb.ToString();
    }

    private static string TomlEncode(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string CreateValidMapName(string? workshopTitle, string workshopId)
    {
        if (string.IsNullOrWhiteSpace(workshopTitle))
            return $"workshop_{workshopId}";

        string valid = Regex.Replace(workshopTitle, @"[^a-zA-Z0-9_\-.]", "_");

        if (string.IsNullOrWhiteSpace(valid) || valid.StartsWith('-') || valid.StartsWith('.'))
            valid = "map_" + valid;

        if (valid.Length > 100)
            valid = valid[..100];

        return valid.ToLowerInvariant();
    }

    // ── Workshop Visibility Check (runs on map start) ──

    private async Task RunVisibilityCheckAsync(CancellationToken ct)
    {
        try
        {
            var mapConfigProvider = ServiceProvider.GetRequiredService<IMcsMapConfigProvider>();
            var targets = new List<(string MapName, long WorkshopId)>();

            foreach (var (mapName, overrides) in mapConfigProvider.GetMapConfigs())
            {
                foreach (var ov in overrides)
                {
                    if (ov.MapConfig.IsDisabled)
                        continue;
                    if (ov.MapConfig.WorkshopId <= 0)
                        continue;

                    targets.Add((mapName, ov.MapConfig.WorkshopId));
                    break;
                }
            }

            if (targets.Count == 0)
                return;

            var workshopIds = targets.Select(t => t.WorkshopId).Distinct().ToList();
            var details = await _apiService.GetPublishedFileDetails(workshopIds, ct);
            var statusById = details.ToDictionary(d => d.PublishedFileId, d => d);

            var result = new WorkshopVisibilityCheckResult();

            foreach (var (mapName, workshopId) in targets)
            {
                if (!statusById.TryGetValue(workshopId, out var info))
                {
                    Logger.LogWarning("Workshop check: {Map} (ID: {Id}) — no API response", mapName, workshopId);
                    result.Errors.Add(new WorkshopMapEntry(mapName, workshopId, null));
                    continue;
                }

                Logger.LogInformation("Workshop check: {Map} (ID: {Id}) — result={Result}, visibility={Vis}, status={Status}, title={Title}",
                    mapName, workshopId, info.ResultCode, info.Visibility, info.Status, info.Title ?? "(null)");

                switch (info.Status)
                {
                    case WorkshopItemStatus.Public:
                    case WorkshopItemStatus.FriendsOnly:
                    case WorkshopItemStatus.Unlisted:
                        result.Unchanged.Add(new WorkshopMapEntry(mapName, workshopId, info.Title));
                        break;
                    case WorkshopItemStatus.Private:
                    case WorkshopItemStatus.NotFound:
                        result.PrivateOrDeleted.Add(new WorkshopMapEntry(mapName, workshopId, info.Title));
                        break;
                    default:
                        result.Errors.Add(new WorkshopMapEntry(mapName, workshopId, info.Title));
                        break;
                }
            }

            SharedSystem.GetModSharp().PushTimer(() =>
            {
                LastVisibilityCheckResult = result;
                ApplyVisibilityResult(result);
            }, 0f, GameTimerFlags.None);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Workshop visibility check failed");
        }
    }

    private void ApplyVisibilityResult(WorkshopVisibilityCheckResult result)
    {
        Logger.LogInformation(
            "Workshop visibility check: Unchanged {Unchanged} | Private/Deleted {Private} | Errors {Errors}",
            result.Unchanged.Count, result.PrivateOrDeleted.Count, result.Errors.Count);

        if (result.PrivateOrDeleted.Count == 0)
            return;

        var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        string configDirectory = ResolveMapConfigDirectory(configProvider);
        int disabled = 0;

        foreach (var entry in result.PrivateOrDeleted)
        {
            Logger.LogWarning("Disabling map {Map} (workshop {Id}): private or deleted",
                entry.MapName, entry.WorkshopId);

            if (TryDisableMapInToml(configDirectory, entry.MapName))
                disabled++;
        }

        if (disabled > 0)
        {
            Logger.LogInformation("Disabled {Count} map(s) in config, reloading...", disabled);
            ServiceProvider.GetRequiredService<IMcsMapConfigProvider>().ReloadConfigs();
        }
    }

    private bool TryDisableMapInToml(string configDirectory, string mapName)
    {
        if (!Directory.Exists(configDirectory))
            return false;

        var sectionPattern = new Regex(
            @"^\[" + Regex.Escape(mapName) + @"\]\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        foreach (string filePath in Directory.EnumerateFiles(configDirectory, "*.toml", SearchOption.AllDirectories))
        {
            try
            {
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                if (!sectionPattern.IsMatch(content))
                    continue;

                string updated = InsertOrReplaceDisabled(content, mapName);
                if (updated == content)
                    continue;

                File.WriteAllText(filePath, updated, Encoding.UTF8);
                return true;
            }
            catch (IOException ex)
            {
                Logger.LogWarning(ex, "Failed to modify {File}", filePath);
            }
        }

        return false;
    }

    private static string InsertOrReplaceDisabled(string content, string mapName)
    {
        var lines = content.Split('\n');
        var result = new List<string>(lines.Length + 1);
        bool inTargetSection = false;
        bool replaced = false;

        string sectionHeader = $"[{mapName}]";

        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimEnd('\r').Trim();

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                if (inTargetSection && !replaced)
                {
                    result.Add("IsDisabled = true");
                    replaced = true;
                }
                inTargetSection = string.Equals(trimmed, sectionHeader, StringComparison.OrdinalIgnoreCase);
            }

            if (inTargetSection && trimmed.StartsWith("IsDisabled", StringComparison.OrdinalIgnoreCase))
            {
                result.Add("IsDisabled = true");
                replaced = true;
                continue;
            }

            result.Add(lines[i].TrimEnd('\r'));
        }

        if (inTargetSection && !replaced)
            result.Add("IsDisabled = true");

        return string.Join("\n", result);
    }
}
