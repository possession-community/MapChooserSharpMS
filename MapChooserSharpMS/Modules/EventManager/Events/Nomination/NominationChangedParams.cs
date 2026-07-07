using System.Globalization;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Nomination;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.Nomination;

internal sealed class NominationChangedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMcsNominationData nominationData,
    IGameClient? client = null
    ): INominationChangeParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        return plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);
    }

    public IGameClient? Client { get; } = client;
    public IMcsNominationData NominationData { get; } = nominationData;
    public bool EnforcedByAdmin => false;
    public IGameClient? Enforcer => null;
}
