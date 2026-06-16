using System.Collections.Generic;
using System.Globalization;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;
using TnmsPluginFoundation;
using TnmsPluginFoundation.Models.Plugin;

namespace MapChooserSharpMS.Modules.EventManager.Events.MapVote;

internal sealed class MapVoteFinishedParams(
    TnmsPlugin plugin,
    PluginModuleBase moduleBase,
    IMapVoteInformation voteInformation,
    bool isRtvVote,
    IReadOnlyDictionary<string, IMcsNominationData> nominatedMaps
) : IMapVoteFinishedEventParams
{
    public string ModulePrefix(CultureInfo? culture = null)
        => plugin.Localizer.ForCulture(moduleBase.ModuleChatPrefix, culture ?? CultureInfo.CurrentCulture);

    public IMapVoteInformation VoteInformation { get; } = voteInformation;

    public bool IsRtvVote { get; } = isRtvVote;

    public IReadOnlyDictionary<string, IMcsNominationData> NominatedMaps { get; } = nominatedMaps;
}
