using System.Collections.Concurrent;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services;

public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<WeakReference<Delegate>>> _subscriptions = new();
    private readonly object _lock = new();

    public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class
    {
        var messageType = typeof(TMessage);
        var weakHandler = new WeakReference<Delegate>(handler);

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<WeakReference<Delegate>>();
                _subscriptions[messageType] = handlers;
            }
            handlers.Add(weakHandler);
        }

        return new SubscriptionToken(() => Unsubscribe(messageType, weakHandler));
    }

    public void Publish<TMessage>(TMessage message) where TMessage : class
    {
        var messageType = typeof(TMessage);
        if (!_subscriptions.TryGetValue(messageType, out var handlers)) return;

        List<Delegate> aliveHandlers = new();
        lock (_lock)
        {
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (handlers[i].TryGetTarget(out var target))
                {
                    aliveHandlers.Add(target);
                }
                else
                {
                    handlers.RemoveAt(i);
                }
            }
        }

        foreach (var handler in aliveHandlers)
        {
            ExecuteHandler(handler, message);
        }
    }

    private void ExecuteHandler(Delegate handler, object message)
    {
        var mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;

        if (mainForm != null && mainForm.InvokeRequired)
        {
            mainForm.Invoke(() => handler.DynamicInvoke(message));
        }
        else
        {
            handler.DynamicInvoke(message);
        }
    }

    private void Unsubscribe(Type messageType, WeakReference<Delegate> weakHandler)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(messageType, out var handlers))
            {
                handlers.Remove(weakHandler);
            }
        }
    }

    private class SubscriptionToken : IDisposable
    {
        private readonly Action _unsubscribe;
        public SubscriptionToken(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}
