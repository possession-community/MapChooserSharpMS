using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.MapConfig;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapCycle;

internal sealed class NextMapRemovedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapConfig previousNextMap
) : INextMapRemovedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapConfig PreviousNextMap { get; } = previousNextMap;
}
