using System;
using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.MapConfig;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapCycle;

internal sealed class MapCooldownApplyEventParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapConfig appliesTo,
    int cooldown,
    TimeSpan timedCooldownDuration
) : IMapCooldownApplyEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapConfig AppliesTo { get; } = appliesTo;

    public int Cooldown { get; set; } = cooldown;

    public TimeSpan TimedCooldownDuration { get; set; } = timedCooldownDuration;

    public bool IsCancelled { get; set; }
}
