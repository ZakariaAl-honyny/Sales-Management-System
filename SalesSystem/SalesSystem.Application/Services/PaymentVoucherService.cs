using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class PaymentVoucherService : IPaymentVoucherService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _documentSequence;
    private readonly IJournalEntryService _journalEntryService;
    private readonly ISystemAccountService _systemAccountService;
    private readonly ILogger<PaymentVoucherService> _logger;

    public PaymentVoucherService(
        IUnitOfWork uow,
        IDocumentSequenceService documentSequence,
        IJournalEntryService journalEntryService,
        ISystemAccountService systemAccountService,
        ILogger<PaymentVoucherService> logger)
    {
        _uow = uow;
        _documentSequence = documentSequence;
        _journalEntryService = journalEntryService;
        _systemAccountService = systemAccountService;
        _logger = logger;
    }

    public async Task<Result<PaymentVoucherDto>> CreateAsync(CreatePaymentVoucherRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var voucherNo = await GetNextVoucherNumberAsync(ct);
            if (voucherNo <= 0)
                return Result<PaymentVoucherDto>.Failure("فشل في توليد رقم سند الصرف");

            var voucher = PaymentVoucher.Create(
                voucherNo,
                request.VoucherDate,
                request.CashBoxId,
                request.AccountId,
                request.TotalAmount,
                request.Notes,
                userId);

            await _uow.PaymentVouchers.AddAsync(voucher, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("PaymentVoucher created (No: {VoucherNo}, ID: {Id}) by User {UserId}",
                voucher.VoucherNo, voucher.Id, userId);

            var created = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == voucher.Id, ct, "CashBox", "Account");
            return Result<PaymentVoucherDto>.Success(MapToDto(created!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating payment voucher: {Message}", ex.Message);
            return Result<PaymentVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment voucher");
            return Result<PaymentVoucherDto>.Failure("حدث خطأ أثناء إنشاء سند الصرف");
        }
    }

    public async Task<Result<PaymentVoucherDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "CashBox", "Account");
            if (voucher == null)
                return Result<PaymentVoucherDto>.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            return Result<PaymentVoucherDto>.Success(MapToDto(voucher));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment voucher {Id}", id);
            return Result<PaymentVoucherDto>.Failure("حدث خطأ أثناء استرجاع بيانات سند الصرف");
        }
    }

    public async Task<Result<PagedResult<PaymentVoucherDto>>> GetAllAsync(
        string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        try
        {
            System.Linq.Expressions.Expression<Func<PaymentVoucher, bool>>? predicate = null;

            if (from.HasValue || to.HasValue || !string.IsNullOrWhiteSpace(search))
            {
                predicate = v =>
                    (!from.HasValue || v.VoucherDate >= from.Value) &&
                    (!to.HasValue || v.VoucherDate <= to.Value) &&
                    (string.IsNullOrWhiteSpace(search) ||
                     v.VoucherNo.ToString().Contains(search) ||
                     (v.Notes != null && v.Notes.Contains(search)));
            }

            var (items, totalCount) = await _uow.PaymentVouchers.GetPagedAsync(
                predicate,
                orderConfig: q => q.OrderByDescending(v => v.VoucherDate).ThenByDescending(v => v.Id),
                page,
                pageSize,
                ct,
                includePaths: new[] { "CashBox", "Account" });

            var dtos = items.Select(MapToDto).ToList();
            var result = PagedResult<PaymentVoucherDto>.Create(dtos, totalCount, page, pageSize);

            return Result<PagedResult<PaymentVoucherDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment vouchers list");
            return Result<PagedResult<PaymentVoucherDto>>.Failure("حدث خطأ أثناء استرجاع قائمة سندات الصرف");
        }
    }

    public async Task<Result<PaymentVoucherDto>> UpdateAsync(int id, UpdatePaymentVoucherRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "CashBox", "Account");
            if (voucher == null)
                return Result<PaymentVoucherDto>.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            voucher.Update(
                request.VoucherDate,
                request.Notes,
                userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("PaymentVoucher {Id} updated by User {UserId}", id, userId);

            var updated = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "CashBox", "Account");
            return Result<PaymentVoucherDto>.Success(MapToDto(updated!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating payment voucher {Id}: {Message}", id, ex.Message);
            return Result<PaymentVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment voucher {Id}", id);
            return Result<PaymentVoucherDto>.Failure("حدث خطأ أثناء تحديث سند الصرف");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.PaymentVouchers.GetByIdAsync(id, ct);
            if (voucher == null)
                return Result.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            if (voucher.Status == VoucherStatus.Posted)
                return Result.Failure("لا يمكن حذف سند صرف مرحّل. قم بإلغائه أولاً.");

            voucher.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("PaymentVoucher {Id} cancelled (deleted)", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment voucher {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف سند الصرف");
        }
    }

    public async Task<Result<PaymentVoucherDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "CashBox", "Account");
            if (voucher == null)
                return Result<PaymentVoucherDto>.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            return await _uow.ExecuteTransactionAsync<Result<PaymentVoucherDto>>(async () =>
            {
                voucher.Post(userId);

                // Create journal entry: Dr Account (debited account) / Cr Cash (default cash account)
                var cashResult = await _systemAccountService.GetMappingAsync(SystemAccountKey.DefaultCash, null, ct);
                if (!cashResult.IsSuccess || cashResult.Value == null)
                    return Result<PaymentVoucherDto>.Failure("الحساب النقدي النظامي غير مهيأ");

                var defaultCashAccountId = cashResult.Value.AccountId;

                var journalRequest = new CreateJournalEntryRequest(
                    EntryDate: voucher.VoucherDate,
                    Description: $"قيد سند صرف رقم {voucher.VoucherNo}",
                    EntryType: JournalEntryType.SupplierPayment,
                    ReferenceType: "PaymentVoucher",
                    ReferenceId: voucher.Id,
                    ReferenceNumber: voucher.VoucherNo.ToString(),
                    Lines: new List<JournalEntryLineRequest>
                    {
                        new(voucher.AccountId, voucher.TotalAmount, 0, $"سند صرف - الجانب المدين ({voucher.Account?.NameAr ?? "الحساب"})"),
                        new(defaultCashAccountId, 0, voucher.TotalAmount, "سند صرف - الجانب الدائن (النقدية)")
                    }
                );

                var entryResult = await _journalEntryService.CreateJournalEntryAsync(journalRequest, userId, ct);
                if (!entryResult.IsSuccess)
                    return Result<PaymentVoucherDto>.Failure(entryResult.Error!);

                var postResult = await _journalEntryService.PostJournalEntryAsync(entryResult.Value, userId, ct);
                if (!postResult.IsSuccess)
                    return Result<PaymentVoucherDto>.Failure(postResult.Error!);

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("PaymentVoucher {Id} posted by User {UserId}", id, userId);

                var posted = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                    v => v.Id == id, ct, "CashBox", "Account");
                return Result<PaymentVoucherDto>.Success(MapToDto(posted!));
            }, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation posting payment voucher {Id}: {Message}", id, ex.Message);
            return Result<PaymentVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting payment voucher {Id}", id);
            return Result<PaymentVoucherDto>.Failure("حدث خطأ أثناء ترحيل سند الصرف");
        }
    }

    public async Task<Result<PaymentVoucherDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "CashBox", "Account");
            if (voucher == null)
                return Result<PaymentVoucherDto>.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            var wasPosted = voucher.Status == VoucherStatus.Posted;

            return await _uow.ExecuteTransactionAsync<Result<PaymentVoucherDto>>(async () =>
            {
                voucher.Cancel(userId);

                // If posted, reverse the journal entry
                if (wasPosted)
                {
                    var cashResult = await _systemAccountService.GetMappingAsync(SystemAccountKey.DefaultCash, null, ct);
                    if (!cashResult.IsSuccess || cashResult.Value == null)
                        return Result<PaymentVoucherDto>.Failure("الحساب النقدي النظامي غير مهيأ");

                    var defaultCashAccountId = cashResult.Value.AccountId;

                    var reverseRequest = new CreateJournalEntryRequest(
                        EntryDate: DateTime.UtcNow,
                        Description: $"قيد عكس سند صرف رقم {voucher.VoucherNo}",
                        EntryType: JournalEntryType.Manual,
                        ReferenceType: "PaymentVoucher",
                        ReferenceId: voucher.Id,
                        ReferenceNumber: $"{voucher.VoucherNo}-REV",
                        Lines: new List<JournalEntryLineRequest>
                        {
                            new(defaultCashAccountId, voucher.TotalAmount, 0, "عكس سند صرف - الجانب المدين (النقدية)"),
                            new(voucher.AccountId, 0, voucher.TotalAmount, $"عكس سند صرف - الجانب الدائن ({voucher.Account?.NameAr ?? "الحساب"})")
                        }
                    );

                    var entryResult = await _journalEntryService.CreateJournalEntryAsync(reverseRequest, userId, ct);
                    if (!entryResult.IsSuccess)
                        return Result<PaymentVoucherDto>.Failure(entryResult.Error!);

                    var postResult = await _journalEntryService.PostJournalEntryAsync(entryResult.Value, userId, ct);
                    if (!postResult.IsSuccess)
                        return Result<PaymentVoucherDto>.Failure(postResult.Error!);
                }

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("PaymentVoucher {Id} cancelled by User {UserId}", id, userId);

                var cancelled = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                    v => v.Id == id, ct, "CashBox", "Account");
                return Result<PaymentVoucherDto>.Success(MapToDto(cancelled!));
            }, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation cancelling payment voucher {Id}: {Message}", id, ex.Message);
            return Result<PaymentVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment voucher {Id}", id);
            return Result<PaymentVoucherDto>.Failure("حدث خطأ أثناء إلغاء سند الصرف");
        }
    }

    // ─── Private Helpers ─────────────────────────────────

    private async Task<int> GetNextVoucherNumberAsync(CancellationToken ct)
    {
        var seqResult = await _documentSequence.GetNextIntAsync("PaymentVoucher", ct);
        return seqResult.IsSuccess ? seqResult.Value : 0;
    }

    private static PaymentVoucherDto MapToDto(PaymentVoucher voucher)
    {
        return new PaymentVoucherDto(
            voucher.Id,
            voucher.VoucherNo,
            voucher.VoucherDate,
            voucher.CashBoxId,
            voucher.CashBox?.Name,
            voucher.AccountId,
            voucher.Account?.NameAr,
            voucher.TotalAmount,
            voucher.Notes,
            (byte)voucher.Status,
            voucher.CreatedAt,
            voucher.PostedAt,
            voucher.CancelledAt
        );
    }
}
