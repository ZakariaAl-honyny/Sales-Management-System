åĞå åí ÇáÃÍÏÇË ÇáÊí ÓíÊã ÊÏÇæáåÇ İí ÇáäÙÇã. ÓäÓÊÎÏã ÇáÜ record áÃäåÇ ÎİíİÉ æãäÇÓÈÉ ÌÏÇğ áåĞÇ ÇáÛÑÖ.
ÃäÔÆ ãáİÇğ ÈÇÓã EntityChangedMessages.cs ÏÇÎá ãÌáÏ Messaging.
C#
namespace SalesSystem.Desktop.Messaging
{
    // ÑÓÇÆá ÊÍÏíË ÇáßíÇäÇÊ ÇáÃÓÇÓíÉ
    public record ProductChangedMessage();
    public record CustomerChangedMessage();
    public record SupplierChangedMessage();
    public record WarehouseChangedMessage();
    
    // íãßä ÊãÑíÑ ÈíÇäÇÊ ÏÇÎá ÇáÑÓÇáÉ ÅĞÇ áÒã ÇáÃãÑ
    public record InvoiceCreatedMessage(int InvoiceId, string InvoiceType);
}

3. ÊÓÌíá ÇáÜ EventBus İí ÇáÜ Dependency Injection
ÇİÊÍ Program.cs İí ãÔÑæÚ SalesSystem.Desktop æŞã ÈÊÓÌíá ÇáÜ EventBus ßÜ Singleton (ãåã ÌÏÇğ Ãä íßæä Singleton áíßæä äÇŞáÇğ ãÑßÒíÇğ æÇÍÏÇğ áßá ÇáÔÇÔÇÊ).
C#
// ÊÓÌíá EventBus ßÜ Singleton
services.AddSingleton<IEventBus, EventBus>();
