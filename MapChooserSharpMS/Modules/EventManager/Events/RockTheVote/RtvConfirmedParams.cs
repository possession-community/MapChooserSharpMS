using System.Globalization;
using MapChooserSharpMS.Shared.Events.RockTheVote.Params;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.RockTheVote;

internal sealed class RtvConfirmedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IGameClient? client,
    bool isForced
) : IRtvConfirmedParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        return plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);
    }

    public IGameClient? Client { get; } = client;
    public bool IsForced { get; } = isForced;
    public bool EnforcedByAdmin => IsForced;
    public IGameClient? Enforcer => IsForced ? Client : null;
}
