using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public interface ICashBoxApiService
{
    // Cash Box CRUD
    Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CashBoxDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<CashBoxDto>> CreateAsync(CreateCashBoxRequest request, CancellationToken ct = default);
    Task<Result<CashBoxDto>> UpdateAsync(int id, UpdateCashBoxRequest request, CancellationToken ct = default);
    Task<Result> DeactivateAsync(int id, CancellationToken ct = default);

    // Receipt Voucher endpoints
    Task<Result<ReceiptVoucherDto>> CreateReceiptVoucherAsync(int cashBoxId, CreateReceiptVoucherRequest request, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> PostReceiptVoucherAsync(int cashBoxId, int voucherId, CancellationToken ct = default);
    Task<Result<ReceiptVoucherDto>> CancelReceiptVoucherAsync(int cashBoxId, int voucherId, CancellationToken ct = default);
    Task<Result<List<ReceiptVoucherDto>>> GetReceiptVouchersAsync(int cashBoxId, CancellationToken ct = default);

    // Payment Voucher endpoints
    Task<Result<PaymentVoucherDto>> CreatePaymentVoucherAsync(int cashBoxId, CreatePaymentVoucherRequest request, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> PostPaymentVoucherAsync(int cashBoxId, int voucherId, CancellationToken ct = default);
    Task<Result<PaymentVoucherDto>> CancelPaymentVoucherAsync(int cashBoxId, int voucherId, CancellationToken ct = default);
    Task<Result<List<PaymentVoucherDto>>> GetPaymentVouchersAsync(int cashBoxId, CancellationToken ct = default);

    // Transfer
    Task<Result> TransferAsync(CashTransferRequest request, CancellationToken ct = default);
}
