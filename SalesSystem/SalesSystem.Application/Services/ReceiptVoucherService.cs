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

public class ReceiptVoucherService : IReceiptVoucherService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _documentSequence;
    private readonly IJournalEntryService _journalEntryService;
    private readonly ISystemAccountService _systemAccountService;
    private readonly ILogger<ReceiptVoucherService> _logger;

    public ReceiptVoucherService(
        IUnitOfWork uow,
        IDocumentSequenceService documentSequence,
        IJournalEntryService journalEntryService,
        ISystemAccountService systemAccountService,
        ILogger<ReceiptVoucherService> logger)
    {
        _uow = uow;
        _documentSequence = documentSequence;
        _journalEntryService = journalEntryService;
        _systemAccountService = systemAccountService;
        _logger = logger;
    }

    public async Task<Result<ReceiptVoucherDto>> CreateAsync(CreateReceiptVoucherRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var voucherNo = await GetNextVoucherNumberAsync(ct);
            if (voucherNo <= 0)
                return Result<ReceiptVoucherDto>.Failure("فشل في توليد رقم سند القبض");

            var voucher = ReceiptVoucher.Create(
                voucherNo,
                request.VoucherDate,
                request.CurrencyId,
                request.CashBoxId,
                request.AccountId,
                request.TotalAmount,
                request.Notes,
                userId);

            await _uow.ReceiptVouchers.AddAsync(voucher, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("ReceiptVoucher created (No: {VoucherNo}, ID: {Id}) by User {UserId}",
                voucher.VoucherNo, voucher.Id, userId);

            var created = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == voucher.Id, ct, "Currency", "CashBox", "Account");
            return Result<ReceiptVoucherDto>.Success(MapToDto(created!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating receipt voucher: {Message}", ex.Message);
            return Result<ReceiptVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating receipt voucher");
            return Result<ReceiptVoucherDto>.Failure("حدث خطأ أثناء إنشاء سند القبض");
        }
    }

    public async Task<Result<ReceiptVoucherDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            if (voucher == null)
                return Result<ReceiptVoucherDto>.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            return Result<ReceiptVoucherDto>.Success(MapToDto(voucher));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipt voucher {Id}", id);
            return Result<ReceiptVoucherDto>.Failure("حدث خطأ أثناء استرجاع بيانات سند القبض");
        }
    }

    public async Task<Result<PagedResult<ReceiptVoucherDto>>> GetAllAsync(
        string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        try
        {
            System.Linq.Expressions.Expression<Func<ReceiptVoucher, bool>>? predicate = null;

            if (from.HasValue || to.HasValue || !string.IsNullOrWhiteSpace(search))
            {
                predicate = v =>
                    (!from.HasValue || v.VoucherDate >= from.Value) &&
                    (!to.HasValue || v.VoucherDate <= to.Value) &&
                    (string.IsNullOrWhiteSpace(search) ||
                     v.VoucherNo.ToString().Contains(search) ||
                     (v.Notes != null && v.Notes.Contains(search)));
            }

            var (items, totalCount) = await _uow.ReceiptVouchers.GetPagedAsync(
                predicate,
                orderConfig: q => q.OrderByDescending(v => v.VoucherDate).ThenByDescending(v => v.Id),
                page,
                pageSize,
                ct,
                includePaths: new[] { "Currency", "CashBox", "Account" });

            var dtos = items.Select(MapToDto).ToList();
            var result = PagedResult<ReceiptVoucherDto>.Create(dtos, totalCount, page, pageSize);

            return Result<PagedResult<ReceiptVoucherDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipt vouchers list");
            return Result<PagedResult<ReceiptVoucherDto>>.Failure("حدث خطأ أثناء استرجاع قائمة سندات القبض");
        }
    }

    public async Task<Result<ReceiptVoucherDto>> UpdateAsync(int id, UpdateReceiptVoucherRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            if (voucher == null)
                return Result<ReceiptVoucherDto>.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            voucher.Update(
                request.VoucherDate,
                request.Notes,
                userId);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("ReceiptVoucher {Id} updated by User {UserId}", id, userId);

            var updated = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            return Result<ReceiptVoucherDto>.Success(MapToDto(updated!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating receipt voucher {Id}: {Message}", id, ex.Message);
            return Result<ReceiptVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating receipt voucher {Id}", id);
            return Result<ReceiptVoucherDto>.Failure("حدث خطأ أثناء تحديث سند القبض");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.ReceiptVouchers.GetByIdAsync(id, ct);
            if (voucher == null)
                return Result.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            if (voucher.Status == VoucherStatus.Posted)
                return Result.Failure("لا يمكن حذف سند قبض مرحّل. قم بإلغائه أولاً.");

            voucher.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("ReceiptVoucher {Id} cancelled (deleted)", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting receipt voucher {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف سند القبض");
        }
    }

    public async Task<Result<ReceiptVoucherDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            if (voucher == null)
                return Result<ReceiptVoucherDto>.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            return await _uow.ExecuteTransactionAsync<Result<ReceiptVoucherDto>>(async () =>
            {
                voucher.Post(userId);

                // Create journal entry: Dr Cash (default cash account) / Cr Account (credited account)
                var cashResult = await _systemAccountService.GetMappingAsync(SystemAccountKey.DefaultCash, null, ct);
                if (!cashResult.IsSuccess || cashResult.Value == null)
                    return Result<ReceiptVoucherDto>.Failure("الحساب النقدي النظامي غير مهيأ");

                var defaultCashAccountId = cashResult.Value.AccountId;

                var journalRequest = new CreateJournalEntryRequest(
                    EntryDate: voucher.VoucherDate,
                    Description: $"قيد سند قبض رقم {voucher.VoucherNo}",
                    EntryType: JournalEntryType.CustomerReceipt,
                    ReferenceType: "ReceiptVoucher",
                    ReferenceId: voucher.Id,
                    ReferenceNumber: voucher.VoucherNo.ToString(),
                    Lines: new List<JournalEntryLineRequest>
                    {
                        new(defaultCashAccountId, voucher.TotalAmount, 0, "سند قبض - الجانب المدين (النقدية)"),
                        new(voucher.AccountId, 0, voucher.TotalAmount, $"سند قبض - الجانب الدائن ({voucher.Account?.NameAr ?? "الحساب"})")
                    }
                );

                var entryResult = await _journalEntryService.CreateJournalEntryAsync(journalRequest, userId, ct);
                if (!entryResult.IsSuccess)
                    return Result<ReceiptVoucherDto>.Failure(entryResult.Error!);

                var postResult = await _journalEntryService.PostJournalEntryAsync(entryResult.Value, userId, ct);
                if (!postResult.IsSuccess)
                    return Result<ReceiptVoucherDto>.Failure(postResult.Error!);

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("ReceiptVoucher {Id} posted by User {UserId}", id, userId);

                var posted = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                    v => v.Id == id, ct, "Currency", "CashBox", "Account");
                return Result<ReceiptVoucherDto>.Success(MapToDto(posted!));
            }, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation posting receipt voucher {Id}: {Message}", id, ex.Message);
            return Result<ReceiptVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting receipt voucher {Id}", id);
            return Result<ReceiptVoucherDto>.Failure("حدث خطأ أثناء ترحيل سند القبض");
        }
    }

    public async Task<Result<ReceiptVoucherDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            if (voucher == null)
                return Result<ReceiptVoucherDto>.Failure("سند القبض غير موجود", ErrorCodes.NotFound);

            var wasPosted = voucher.Status == VoucherStatus.Posted;

            return await _uow.ExecuteTransactionAsync<Result<ReceiptVoucherDto>>(async () =>
            {
                voucher.Cancel(userId);

                // If posted, reverse the journal entry
                if (wasPosted)
                {
                    var cashResult = await _systemAccountService.GetMappingAsync(SystemAccountKey.DefaultCash, null, ct);
                    if (!cashResult.IsSuccess || cashResult.Value == null)
                        return Result<ReceiptVoucherDto>.Failure("الحساب النقدي النظامي غير مهيأ");

                    var defaultCashAccountId = cashResult.Value.AccountId;

                    var reverseRequest = new CreateJournalEntryRequest(
                        EntryDate: DateTime.UtcNow,
                        Description: $"قيد عكس سند قبض رقم {voucher.VoucherNo}",
                        EntryType: JournalEntryType.Manual,
                        ReferenceType: "ReceiptVoucher",
                        ReferenceId: voucher.Id,
                        ReferenceNumber: $"{voucher.VoucherNo}-REV",
                        Lines: new List<JournalEntryLineRequest>
                        {
                            new(voucher.AccountId, voucher.TotalAmount, 0, $"عكس سند قبض - الجانب المدين ({voucher.Account?.NameAr ?? "الحساب"})"),
                            new(defaultCashAccountId, 0, voucher.TotalAmount, "عكس سند قبض - الجانب الدائن (النقدية)")
                        }
                    );

                    var entryResult = await _journalEntryService.CreateJournalEntryAsync(reverseRequest, userId, ct);
                    if (!entryResult.IsSuccess)
                        return Result<ReceiptVoucherDto>.Failure(entryResult.Error!);

                    var postResult = await _journalEntryService.PostJournalEntryAsync(entryResult.Value, userId, ct);
                    if (!postResult.IsSuccess)
                        return Result<ReceiptVoucherDto>.Failure(postResult.Error!);
                }

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("ReceiptVoucher {Id} cancelled by User {UserId}", id, userId);

                var cancelled = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                    v => v.Id == id, ct, "Currency", "CashBox", "Account");
                return Result<ReceiptVoucherDto>.Success(MapToDto(cancelled!));
            }, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation cancelling receipt voucher {Id}: {Message}", id, ex.Message);
            return Result<ReceiptVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling receipt voucher {Id}", id);
            return Result<ReceiptVoucherDto>.Failure("حدث خطأ أثناء إلغاء سند القبض");
        }
    }

    // ─── Private Helpers ─────────────────────────────────

    private async Task<int> GetNextVoucherNumberAsync(CancellationToken ct)
    {
        var seqResult = await _documentSequence.GetNextIntAsync("ReceiptVoucher", ct);
        return seqResult.IsSuccess ? seqResult.Value : 0;
    }

    private static ReceiptVoucherDto MapToDto(ReceiptVoucher voucher)
    {
        return new ReceiptVoucherDto(
            voucher.Id,
            voucher.VoucherNo,
            voucher.VoucherDate,
            voucher.CurrencyId,
            voucher.Currency?.Name,
            voucher.Currency?.Code,
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
