using System.Globalization;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.Nomination;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.Nomination;

internal sealed class UnNominateEventParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMcsNominationData nominationData,
    int slot,
    UnNominateReason reason,
    IGameClient? client = null
) : IUnNominateParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        return plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);
    }

    public IGameClient? Client { get; } = client;
    public IMcsNominationData NominationData { get; } = nominationData;
    public int Slot { get; } = slot;
    public UnNominateReason Reason { get; } = reason;
}
