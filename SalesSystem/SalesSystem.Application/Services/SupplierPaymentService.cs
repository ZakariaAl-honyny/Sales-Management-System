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
    private readonly ILogger<SupplierPaymentService> _logger;

    public SupplierPaymentService(
        IUnitOfWork uow,
        IDocumentSequenceService sequenceService,
        ILogger<SupplierPaymentService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Generate payment number
            var paymentNoResult = await _sequenceService.GetNextNumberAsync("SP", ct);
            if (!paymentNoResult.IsSuccess)
                return Result<SupplierPaymentDto>.Failure(paymentNoResult.Error ?? "فشل في توليد رقم السداد");

            // Validate supplier exists
            var supplier = await _uow.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, ct, "Party");
            if (supplier == null)
                return Result<SupplierPaymentDto>.Failure("المورد غير موجود", ErrorCodes.NotFound);

            var payment = SupplierPayment.Create(
                paymentNo: paymentNoResult.Value!,
                supplierId: request.SupplierId,
                amount: request.Amount,
                paymentMethod: (PaymentMethod)request.PaymentMethod,
                purchaseInvoiceId: request.PurchaseInvoiceId,
                notes: request.Notes,
                cashBoxId: request.CashBoxId,
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
            var (items, totalCount) = await _uow.SupplierPayments.GetPagedAsync(
                predicate: p =>
                    (string.IsNullOrEmpty(search) || p.PaymentNo.Contains(search) ||
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

            payment.Update(
                amount: request.Amount,
                paymentMethod: (PaymentMethod)request.PaymentMethod,
                paymentDate: request.PaymentDate,
                notes: request.Notes,
                updatedByUserId: userId);

            await _uow.SaveChangesAsync(ct);

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

    public async Task<Result> DeleteAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var payment = await _uow.SupplierPayments.GetByIdAsync(id, ct);
            if (payment == null)
                return Result.Failure("سند الصرف غير موجود", ErrorCodes.NotFound);

            // Supplier payment uses soft-cancel — mark as cancelled
            // We set the Status property directly since SupplierPayment entity
            // inherits DocumentEntity (which has Status but no Cancel() method)
            if (payment.Status == InvoiceStatus.Cancelled)
                return Result.Failure("سند الصرف ملغي بالفعل", ErrorCodes.InvalidOperation);

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
            payment.PaymentNo,
            payment.SupplierId,
            supplier?.Party?.Name ?? string.Empty,
            payment.Amount,
            (byte)payment.PaymentMethod,
            payment.CurrencyId,
            payment.ExchangeRate,
            payment.PaymentDate,
            payment.PurchaseInvoiceId,
            payment.Notes);
    }
}
