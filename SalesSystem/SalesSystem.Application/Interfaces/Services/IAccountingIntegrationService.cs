using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Auto-creates balanced double-entry journal entries for all business operations
/// (sales, purchases, payments, opening balances).
/// Callers are responsible for wrapping calls inside <see cref="IUnitOfWork.ExecuteTransactionAsync"/>.
/// </summary>
public interface IAccountingIntegrationService
{
    /// <summary>
    /// Creates opening balance journal entry for a customer.
    /// Dr AccountsReceivableAccount / Cr OpeningBalanceEquityAccount.
    /// </summary>
    Task<Result<int>> CreateCustomerOpeningEntryAsync(
        int customerId,
        string customerName,
        int customerAccountId,
        decimal openingBalance,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default);

    /// <summary>
    /// Creates opening balance journal entry for a supplier.
    /// Dr OpeningBalanceEquityAccount / Cr AccountsPayableAccount.
    /// </summary>
    Task<Result<int>> CreateSupplierOpeningEntryAsync(
        int supplierId,
        string supplierName,
        int supplierAccountId,
        decimal openingBalance,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default);

    /// <summary>
    /// Creates opening balance journal entry for product (inventory opening stock).
    /// Dr InventoryAssetAccount / Cr OpeningBalanceEquityAccount.
    /// </summary>
    Task<Result<int>> CreateProductOpeningEntryAsync(
        int productId,
        string productName,
        decimal totalOpeningValue,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default);

    /// <summary>
    /// Creates journal entry for a posted sales invoice.
    /// Revenue side: Dr Cash/AR / Cr SalesRevenue + DeliveryChargesRevenue + VatOutput.
    /// COGS side: Dr COGS / Cr Inventory.
    /// </summary>
    Task<Result<int>> CreateSalesPostEntryAsync(
        SalesInvoice invoice,
        int createdByUserId,
        decimal totalCost,
        CancellationToken ct = default);

    /// <summary>
    /// Reverses (cancels) a previously posted sales invoice journal entry.
    /// </summary>
    Task<Result<int>> ReverseSalesPostEntryAsync(
        SalesInvoice invoice,
        int reversedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates journal entry for a posted purchase invoice.
    /// Dr Inventory + VatInput / Cr Cash + AccountsPayable.
    /// </summary>
    Task<Result<int>> CreatePurchasePostEntryAsync(
        PurchaseInvoice invoice,
        int createdByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Reverses (cancels) a previously posted purchase invoice journal entry.
    /// </summary>
    Task<Result<int>> ReversePurchasePostEntryAsync(
        PurchaseInvoice invoice,
        int reversedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates journal entry for a customer receipt.
    /// Dr Cash / Cr AccountsReceivable.
    /// </summary>
    Task<Result<int>> CreateCustomerPaymentEntryAsync(
        CustomerReceipt receipt,
        string customerName,
        int createdByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Reverses a customer receipt journal entry.
    /// Dr AccountsReceivable / Cr Cash.
    /// </summary>
    Task<Result<int>> ReverseCustomerPaymentEntryAsync(
        int receiptId,
        decimal amount,
        string customerName,
        int customerAccountId,
        int reversedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates journal entry for a supplier payment.
    /// Dr AccountsPayable / Cr Cash.
    /// </summary>
    Task<Result<int>> CreateSupplierPaymentEntryAsync(
        SupplierPayment payment,
        string supplierName,
        int createdByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Reverses a supplier payment journal entry.
    /// Dr Cash / Cr AccountsPayable.
    /// </summary>
    Task<Result<int>> ReverseSupplierPaymentEntryAsync(
        int paymentId,
        decimal amount,
        string supplierName,
        int supplierAccountId,
        int reversedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates journal entry for a standalone sales return.
    /// Reverses revenue: Dr SalesReturnsAccount / Cr CustomerAccount (for the return amount).
    /// Reverses COGS: Dr InventoryAccount / Cr COGSAccount (for the returned items' cost).
    /// Uses per-entity Customer.AccountId with fallback to AccountsReceivable.
    /// </summary>
    Task<Result<int>> CreateSalesReturnEntryAsync(
        SalesReturn salesReturn,
        decimal totalCost,
        int createdByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Reverses a posted sales return journal entry (cancellation of return).
    /// Re-creates the original sale effect:
    /// Dr: CustomerAccount = TotalAmount
    /// Cr: SalesReturnsAccount = TotalAmount
    /// Dr: COGSAccount = totalCost
    /// Cr: InventoryAccount = totalCost
    /// </summary>
    Task<Result<int>> ReverseSalesReturnEntryAsync(
        SalesReturn salesReturn,
        decimal totalCost,
        int reversedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates journal entry for a posted purchase return.
    /// Reverses the original purchase entry:
    /// Dr: AccountsPayable (supplier account) = TotalAmount
    /// Cr: PurchaseReturnAccount = TotalAmount
    /// </summary>
    Task<Result<int>> CreatePurchaseReturnEntryAsync(
        PurchaseReturn purchaseReturn,
        int createdByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Reverses a posted purchase return journal entry (cancellation of return).
    /// Re-creates the original purchase effect:
    /// Dr: PurchaseReturnAccount = TotalAmount
    /// Cr: AccountsPayable (supplier account) = TotalAmount
    /// </summary>
    Task<Result<int>> ReversePurchaseReturnEntryAsync(
        PurchaseReturn purchaseReturn,
        int reversedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates journal entry for a posted expense.
    /// Dr ExpenseAccount (from Expense.ExpenseAccountId) / Cr CashBox.Account (from CashBox linked Account).
    /// </summary>
    Task<Result<int>> CreateExpenseEntryAsync(
        Expense expense,
        int createdByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Reverses a posted expense journal entry (cancellation of expense).
    /// Dr CashBox.Account / Cr ExpenseAccount.
    /// </summary>
    Task<Result<int>> ReverseExpenseEntryAsync(
        Expense expense,
        int reversedByUserId,
        CancellationToken ct = default);
}
