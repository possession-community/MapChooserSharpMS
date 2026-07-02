using MapChooserSharpMS.Shared.MapConfig;

namespace MapChooserSharpMS.Shared.Events.MapCycle.Params;

/// <summary>
/// Fired when an extend vote concluded (passed or failed).
/// Not fired on cancellation — see IExtendVoteCancelledEventParams.
/// </summary>
public interface IExtendVoteFinishedEventParams: IEventBaseParams
{
    /// <summary>
    /// The map that was being voted to extend (current map).
    /// null when MCS has no config for the current map.
    /// </summary>
    IMapConfig? CurrentMap { get; }

    /// <summary>
    /// True when the vote passed (the map got extended).
    /// </summary>
    bool Passed { get; }

    /// <summary>
    /// Number of "yes" votes cast in the extend vote.
    /// </summary>
    int YesCount { get; }

    /// <summary>
    /// Number of "no" votes cast in the extend vote.
    /// </summary>
    int NoCount { get; }
}
