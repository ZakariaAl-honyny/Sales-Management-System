using System.Net.Http;
using System.Collections.Concurrent;

using SalesSystem.DesktopPWF.Messaging.Messages;

namespace SalesSystem.DesktopPWF.Services.App;

/// <summary>
/// WPF EventBus for cross-module communication (Pub/Sub pattern)
/// </summary>
public interface IEventBus
{
    void Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class;
    void Unsubscribe<TMessage>(Action<TMessage> handler) where TMessage : class;
    void Publish<TMessage>(TMessage message) where TMessage : class;
}

/// <summary>
/// Simple in-memory EventBus implementation
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class
    {
        var messageType = typeof(TMessage);
        _handlers.AddOrUpdate(
            messageType,
            _ => new List<Delegate> {
handler },
            (_, existing) =>
            {
                lock (existing)
                {
                    if (!existing.Contains(handler))
                        existing.Add(handler);
                }
                return existing;
            });
    }

    public void Unsubscribe<TMessage>(Action<TMessage> handler) where TMessage : class
    {
        var messageType = typeof(TMessage);
        if (_handlers.TryGetValue(messageType, out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }

    public void Publish<TMessage>(TMessage message) where TMessage : class
    {
        var messageType = typeof(TMessage);
        if (_handlers.TryGetValue(messageType, out var handlers))
        {
            List<Delegate> handlersCopy;
            lock (handlers)
            {
                handlersCopy = handlers.ToList();
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    ((Action<TMessage>)handler)(message);
                }
                catch (Exception ex)
                {
                    // Log but don't crash
                    System.Diagnostics.Debug.WriteLine($"EventBus handler error: {ex.Message}");
                }
            }
        }
    }
}



