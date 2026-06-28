using System.Collections.Generic;
using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapConfig;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class NominatedMapPickParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IReadOnlyList<IMapConfig> selectedMaps
) : INominatedMapPickParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IReadOnlyList<IMapConfig> SelectedMaps { get; } = selectedMaps;
}
