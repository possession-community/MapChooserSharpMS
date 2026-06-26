using System.Globalization;
using MapChooserSharpMS.Shared.Events.Nomination.Params;
using MapChooserSharpMS.Shared.MapConfig;
using Sharp.Shared.Objects;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.Nomination;

internal sealed class NominationCheckPassedEventParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapConfig mapConfig,
    IGameClient? nominator = null
    ): INominationCheckPassedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        return plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture);
    }

    public IGameClient? Client { get; } = nominator;
    public IMapConfig MapConfig { get; } = mapConfig;
}