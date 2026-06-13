using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class MapVoteCancelledParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IGameClient? cancelledBy
) : IMapVoteCancelledParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IGameClient? CancelledBy { get; } = cancelledBy;
}
