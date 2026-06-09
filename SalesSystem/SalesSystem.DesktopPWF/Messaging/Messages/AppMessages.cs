namespace SalesSystem.DesktopPWF.Messaging.Messages;

public record ProductChangedMessage(int ProductId);
public record CategoryChangedMessage(int CategoryId);
public record UnitChangedMessage(int UnitId);
public record CustomerChangedMessage(int CustomerId);
public record SupplierChangedMessage(int SupplierId);
public record UserChangedMessage(int UserId);
public record WarehouseChangedMessage(int WarehouseId);
public record SaleInvoiceChangedMessage(int InvoiceId);
public record PurchaseInvoiceChangedMessage(int InvoiceId);
public record SalesReturnChangedMessage(int ReturnId);
public record PurchaseReturnChangedMessage(int ReturnId);
public record StockTransferChangedMessage(int TransferId);
public record CustomerPaymentChangedMessage(int PaymentId);
public record SupplierPaymentChangedMessage(int PaymentId);
public record StockChangedMessage(int ProductId, int WarehouseId);
public record CashBoxChangedMessage(int CashBoxId);
public record TaxChangedMessage(int TaxId);
public record CurrencyChangedMessage(int CurrencyId);
public record CurrencyRateChangedMessage(int CurrencyId);

/// <summary>
/// Published when store settings are updated. Carries no data payload (RULE-034).
/// </summary>
public record StoreSettingsChangedMessage;

/// <summary>
/// Published when the application needs to shut down (e.g., after backup restore).
/// The Shell/MainWindow should subscribe and call Application.Current.Shutdown().
/// </summary>
public record ApplicationShutdownMessage;

public record AccountChangedMessage(int AccountId);

/// <summary>
/// Published when a journal entry is created, posted, reversed, or modified.
/// Carries the entry ID only — NO data payload (RULE-034).
/// </summary>
public record JournalEntryChangedMessage(int EntryId);

/// <summary>
/// Published when a product price is created, updated, or deactivated.
/// Carries the price ID only — NO data payload (RULE-034).
/// </summary>
public record ProductPriceChangedMessage(int PriceId);

/// <summary>
/// Published when an inventory batch is created or deactivated.
/// Carries the batch ID only — NO data payload (RULE-034).
/// </summary>
public record InventoryBatchChangedMessage(int BatchId);

/// <summary>
/// Published when a product image is created, set as primary, or deleted.
/// Carries the image ID only — NO data payload (RULE-034).
/// </summary>
public record ProductImageChangedMessage(int ImageId);

/// <summary>
/// Published when an inventory operation is created, posted, or cancelled.
/// Carries the operation ID only — NO data payload (RULE-034).
/// </summary>
public record InventoryOperationChangedMessage(int Id);

/// <summary>
/// Published when a purchase order is created, updated, or cancelled.
/// Carries the order ID only — NO data payload (RULE-034).
/// </summary>
public record PurchaseOrderChangedMessage(int OrderId);

