using System;
using MapChooserSharpMS.Modules.Nomination.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MapChooserSharpMS.Tests.Nomination;

public class PlayerNominationCooldownServiceTests
{
    private static PlayerNominationCooldownService Create()
        => new(NullLogger.Instance, "test-server", new StubWulingSurreal());

    private const ulong Player1 = 76561198000000001;
    private const ulong Player2 = 76561198000000002;

    [Fact]
    public void NotInCooldown_ByDefault()
    {
        var svc = Create();
        Assert.False(svc.IsInCooldown(Player1));
    }

    [Fact]
    public void SetCooldown_CountBased_BlocksPlayer()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 3, 0);
        Assert.True(svc.IsInCooldown(Player1));
        Assert.False(svc.IsInCooldown(Player2));
    }

    [Fact]
    public void SetCooldown_TimedBased_BlocksPlayer()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 0, 60f);
        Assert.True(svc.IsInCooldown(Player1));
    }

    [Fact]
    public void SetCooldown_BothAxes_BlocksPlayer()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 2, 60f);
        Assert.True(svc.IsInCooldown(Player1));

        var state = svc.GetState(Player1);
        Assert.NotNull(state);
        Assert.Equal(2, state.RemainingCount);
        Assert.True(state.CooldownUntil > DateTime.UtcNow);
    }

    [Fact]
    public void SetCooldown_Zero_RemovesCooldown()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 3, 0);
        Assert.True(svc.IsInCooldown(Player1));

        svc.SetCooldown(Player1, 0, 0);
        Assert.False(svc.IsInCooldown(Player1));
    }

    [Fact]
    public void DecrementAll_ReducesCount()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 3, 0);
        svc.SetCooldown(Player2, 1, 0);

        svc.DecrementAll();

        var s1 = svc.GetState(Player1);
        Assert.NotNull(s1);
        Assert.Equal(2, s1.RemainingCount);

        Assert.False(svc.IsInCooldown(Player2));
        Assert.Null(svc.GetState(Player2));
    }

    [Fact]
    public void DecrementAll_RemovesWhenCountReachesZero()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 1, 0);

        svc.DecrementAll();

        Assert.False(svc.IsInCooldown(Player1));
    }

    [Fact]
    public void DecrementAll_KeepsTimedEvenWhenCountZero()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 1, 3600f);

        svc.DecrementAll();

        Assert.True(svc.IsInCooldown(Player1));
        var state = svc.GetState(Player1);
        Assert.NotNull(state);
        Assert.Equal(0, state.RemainingCount);
        Assert.True(state.CooldownUntil > DateTime.UtcNow);
    }

    [Fact]
    public void MultipleDecrements_EventuallyRemove()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 3, 0);

        svc.DecrementAll();
        svc.DecrementAll();
        svc.DecrementAll();

        Assert.False(svc.IsInCooldown(Player1));
    }

    [Fact]
    public void GetState_ReturnsNull_WhenNotSet()
    {
        var svc = Create();
        Assert.Null(svc.GetState(Player1));
    }

    [Fact]
    public void IsInCooldown_ClearsExpiredState()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 0, -1f);
        Assert.False(svc.IsInCooldown(Player1));
    }

    [Fact]
    public void IndependentPlayers_DontAffectEachOther()
    {
        var svc = Create();
        svc.SetCooldown(Player1, 5, 0);
        svc.SetCooldown(Player2, 2, 0);

        svc.DecrementAll();

        Assert.Equal(4, svc.GetState(Player1)!.RemainingCount);
        Assert.Equal(1, svc.GetState(Player2)!.RemainingCount);
    }
}
