using MapChooserSharpMS.Modules.PluginConfig.Enums;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IMcsGeneralConfig
{
    internal bool ShouldUseAliasMapNameIfAvailable { get; }

    internal bool VerboseCooldownPrint { get; }

    internal string[] WorkshopCollectionIds { get; }

    internal bool ShouldAutoFixMapName { get; }

    internal RtvMapChangeBehaviourType RtvMapChangeBehaviour { get; }

    internal string SteamWebApiKey { get; }
}
