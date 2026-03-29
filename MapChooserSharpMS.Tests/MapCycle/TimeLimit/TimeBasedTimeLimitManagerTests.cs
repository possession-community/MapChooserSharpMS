using System;
using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;
using Xunit;

namespace MapChooserSharpMS.Tests.MapCycle.TimeLimit;

public class TimeBasedTimeLimitManagerTests
{
    private readonly FakeTimeLimitClock _clock = new();

    private TimeBasedTimeLimitManager CreateManager(TimeSpan initialTimeLimit)
    {
        return new TimeBasedTimeLimitManager(
            initialTimeLimit: initialTimeLimit,
            clock: _clock);
    }

    // --- Initial State ---

    [Fact]
    public void InitialState_TimeLeft_EqualsInitialLimit()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(30));

        Assert.Equal(TimeSpan.FromMinutes(30), manager.TimeLeft);
    }

    [Fact]
    public void InitialState_IsLimitReached_IsFalse()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(30));

        Assert.False(manager.IsLimitReached);
    }

    [Fact]
    public void InitialState_ZeroDuration_IsLimitReached_IsTrue()
    {
        var manager = CreateManager(TimeSpan.Zero);

        Assert.True(manager.IsLimitReached);
    }

    // --- On-demand Calculation ---

    [Fact]
    public void TimeLeft_DecreasesWithClock()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(30));

        _clock.Advance(TimeSpan.FromMinutes(10));
        manager.OnTick();

        Assert.Equal(TimeSpan.FromMinutes(20), manager.TimeLeft);
    }

    [Fact]
    public void TimeLeft_NeverGoesBelowZero()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(5));

        _clock.Advance(TimeSpan.FromHours(1));
        manager.OnTick();

        Assert.Equal(TimeSpan.Zero, manager.TimeLeft);
    }

    [Fact]
    public void TimeLeft_CachedWithinSameTick()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(30));

        var first = manager.TimeLeft;

        _clock.Advance(TimeSpan.FromMinutes(10));
        var second = manager.TimeLeft;

        Assert.Equal(first, second);
    }

    [Fact]
    public void TimeLeft_RefreshedAfterOnTick()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(30));

        _ = manager.TimeLeft;

        _clock.Advance(TimeSpan.FromMinutes(10));
        manager.OnTick();

        Assert.Equal(TimeSpan.FromMinutes(20), manager.TimeLeft);
    }

    // --- Extend ---

    [Fact]
    public void Extend_IncreasesTimeLeft()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(10));

        manager.Extend(TimeSpan.FromMinutes(5));
        manager.OnTick();

        Assert.Equal(TimeSpan.FromMinutes(15), manager.TimeLeft);
    }

    [Fact]
    public void Extend_WhenExpired_RestartsFromNow()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(5));

        _clock.Advance(TimeSpan.FromMinutes(10));
        manager.Extend(TimeSpan.FromMinutes(3));
        manager.OnTick();

        Assert.Equal(TimeSpan.FromMinutes(3), manager.TimeLeft);
        Assert.False(manager.IsLimitReached);
    }

    [Fact]
    public void Extend_InvalidatesCache()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(10));

        _ = manager.TimeLeft;

        manager.Extend(TimeSpan.FromMinutes(5));

        Assert.Equal(TimeSpan.FromMinutes(15), manager.TimeLeft);
    }

    // --- Set ---

    [Fact]
    public void Set_OverwritesTimeLeft()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(30));

        manager.Set(TimeSpan.FromMinutes(5));
        manager.OnTick();

        Assert.Equal(TimeSpan.FromMinutes(5), manager.TimeLeft);
    }

    [Fact]
    public void Set_Zero_MakesLimitReached()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(30));

        manager.Set(TimeSpan.Zero);

        Assert.True(manager.IsLimitReached);
    }

    [Fact]
    public void Set_InvalidatesCache()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(30));

        _ = manager.TimeLeft;

        manager.Set(TimeSpan.FromMinutes(5));

        Assert.Equal(TimeSpan.FromMinutes(5), manager.TimeLeft);
    }

    // --- GetFormattedTimeLeft ---

    [Fact]
    public void GetFormattedTimeLeft_WhenExpired_ReturnsThresholdReached()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(5));

        _clock.Advance(TimeSpan.FromMinutes(10));
        manager.OnTick();

        Assert.Equal("ThresholdReached", manager.GetFormattedTimeLeft());
    }

    [Fact]
    public void GetFormattedTimeLeft_UnderOneHour_ReturnsMmSs()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(45) + TimeSpan.FromSeconds(30));

        Assert.Equal("45:30", manager.GetFormattedTimeLeft());
    }

    [Fact]
    public void GetFormattedTimeLeft_OneHourOrMore_ReturnsHMmSs()
    {
        var manager = CreateManager(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(23) + TimeSpan.FromSeconds(45));

        Assert.Equal("1:23:45", manager.GetFormattedTimeLeft());
    }

    [Fact]
    public void GetFormattedTimeLeft_WithCultureInfo_RespectsFormat()
    {
        var manager = CreateManager(TimeSpan.FromMinutes(10));

        var result = manager.GetFormattedTimeLeft(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("10:00", result);
    }
}
