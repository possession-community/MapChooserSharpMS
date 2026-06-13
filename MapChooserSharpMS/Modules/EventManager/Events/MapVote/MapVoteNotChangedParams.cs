using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class MapVoteNotChangedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase
) : IMapVoteNotChangedParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);
}
