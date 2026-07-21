using MapChooserSharpMS.Modules.PluginConfig.Enums;

namespace MapChooserSharpMS.Modules.PluginConfig.Interfaces;

internal interface IMcsCooldownScopeConfig
{
    /// <summary>
    /// How <see cref="ScopePattern"/> is matched against cooldown record server keys.
    /// </summary>
    internal McsCooldownScopeMatchMode ScopeMatchMode { get; }

    /// <summary>
    /// Server key pattern used to select which servers' cooldown records apply here.
    /// Empty = this server's own key (Wuling ServerId).
    /// </summary>
    internal string ScopePattern { get; }
}
