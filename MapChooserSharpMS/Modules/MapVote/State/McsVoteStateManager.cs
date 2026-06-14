using MapChooserSharpMS.Modules.MapVote.Interfaces;
using MapChooserSharpMS.Shared.MapVote;

namespace MapChooserSharpMS.Modules.MapVote.State;

/// <summary>
/// Single concrete holder for both vote-kind states (main map-selection vote
/// and map-extend vote). Exposes a combined read-only view through
/// <see cref="IMcsReadOnlyVoteState"/> — <c>IsVotingPeriod()</c> returns
/// <c>true</c> when <b>either</b> vote is in progress. Writers for each kind
/// go through the matching internal interface (explicit implementation below
/// so the two <c>SetState</c> / <c>Reset</c> signatures don't collide).
/// </summary>
internal sealed class McsVoteStateManager
    : IMcsReadOnlyVoteState,
      IMcsInternalMainVoteState,
      IMcsInternalExtendVoteState
{
    private McsMapVoteState? _mainState;
    private McsMapVoteState? _extendState;

    public McsMapVoteState? CurrentVoteState => _mainState ?? _extendState;

    public bool IsVotingPeriod() => IsVoting(_mainState) || IsVoting(_extendState);

    void IMcsInternalMainVoteState.SetState(McsMapVoteState? state) => _mainState = state;
    void IMcsInternalMainVoteState.Reset() => _mainState = null;

    void IMcsInternalExtendVoteState.SetState(McsMapVoteState? state) => _extendState = state;
    void IMcsInternalExtendVoteState.Reset() => _extendState = null;

    private static bool IsVoting(McsMapVoteState? state)
        => state is McsMapVoteState.Voting
            or McsMapVoteState.RunoffVoting
            or McsMapVoteState.Finalizing;
}
