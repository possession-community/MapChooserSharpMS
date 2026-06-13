using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapCycle;

internal sealed class ExtendVoteStartedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapConfig? currentMap,
    IGameClient? initiator,
    float voteDuration
) : IExtendVoteStartedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapConfig? CurrentMap { get; } = currentMap;

    public IGameClient? Initiator { get; } = initiator;

    public float VoteDuration { get; } = voteDuration;
}
