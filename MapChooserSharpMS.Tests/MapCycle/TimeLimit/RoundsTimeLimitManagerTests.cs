using MapChooserSharpMS.Modules.MapCycle.Managers.TimeLimit;
using Xunit;

namespace MapChooserSharpMS.Tests.MapCycle.TimeLimit;

public class RoundsTimeLimitManagerTests
{
    private static RoundsTimeLimitManager CreateManager(int initialRoundLimit)
    {
        return new RoundsTimeLimitManager(initialRoundLimit);
    }

    // --- Initial State ---

    [Fact]
    public void InitialState_RoundsLeft_EqualsInitialLimit()
    {
        var manager = CreateManager(10);

        Assert.Equal(10, manager.RoundsLeft);
    }

    [Fact]
    public void InitialState_IsLimitReached_IsFalse()
    {
        var manager = CreateManager(10);

        Assert.False(manager.IsLimitReached);
    }

    [Fact]
    public void InitialState_ZeroRounds_IsLimitReached_IsTrue()
    {
        var manager = CreateManager(0);

        Assert.True(manager.IsLimitReached);
    }

    // --- OnTick (Round Decrement) ---

    [Fact]
    public void OnTick_DecrementsRoundsLeft()
    {
        var manager = CreateManager(10);

        manager.OnTick();

        Assert.Equal(9, manager.RoundsLeft);
    }

    [Fact]
    public void OnTick_RoundsLeft_NeverGoesBelowZero()
    {
        var manager = CreateManager(0);

        manager.OnTick();

        Assert.Equal(0, manager.RoundsLeft);
    }

    [Fact]
    public void OnTick_MultipleCalls_DecrementsCorrectly()
    {
        var manager = CreateManager(10);

        manager.OnTick();
        manager.OnTick();
        manager.OnTick();

        Assert.Equal(7, manager.RoundsLeft);
    }

    // --- Extend / Set ---

    [Fact]
    public void Extend_IncreasesRoundsLeft()
    {
        var manager = CreateManager(5);

        manager.Extend(3);

        Assert.Equal(8, manager.RoundsLeft);
    }

    [Fact]
    public void Set_OverwritesRoundsLeft()
    {
        var manager = CreateManager(10);

        manager.Set(3);

        Assert.Equal(3, manager.RoundsLeft);
    }

    [Fact]
    public void Set_Zero_MakesLimitReached()
    {
        var manager = CreateManager(10);

        manager.Set(0);

        Assert.True(manager.IsLimitReached);
    }

    // --- GetFormattedRoundsLeft ---

    [Fact]
    public void GetFormattedRoundsLeft_WhenExpired_ReturnsZero()
    {
        var manager = CreateManager(0);

        Assert.Equal("0", manager.GetFormattedRoundsLeft());
    }

    [Fact]
    public void GetFormattedRoundsLeft_Positive_ReturnsString()
    {
        var manager = CreateManager(5);

        Assert.Equal("5", manager.GetFormattedRoundsLeft());
    }
}
