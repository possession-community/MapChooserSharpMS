using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events;
using Xunit;

namespace MapChooserSharpMS.Tests.Events;

public class ValueOverrideDispatchTests
{
    private interface ITestListener : IEventListenerBase
    {
        McsValueOverrideEvent<List<string>> OnPick();
    }

    private sealed class NoOverrideListener(int priority) : ITestListener
    {
        public int ListenerPriority => priority;
        public McsValueOverrideEvent<List<string>> OnPick() => McsValueOverrideEvent<List<string>>.NoOverride;
    }

    private sealed class OverrideListener(int priority, List<string> value) : ITestListener
    {
        public int ListenerPriority => priority;
        public McsValueOverrideEvent<List<string>> OnPick() => new(value);
    }

    [Fact]
    public void NoListeners_ReturnsDefault()
    {
        var dispatcher = new TestEventDispatcher();
        var result = dispatcher.FireWithResult<ITestListener, McsValueOverrideEvent<List<string>>>(
            e => e.OnPick(),
            r => r.HasValue,
            McsValueOverrideEvent<List<string>>.NoOverride);

        Assert.False(result.HasValue);
    }

    [Fact]
    public void AllNoOverride_ReturnsDefault()
    {
        var dispatcher = new TestEventDispatcher();
        dispatcher.RegisterListener<ITestListener>(new NoOverrideListener(100));
        dispatcher.RegisterListener<ITestListener>(new NoOverrideListener(50));

        var result = dispatcher.FireWithResult<ITestListener, McsValueOverrideEvent<List<string>>>(
            e => e.OnPick(),
            r => r.HasValue,
            McsValueOverrideEvent<List<string>>.NoOverride);

        Assert.False(result.HasValue);
    }

    [Fact]
    public void FirstOverride_Wins()
    {
        var dispatcher = new TestEventDispatcher();
        var highPriorityList = new List<string> { "map_a", "map_b" };
        var lowPriorityList = new List<string> { "map_c" };

        dispatcher.RegisterListener<ITestListener>(new OverrideListener(100, highPriorityList));
        dispatcher.RegisterListener<ITestListener>(new OverrideListener(50, lowPriorityList));

        var result = dispatcher.FireWithResult<ITestListener, McsValueOverrideEvent<List<string>>>(
            e => e.OnPick(),
            r => r.HasValue,
            McsValueOverrideEvent<List<string>>.NoOverride);

        Assert.True(result.HasValue);
        Assert.Same(highPriorityList, result.Value);
    }

    [Fact]
    public void LowPriorityOverride_AfterHighPriorityNoOverride()
    {
        var dispatcher = new TestEventDispatcher();
        var overrideList = new List<string> { "map_x" };

        dispatcher.RegisterListener<ITestListener>(new NoOverrideListener(100));
        dispatcher.RegisterListener<ITestListener>(new OverrideListener(50, overrideList));

        var result = dispatcher.FireWithResult<ITestListener, McsValueOverrideEvent<List<string>>>(
            e => e.OnPick(),
            r => r.HasValue,
            McsValueOverrideEvent<List<string>>.NoOverride);

        Assert.True(result.HasValue);
        Assert.Same(overrideList, result.Value);
    }
}
