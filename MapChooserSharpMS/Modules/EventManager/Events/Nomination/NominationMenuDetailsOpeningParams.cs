using System.Collections.Generic;
using System.Globalization;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.MapConfig;
using MapChooserSharpMS.Shared.Ui.Menu;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.Nomination;

internal sealed class NominationMenuDetailsOpeningParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapConfig mapConfig,
    IGameClient client
) : INominationMenuDetailsOpeningParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapConfig MapConfig { get; } = mapConfig;

    public IGameClient Client { get; } = client;

    public List<McsVoteMenuItem> ExtraItems { get; } = [];
}
