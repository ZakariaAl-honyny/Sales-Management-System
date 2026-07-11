namespace SalesSystem.DesktopPWF.Messaging.Messages;

public record ProductChangedMessage(int ProductId);
public record UnitChangedMessage(int UnitId);
public record CustomerChangedMessage(int CustomerId);
public record SupplierChangedMessage(int SupplierId);
public record UserChangedMessage(int UserId);
public record WarehouseChangedMessage(int WarehouseId);
public record SaleInvoiceChangedMessage(int InvoiceId);
public record PurchaseInvoiceChangedMessage(int InvoiceId);
public record SalesReturnChangedMessage(int ReturnId);
public record PurchaseReturnChangedMessage(int ReturnId);
public record WarehouseTransferChangedMessage(int TransferId);
public record SupplierPaymentChangedMessage(int PaymentId);
public record StockChangedMessage(int ProductId, int WarehouseId);
public record CashBoxChangedMessage(int CashBoxId);
public record TaxChangedMessage(int TaxId);
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
/// Published when a customer receipt is created, posted, or cancelled.
/// Carries the receipt ID only — NO data payload (RULE-034).
/// </summary>
public record CustomerReceiptChangedMessage(int ReceiptId);

/// <summary>
/// Published when an inventory count is created, posted, or cancelled.
/// Carries the count ID only — NO data payload (RULE-034).
/// </summary>
public record InventoryCountChangedMessage(int CountId);

/// <summary>
/// Published when an inventory adjustment is created, posted, or cancelled.
/// Carries the adjustment ID only — NO data payload (RULE-034).
/// </summary>
public record InventoryAdjustmentChangedMessage(int AdjustmentId);

/// <summary>
/// Published when a notification is marked as read or deleted.
/// Carries no data payload (RULE-034) — notifications are per-user.
/// </summary>
public record NotificationChangedMessage;

/// <summary>
/// Published when an attachment is created, updated, or deleted.
/// Carries no data payload (RULE-034).
/// </summary>
public record AttachmentChangedMessage;

/// <summary>
/// Published when a payment voucher is created, posted, cancelled, or modified.
/// Carries the voucher ID only — NO data payload (RULE-034).
/// </summary>
public record PaymentVoucherChangedMessage(int VoucherId);

/// <summary>
/// Published when a receipt voucher is created, posted, cancelled, or updated.
/// Carries the voucher ID for targeted refresh (RULE-034).
/// </summary>
public record ReceiptVoucherChangedMessage(int VoucherId);

/// <summary>
/// Published when an inventory transaction is created, posted, or cancelled.
/// Carries the transaction ID only — NO data payload (RULE-034).
/// </summary>
public record InventoryTransactionChangedMessage(int TransactionId);


