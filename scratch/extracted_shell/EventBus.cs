    internal class SubscriptionToken : IDisposable
    {
        private readonly Action _unsubscribe;

        public SubscriptionToken(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            _unsubscribe?.Invoke();
        }
    }
}
EventBus.cs
ÇáÊØÈíŞ ÇáİÚáí ááäÇŞá¡ ãÚ ãÑÇÚÇÉ ÃãÇä ÇáÎíæØ (Thread-Safety).
C#
using System.Collections.Concurrent;

namespace SalesSystem.Desktop.Messaging
{
    public class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, List<object>> _subscribers = new();

        public IDisposable Subscribe<TMessage>(Action<TMessage> handler)
        {
            var messageType = typeof(TMessage);
            
            if (!_subscribers.ContainsKey(messageType))
            {
                _subscribers[messageType] = new List<object>();
            }

            _subscribers[messageType].Add(handler);

            // ÅÑÌÇÚ ßÇÆä ãÓÄæá Úä ÍĞİ åĞÇ ÇáÇÔÊÑÇß ÚäÏ ÊÏãíÑ ÇáÔÇÔÉ (Dispose)
            return new SubscriptionToken(() =>
            {
                if (_subscribers.TryGetValue(messageType, out var handlers))
                {
                    lock (handlers)
                    {
                        handlers.Remove(handler);
                    }
                }
            });
        }

        public void Publish<TMessage>(TMessage message)
        {
            var messageType = typeof(TMessage);

            if (_subscribers.TryGetValue(messageType, out var handlers))
            {
                // äÃÎĞ äÓÎÉ ãä ÇáŞÇÆãÉ (ToList) áÊÌäÈ ÎØÃ ÊÚÏíá ÇáŞÇÆãÉ ÃËäÇÁ ÇáãÑæÑ ÚáíåÇ
                List<object> handlersCopy;
                lock (handlers)
                {
                    handlersCopy = handlers.ToList();
                }

                foreach (var handler in handlersCopy)
                {
                    var action = (Action<TMessage>)handler;
                    action(message);
                }
            }
        }
    }
}

2. ÊÚÑíİ ÇáÑÓÇÆá (Messages)
