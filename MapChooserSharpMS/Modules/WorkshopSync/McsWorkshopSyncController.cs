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
    : PluginModuleBase(serviceProvider, hotReload)
{
    public override string PluginModuleName => "McsWorkshopSyncController";
    public override string ModuleChatPrefix => "";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    private SteamWorkshopApiService? _apiService;
    private CancellationTokenSource? _cts;

    protected override void OnAllModulesLoaded()
    {
        var configProvider = ServiceProvider.GetRequiredService<IMcsPluginConfigProvider>();
        var collectionIds = configProvider.PluginConfig.GeneralConfig.WorkshopCollectionIds;

        if (collectionIds.Length == 0)
        {
            Logger.LogDebug("No WorkshopCollectionIds configured, skipping sync");
            return;
        }

        _apiService = new SteamWorkshopApiService();
        _cts = new CancellationTokenSource();

        Task.Run(() => RunSyncAsync(collectionIds, _cts.Token));
    }

    protected override void OnUnloadModule()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _apiService?.Dispose();
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
            if (item.Status != WorkshopItemStatus.Public)
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
}
