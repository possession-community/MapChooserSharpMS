using System;
using System.Globalization;
using MapChooserSharpMS.Shared.Events;
using Xunit;

namespace MapChooserSharpMS.Tests.Events;

public class EditableEventTests
{
    private interface ITestListener : IEventListenerBase
    {
        void OnEditableEvent(ITestEditableParams p);
    }

    private interface ITestEditableParams : IMcsEditableEvent
    {
        int Value { get; set; }
    }

    private sealed class TestEditableParams : ITestEditableParams
    {
        public bool IsCancelled { get; set; }
        public int Value { get; set; }
    }

    private sealed class ModifyingListener(int priority, int newValue) : ITestListener
    {
        public int ListenerPriority => priority;
        public void OnEditableEvent(ITestEditableParams p) => p.Value = newValue;
    }

    private sealed class CancellingListener(int priority) : ITestListener
    {
        public int ListenerPriority => priority;
        public void OnEditableEvent(ITestEditableParams p) => p.IsCancelled = true;
    }

    private sealed class ConditionalModifyListener(int priority, int newValue) : ITestListener
    {
        public int ListenerPriority => priority;

        public void OnEditableEvent(ITestEditableParams p)
        {
            if (!p.IsCancelled)
                p.Value = newValue;
        }
    }

    [Fact]
    public void Listener_CanModifyParams()
    {
        var dispatcher = new TestEventDispatcher();
        dispatcher.RegisterListener<ITestListener>(new ModifyingListener(100, 42));

        var p = new TestEditableParams { Value = 0 };
        dispatcher.Fire<ITestListener>(e => e.OnEditableEvent(p));

        Assert.Equal(42, p.Value);
        Assert.False(p.IsCancelled);
    }

    [Fact]
    public void Listener_CanCancelEvent()
    {
        var dispatcher = new TestEventDispatcher();
        dispatcher.RegisterListener<ITestListener>(new CancellingListener(100));

        var p = new TestEditableParams { Value = 10 };
        dispatcher.Fire<ITestListener>(e => e.OnEditableEvent(p));

        Assert.True(p.IsCancelled);
        Assert.Equal(10, p.Value);
    }

    [Fact]
    public void MultipleListeners_LastWriteWins()
    {
        var dispatcher = new TestEventDispatcher();
        dispatcher.RegisterListener<ITestListener>(new ModifyingListener(100, 10));
        dispatcher.RegisterListener<ITestListener>(new ModifyingListener(50, 20));

        var p = new TestEditableParams { Value = 0 };
        dispatcher.Fire<ITestListener>(e => e.OnEditableEvent(p));

        Assert.Equal(20, p.Value);
    }

    [Fact]
    public void CancelledByHighPriority_LowPrioritySeesIt()
    {
        var dispatcher = new TestEventDispatcher();
        dispatcher.RegisterListener<ITestListener>(new CancellingListener(100));
        dispatcher.RegisterListener<ITestListener>(new ConditionalModifyListener(50, 99));

        var p = new TestEditableParams { Value = 0 };
        dispatcher.Fire<ITestListener>(e => e.OnEditableEvent(p));

        Assert.True(p.IsCancelled);
        Assert.Equal(0, p.Value);
    }

    [Fact]
    public void IMapCooldownApplyEventParams_ImplementsIMcsEditableEvent()
    {
        Assert.True(typeof(IMcsEditableEvent).IsAssignableFrom(
            typeof(MapChooserSharpMS.Shared.Events.MapCycle.Params.IMapCooldownApplyEventParams)));
    }
}
