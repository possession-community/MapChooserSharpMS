using System.Collections.Generic;
using MapChooserSharpMS.Shared.Events;
using Xunit;

namespace MapChooserSharpMS.Tests.Events;

public class FireCancellableTests
{
    private interface ITestListener : IEventListenerBase
    {
        McsCancellableEvent OnTestEvent();
    }

    private sealed class TestListener(int priority, McsCancellableEvent result) : ITestListener
    {
        public int ListenerPriority => priority;
        internal int CallCount { get; private set; }

        public McsCancellableEvent OnTestEvent()
        {
            CallCount++;
            return result;
        }
    }

    [Fact]
    public void NoListeners_ReturnsContinue()
    {
        var dispatcher = new TestEventDispatcher();
        var result = dispatcher.FireCancellable<ITestListener>(e => e.OnTestEvent());
        Assert.Equal(McsCancellableEvent.Continue, result);
    }

    [Fact]
    public void AllContinue_ReturnsContinue()
    {
        var dispatcher = new TestEventDispatcher();
        var l1 = new TestListener(100, McsCancellableEvent.Continue);
        var l2 = new TestListener(50, McsCancellableEvent.Continue);
        dispatcher.RegisterListener<ITestListener>(l1);
        dispatcher.RegisterListener<ITestListener>(l2);

        var result = dispatcher.FireCancellable<ITestListener>(e => e.OnTestEvent());

        Assert.Equal(McsCancellableEvent.Continue, result);
        Assert.Equal(1, l1.CallCount);
        Assert.Equal(1, l2.CallCount);
    }

    [Fact]
    public void Stop_StopsPropagationAndReturnsStop()
    {
        var dispatcher = new TestEventDispatcher();
        var l1 = new TestListener(100, McsCancellableEvent.Stop);
        var l2 = new TestListener(50, McsCancellableEvent.Continue);
        dispatcher.RegisterListener<ITestListener>(l1);
        dispatcher.RegisterListener<ITestListener>(l2);

        var result = dispatcher.FireCancellable<ITestListener>(e => e.OnTestEvent());

        Assert.Equal(McsCancellableEvent.Stop, result);
        Assert.Equal(1, l1.CallCount);
        Assert.Equal(0, l2.CallCount);
    }

    [Fact]
    public void Handled_StopsPropagationAndReturnsHandled()
    {
        var dispatcher = new TestEventDispatcher();
        var l1 = new TestListener(100, McsCancellableEvent.Handled);
        var l2 = new TestListener(50, McsCancellableEvent.Continue);
        dispatcher.RegisterListener<ITestListener>(l1);
        dispatcher.RegisterListener<ITestListener>(l2);

        var result = dispatcher.FireCancellable<ITestListener>(e => e.OnTestEvent());

        Assert.Equal(McsCancellableEvent.Handled, result);
        Assert.Equal(1, l1.CallCount);
        Assert.Equal(0, l2.CallCount);
    }

    [Fact]
    public void LowPriority_Stop_StillStopsAfterHighPriorityContinue()
    {
        var dispatcher = new TestEventDispatcher();
        var l1 = new TestListener(100, McsCancellableEvent.Continue);
        var l2 = new TestListener(50, McsCancellableEvent.Stop);
        var l3 = new TestListener(10, McsCancellableEvent.Continue);
        dispatcher.RegisterListener<ITestListener>(l1);
        dispatcher.RegisterListener<ITestListener>(l2);
        dispatcher.RegisterListener<ITestListener>(l3);

        var result = dispatcher.FireCancellable<ITestListener>(e => e.OnTestEvent());

        Assert.Equal(McsCancellableEvent.Stop, result);
        Assert.Equal(1, l1.CallCount);
        Assert.Equal(1, l2.CallCount);
        Assert.Equal(0, l3.CallCount);
    }

    [Fact]
    public void PriorityOrdering_HighPriorityCalledFirst()
    {
        var dispatcher = new TestEventDispatcher();
        var callOrder = new List<string>();

        var l1 = new OrderTrackingListener(10, McsCancellableEvent.Continue, "low", callOrder);
        var l2 = new OrderTrackingListener(100, McsCancellableEvent.Continue, "high", callOrder);
        var l3 = new OrderTrackingListener(50, McsCancellableEvent.Continue, "mid", callOrder);

        dispatcher.RegisterListener<ITestListener>(l1);
        dispatcher.RegisterListener<ITestListener>(l2);
        dispatcher.RegisterListener<ITestListener>(l3);

        dispatcher.FireCancellable<ITestListener>(e => e.OnTestEvent());

        Assert.Equal(["high", "mid", "low"], callOrder);
    }

    private sealed class OrderTrackingListener(int priority, McsCancellableEvent result, string name, List<string> callOrder) : ITestListener
    {
        public int ListenerPriority => priority;

        public McsCancellableEvent OnTestEvent()
        {
            callOrder.Add(name);
            return result;
        }
    }
}
