using System;
using System.Collections.Generic;

namespace MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;

internal sealed class TimeLimitTransitionTracker
{
    private static readonly IReadOnlyList<TimeLimitTransitionState> EmptyTransitions = [];

    private readonly Func<bool> _isVoteThresholdReached;
    private readonly Func<bool> _isLimitReached;

    private bool _voteThresholdFired;
    private bool _limitReachedFired;

    public TimeLimitTransitionTracker(
        Func<bool> isVoteThresholdReached,
        Func<bool> isLimitReached)
    {
        _isVoteThresholdReached = isVoteThresholdReached;
        _isLimitReached = isLimitReached;

        _voteThresholdFired = isVoteThresholdReached();
        _limitReachedFired = isLimitReached();
    }

    public IReadOnlyList<TimeLimitTransitionState> CheckTransitions()
    {
        List<TimeLimitTransitionState>? transitions = null;

        if (!_voteThresholdFired && _isVoteThresholdReached())
        {
            _voteThresholdFired = true;
            transitions = [TimeLimitTransitionState.VoteStartThresholdReached];
        }

        if (!_limitReachedFired && _isLimitReached())
        {
            _limitReachedFired = true;
            transitions ??= [];
            transitions.Add(TimeLimitTransitionState.LimitReached);
        }

        return transitions ?? EmptyTransitions;
    }

    public void ResetFlags()
    {
        _voteThresholdFired = false;
        _limitReachedFired = false;
    }
}
