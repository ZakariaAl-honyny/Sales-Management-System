using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class SupplierPaymentService : ISupplierPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly IAccountingIntegrationService _accountingService;
    private readonly ILogger<SupplierPaymentService> _logger;

    public SupplierPaymentService(
        IUnitOfWork uow,
        IDocumentSequenceService sequenceService,
        IAccountingIntegrationService accountingService,
        ILogger<SupplierPaymentService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _accountingService = accountingService;
        _logger = logger;
    }

    public async Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Generate payment number via thread-safe DocumentSequenceService
            var seqResult = await _sequenceService.GetNextIntAsync("SupplierPayment", ct);
            if (!seqResult.IsSuccess)
                return Result<SupplierPaymentDto>.Failure(seqResult.Error ?? "فشل في توليد رقم السداد");

            var paymentNo = seqResult.Value;

            // Validate supplier exists
            var supplier = await _uow.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, ct, "Party");
            if (supplier == null)
                return Result<SupplierPaymentDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

            var payment = SupplierPayment.Create(
                paymentNo: paymentNo,
                supplierId: request.SupplierId,
                cashBoxId: request.CashBoxId ?? 0,
                currencyId: request.CurrencyId,
                amount: request.Amount,
                paymentMethod: (PaymentMethod)request.PaymentMethod,
                notes: request.Notes,
                createdByUserId: userId,
                paymentDate: request.PaymentDate);

            await _uow.SupplierPayments.AddAsync(payment, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Supplier payment created (No: {PaymentNo}, ID: {Id}) by User {UserId}",
                payment.PaymentNo, payment.Id, userId);

            return Result<SupplierPaymentDto>.Success(MapToDto(payment, supplier));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating supplier payment: {Message}", ex.Message);
            return Result<SupplierPaymentDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier payment");
            return Result<SupplierPaymentDto>.Failure("حدث خطأ أثناء إنشاء سند الصرف");
        }
    }

    public async Task<Result<PagedResult<SupplierPaymentDto>>> GetAllAsync(
        string? search, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        try
        {
            // Pre-parse search for numeric matching (expression trees can't contain out vars)
            int? searchPaymentNo = null;
            if (!string.IsNullOrEmpty(search) && int.TryParse(search, out var parsedNo))
                searchPaymentNo = parsedNo;

            var (items, totalCount) = await _uow.SupplierPayments.GetPagedAsync(
                predicate: p =>
                    (string.IsNullOrEmpty(search) ||
                     (searchPaymentNo.HasValue && p.PaymentNo == searchPaymentNo.Value) ||
                     (p.Supplier != null && p.Supplier.Party != null && p.Supplier.Party.Name.Contains(search))) &&
                    (!from.HasValue || p.PaymentDate >= from.Value) &&
                    (!to.HasValue || p.PaymentDate <= to.Value),
                orderConfig: q => q.OrderByDescending(p => p.PaymentDate),
                page,
                pageSize,
                ct,
                ignoreQueryFilters: false,
                includePaths: new[] { "Supplier", "Supplier.Party" });

            var dtos = items.Select(p => MapToDto(p, p.Supplier)).ToList();

            var result = PagedResult<SupplierPaymentDto>.Create(dtos, totalCount, page, pageSize);
            return Result<PagedResult<SupplierPaymentDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier payments");
            return Result<PagedResult<SupplierPaymentDto>>.Failure("حدث خطأ أثناء استرجاع سندات الصرف");
        }
    }

    public async Task<Result<SupplierPaymentDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var payment = await _uow.SupplierPayments.FirstOrDefaultAsync(
                p => p.Id == id, ct, "Supplier", "Supplier.Party");
            if (payment == null)
                return Result<SupplierPaymentDto>.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            return Result<SupplierPaymentDto>.Success(MapToDto(payment, payment.Supplier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supplier payment {Id}", id);
            return Result<SupplierPaymentDto>.Failure("حدث خطأ أثناء استرجاع بيانات سند الصرف");
        }
    }

    public async Task<Result<SupplierPaymentDto>> UpdateAsync(int id, UpdateSupplierPaymentRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var payment = await _uow.SupplierPayments.FirstOrDefaultAsync(
                p => p.Id == id, ct, "Supplier", "Supplier.Party");
            if (payment == null)
                return Result<SupplierPaymentDto>.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            // If the payment is posted and amount changed, we must reverse the old journal entry,
            // create a new one for the updated amount, all inside a transaction.
            if (payment.Status == InvoiceStatus.Posted)
            {
                var amountChanged = Math.Abs(payment.Amount - request.Amount) > 0.0001m;
                if (amountChanged)
                {
                    var supplierAccountId = payment.Supplier?.AccountId ?? 0;
                    var supplierName = payment.Supplier?.Party?.Name ?? "";

                    var oldAmount = payment.Amount;

                    return await _uow.ExecuteTransactionAsync<Result<SupplierPaymentDto>>(async () =>
                    {
                        // 1. Reverse the original journal entry (uses per-entity supplier account)
                        var reverseResult = await _accountingService.ReverseSupplierPaymentEntryAsync(
                            payment.Id, oldAmount, supplierName, supplierAccountId, userId, ct);
                        if (!reverseResult.IsSuccess)
                            return Result<SupplierPaymentDto>.Failure(reverseResult.Error!);

                        // 2. Update payment with new amount
                        payment.Update(
                            amount: request.Amount,
                            paymentMethod: (PaymentMethod)request.PaymentMethod,
                            paymentDate: request.PaymentDate,
                            notes: request.Notes,
                            updatedByUserId: userId);

                        // 3. Create new journal entry for updated amount
                        var entryResult = await _accountingService.CreateSupplierPaymentEntryAsync(
                            payment, supplierName, userId, ct);
                        if (!entryResult.IsSuccess)
                            return Result<SupplierPaymentDto>.Failure(entryResult.Error!);

                        _logger.LogInformation(
                            "Supplier payment {Id} amount changed from {OldAmount} to {NewAmount}, " +
                            "journal entry reversed and recreated by User {UserId}",
                            id, oldAmount, request.Amount, userId);

                        return Result<SupplierPaymentDto>.Success(MapToDto(payment, payment.Supplier));
                    }, ct);
                }
                else
                {
                    // Amount didn't change — just update other fields (no journal entry impact)
                    payment.Update(
                        amount: request.Amount,
                        paymentMethod: (PaymentMethod)request.PaymentMethod,
                        paymentDate: request.PaymentDate,
                        notes: request.Notes,
                        updatedByUserId: userId);
                    await _uow.SaveChangesAsync(ct);
                }
            }
            else if (payment.Status == InvoiceStatus.Cancelled)
            {
                return Result<SupplierPaymentDto>.Failure(
                    "لا يمكن تعديل سند صرف ملغي", ErrorCodes.InvalidOperation);
            }
            else
            {
                // Draft payment: simple update without journal entry changes
                payment.Update(
                    amount: request.Amount,
                    paymentMethod: (PaymentMethod)request.PaymentMethod,
                    paymentDate: request.PaymentDate,
                    notes: request.Notes,
                    updatedByUserId: userId);
                await _uow.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Supplier payment {Id} updated by User {UserId}", id, userId);
            return Result<SupplierPaymentDto>.Success(MapToDto(payment, payment.Supplier));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating supplier payment {Id}: {Message}", id, ex.Message);
            return Result<SupplierPaymentDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating supplier payment {Id}", id);
            return Result<SupplierPaymentDto>.Failure("حدث خطأ أثناء تحديث سند الصرف");
        }
    }

    public async Task<Result> PostAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var payment = await _uow.SupplierPayments.FirstOrDefaultAsync(
                p => p.Id == id, ct, "Supplier", "Supplier.Party");
            if (payment == null)
                return Result.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            payment.Post();
            payment.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            // Create journal entry: Dr AP / Cr Cash
            var supplierName = payment.Supplier?.Party?.Name ?? "";
            var entryResult = await _accountingService.CreateSupplierPaymentEntryAsync(
                payment, supplierName, userId, ct);
            if (!entryResult.IsSuccess)
                return Result.Failure(entryResult.Error!);

            _logger.LogInformation("Supplier payment {Id} posted by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation posting supplier payment {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting supplier payment {Id}", id);
            return Result.Failure("حدث خطأ أثناء ترحيل سند الصرف");
        }
    }

    public async Task<Result> CancelAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var payment = await _uow.SupplierPayments.FirstOrDefaultAsync(
                p => p.Id == id, ct, "Supplier", "Supplier.Party");
            if (payment == null)
                return Result.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            // If already posted, reverse the journal entry first
            if (payment.Status == InvoiceStatus.Posted)
            {
                var supplierAccountId = payment.Supplier?.AccountId ?? 0;
                var supplierName = payment.Supplier?.Party?.Name ?? "";
                var reverseResult = await _accountingService.ReverseSupplierPaymentEntryAsync(
                    payment.Id, payment.Amount, supplierName, supplierAccountId, userId, ct);
                if (!reverseResult.IsSuccess)
                    return Result.Failure(reverseResult.Error!);
            }

            payment.Cancel();
            payment.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Supplier payment {Id} cancelled by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation cancelling supplier payment {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling supplier payment {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء سند الصرف");
        }
    }

    public async Task<Result> DeleteAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var payment = await _uow.SupplierPayments.FirstOrDefaultAsync(
                p => p.Id == id, ct, "Supplier", "Supplier.Party");
            if (payment == null)
                return Result.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            if (payment.Status == InvoiceStatus.Cancelled)
                return Result.Failure("سند الصرف ملغي بالفعل", ErrorCodes.InvalidOperation);

            // If already posted, reverse the journal entry first
            if (payment.Status == InvoiceStatus.Posted)
            {
                var supplierAccountId = payment.Supplier?.AccountId ?? 0;
                var supplierName = payment.Supplier?.Party?.Name ?? "";
                var reverseResult = await _accountingService.ReverseSupplierPaymentEntryAsync(
                    payment.Id, payment.Amount, supplierName, supplierAccountId, userId, ct);
                if (!reverseResult.IsSuccess)
                    return Result.Failure(reverseResult.Error!);
            }

            payment.UpdateTimestamp();
            _uow.SupplierPayments.DeleteRange(new[] { payment });

            _logger.LogInformation("Supplier payment {Id} deleted (cancelled) by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting supplier payment {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف سند الصرف");
        }
    }

    // ─── Private Helpers ─────────────────────────────────

    private static SupplierPaymentDto MapToDto(SupplierPayment payment, Supplier? supplier)
    {
        return new SupplierPaymentDto(
            payment.Id,
            payment.PaymentNo.ToString(),
            payment.SupplierId,
            supplier?.Party?.Name ?? string.Empty,
            payment.Amount,
            (byte)payment.PaymentMethod,
            (int?)payment.CurrencyId,
            null,  // ExchangeRate not on SupplierPayment entity
            payment.PaymentDate,
            null,  // PurchaseInvoiceId not on SupplierPayment entity
            payment.Notes);
    }
}
