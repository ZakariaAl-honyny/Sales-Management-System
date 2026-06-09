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

/// <summary>
/// تنفيذ خدمة عروض الأسعار — إدارة دورة حياة عرض السعر مع دعم التحويل لفاتورة بيع.
/// </summary>
public class SalesQuotationService : ISalesQuotationService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _documentSequenceService;
    private readonly ISalesService _salesService;
    private readonly ILogger<SalesQuotationService> _logger;

    public SalesQuotationService(
        IUnitOfWork uow,
        IDocumentSequenceService documentSequenceService,
        ISalesService salesService,
        ILogger<SalesQuotationService> logger)
    {
        _uow = uow;
        _documentSequenceService = documentSequenceService;
        _salesService = salesService;
        _logger = logger;
    }

    public async Task<Result<SalesQuotationDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Customer", "Warehouse", "Items.Product", "Currency");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        return Result<SalesQuotationDto>.Success(MapToDto(quotation));
    }

    public async Task<Result<List<SalesQuotationDto>>> GetAllAsync(
        int? customerId = null,
        byte? status = null,
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var searchLower = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        var quotations = await _uow.SalesQuotations.ToListAsync(
            q => (!customerId.HasValue || q.CustomerId == customerId.Value) &&
                 (!status.HasValue || (byte)q.Status == status.Value) &&
                 (!from.HasValue || q.QuotationDate >= from.Value) &&
                 (!to.HasValue || q.QuotationDate <= to.Value) &&
                 (searchLower == null ||
                  (q.Customer != null && q.Customer.Name.ToLower().Contains(searchLower)) ||
                  q.QuotationNo.ToLower().Contains(searchLower) ||
                  (q.Notes != null && q.Notes.ToLower().Contains(searchLower))),
            q => q.OrderByDescending(x => x.QuotationDate),
            ct,
            false,
            "Customer", "Warehouse", "Items.Product", "Currency");

        var dtos = quotations.Select(MapToDto).ToList();
        return Result<List<SalesQuotationDto>>.Success(dtos);
    }

    public async Task<Result<SalesQuotationDto>> CreateAsync(CreateSalesQuotationRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Validate warehouse
            var warehouse = await _uow.Warehouses.GetByIdAsync(request.WarehouseId, ct);
            if (warehouse == null)
                return Result<SalesQuotationDto>.Failure("المستودع غير موجود");

            // Generate QuotationNo
            var seqResult = await _documentSequenceService.GetNextNumberAsync("SQ", ct);
            if (!seqResult.IsSuccess)
                return Result<SalesQuotationDto>.Failure("فشل في توليد رقم عرض السعر");
            var quotationNo = seqResult.Value!;

            var quotation = SalesQuotation.Create(
                quotationNo,
                request.WarehouseId,
                request.CustomerId,
                request.QuotationDate,
                request.ExpiryDate,
                request.DiscountAmount,
                request.Notes,
                request.CurrencyId,
                request.ExchangeRate,
                userId);

            foreach (var item in request.Items)
            {
                var quotationItem = SalesQuotationItem.Create(
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountAmount,
                    item.Mode,
                    item.Notes);
                quotation.AddItem(quotationItem);
            }

            await _uow.SalesQuotations.AddAsync(quotation, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم إنشاء عرض سعر: رقم {QuotationNo} (المعرف {Id}) بواسطة المستخدم {UserId}",
                quotationNo, quotation.Id, userId);

            return await GetByIdAsync(quotation.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء إنشاء عرض السعر: {Message}", ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء إنشاء عرض السعر");
            return Result<SalesQuotationDto>.Failure("حدث خطأ أثناء حفظ عرض السعر");
        }
    }

    public async Task<Result<SalesQuotationDto>> UpdateAsync(int id, UpdateSalesQuotationRequest request, int userId, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Items");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        if (quotation.Status != QuotationStatus.Draft)
            return Result<SalesQuotationDto>.Failure("يمكن تعديل عروض الأسعار المسودة فقط");

        try
        {
            // Remove existing items
            foreach (var existingItem in quotation.Items.ToList())
            {
                quotation.RemoveItem(existingItem);
            }

            // Re-add items from request
            foreach (var item in request.Items)
            {
                var quotationItem = SalesQuotationItem.Create(
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountAmount,
                    item.Mode,
                    item.Notes);
                quotation.AddItem(quotationItem);
            }

            quotation.RecalculateTotals();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم تحديث عرض السعر: المعرف {Id} بواسطة المستخدم {UserId}", id, userId);

            return await GetByIdAsync(id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء تحديث عرض السعر {Id}: {Message}", id, ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء تحديث عرض السعر {Id}", id);
            return Result<SalesQuotationDto>.Failure("حدث خطأ أثناء تحديث عرض السعر");
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.GetByIdAsync(id, ct);
        if (quotation == null)
            return Result<bool>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        try
        {
            quotation.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم حذف عرض السعر: المعرف {Id}", id);
            return Result<bool>.Success(true);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء حذف عرض السعر {Id}: {Message}", id, ex.Message);
            return Result<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء حذف عرض السعر {Id}", id);
            return Result<bool>.Failure("حدث خطأ أثناء حذف عرض السعر");
        }
    }

    public async Task<Result<SalesQuotationDto>> ConfirmAsync(int id, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Items");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        try
        {
            quotation.Confirm();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم تأكيد عرض السعر: المعرف {Id}", id);
            return await GetByIdAsync(id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء تأكيد عرض السعر {Id}: {Message}", id, ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء تأكيد عرض السعر {Id}", id);
            return Result<SalesQuotationDto>.Failure("حدث خطأ أثناء تأكيد عرض السعر");
        }
    }

    public async Task<Result<SalesQuotationDto>> ExpireAsync(int id, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct);

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        try
        {
            quotation.Expire();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم إنهاء صلاحية عرض السعر: المعرف {Id}", id);
            return await GetByIdAsync(id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء إنهاء صلاحية عرض السعر {Id}: {Message}", id, ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء إنهاء صلاحية عرض السعر {Id}", id);
            return Result<SalesQuotationDto>.Failure("حدث خطأ أثناء إنهاء صلاحية عرض السعر");
        }
    }

    public async Task<Result<SalesQuotationDto>> ConvertToInvoiceAsync(int id, ConvertQuotationToInvoiceRequest request, int userId, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Items.Product", "Customer", "Warehouse");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        if (quotation.Status != QuotationStatus.Confirmed)
            return Result<SalesQuotationDto>.Failure("يمكن تحويل عروض الأسعار المؤكدة فقط");

        if (!quotation.Items.Any())
            return Result<SalesQuotationDto>.Failure("لا يمكن تحويل عرض سعر بدون أصناف");

        try
        {
            // Create invoice items from quotation items
            var invoiceItems = quotation.Items.Select(qi => new CreateSalesInvoiceItemRequest(
                qi.ProductId,
                qi.Quantity,
                qi.UnitPrice,
                qi.DiscountAmount,
                (SalesSystem.Contracts.Enums.SaleMode)(int)qi.Mode,
                qi.Notes,
                ProductUnitId: null,
                IsPriceOverridden: false
            )).ToList();

            var invoiceRequest = new CreateSalesInvoiceRequest(
                request.WarehouseId,
                InvoiceNo: null,
                request.CustomerId ?? quotation.CustomerId,
                request.CashBoxId,
                DateTime.UtcNow,
                DueDate: null,
                (SalesSystem.Contracts.Enums.PaymentType)request.PaymentType,
                request.DiscountAmount,
                request.TaxAmount,
                request.PaidAmount,
                request.Notes,
                QuotationId: quotation.Id,
                request.CurrencyId ?? quotation.CurrencyId,
                request.ExchangeRate ?? quotation.ExchangeRate,
                TaxId: null,
                invoiceItems
            );

            var invoiceResult = await _salesService.CreateAsync(invoiceRequest, userId, ct);
            if (!invoiceResult.IsSuccess)
                return Result<SalesQuotationDto>.Failure(invoiceResult.Error!);

            // Mark quotation as converted
            quotation.MarkAsConverted();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم تحويل عرض السعر {QuotationId} إلى فاتورة بيع {InvoiceId}",
                quotation.Id, invoiceResult.Value!.Id);

            return await GetByIdAsync(quotation.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء تحويل عرض السعر {Id}: {Message}", id, ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء تحويل عرض السعر {Id}", id);
            return Result<SalesQuotationDto>.Failure("حدث خطأ أثناء تحويل عرض السعر لفاتورة");
        }
    }

    private static SalesQuotationDto MapToDto(SalesQuotation q)
    {
        return new SalesQuotationDto(
            q.Id,
            q.QuotationNo,
            q.CustomerId,
            q.Customer?.Name ?? "غير معروف",
            q.WarehouseId,
            q.Warehouse?.Name ?? "غير معروف",
            q.QuotationDate,
            q.ExpiryDate,
            (byte)q.Status,
            q.SubTotal,
            q.DiscountAmount,
            q.TaxAmount,
            q.TotalAmount,
            q.Notes,
            q.CurrencyId,
            q.Currency?.Code,
            null, // CreatedByUserName
            q.CreatedAt,
            q.Items.Select(i => new SalesQuotationItemDto(
                i.Id,
                i.ProductId,
                i.Product?.Name ?? "غير معروف",
                i.Quantity,
                i.UnitPrice,
                i.DiscountAmount,
                i.LineTotal,
                i.Mode,
                i.Notes
            )).ToList()
        );
    }
}
