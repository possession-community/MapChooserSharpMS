using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapCycle.Params;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapCycle;

internal sealed class TimeLimitReachedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    TimeLimitType limitType
) : ITimeLimitReachedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
    {
        return plugin.Localizer[moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture];
    }

    public TimeLimitType LimitType { get; } = limitType;
}
