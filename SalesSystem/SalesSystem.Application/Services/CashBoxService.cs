using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// Service for managing cash boxes and their associated receipt/payment vouchers.
/// Handles CRUD, auto-account creation, voucher lifecycle, transfers, and
/// invoice payment recording for the cash module.
/// Schema §4.3 CashBoxes: lightweight register, balance tracked on linked Account.
/// </summary>
public class CashBoxService : ICashBoxService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _documentSequence;
    private readonly IJournalEntryService _journalEntryService;
    private readonly ILogger<CashBoxService> _logger;

    public CashBoxService(
        IUnitOfWork uow,
        IDocumentSequenceService documentSequence,
        IJournalEntryService journalEntryService,
        ILogger<CashBoxService> logger)
    {
        _uow = uow;
        _documentSequence = documentSequence;
        _journalEntryService = journalEntryService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // Cash Box CRUD
    // ═══════════════════════════════════════════════════════════

    public async Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var boxes = await _uow.CashBoxes.ToListAsync(ct, "Account");
            var dtos = boxes.Select(MapToDto).ToList();
            return Result<List<CashBoxDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all cash boxes");
            return Result<List<CashBoxDto>>.Failure("حدث خطأ أثناء استرجاع قائمة الصناديق النقدية");
        }
    }

    public async Task<Result<CashBoxDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.FirstOrDefaultAsync(
                b => b.Id == id, ct, "Account");
            if (box == null)
                return Result<CashBoxDto>.Failure("الصندوق النقدي غير موجود", ErrorCodes.NotFound);

            return Result<CashBoxDto>.Success(MapToDto(box));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cash box {Id}", id);
            return Result<CashBoxDto>.Failure("حدث خطأ أثناء استرجاع بيانات الصندوق النقدي");
        }
    }

    public async Task<Result<CashBoxDto>> CreateAsync(CreateCashBoxRequest request, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<CashBoxDto>>(async () =>
        {
            try
            {
                int accountId;

                // Resolve the chart-of-accounts account: either use the provided AccountId
                // or auto-create a Level-4 detail account under parent "1110 — النقدية".
                if (request.AccountId.HasValue && request.AccountId.Value > 0)
                {
                    accountId = request.AccountId.Value;

                    var accountExists = await _uow.Accounts.AnyAsync(
                        a => a.Id == accountId, ct);
                    if (!accountExists)
                        return Result<CashBoxDto>.Failure(
                            "الحساب المحاسبي المحدد غير موجود", ErrorCodes.NotFound);
                }
                else
                {
                    var accountResult = await CreateCashBoxAccountAsync(request.Name, userId, ct);
                    if (!accountResult.IsSuccess || accountResult.Value == null)
                        return Result<CashBoxDto>.Failure(
                            accountResult.Error ?? "فشل إنشاء الحساب المحاسبي للصندوق");

                    accountId = accountResult.Value.Id;
                }

                // Create the cash box domain entity — AccountId is always resolved before this call
                var box = CashBox.Create(
                    request.Name,
                    request.BranchId,
                    accountId,
                    description: request.Description,
                    createdByUserId: userId);

                await _uow.CashBoxes.AddAsync(box, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "CashBox created: {Name} (ID: {Id}, AccountId: {AccountId}) by User {UserId}",
                    box.Name, box.Id, box.AccountId, userId);

                var created = await _uow.CashBoxes.FirstOrDefaultAsync(
                    b => b.Id == box.Id, ct, "Account");
                return Result<CashBoxDto>.Success(MapToDto(created!));
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation creating cash box: {Message}", ex.Message);
                return Result<CashBoxDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cash box");
                return Result<CashBoxDto>.Failure("حدث خطأ أثناء إنشاء الصندوق النقدي");
            }
        }, ct);
    }

    public async Task<Result<CashBoxDto>> UpdateAsync(int id, UpdateCashBoxRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.FirstOrDefaultAsync(
                b => b.Id == id, ct, "Account");
            if (box == null)
                return Result<CashBoxDto>.Failure("الصندوق النقدي غير موجود", ErrorCodes.NotFound);

            box.Update(
                request.Name,
                request.BranchId,
                description: request.Description);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "CashBox updated: {Name} (ID: {Id}) by User {UserId}",
                box.Name, id, userId);

            var updated = await _uow.CashBoxes.FirstOrDefaultAsync(
                b => b.Id == id, ct, "Account");
            return Result<CashBoxDto>.Success(MapToDto(updated!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating cash box {Id}: {Message}", id, ex.Message);
            return Result<CashBoxDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cash box {Id}", id);
            return Result<CashBoxDto>.Failure("حدث خطأ أثناء تحديث بيانات الصندوق النقدي");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var box = await _uow.CashBoxes.GetByIdAsync(id, ct);
            if (box == null)
                return Result.Failure("الصندوق النقدي غير موجود", ErrorCodes.NotFound);

            box.MarkAsDeleted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("CashBox deactivated: {Name} (ID: {Id})", box.Name, id);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation deactivating cash box {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating cash box {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط الصندوق النقدي");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Receipt Vouchers (سندات قبض)
    // ═══════════════════════════════════════════════════════════

    public async Task<Result<ReceiptVoucherDto>> CreateReceiptVoucherAsync(
        CreateReceiptVoucherRequest request, int userId, CancellationToken ct)
    {
        return await CreateReceiptVoucherInternalAsync(
            request.CashBoxId,
            request.CurrencyId,
            request.AccountId,
            request.TotalAmount,
            request.Notes,
            null,   // no reference for user-created vouchers
            null,
            userId,
            ct);
    }

    public async Task<Result<ReceiptVoucherDto>> PostReceiptVoucherAsync(
        int id, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            if (voucher == null)
                return Result<ReceiptVoucherDto>.Failure(
                    "سند القبض غير موجود", ErrorCodes.NotFound);

            voucher.Post(userId);

            // Build journal entry: Dr CashBox Account / Cr Credited Account
            var box = await _uow.CashBoxes.FirstOrDefaultAsync(
                b => b.Id == voucher.CashBoxId, ct, "Account");
            if (box == null)
                return Result<ReceiptVoucherDto>.Failure(
                    "الصندوق النقدي غير موجود");

            var cashAccountId = box.AccountId;

            var journalRequest = new CreateJournalEntryRequest(
                EntryDate: voucher.VoucherDate,
                Description: $"قيد سند قبض رقم {voucher.VoucherNo}",
                EntryType: JournalEntryType.CustomerReceipt,
                ReferenceType: "ReceiptVoucher",
                ReferenceId: voucher.Id,
                ReferenceNumber: voucher.VoucherNo.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(cashAccountId, voucher.TotalAmount, 0,
                        "سند قبض - الجانب المدين (النقدية)"),
                    new(voucher.AccountId, 0, voucher.TotalAmount,
                        $"سند قبض - الجانب الدائن ({voucher.Account?.NameAr ?? "الحساب"})")
                });

            var entryResult = await _journalEntryService.CreateJournalEntryAsync(
                journalRequest, userId, ct);
            if (!entryResult.IsSuccess)
                return Result<ReceiptVoucherDto>.Failure(entryResult.Error!);

            var postResult = await _journalEntryService.PostJournalEntryAsync(
                entryResult.Value, userId, ct);
            if (!postResult.IsSuccess)
                return Result<ReceiptVoucherDto>.Failure(postResult.Error!);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "ReceiptVoucher {Id} posted by User {UserId}", id, userId);

            var posted = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            return Result<ReceiptVoucherDto>.Success(MapReceiptVoucherToDto(posted!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex, "Domain rule violation posting receipt voucher {Id}: {Message}",
                id, ex.Message);
            return Result<ReceiptVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting receipt voucher {Id}", id);
            return Result<ReceiptVoucherDto>.Failure(
                "حدث خطأ أثناء ترحيل سند القبض");
        }
    }

    public async Task<Result<ReceiptVoucherDto>> CancelReceiptVoucherAsync(
        int id, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            if (voucher == null)
                return Result<ReceiptVoucherDto>.Failure(
                    "سند القبض غير موجود", ErrorCodes.NotFound);

            var wasPosted = voucher.Status == VoucherStatus.Posted;
            voucher.Cancel(userId);

            // Reverse journal entry if the voucher was previously posted
            if (wasPosted)
            {
                var box = await _uow.CashBoxes.FirstOrDefaultAsync(
                    b => b.Id == voucher.CashBoxId, ct, "Account");
                if (box == null)
                    return Result<ReceiptVoucherDto>.Failure(
                        "الصندوق النقدي غير موجود");

                var cashAccountId = box.AccountId;

                var reverseRequest = new CreateJournalEntryRequest(
                    EntryDate: DateTime.UtcNow,
                    Description: $"قيد عكس سند قبض رقم {voucher.VoucherNo}",
                    EntryType: JournalEntryType.Manual,
                    ReferenceType: "ReceiptVoucher",
                    ReferenceId: voucher.Id,
                    ReferenceNumber: $"{voucher.VoucherNo}-REV",
                    Lines: new List<JournalEntryLineRequest>
                    {
                        new(voucher.AccountId, voucher.TotalAmount, 0,
                            $"عكس سند قبض - الجانب المدين ({voucher.Account?.NameAr ?? "الحساب"})"),
                        new(cashAccountId, 0, voucher.TotalAmount,
                            "عكس سند قبض - الجانب الدائن (النقدية)")
                    });

                var entryResult = await _journalEntryService.CreateJournalEntryAsync(
                    reverseRequest, userId, ct);
                if (!entryResult.IsSuccess)
                    return Result<ReceiptVoucherDto>.Failure(entryResult.Error!);

                var postResult = await _journalEntryService.PostJournalEntryAsync(
                    entryResult.Value, userId, ct);
                if (!postResult.IsSuccess)
                    return Result<ReceiptVoucherDto>.Failure(postResult.Error!);
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "ReceiptVoucher {Id} cancelled by User {UserId}", id, userId);

            var cancelled = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            return Result<ReceiptVoucherDto>.Success(MapReceiptVoucherToDto(cancelled!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex, "Domain rule violation cancelling receipt voucher {Id}: {Message}",
                id, ex.Message);
            return Result<ReceiptVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling receipt voucher {Id}", id);
            return Result<ReceiptVoucherDto>.Failure(
                "حدث خطأ أثناء إلغاء سند القبض");
        }
    }

    public async Task<Result<List<ReceiptVoucherDto>>> GetReceiptVouchersAsync(
        int cashBoxId, CancellationToken ct)
    {
        try
        {
            var boxExists = await _uow.CashBoxes.AnyAsync(b => b.Id == cashBoxId, ct);
            if (!boxExists)
                return Result<List<ReceiptVoucherDto>>.Failure(
                    "الصندوق النقدي غير موجود", ErrorCodes.NotFound);

            var vouchers = await _uow.ReceiptVouchers.ToListAsync(
                v => v.CashBoxId == cashBoxId,
                q => q.OrderByDescending(v => v.VoucherDate)
                      .ThenByDescending(v => v.Id),
                ct: ct,
                includePaths: new[] { "Currency", "CashBox", "Account" });

            var dtos = vouchers.Select(MapReceiptVoucherToDto).ToList();
            return Result<List<ReceiptVoucherDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Error retrieving receipt vouchers for cash box {CashBoxId}",
                cashBoxId);
            return Result<List<ReceiptVoucherDto>>.Failure(
                "حدث خطأ أثناء استرجاع سندات القبض");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Payment Vouchers (سندات صرف)
    // ═══════════════════════════════════════════════════════════

    public async Task<Result<PaymentVoucherDto>> CreatePaymentVoucherAsync(
        CreatePaymentVoucherRequest request, int userId, CancellationToken ct)
    {
        return await CreatePaymentVoucherInternalAsync(
            request.CashBoxId,
            request.CurrencyId,
            request.AccountId,
            request.TotalAmount,
            request.Notes,
            userId,
            ct);
    }

    public async Task<Result<PaymentVoucherDto>> PostPaymentVoucherAsync(
        int id, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            if (voucher == null)
                return Result<PaymentVoucherDto>.Failure(
                    "سند الصرف غير موجود", ErrorCodes.NotFound);

            voucher.Post(userId);

            // Build journal entry: Dr Debited Account / Cr CashBox Account
            var box = await _uow.CashBoxes.FirstOrDefaultAsync(
                b => b.Id == voucher.CashBoxId, ct, "Account");
            if (box == null)
                return Result<PaymentVoucherDto>.Failure(
                    "الصندوق النقدي غير موجود");

            var cashAccountId = box.AccountId;

            var journalRequest = new CreateJournalEntryRequest(
                EntryDate: voucher.VoucherDate,
                Description: $"قيد سند صرف رقم {voucher.VoucherNo}",
                EntryType: JournalEntryType.SupplierPayment,
                ReferenceType: "PaymentVoucher",
                ReferenceId: voucher.Id,
                ReferenceNumber: voucher.VoucherNo.ToString(),
                Lines: new List<JournalEntryLineRequest>
                {
                    new(voucher.AccountId, voucher.TotalAmount, 0,
                        $"سند صرف - الجانب المدين ({voucher.Account?.NameAr ?? "الحساب"})"),
                    new(cashAccountId, 0, voucher.TotalAmount,
                        "سند صرف - الجانب الدائن (النقدية)")
                });

            var entryResult = await _journalEntryService.CreateJournalEntryAsync(
                journalRequest, userId, ct);
            if (!entryResult.IsSuccess)
                return Result<PaymentVoucherDto>.Failure(entryResult.Error!);

            var postResult = await _journalEntryService.PostJournalEntryAsync(
                entryResult.Value, userId, ct);
            if (!postResult.IsSuccess)
                return Result<PaymentVoucherDto>.Failure(postResult.Error!);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "PaymentVoucher {Id} posted by User {UserId}", id, userId);

            var posted = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            return Result<PaymentVoucherDto>.Success(MapPaymentVoucherToDto(posted!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex, "Domain rule violation posting payment voucher {Id}: {Message}",
                id, ex.Message);
            return Result<PaymentVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting payment voucher {Id}", id);
            return Result<PaymentVoucherDto>.Failure(
                "حدث خطأ أثناء ترحيل سند الصرف");
        }
    }

    public async Task<Result<PaymentVoucherDto>> CancelPaymentVoucherAsync(
        int id, int userId, CancellationToken ct)
    {
        try
        {
            var voucher = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            if (voucher == null)
                return Result<PaymentVoucherDto>.Failure(
                    "سند الصرف غير موجود", ErrorCodes.NotFound);

            var wasPosted = voucher.Status == VoucherStatus.Posted;
            voucher.Cancel(userId);

            // Reverse journal entry if the voucher was previously posted
            if (wasPosted)
            {
                var box = await _uow.CashBoxes.FirstOrDefaultAsync(
                    b => b.Id == voucher.CashBoxId, ct, "Account");
                if (box == null)
                    return Result<PaymentVoucherDto>.Failure(
                        "الصندوق النقدي غير موجود");

                var cashAccountId = box.AccountId;

                var reverseRequest = new CreateJournalEntryRequest(
                    EntryDate: DateTime.UtcNow,
                    Description: $"قيد عكس سند صرف رقم {voucher.VoucherNo}",
                    EntryType: JournalEntryType.Manual,
                    ReferenceType: "PaymentVoucher",
                    ReferenceId: voucher.Id,
                    ReferenceNumber: $"{voucher.VoucherNo}-REV",
                    Lines: new List<JournalEntryLineRequest>
                    {
                        new(cashAccountId, voucher.TotalAmount, 0,
                            "عكس سند صرف - الجانب المدين (النقدية)"),
                        new(voucher.AccountId, 0, voucher.TotalAmount,
                            $"عكس سند صرف - الجانب الدائن ({voucher.Account?.NameAr ?? "الحساب"})")
                    });

                var entryResult = await _journalEntryService.CreateJournalEntryAsync(
                    reverseRequest, userId, ct);
                if (!entryResult.IsSuccess)
                    return Result<PaymentVoucherDto>.Failure(entryResult.Error!);

                var postResult = await _journalEntryService.PostJournalEntryAsync(
                    entryResult.Value, userId, ct);
                if (!postResult.IsSuccess)
                    return Result<PaymentVoucherDto>.Failure(postResult.Error!);
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "PaymentVoucher {Id} cancelled by User {UserId}", id, userId);

            var cancelled = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == id, ct, "Currency", "CashBox", "Account");
            return Result<PaymentVoucherDto>.Success(MapPaymentVoucherToDto(cancelled!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex, "Domain rule violation cancelling payment voucher {Id}: {Message}",
                id, ex.Message);
            return Result<PaymentVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment voucher {Id}", id);
            return Result<PaymentVoucherDto>.Failure(
                "حدث خطأ أثناء إلغاء سند الصرف");
        }
    }

    public async Task<Result<List<PaymentVoucherDto>>> GetPaymentVouchersAsync(
        int cashBoxId, CancellationToken ct)
    {
        try
        {
            var boxExists = await _uow.CashBoxes.AnyAsync(b => b.Id == cashBoxId, ct);
            if (!boxExists)
                return Result<List<PaymentVoucherDto>>.Failure(
                    "الصندوق النقدي غير موجود", ErrorCodes.NotFound);

            var vouchers = await _uow.PaymentVouchers.ToListAsync(
                v => v.CashBoxId == cashBoxId,
                q => q.OrderByDescending(v => v.VoucherDate)
                      .ThenByDescending(v => v.Id),
                ct: ct,
                includePaths: new[] { "Currency", "CashBox", "Account" });

            var dtos = vouchers.Select(MapPaymentVoucherToDto).ToList();
            return Result<List<PaymentVoucherDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Error retrieving payment vouchers for cash box {CashBoxId}",
                cashBoxId);
            return Result<List<PaymentVoucherDto>>.Failure(
                "حدث خطأ أثناء استرجاع سندات الصرف");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Transfer
    // ═══════════════════════════════════════════════════════════

    public async Task<Result> TransferAsync(
        CashTransferRequest request, int userId, CancellationToken ct)
    {
        try
        {
            if (request.SourceCashBoxId == request.DestinationCashBoxId)
                return Result.Failure("لا يمكن التحويل من صندوق إلى نفسه");

            if (request.Amount <= 0)
                return Result.Failure("مبلغ التحويل يجب أن يكون أكبر من الصفر");

            var sourceBox = await _uow.CashBoxes.FirstOrDefaultAsync(
                b => b.Id == request.SourceCashBoxId, ct, "Account");
            if (sourceBox == null)
                return Result.Failure("صندوق المصدر غير موجود", ErrorCodes.NotFound);

            var destBox = await _uow.CashBoxes.FirstOrDefaultAsync(
                b => b.Id == request.DestinationCashBoxId, ct, "Account");
            if (destBox == null)
                return Result.Failure("صندوق الوجهة غير موجود", ErrorCodes.NotFound);

            // Execute transfer atomically within a single transaction
            await _uow.ExecuteTransactionAsync(async () =>
            {
                // Payment from source cash box (Dr Destination Account / Cr Source Cash Account)
                var paymentResult = await CreatePaymentVoucherInternalAsync(
                    request.SourceCashBoxId,
                    request.CurrencyId,
                    destBox.AccountId,
                    request.Amount,
                    $"تحويل إلى {destBox.Name}" +
                        (string.IsNullOrWhiteSpace(request.Notes) ? "" : $" - {request.Notes}"),
                    userId,
                    ct);

                if (!paymentResult.IsSuccess || paymentResult.Value == null)
                    throw new DomainException(
                        paymentResult.Error ?? "فشل إنشاء سند الصرف للتحويل");

                // Receipt to destination cash box (Dr Destination Cash Account / Cr Source Account)
                var receiptResult = await CreateReceiptVoucherInternalAsync(
                    request.DestinationCashBoxId,
                    request.CurrencyId,
                    sourceBox.AccountId,
                    request.Amount,
                    $"تحويل من {sourceBox.Name}" +
                        (string.IsNullOrWhiteSpace(request.Notes) ? "" : $" - {request.Notes}"),
                    null,
                    null,
                    userId,
                    ct);

                if (!receiptResult.IsSuccess || receiptResult.Value == null)
                    throw new DomainException(
                        receiptResult.Error ?? "فشل إنشاء سند القبض للتحويل");

                // Post the payment voucher
                var postPayment = await PostPaymentVoucherAsync(
                    paymentResult.Value.Id, userId, ct);
                if (!postPayment.IsSuccess)
                    throw new DomainException(
                        postPayment.Error ?? "فشل ترحيل سند الصرف للتحويل");

                // Post the receipt voucher
                var postReceipt = await PostReceiptVoucherAsync(
                    receiptResult.Value.Id, userId, ct);
                if (!postReceipt.IsSuccess)
                    throw new DomainException(
                        postReceipt.Error ?? "فشل ترحيل سند القبض للتحويل");
            }, ct);

            _logger.LogInformation(
                "Cash transfer completed: {Amount} from CashBox {SourceId} to CashBox {DestId} by User {UserId}",
                request.Amount, request.SourceCashBoxId, request.DestinationCashBoxId, userId);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex, "Domain rule violation during transfer: {Message}", ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Error during cash transfer from {Source} to {Dest}",
                request.SourceCashBoxId, request.DestinationCashBoxId);
            return Result.Failure("حدث خطأ أثناء التحويل النقدي");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Invoice Payment Recording (called by other services)
    // ═══════════════════════════════════════════════════════════

    public async Task<Result<ReceiptVoucherDto>> RecordInvoiceReceiptAsync(
        int cashBoxId, short currencyId, decimal amount, int accountId,
        string? notes = null, int? referenceId = null, string? referenceType = null,
        int userId = 0, CancellationToken ct = default)
    {
        return await CreateReceiptVoucherInternalAsync(
            cashBoxId, currencyId, accountId, amount, notes,
            referenceId, referenceType, userId, ct);
    }

    public async Task<Result<PaymentVoucherDto>> RecordInvoicePaymentAsync(
        int cashBoxId, short currencyId, decimal amount, int accountId,
        string? notes = null, int userId = 0,
        CancellationToken ct = default)
    {
        return await CreatePaymentVoucherInternalAsync(
            cashBoxId, currencyId, accountId, amount, notes,
            userId, ct);
    }

    // ═══════════════════════════════════════════════════════════
    // Private Helpers
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Auto-creates a Level-4 detail account under parent "1110 — النقدية"
    /// (Cash & Cash Equivalents) for a new cash box.
    /// Account code auto-increments from existing child codes.
    /// </summary>
    private async Task<Result<Account>> CreateCashBoxAccountAsync(
        string boxName, int userId, CancellationToken ct)
    {
        try
        {
            // Find the Cash & Cash Equivalents parent account (1110)
            var parent = await _uow.Accounts.FirstOrDefaultAsync(
                a => a.AccountCode == "1110", ct);
            if (parent == null)
                return Result<Account>.Failure(
                    "الحساب الرئيسي للنقدية (1110) غير موجود في شجرة الحسابات");

            // Find max child code under parent to auto-increment
            var children = await _uow.Accounts.ToListAsync(
                a => a.ParentId == parent.Id,
                q => q.OrderByDescending(a => a.AccountCode),
                ct: ct);

            var maxCodeStr = children.FirstOrDefault()?.AccountCode
                             ?? parent.AccountCode;
            var newCode = (int.Parse(maxCodeStr) + 1).ToString();

            // Create Level-4 detail account with Asset type
            var account = Account.Create(
                accountCode: newCode,
                nameAr: $"صندوق {boxName}",
                nameEn: $"Cash Box {boxName}",
                nature: (byte)AccountType.Asset,
                isLeaf: true,
                parentId: parent.Id,
                isSystem: false,
                categoryId: null,
                level: 4,
                createdByUserId: userId);

            await _uow.Accounts.AddAsync(account, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Auto-created account for cash box '{BoxName}': Code={AccountCode}, Id={AccountId}",
                boxName, account.AccountCode, account.Id);

            return Result<Account>.Success(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Error auto-creating account for cash box '{BoxName}'", boxName);
            return Result<Account>.Failure(
                "فشل إنشاء الحساب المحاسبي للصندوق النقدي");
        }
    }

    private async Task<Result<ReceiptVoucherDto>> CreateReceiptVoucherInternalAsync(
        int cashBoxId, short currencyId, int accountId, decimal amount,
        string? notes, int? referenceId, string? referenceType,
        int userId, CancellationToken ct)
    {
        try
        {
            var voucherNo = await GetNextReceiptVoucherNumberAsync(ct);
            if (voucherNo <= 0)
                return Result<ReceiptVoucherDto>.Failure(
                    "فشل في توليد رقم سند القبض");

            var voucher = ReceiptVoucher.Create(
                voucherNo,
                DateTime.UtcNow,
                currencyId,
                cashBoxId,
                accountId,
                amount,
                notes,
                userId);

            await _uow.ReceiptVouchers.AddAsync(voucher, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "ReceiptVoucher created (No: {VoucherNo}, ID: {Id}, Amount: {Amount}) " +
                "by User {UserId}",
                voucher.VoucherNo, voucher.Id, amount, userId);

            var created = await _uow.ReceiptVouchers.FirstOrDefaultAsync(
                v => v.Id == voucher.Id, ct, "Currency", "CashBox", "Account");
            return Result<ReceiptVoucherDto>.Success(
                MapReceiptVoucherToDto(created!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex, "Domain rule violation creating receipt voucher: {Message}",
                ex.Message);
            return Result<ReceiptVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating receipt voucher");
            return Result<ReceiptVoucherDto>.Failure(
                "حدث خطأ أثناء إنشاء سند القبض");
        }
    }

    private async Task<Result<PaymentVoucherDto>> CreatePaymentVoucherInternalAsync(
        int cashBoxId, short currencyId, int accountId, decimal amount,
        string? notes, int userId, CancellationToken ct)
    {
        try
        {
            var voucherNo = await GetNextPaymentVoucherNumberAsync(ct);
            if (voucherNo <= 0)
                return Result<PaymentVoucherDto>.Failure(
                    "فشل في توليد رقم سند الصرف");

            var voucher = PaymentVoucher.Create(
                voucherNo,
                DateTime.UtcNow,
                currencyId,
                cashBoxId,
                accountId,
                amount,
                notes,
                userId);

            await _uow.PaymentVouchers.AddAsync(voucher, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "PaymentVoucher created (No: {VoucherNo}, ID: {Id}, Amount: {Amount}) " +
                "by User {UserId}",
                voucher.VoucherNo, voucher.Id, amount, userId);

            var created = await _uow.PaymentVouchers.FirstOrDefaultAsync(
                v => v.Id == voucher.Id, ct, "Currency", "CashBox", "Account");
            return Result<PaymentVoucherDto>.Success(
                MapPaymentVoucherToDto(created!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                ex, "Domain rule violation creating payment voucher: {Message}",
                ex.Message);
            return Result<PaymentVoucherDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment voucher");
            return Result<PaymentVoucherDto>.Failure(
                "حدث خطأ أثناء إنشاء سند الصرف");
        }
    }

    private async Task<int> GetNextReceiptVoucherNumberAsync(CancellationToken ct)
    {
        var seqResult = await _documentSequence.GetNextIntAsync("ReceiptVoucher", ct);
        return seqResult.IsSuccess ? seqResult.Value : 0;
    }

    private async Task<int> GetNextPaymentVoucherNumberAsync(CancellationToken ct)
    {
        var seqResult = await _documentSequence.GetNextIntAsync("PaymentVoucher", ct);
        return seqResult.IsSuccess ? seqResult.Value : 0;
    }

    // ═══════════════════════════════════════════════════════════
    // Mapping Helpers
    // ═══════════════════════════════════════════════════════════

    private static CashBoxDto MapToDto(CashBox box)
    {
        return new CashBoxDto(
            box.Id,
            box.Name,
            box.AccountId,
            box.Account?.NameAr,
            box.Account?.AccountCode,
            box.BranchId,
            null,   // BranchName (future: load from Branches table)
            box.Description,
            box.IsActive
        );
    }

    private static ReceiptVoucherDto MapReceiptVoucherToDto(ReceiptVoucher voucher)
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

    private static PaymentVoucherDto MapPaymentVoucherToDto(PaymentVoucher voucher)
    {
        return new PaymentVoucherDto(
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
