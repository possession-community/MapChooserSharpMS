using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal class GeneralConfig(
    bool shouldUseAliasMapNameIfAvailable,
    bool verboseCooldownPrint,
    string[] workshopCollectionIds,
    bool shouldAutoFixMapName,
    RtvMapChangeBehaviourType rtvMapChangeBehaviour,
    string steamWebApiKey)
    : IMcsGeneralConfig
{
    public bool ShouldUseAliasMapNameIfAvailable { get; } = shouldUseAliasMapNameIfAvailable;
    public bool VerboseCooldownPrint { get; } = verboseCooldownPrint;
    public string[] WorkshopCollectionIds { get; } = workshopCollectionIds;
    public bool ShouldAutoFixMapName { get; } = shouldAutoFixMapName;
    public RtvMapChangeBehaviourType RtvMapChangeBehaviour { get; } = rtvMapChangeBehaviour;
    public string SteamWebApiKey { get; } = steamWebApiKey;
}
