using System.Collections.Generic;
using MapChooserSharpMS.Shared.MapVote;
using MapChooserSharpMS.Shared.Nomination;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

/// <summary>
/// Fired when map vote is finished, before individual result events (Confirmed/Extended/NotChanged).
/// </summary>
public interface IMapVoteFinishedEventParams : IEventBaseParams
{
    IMapVoteInformation VoteInformation { get; }

    bool IsRtvVote { get; }

    /// <summary>
    /// Snapshot of nominated maps at the time the vote finished.
    /// </summary>
    IReadOnlyDictionary<string, IMcsNominationData> NominatedMaps { get; }
}