using System;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;
using Xunit;

namespace MapChooserSharpMS.Tests.MapCycle.TimeLimit;

public class TimeLimitTransitionTrackerTests
{
    // --- With TimeBasedTimeLimitManager ---

    [Fact]
    public void TimeBased_FiresVoteThreshold_WhenCrossed()
    {
        var clock = new FakeTimeLimitClock();
        var manager = new TimeBasedTimeLimitManager(TimeSpan.FromMinutes(30), clock);
        var threshold = TimeSpan.FromMinutes(5);
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.TimeLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        clock.Advance(TimeSpan.FromMinutes(26));
        manager.OnTick();
        var transitions = tracker.CheckTransitions();

        Assert.Single(transitions);
        Assert.Equal(TimeLimitTransitionState.VoteStartThresholdReached, transitions[0]);
    }

    [Fact]
    public void TimeBased_FiresLimitReached_WhenExpired()
    {
        var clock = new FakeTimeLimitClock();
        var manager = new TimeBasedTimeLimitManager(TimeSpan.FromMinutes(10), clock);
        var threshold = TimeSpan.FromMinutes(5);
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.TimeLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        // Cross threshold first
        clock.Advance(TimeSpan.FromMinutes(6));
        manager.OnTick();
        tracker.CheckTransitions();

        // Expire
        clock.Advance(TimeSpan.FromMinutes(5));
        manager.OnTick();
        var transitions = tracker.CheckTransitions();

        Assert.Single(transitions);
        Assert.Equal(TimeLimitTransitionState.LimitReached, transitions[0]);
    }

    [Fact]
    public void TimeBased_DoesNotReFire_OnSubsequentChecks()
    {
        var clock = new FakeTimeLimitClock();
        var manager = new TimeBasedTimeLimitManager(TimeSpan.FromMinutes(10), clock);
        var threshold = TimeSpan.FromMinutes(5);
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.TimeLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        clock.Advance(TimeSpan.FromMinutes(6));
        manager.OnTick();
        var first = tracker.CheckTransitions();
        Assert.Single(first);

        var second = tracker.CheckTransitions();
        Assert.Empty(second);
    }

    [Fact]
    public void TimeBased_FiresBoth_WhenThresholdIsZero()
    {
        var clock = new FakeTimeLimitClock();
        var manager = new TimeBasedTimeLimitManager(TimeSpan.FromMinutes(10), clock);
        var threshold = TimeSpan.Zero;
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.TimeLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        clock.Advance(TimeSpan.FromMinutes(10));
        manager.OnTick();
        var transitions = tracker.CheckTransitions();

        Assert.Equal(2, transitions.Count);
        Assert.Contains(TimeLimitTransitionState.VoteStartThresholdReached, transitions);
        Assert.Contains(TimeLimitTransitionState.LimitReached, transitions);
    }

    [Fact]
    public void TimeBased_ResetFlags_ThenReFiresAfterExtend()
    {
        var clock = new FakeTimeLimitClock();
        var manager = new TimeBasedTimeLimitManager(TimeSpan.FromMinutes(10), clock);
        var threshold = TimeSpan.FromMinutes(3);
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.TimeLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        // Expire
        clock.Advance(TimeSpan.FromMinutes(10));
        manager.OnTick();
        tracker.CheckTransitions();

        // Extend above threshold and reset
        manager.Extend(TimeSpan.FromMinutes(10));
        tracker.ResetFlags();

        // Expire again
        clock.Advance(TimeSpan.FromMinutes(10));
        manager.OnTick();
        var transitions = tracker.CheckTransitions();

        Assert.Contains(TimeLimitTransitionState.VoteStartThresholdReached, transitions);
        Assert.Contains(TimeLimitTransitionState.LimitReached, transitions);
    }

    [Fact]
    public void TimeBased_InitialState_BelowThreshold_DoesNotFireOnFirstCheck()
    {
        var clock = new FakeTimeLimitClock();
        var manager = new TimeBasedTimeLimitManager(TimeSpan.FromMinutes(3), clock);
        var threshold = TimeSpan.FromMinutes(5);
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.TimeLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        // Already below threshold at construction — should not fire
        var transitions = tracker.CheckTransitions();
        Assert.Empty(transitions);
    }

    // --- With RoundsTimeLimitManager ---

    [Fact]
    public void RoundBased_FiresVoteThreshold_WhenCrossed()
    {
        var manager = new RoundsTimeLimitManager(5);
        const int threshold = 2;
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.RoundsLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        manager.OnTick(); // 4
        tracker.CheckTransitions();
        manager.OnTick(); // 3
        tracker.CheckTransitions();
        manager.OnTick(); // 2 — crosses threshold
        var transitions = tracker.CheckTransitions();

        Assert.Single(transitions);
        Assert.Equal(TimeLimitTransitionState.VoteStartThresholdReached, transitions[0]);
    }

    [Fact]
    public void RoundBased_FiresLimitReached_WhenZero()
    {
        var manager = new RoundsTimeLimitManager(3);
        const int threshold = 2;
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.RoundsLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        manager.OnTick(); // 2 — threshold
        tracker.CheckTransitions();
        manager.OnTick(); // 1
        tracker.CheckTransitions();
        manager.OnTick(); // 0 — limit reached
        var transitions = tracker.CheckTransitions();

        Assert.Single(transitions);
        Assert.Equal(TimeLimitTransitionState.LimitReached, transitions[0]);
    }

    [Fact]
    public void RoundBased_ResetFlags_ThenReFiresAfterExtend()
    {
        var manager = new RoundsTimeLimitManager(3);
        const int threshold = 2;
        var tracker = new TimeLimitTransitionTracker(
            isVoteThresholdReached: () => manager.RoundsLeft <= threshold,
            isLimitReached: () => manager.IsLimitReached);

        // Exhaust rounds
        manager.OnTick(); // 2
        tracker.CheckTransitions();
        manager.OnTick(); // 1
        tracker.CheckTransitions();
        manager.OnTick(); // 0
        tracker.CheckTransitions();

        // Extend and reset
        manager.Extend(5); // 5
        tracker.ResetFlags();

        // Decrement back down
        manager.OnTick(); // 4
        tracker.CheckTransitions();
        manager.OnTick(); // 3
        tracker.CheckTransitions();
        manager.OnTick(); // 2 — threshold re-fires
        var thresholdTransitions = tracker.CheckTransitions();
        Assert.Contains(TimeLimitTransitionState.VoteStartThresholdReached, thresholdTransitions);

        manager.OnTick(); // 1
        tracker.CheckTransitions();
        manager.OnTick(); // 0 — limit re-fires
        var limitTransitions = tracker.CheckTransitions();
        Assert.Contains(TimeLimitTransitionState.LimitReached, limitTransitions);
    }
}
