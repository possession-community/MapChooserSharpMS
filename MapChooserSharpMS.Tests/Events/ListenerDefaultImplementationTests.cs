using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events;
using MapChooserSharpMS.Shared.Events.MapCycle;
using MapChooserSharpMS.Shared.Events.MapVote;
using MapChooserSharpMS.Shared.Events.Nomination;
using MapChooserSharpMS.Shared.Events.RockTheVote;
using MapChooserSharpMS.Shared.MapConfig;
using Xunit;

namespace MapChooserSharpMS.Tests.Events;

public class ListenerDefaultImplementationTests
{
    private sealed class MinimalMapVoteListener : IMapVoteEventListener
    {
        public int ListenerPriority => 0;
    }

    private sealed class MinimalRtvListener : IRockTheVoteEventListener
    {
        public int ListenerPriority => 0;
    }

    private sealed class MinimalMapCycleListener : IMapCycleEventListener
    {
        public int ListenerPriority => 0;
    }

    private sealed class MinimalNominationListener : INominationEventListener
    {
        public int ListenerPriority => 0;
    }

    [Fact]
    public void MapVoteListener_OnMapVoteStart_DefaultsContinue()
    {
        IMapVoteEventListener listener = new MinimalMapVoteListener();
        Assert.Equal(McsCancellableEvent.Continue, listener.OnMapVoteStart(null!));
    }

    [Fact]
    public void MapVoteListener_OnRandomMapPick_DefaultsNoOverride()
    {
        IMapVoteEventListener listener = new MinimalMapVoteListener();
        var result = listener.OnRandomMapPick(null!);
        Assert.False(result.HasValue);
    }

    [Fact]
    public void RtvListener_OnClientRtvCast_DefaultsContinue()
    {
        IRockTheVoteEventListener listener = new MinimalRtvListener();
        Assert.Equal(McsCancellableEvent.Continue, listener.OnClientRtvCast(null!));
    }

    [Fact]
    public void RtvListener_OnClientRtvUnCast_DefaultsContinue()
    {
        IRockTheVoteEventListener listener = new MinimalRtvListener();
        Assert.Equal(McsCancellableEvent.Continue, listener.OnClientRtvUnCast(null!));
    }

    [Fact]
    public void RtvListener_OnForceRtv_DefaultsContinue()
    {
        IRockTheVoteEventListener listener = new MinimalRtvListener();
        Assert.Equal(McsCancellableEvent.Continue, listener.OnForceRtv(null!));
    }

    [Fact]
    public void MapCycleListener_OnExtCommandExecute_DefaultsContinue()
    {
        IMapCycleEventListener listener = new MinimalMapCycleListener();
        Assert.Equal(McsCancellableEvent.Continue, listener.OnExtCommandExecute(null!));
    }

    [Fact]
    public void NominationListener_OnNominationCheckPassed_DefaultsContinue()
    {
        INominationEventListener listener = new MinimalNominationListener();
        Assert.Equal(McsCancellableEvent.Continue, listener.OnNominationCheckPassed(null!));
    }

    [Fact]
    public void NominationListener_OnNomination_DefaultsContinue()
    {
        INominationEventListener listener = new MinimalNominationListener();
        Assert.Equal(McsCancellableEvent.Continue, listener.OnNomination(null!));
    }

    [Fact]
    public void NominationListener_OnAdminNomination_DefaultsContinue()
    {
        INominationEventListener listener = new MinimalNominationListener();
        Assert.Equal(McsCancellableEvent.Continue, listener.OnAdminNomination(null!));
    }

    [Fact]
    public void CancellableEvent_StopDoesNotEqualContinue()
    {
        Assert.True(McsCancellableEvent.Stop != McsCancellableEvent.Continue);
    }

    [Fact]
    public void CancellableEvent_StopDoesNotEqualHandled()
    {
        Assert.True(McsCancellableEvent.Stop != McsCancellableEvent.Handled);
    }
}
