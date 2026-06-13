using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.MapConfig;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapCycle;

internal sealed class NextMapConfirmedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapConfig nextMap,
    IMapConfig? oldNextMap
) : INextMapConfirmedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapConfig NextMap { get; } = nextMap;

    public IMapConfig? OldNextMap { get; } = oldNextMap;
}
