using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapChooserSharpMS.Modules.MapConfig.Interfaces;
using MapChooserSharpMS.Modules.MapConfig.Services;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapConfig.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TnmsPluginFoundation.Models.Plugin;
using TnmsPluginFoundation.Utils.Other;

namespace MapChooserSharpMS.Modules.MapConfig;

internal sealed class MapConfigProvider(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload), IMcsMapConfigProvider, Sharp.Shared.Listeners.IGameListener
{
    public override string PluginModuleName => "MapChooserSharpMS - MapConfigProvider";
    public override string ModuleChatPrefix => "";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    public int ListenerVersion => 1;
    public int ListenerPriority => 0;
    
    
    private Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>>  _mapGroupSettings = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>>  _mapConfigsNameMapping = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>>  _mapConfigsWorkshopIdMapping = new();

    public IMapConfigToolingService ToolingService { get; private set; } = null!;

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsMapConfigProvider>(this);
        services.AddTransient<IMapConfigGenerationService, MapConfigGenerationService>();
        services.AddTransient<IMapConfigParsingService, MapConfigParsingService>();
        services.AddSingleton<IMapConfigToolingService>(ToolingService);
    }


    protected override void OnInitialize()
    {
        ToolingService = new MapConfigToolingService(() =>
        {
            var cp = ServiceProvider.GetService<IMcsPluginConfigProvider>();
            try { return cp?.PluginConfig.GeneralConfig.ShouldUseAliasMapNameIfAvailable ?? true; }
            catch { return true; }
        });

        ReloadConfigs();
        SharedSystem.GetModSharp().InstallGameListener(this);
    }

    protected override void OnUnloadModule()
    {
        SharedSystem.GetModSharp().RemoveGameListener(this);
    }

    public void OnGameActivate()
    {
        TryAutoFixMapName();
    }

    public void ReloadConfigs()
    {
        var mapConfigParseService = new MapConfigParsingService();
        string configDirectory = ResolveMapConfigDirectory();

        IMapConfigParsingResult? parseResult;

        try
        {
            parseResult = mapConfigParseService.ParseConfigs(configDirectory);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse map configs from {Path}", configDirectory);
            return;
        }

        foreach (var warning in mapConfigParseService.Warnings)
        {
            Logger.LogWarning("{Warning}", warning);
        }

        if (parseResult == null)
        {
            Logger.LogError("No map configs found in {Path}", configDirectory);
            return;
        }

        _mapGroupSettings = parseResult.MapGroupSettings;
        _mapConfigsNameMapping = parseResult.MapConfigsNameMapping;
        _mapConfigsWorkshopIdMapping = parseResult.MapConfigsWorkshopIdMapping;

        int mapCount = _mapConfigsNameMapping.Count;
        int groupCount = _mapGroupSettings.Count;
        int overrideCount = _mapConfigsNameMapping.Values.Sum(v => Math.Max(0, v.Count - 1))
                          + _mapGroupSettings.Values.Sum(v => Math.Max(0, v.Count - 1));

        Logger.LogInformation("Loaded {Maps} maps, {Groups} groups, {Overrides} day-setting overrides",
            mapCount, groupCount, overrideCount);
    }

    // Map configs are scanned only under the configured map config directory
    // (default "maps/" relative to the module dir) so the plugin's own
    // config.toml is never picked up as a map section. Falls back to the
    // default when the plugin config has not been loaded yet.
    private string ResolveMapConfigDirectory()
    {
        string relativePath = "maps/";

        var configProvider = ServiceProvider.GetService<IMcsPluginConfigProvider>();
        if (configProvider is not null)
        {
            try
            {
                relativePath = configProvider.PluginConfig.MapCycleConfig.MapConfigDirectoryPath;
            }
            catch (InvalidOperationException)
            {
                // Plugin config not loaded yet — use the default path.
            }
        }

        return Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(Plugin.BaseCfgDirectoryPath, relativePath);
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>> GetGroupSettings()
    {
        return _mapGroupSettings;
    }

    public IReadOnlyDictionary<string, IReadOnlyCollection<IMapConfigOverrides>> GetMapConfigs()
    {
        return _mapConfigsNameMapping;
    }

    public bool TryGetMapConfigs(string mapName, out IReadOnlyCollection<IMapConfigOverrides>? found)
    {
        return _mapConfigsNameMapping.TryGetValue(mapName, out found);
    }

    public bool TryGetMapConfigs(long workshopId, out IReadOnlyCollection<IMapConfigOverrides>? found)
    {
        return _mapConfigsWorkshopIdMapping.TryGetValue(workshopId, out found);
    }

    public bool TryGetMapConfig(string mapName, out IMapConfig found)
    {
        if (_mapConfigsNameMapping.TryGetValue(mapName, out var overrides) && overrides.Count > 0)
        {
            found = ResolveOverride(overrides).MapConfig;
            return true;
        }

        found = null!;
        return false;
    }

    public bool TryGetMapConfig(long workshopId, out IMapConfig found)
    {
        if (_mapConfigsWorkshopIdMapping.TryGetValue(workshopId, out var overrides) && overrides.Count > 0)
        {
            found = ResolveOverride(overrides).MapConfig;
            return true;
        }

        found = null!;
        return false;
    }

    private void TryAutoFixMapName()
    {
        var configProvider = ServiceProvider.GetService<IMcsPluginConfigProvider>();
        if (configProvider is null || !configProvider.PluginConfig.GeneralConfig.ShouldAutoFixMapName)
            return;

        long workshopId = MapUtil.GetCurrentMapWorkshopId();
        if (workshopId < 0)
            return;

        string actualMapName = SharedSystem.GetModSharp().GetMapName() ?? string.Empty;
        if (string.IsNullOrEmpty(actualMapName))
            return;

        if (!TryGetMapConfig(workshopId, out var mapConfig))
        {
            Logger.LogDebug("AutoFix: No config found for workshop ID {WorkshopId}", workshopId);
            return;
        }

        if (string.Equals(mapConfig.MapName, actualMapName, StringComparison.OrdinalIgnoreCase))
            return;

        string configDirectory = ResolveMapConfigDirectory();
        string oldPath = Path.Combine(configDirectory, $"{mapConfig.MapName}.toml");
        string newPath = Path.Combine(configDirectory, $"{actualMapName}.toml");

        if (!File.Exists(oldPath))
        {
            Logger.LogWarning("AutoFix: Config file not found at {Path}, skipping", oldPath);
            return;
        }

        try
        {
            File.Move(oldPath, newPath, overwrite: false);
            Logger.LogInformation("AutoFix: Renamed {Old} → {New} (workshop {Id})",
                Path.GetFileName(oldPath), Path.GetFileName(newPath), workshopId);
            ReloadConfigs();
        }
        catch (IOException ex)
        {
            Logger.LogWarning(ex, "AutoFix: Failed to rename config file");
        }
    }

    private static IMapConfigOverrides ResolveOverride(IReadOnlyCollection<IMapConfigOverrides> overrides)
    {
        if (overrides.Count == 1)
            return overrides.First();

        var now = DateTime.Now;
        var today = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        IMapConfigOverrides? baseConfig = null;
        IMapConfigOverrides? best = null;

        foreach (var ov in overrides)
        {
            if (ov.OverrideConfigName == IBaseOverrideConfig.BaseConfigName)
            {
                baseConfig = ov;
                continue;
            }

            if (!ov.Enabled)
                continue;

            if (ov.TargetDays.Count > 0 && !ov.TargetDays.Contains(today))
                continue;

            if (ov.TargetTimeRanges.Count > 0 && !ov.TargetTimeRanges.Any(tr => tr.IsInRange(currentTime)))
                continue;

            if (best is null
                || ov.ForceOverride && !best.ForceOverride
                || ov.ForceOverride == best.ForceOverride && ov.OverridePriority > best.OverridePriority)
            {
                best = ov;
            }
        }

        return best ?? baseConfig ?? overrides.First();
    }

    public IMapGroupConfig? TryGetGroupConfig(IMapGroupConfigOverrides overrides)
    {
        return overrides.GroupConfig;
    }
}