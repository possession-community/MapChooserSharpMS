using MapChooserSharpMS.Shared.MapVote;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

/// <summary>
/// Fired when map vote is finished, before individual result events (Confirmed/Extended/NotChanged).
/// </summary>
public interface IMapVoteFinishedEventParams : IEventBaseParams
{
    IMapVoteInformation VoteInformation { get; }

    bool IsRtvVote { get; }
}