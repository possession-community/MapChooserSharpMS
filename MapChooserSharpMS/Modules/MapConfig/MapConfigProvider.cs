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

namespace MapChooserSharpMS.Modules.MapConfig;

internal sealed class MapConfigProvider(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload), IMcsMapConfigProvider
{
    public override string PluginModuleName => "MapChooserSharpMS - MapConfigProvider";
    public override string ModuleChatPrefix => "";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;
    
    
    private Dictionary<string, IReadOnlyCollection<IMapGroupConfigOverrides>>  _mapGroupSettings = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, IReadOnlyCollection<IMapConfigOverrides>>  _mapConfigsNameMapping = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<long, IReadOnlyCollection<IMapConfigOverrides>>  _mapConfigsWorkshopIdMapping = new();

    // Stateless; built once here and shared with the DI container so both
    // the `IMcsMapConfigProvider.ToolingService` getter and direct DI resolve
    // return the same instance.
    public IMapConfigToolingService ToolingService { get; } = new MapConfigToolingService();

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsMapConfigProvider>(this);
        services.AddTransient<IMapConfigGenerationService, MapConfigGenerationService>();
        services.AddTransient<IMapConfigParsingService, MapConfigParsingService>();
        services.AddSingleton<IMapConfigToolingService>(ToolingService);
    }


    protected override void OnInitialize()
    {
        ReloadConfigs();
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

    // Naive resolution: returns the first override's MapConfig. Day-of-week
    // / time-range aware selection is deferred — this is the minimum needed
    // to unblock MapTransitionManager and downstream consumers.
    public bool TryGetMapConfig(string mapName, out IMapConfig found)
    {
        if (_mapConfigsNameMapping.TryGetValue(mapName, out var overrides) && overrides.Count > 0)
        {
            found = overrides.First().MapConfig;
            return true;
        }

        found = null!;
        return false;
    }

    public bool TryGetMapConfig(long workshopId, out IMapConfig found)
    {
        if (_mapConfigsWorkshopIdMapping.TryGetValue(workshopId, out var overrides) && overrides.Count > 0)
        {
            found = overrides.First().MapConfig;
            return true;
        }

        found = null!;
        return false;
    }

    public IMapGroupConfig? TryGetGroupConfig(IMapGroupConfigOverrides overrides)
    {
        return overrides.GroupConfig;
    }
}