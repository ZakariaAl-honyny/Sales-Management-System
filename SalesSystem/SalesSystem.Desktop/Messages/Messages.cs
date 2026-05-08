namespace SalesSystem.Desktop.Messages;

// Base — all messages derive from this
public abstract record EntityChangedMessage(int EntityId);

// Concrete message types
public record ProductChangedMessage(int EntityId)    : EntityChangedMessage(EntityId);
public record CustomerChangedMessage(int EntityId)   : EntityChangedMessage(EntityId);
public record SupplierChangedMessage(int EntityId)   : EntityChangedMessage(EntityId);
public record SalesInvoiceChangedMessage(int EntityId) : EntityChangedMessage(EntityId);
public record PurchaseInvoiceChangedMessage(int EntityId) : EntityChangedMessage(EntityId);
public record StockChangedMessage(int EntityId)      : EntityChangedMessage(EntityId);
public record SessionExpiredMessage(int EntityId = 0) : EntityChangedMessage(EntityId);
