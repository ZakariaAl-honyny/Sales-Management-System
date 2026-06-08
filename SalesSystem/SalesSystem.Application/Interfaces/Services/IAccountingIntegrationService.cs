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
        decimal openingBalance,
        int createdByUserId,
        DateTime transactionDate,
        CancellationToken ct = default);

    /// <summary>
    /// Creates journal entry for a posted sales invoice.
    /// Revenue side: Dr Cash/AR / Cr SalesRevenue + VatOutput.
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
    /// Creates journal entry for a customer payment (receipt).
    /// Dr Cash / Cr AccountsReceivable.
    /// </summary>
    Task<Result<int>> CreateCustomerPaymentEntryAsync(
        CustomerPayment payment,
        string customerName,
        int createdByUserId,
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
    /// Reverses a customer payment journal entry.
    /// Dr AccountsReceivable / Cr Cash.
    /// </summary>
    Task<Result<int>> ReverseCustomerPaymentEntryAsync(
        int paymentId,
        decimal amount,
        string customerName,
        int reversedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Reverses a supplier payment journal entry.
    /// Dr Cash / Cr AccountsPayable.
    /// </summary>
    Task<Result<int>> ReverseSupplierPaymentEntryAsync(
        int paymentId,
        decimal amount,
        string supplierName,
        int reversedByUserId,
        CancellationToken ct = default);
}
