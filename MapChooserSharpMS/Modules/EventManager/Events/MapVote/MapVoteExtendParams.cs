using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapCycle.Managers.TimeLimit;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class MapVoteExtendParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    int extendTime,
    TimeLimitType timeLimitType
) : IMapVoteExtendParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer[moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture];

    public int ExtendTime { get; } = extendTime;

    public TimeLimitType TimeLimitType { get; } = timeLimitType;
}
