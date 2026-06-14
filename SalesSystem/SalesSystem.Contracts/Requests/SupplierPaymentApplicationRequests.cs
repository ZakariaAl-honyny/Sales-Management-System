namespace SalesSystem.Contracts.Requests;

public record CreateSupplierPaymentApplicationRequest(
    int SupplierPaymentId,
    int PurchaseInvoiceId,
    decimal AppliedAmount);
