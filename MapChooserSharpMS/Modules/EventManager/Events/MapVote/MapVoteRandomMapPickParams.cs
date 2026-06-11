using System.Collections.Generic;
using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapConfig;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class MapVoteRandomMapPickParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    int minimumMapCounts,
    IReadOnlyDictionary<string, IMapConfig> mapConfigs
) : IMapVoteRandomMapPickParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer[moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture];

    public int MinimumMapCounts { get; } = minimumMapCounts;

    public IReadOnlyDictionary<string, IMapConfig> MapConfigs { get; } = mapConfigs;
}
