namespace SalesSystem.Desktop.Services.Interfaces;

public interface IEventBus
{
    IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class;
    void Publish<TMessage>(TMessage message) where TMessage : class;
}
