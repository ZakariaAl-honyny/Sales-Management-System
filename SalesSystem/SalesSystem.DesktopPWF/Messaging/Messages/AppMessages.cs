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

