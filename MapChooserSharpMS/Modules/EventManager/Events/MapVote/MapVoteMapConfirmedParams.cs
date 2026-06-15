using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.MapCycle.Managers.MapTransition;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class MapVoteMapConfirmedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapInformation mapInformation,
    bool isRtvVote
) : IMapVoteMapConfirmedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapConfig ConfirmedMap { get; } = mapInformation.MapConfig;

    public IMapInformation MapInformation { get; } = mapInformation;

    public bool IsRtvVote { get; } = isRtvVote;
}
