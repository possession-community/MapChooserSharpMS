using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapCycle;

internal sealed class ExtendVoteCancelledParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapConfig? currentMap,
    IGameClient? cancelledBy
) : IExtendVoteCancelledEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapConfig? CurrentMap { get; } = currentMap;

    public IGameClient? CancelledBy { get; } = cancelledBy;
}
