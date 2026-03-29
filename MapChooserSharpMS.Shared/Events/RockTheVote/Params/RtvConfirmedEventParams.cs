using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.RockTheVote.Params;

/// <summary>
/// Fired when RTV has been confirmed and a map vote should be initiated.
/// This event is non-cancellable.
/// </summary>
public interface IRtvConfirmedParams : IEventBaseParams, IEnforceableEvent
{
    /// <summary>
    /// Client who triggered the RTV threshold, or who forced RTV.
    /// Null when triggered by console.
    /// </summary>
    IGameClient? Client { get; }

    /// <summary>
    /// True if this was triggered by a force RTV command rather than reaching the vote threshold.
    /// </summary>
    bool IsForced { get; }
}
