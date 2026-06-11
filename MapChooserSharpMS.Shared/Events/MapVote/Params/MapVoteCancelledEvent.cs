using Sharp.Shared.Objects;

namespace MapChooserSharpMS.Shared.Events.MapVote.Params;

/// <summary>
/// Fired when vote is cancelled.
/// </summary>
public interface IMapVoteCancelledParams : IEventBaseParams
{
    /// <summary>
    /// Client who cancelled the vote, or null if cancelled by system/console.
    /// </summary>
    IGameClient? CancelledBy { get; }
}