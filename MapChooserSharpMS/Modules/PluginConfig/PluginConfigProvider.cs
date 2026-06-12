using System;
using System.IO;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;
using MapChooserSharpMS.Modules.PluginConfig.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.PluginConfig;

internal sealed class PluginConfigProvider(IServiceProvider serviceProvider, bool hotReload)
    : PluginModuleBase(serviceProvider, hotReload), IMcsPluginConfigProvider
{
    public override string PluginModuleName => "MapChooserSharpMS - PluginConfigProvider";
    public override string ModuleChatPrefix => "";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    private IMcsPluginConfig? _pluginConfig;

    public IMcsPluginConfig PluginConfig =>
        _pluginConfig ?? throw new InvalidOperationException("PluginConfig has not been loaded yet.");

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMcsPluginConfigProvider>(this);
        services.AddTransient<IPluginConfigParsingService, PluginConfigParsingService>();
    }

    protected override void OnInitialize()
    {
        ReloadConfig();
    }

    public void ReloadConfig()
    {
        var parsingService = new Services.PluginConfigParsingService();

        var configFilePath = Path.Combine(Plugin.BaseCfgDirectoryPath, "config.toml");

        if (!File.Exists(configFilePath))
        {
            Logger.LogWarning("Config file not found at {Path}, using defaults", configFilePath);
            File.WriteAllText(configFilePath, "# MapChooserSharpMS config — see documentation for options\n");
        }

        try
        {
            _pluginConfig = parsingService.ParseConfig(configFilePath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse plugin config from {Path}", configFilePath);
        }
    }
}
