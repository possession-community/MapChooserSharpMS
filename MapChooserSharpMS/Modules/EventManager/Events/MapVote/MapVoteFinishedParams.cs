using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapVote;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class MapVoteFinishedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapVoteInformation voteInformation,
    bool isRtvVote
) : IMapVoteFinishedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer[moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture];

    public IMapVoteInformation VoteInformation { get; } = voteInformation;

    public bool IsRtvVote { get; } = isRtvVote;
}
