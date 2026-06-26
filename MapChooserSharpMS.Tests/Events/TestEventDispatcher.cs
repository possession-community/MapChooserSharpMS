using System;
using System.Collections.Generic;
using System.Linq;
using MapChooserSharpMS.Shared.Events;

namespace MapChooserSharpMS.Tests.Events;

internal sealed class TestEventDispatcher
{
    private readonly Dictionary<Type, List<IEventListenerBase>> _listeners = new();

    internal void RegisterListener<TListener>(TListener listener) where TListener : IEventListenerBase
    {
        var key = typeof(TListener);
        if (!_listeners.TryGetValue(key, out var list))
        {
            list = new List<IEventListenerBase>();
            _listeners[key] = list;
        }
        list.Add(listener);
        list.Sort((a, b) => b.ListenerPriority.CompareTo(a.ListenerPriority));
    }

    internal void Fire<TListener>(Action<TListener> action) where TListener : IEventListenerBase
    {
        foreach (var listener in GetListeners<TListener>())
            action((TListener)listener);
    }

    internal McsCancellableEvent FireCancellable<TListener>(Func<TListener, McsCancellableEvent> handler)
        where TListener : IEventListenerBase
    {
        foreach (var listener in GetListeners<TListener>())
        {
            var result = handler((TListener)listener);
            if (result == McsCancellableEvent.Stop)
                return McsCancellableEvent.Stop;
            if (result == McsCancellableEvent.Handled)
                return McsCancellableEvent.Handled;
        }
        return McsCancellableEvent.Continue;
    }

    internal TResult? FireWithResult<TListener, TResult>(
        Func<TListener, TResult> selector,
        Func<TResult, bool> shouldStop,
        TResult? defaultValue = default)
        where TListener : IEventListenerBase
    {
        foreach (var listener in GetListeners<TListener>())
        {
            var result = selector((TListener)listener);
            if (shouldStop(result))
                return result;
        }
        return defaultValue;
    }

    private List<IEventListenerBase> GetListeners<TListener>() where TListener : IEventListenerBase
    {
        return _listeners.TryGetValue(typeof(TListener), out var list)
            ? list.ToList()
            : [];
    }
}
