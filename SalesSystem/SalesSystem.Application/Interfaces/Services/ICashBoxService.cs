using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing cash boxes and their associated receipt/payment vouchers.
/// </summary>
public interface ICashBoxService
{
    // Cash Box CRUD
    Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CashBoxDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<CashBoxDto>> CreateAsync(CreateCashBoxRequest request, int userId, CancellationToken ct = default);
    Task<Result<CashBoxDto>> UpdateAsync(int id, UpdateCashBoxRequest request, int userId, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);

    // Receipt Vouchers (سندات قبض)
    Task<Result<ReceiptVoucherDto>> CreateReceiptVoucherAsync(CreateReceiptVoucherRequest request, int userId, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> PostReceiptVoucherAsync(int id, int userId, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> CancelReceiptVoucherAsync(int id, int userId, CancellationToken ct = default);
    Task<Result<List<ReceiptVoucherDto>>> GetReceiptVouchersAsync(int cashBoxId, CancellationToken ct = default);

    // Payment Vouchers (سندات صرف)
    Task<Result<PaymentVoucherDto>> CreatePaymentVoucherAsync(CreatePaymentVoucherRequest request, int userId, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> PostPaymentVoucherAsync(int id, int userId, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> CancelPaymentVoucherAsync(int id, int userId, CancellationToken ct = default);
    Task<Result<List<PaymentVoucherDto>>> GetPaymentVouchersAsync(int cashBoxId, CancellationToken ct = default);

    // Transfer
    Task<Result> TransferAsync(CashTransferRequest request, int userId, CancellationToken ct = default);

    // Invoice payment recording (called by payment services)
    Task<Result<ReceiptVoucherDto>> RecordInvoiceReceiptAsync(int cashBoxId, short currencyId, decimal amount, int accountId, string? notes = null, int? referenceId = null, string? referenceType = null, int userId = 0, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> RecordInvoicePaymentAsync(int cashBoxId, short currencyId, decimal amount, int accountId, string? notes = null, int userId = 0, CancellationToken ct = default);
}
