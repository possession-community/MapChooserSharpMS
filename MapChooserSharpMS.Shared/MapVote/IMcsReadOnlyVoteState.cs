namespace MapChooserSharpMS.Shared.MapVote;

/// <summary>
/// Read-only view of the map-vote state, used as the narrow base for
/// <see cref="IMcsMapVoteController"/>. Modules that only need to
/// <b>query</b> "is a vote in progress right now?" should depend on this
/// interface instead of the full controller; state mutation
/// (start / cancel / finalize) lives on the derived interfaces and is
/// inaccessible through this one.
/// </summary>
public interface IMcsReadOnlyVoteState
{
    /// <summary>
    /// Current state of the vote, or <c>null</c> when no vote is active.
    /// </summary>
    McsMapVoteState? CurrentVoteState { get; }

    /// <summary>
    /// True when the vote is in any state that represents "a vote is
    /// currently happening" — voting, runoff, initialising, finalising, etc.
    /// Single source of truth for the question "can I safely start another
    /// vote right now?".
    /// </summary>
    bool IsVotingPeriod();
}
