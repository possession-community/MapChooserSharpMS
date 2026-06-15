using System;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.MapVote.Services;

namespace MapChooserSharpMS.Shared.MapVote;

/// <summary>
/// Public MapVoteController API.
/// </summary>
public interface IMcsMapVoteController
{
    /// <summary>
    /// Read-only view of the current vote state.
    /// </summary>
    IMcsReadOnlyVoteState VoteState { get; }

    /// <summary>
    /// Service for initiating, cancelling, and force-resetting votes.
    /// </summary>
    IMapVoteControllingService MapVoteControllingService { get; }

    void InstallEventListener(IMapVoteEventListener listener);

    void RemoveEventListener(IMapVoteEventListener listener);

    /// <summary>
    /// External plugins can provide a custom winner threshold for initial votes.
    /// The func is invoked each time a vote starts; the returned float is used as
    /// the pass threshold (0.0–1.0). Set to <c>null</c> to revert to the default
    /// ConVar-based threshold. Runoff votes always pass regardless of this value.
    /// </summary>
    Func<float>? CustomWinnerThresholdProvider { get; set; }
}