using MapChooserSharpMS.Shared.Events.RockTheVote.Params;

namespace MapChooserSharpMS.Shared.Events.RockTheVote;

public interface IRockTheVoteEventListener: IEventListenerBase
{
    /// <summary>
    /// If true, client's RTV will be cancelled
    /// </summary>
    bool OnClientRtvCast(IClientRtvCastParams @params)
        => false;

    /// <summary>
    /// If true, client's RTV will be cancelled
    /// </summary>
    bool OnClientRtvUnCast(IClientRtvUnCastParams @params)
        => false;

    /// <summary>
    /// If true, force RTV will be cancelled
    /// </summary>
    bool OnForceRtv(IForceRtvParam @params)
        => false;

    /// <summary>
    /// Fired after RTV has been confirmed (either by reaching the vote threshold or by force RTV).
    /// MapVoteController should listen to this event to initiate a map vote.
    /// This event is non-cancellable.
    /// </summary>
    void OnRtvConfirmed(IRtvConfirmedParams @params)
    {
    }
}