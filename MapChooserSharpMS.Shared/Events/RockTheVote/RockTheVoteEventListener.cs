using MapChooserSharpMS.Shared.Events.RockTheVote.Params;

namespace MapChooserSharpMS.Shared.Events.RockTheVote;

public interface IRockTheVoteEventListener: IEventListenerBase
{
    /// <summary>
    /// Fired when a client casts an RTV vote.
    /// Return <see cref="McsCancellableEvent.Stop"/> to cancel the RTV cast.
    /// </summary>
    McsCancellableEvent OnClientRtvCast(IClientRtvCastParams @params)
        => McsCancellableEvent.Continue;

    /// <summary>
    /// Fired when a client retracts their RTV vote.
    /// Return <see cref="McsCancellableEvent.Stop"/> to cancel the retraction.
    /// </summary>
    McsCancellableEvent OnClientRtvUnCast(IClientRtvUnCastParams @params)
        => McsCancellableEvent.Continue;

    /// <summary>
    /// Fired when an admin force-RTVs.
    /// Return <see cref="McsCancellableEvent.Stop"/> to cancel the force RTV.
    /// </summary>
    McsCancellableEvent OnForceRtv(IForceRtvParam @params)
        => McsCancellableEvent.Continue;

    /// <summary>
    /// Fired after RTV has been confirmed (either by reaching the vote threshold or by force RTV).
    /// MapVoteController should listen to this event to initiate a map vote.
    /// This event is non-cancellable.
    /// </summary>
    void OnRtvConfirmed(IRtvConfirmedParams @params)
    {
    }
}