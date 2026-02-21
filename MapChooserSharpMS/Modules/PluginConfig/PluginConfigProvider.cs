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

    protected override void OnAllModulesLoaded()
    {
        ReloadConfig();
    }

    public void ReloadConfig()
    {
        var parsingService = ServiceProvider.GetRequiredService<IPluginConfigParsingService>();

        var configFilePath = Path.Combine(Plugin.BaseCfgDirectoryPath, "config.toml");

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
