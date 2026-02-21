namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IPluginConfigParsingService
{
    IMcsPluginConfig ParseConfig(string configFilePath);
}
