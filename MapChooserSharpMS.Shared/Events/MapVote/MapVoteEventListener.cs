using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events.MapVote.Params;
using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapVote;

public interface IMapVoteEventListener: IEventListenerBase
{
    /// <summary>
    /// Fired when a map vote is about to start.
    /// Return <see cref="McsCancellableEvent.Stop"/> to cancel the vote.
    /// </summary>
    McsCancellableEvent OnMapVoteStart(IMapVoteStartParams @params)
        => McsCancellableEvent.Continue;

    /// <summary>
    /// Fired during random map picking. Return a non-empty override to replace
    /// the randomly-selected candidate list.
    /// </summary>
    McsValueOverrideEvent<List<IMapConfig>> OnRandomMapPick(IMapVoteRandomMapPickParams @params)
        => McsValueOverrideEvent<List<IMapConfig>>.NoOverride;

    void OnMapVoteFinished(IMapVoteFinishedEventParams @params) {}

    void OnMapVoteCancelled(IMapVoteCancelledParams @params) {}

    void OnMapExtended(IMapVoteExtendParams @params) {}

    void OnMapNotChanged(IMapVoteNotChangedParams @params) {}

    void OnMapConfirmed(IMapVoteMapConfirmedEventParams @params) {}
}