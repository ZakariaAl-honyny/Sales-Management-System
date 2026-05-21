using SalesSystem.Desktop.Services.Interfaces;
using System.Collections.Concurrent;

namespace SalesSystem.Desktop.Services;

public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<WeakReference<object>>> _subscriptions = new();

    public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class
    {
        var messageType = typeof(TMessage);
        var handlers = _subscriptions.GetOrAdd(messageType, _ => new List<WeakReference<object>>());
        
        var reference = new WeakReference<object>(handler);
        lock (handlers)
        {
            handlers.Add(reference);
        }

        return new Unsubscriber(handlers, reference);
    }

    public void Publish<TMessage>(TMessage message) where TMessage : class
    {
        var messageType = typeof(TMessage);
        if (_subscriptions.TryGetValue(messageType, out var handlers))
        {
            List<Action<TMessage>> toInvoke = new();
            
            lock (handlers)
            {
                for (int i = handlers.Count - 1; i >= 0; i--)
                {
                    if (handlers[i].TryGetTarget(out var target))
                    {
                        toInvoke.Add((Action<TMessage>)target);
                    }
                    else
                    {
                        handlers.RemoveAt(i); // Cleanup dead refs
                    }
                }
            }

            foreach (var action in toInvoke)
            {
                InvokeHandler(action, message);
            }
        }
    }

    private void InvokeHandler<TMessage>(Action<TMessage> handler, TMessage message)
    {
        var mainForm = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
        
        if (mainForm != null && mainForm.InvokeRequired)
        {
            mainForm.BeginInvoke(() => handler(message));
        }
        else
        {
            handler(message);
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<WeakReference<object>> _handlers;
        private readonly WeakReference<object> _reference;

        public Unsubscriber(List<WeakReference<object>> handlers, WeakReference<object> reference)
        {
            _handlers = handlers;
            _reference = reference;
        }

        public void Dispose()
        {
            lock (_handlers)
            {
                _handlers.Remove(_reference);
            }
        }
    }
}

