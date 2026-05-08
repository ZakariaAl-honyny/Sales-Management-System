using SalesSystem.Desktop.Services.Interfaces;
using System.Collections.Concurrent;

namespace SalesSystem.Desktop.Services;

public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<object>> _subscriptions = new();

    public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class
    {
        var messageType = typeof(TMessage);
        var handlers = _subscriptions.GetOrAdd(messageType, _ => new List<object>());
        
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new Unsubscriber(handlers, handler);
    }

    public void Publish<TMessage>(TMessage message) where TMessage : class
    {
        var messageType = typeof(TMessage);
        if (_subscriptions.TryGetValue(messageType, out var handlers))
        {
            List<object> handlersCopy;
            lock (handlers)
            {
                handlersCopy = new List<object>(handlers);
            }

            foreach (var handler in handlersCopy)
            {
                ((Action<TMessage>)handler).Invoke(message);
            }
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<object> _handlers;
        private readonly object _handler;

        public Unsubscriber(List<object> handlers, object handler)
        {
            _handlers = handlers;
            _handler = handler;
        }

        public void Dispose()
        {
            lock (_handlers)
            {
                _handlers.Remove(_handler);
            }
        }
    }
}
