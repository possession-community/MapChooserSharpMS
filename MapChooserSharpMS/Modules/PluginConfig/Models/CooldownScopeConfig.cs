using MapChooserSharpMS.Modules.PluginConfig.Enums;
using MapChooserSharpMS.Modules.PluginConfig.Interfaces;

namespace MapChooserSharpMS.Modules.PluginConfig.Models;

internal sealed class CooldownScopeConfig(
    McsCooldownScopeMatchMode scopeMatchMode,
    string scopePattern)
    : IMcsCooldownScopeConfig
{
    public McsCooldownScopeMatchMode ScopeMatchMode { get; } = scopeMatchMode;
    public string ScopePattern { get; } = scopePattern;
}
