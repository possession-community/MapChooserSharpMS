using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.MapConfig;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapCycle;

internal sealed class ExtendVoteFinishedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapConfig? currentMap,
    bool passed,
    int yesCount,
    int noCount
) : IExtendVoteFinishedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapConfig? CurrentMap { get; } = currentMap;

    public bool Passed { get; } = passed;

    public int YesCount { get; } = yesCount;

    public int NoCount { get; } = noCount;
}
