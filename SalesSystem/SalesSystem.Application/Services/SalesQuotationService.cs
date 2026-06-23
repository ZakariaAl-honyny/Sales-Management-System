using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class SalesQuotationService : ISalesQuotationService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<SalesQuotationService> _logger;

    public SalesQuotationService(
        IUnitOfWork uow,
        IDocumentSequenceService sequenceService,
        ILogger<SalesQuotationService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<SalesQuotationDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Customer", "Warehouse", "Currency", "Items.Product", "Items.ProductUnit.Unit");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        return Result<SalesQuotationDto>.Success(MapToDto(quotation));
    }

    public async Task<Result<PagedResult<SalesQuotationDto>>> GetAllAsync(
        int? customerId,
        int? status,
        string? search,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        bool includeInactive,
        CancellationToken ct)
    {
        var searchLower = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();
        int? searchId = int.TryParse(searchLower, out var parsedId) ? parsedId : null;

        System.Linq.Expressions.Expression<System.Func<SalesQuotation, bool>> predicate = q =>
            (includeInactive || q.Status != QuotationStatus.Rejected) &&
            (!customerId.HasValue || q.CustomerId == customerId.Value) &&
            (!status.HasValue || (int)q.Status == status.Value) &&
            (!from.HasValue || q.QuotationDate >= from.Value) &&
            (!to.HasValue || q.QuotationDate <= to.Value) &&
            (searchLower == null ||
             (searchId.HasValue && q.Id == searchId.Value) ||
             (q.Customer != null && q.Customer.Name.ToLower().Contains(searchLower)) ||
             (q.Notes != null && q.Notes.ToLower().Contains(searchLower)));

        var includes = new[] { "Customer", "Warehouse", "Items.Product" };

        var (items, total) = await _uow.SalesQuotations.GetPagedAsync(
            predicate, q => q.OrderByDescending(x => x.QuotationDate), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<SalesQuotationDto>>.Success(PagedResult<SalesQuotationDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<SalesQuotationDto>> CreateAsync(CreateSalesQuotationRequest request, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<SalesQuotationDto>>(async () =>
        {
            try
            {
                // Resolve QuotationNo
                int quotationNo;
                if (request.QuotationNo.HasValue && request.QuotationNo.Value > 0)
                {
                    var existing = await _uow.SalesQuotations.AnyAsync(q => q.QuotationNo == request.QuotationNo.Value, ct);
                    if (existing)
                        return Result<SalesQuotationDto>.Failure("رقم عرض السعر موجود بالفعل");
                    quotationNo = request.QuotationNo.Value;
                }
                else
                {
                    var seqResult = await _sequenceService.GetNextIntAsync("SalesQuotation", ct);
                    if (!seqResult.IsSuccess)
                        return Result<SalesQuotationDto>.Failure("فشل في توليد رقم عرض السعر");
                    quotationNo = seqResult.Value;
                }

                var quotation = SalesQuotation.Create(
                    quotationNo,
                    request.CustomerId,
                    request.WarehouseId,
                    request.CurrencyId,
                    request.QuotationDate,
                    request.ValidUntil,
                    request.ExchangeRate,
                    (PaymentType)request.PaymentType,
                    request.DiscountAmount,
                    request.TaxAmount,
                    request.Notes,
                    request.TermsAndConditions,
                    userId
                );

                foreach (var item in request.Items)
                {
                    var line = SalesQuotationItem.Create(
                        item.ProductId,
                        item.ProductUnitId,
                        item.Quantity,
                        item.UnitPrice,
                        item.DiscountAmount,
                        item.Notes);
                    quotation.AddItem(line);
                }

                await _uow.SalesQuotations.AddAsync(quotation, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Sales Quotation created as Draft: No={QuotationNo} (ID: {Id})", quotation.QuotationNo, quotation.Id);

                return await GetByIdAsync(quotation.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception creating sales quotation: {Message}", ex.Message);
                return Result<SalesQuotationDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sales quotation");
                return Result<SalesQuotationDto>.Failure("حدث خطأ أثناء حفظ عرض السعر");
            }
        }, ct);
    }

    public async Task<Result<SalesQuotationDto>> UpdateAsync(int id, UpdateSalesQuotationRequest request, int userId, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Items", "Customer", "Warehouse", "Currency");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        if (quotation.Status != QuotationStatus.Draft)
            return Result<SalesQuotationDto>.Failure("يمكن تعديل عروض السعر المسودة فقط");

        try
        {
            // Delete existing items and recreate
            _uow.SalesQuotationItems.DeleteRange(quotation.Items.ToList());

            // Update header fields via domain methods
            quotation.SetDiscount(request.DiscountAmount);
            quotation.SetTax(request.TaxAmount);

            // Rebuild items from request
            foreach (var item in request.Items)
            {
                var line = SalesQuotationItem.Create(
                    item.ProductId,
                    item.ProductUnitId,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountAmount,
                    item.Notes);
                quotation.AddItem(line);
            }

            quotation.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Sales Quotation updated: No={QuotationNo} (ID: {Id})", quotation.QuotationNo, quotation.Id);

            return await GetByIdAsync(quotation.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception updating sales quotation {Id}: {Message}", id, ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sales quotation {Id}", id);
            return Result<SalesQuotationDto>.Failure("حدث خطأ أثناء تحديث عرض السعر");
        }
    }

    public async Task<Result<SalesQuotationDto>> SendAsync(int id, int userId, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Items", "Customer", "Warehouse", "Currency");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        try
        {
            quotation.Send();
            quotation.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Sales Quotation sent: No={QuotationNo} (ID: {Id})", quotation.QuotationNo, quotation.Id);
            return await GetByIdAsync(quotation.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception sending sales quotation {Id}: {Message}", id, ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<SalesQuotationDto>> AcceptAsync(int id, int userId, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Items", "Customer", "Warehouse", "Currency");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        try
        {
            quotation.Accept();
            quotation.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Sales Quotation accepted: No={QuotationNo} (ID: {Id})", quotation.QuotationNo, quotation.Id);
            return await GetByIdAsync(quotation.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception accepting sales quotation {Id}: {Message}", id, ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<SalesQuotationDto>> RejectAsync(int id, string? reason, int userId, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
            q => q.Id == id, ct, "Items", "Customer", "Warehouse", "Currency");

        if (quotation == null)
            return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        try
        {
            quotation.Reject(reason);
            quotation.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Sales Quotation rejected: No={QuotationNo} (ID: {Id}), Reason={Reason}",
                quotation.QuotationNo, quotation.Id, reason);
            return await GetByIdAsync(quotation.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception rejecting sales quotation {Id}: {Message}", id, ex.Message);
            return Result<SalesQuotationDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<SalesQuotationDto>> ConvertToInvoiceAsync(int id, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<SalesQuotationDto>>(async () =>
        {
            try
            {
                var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(
                    q => q.Id == id, ct, "Items", "Customer", "Warehouse", "Currency",
                    "Items.Product", "Items.ProductUnit.Unit");

                if (quotation == null)
                    return Result<SalesQuotationDto>.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

                // Generate InvoiceNo for the new draft invoice
                var seqResult = await _sequenceService.GetNextIntAsync("SalesInvoice", ct);
                if (!seqResult.IsSuccess)
                    return Result<SalesQuotationDto>.Failure("فشل في توليد رقم الفاتورة");
                var invoiceNo = seqResult.Value;

                // Create a draft SalesInvoice from the quotation data
                    var invoice = SalesInvoice.Create(
                    quotation.WarehouseId,
                    invoiceNo,
                    quotation.CustomerId,
                    invoiceDate: quotation.QuotationDate,
                    paymentType: quotation.PaymentType,
                    discountAmount: quotation.DiscountAmount,
                    notes: $"عرض سعر رقم {quotation.QuotationNo} - {quotation.Notes}",
                    currencyId: quotation.CurrencyId,
                    exchangeRate: quotation.ExchangeRate,
                    createdByUserId: userId
                );

                foreach (var item in quotation.Items)
                {
                    var invoiceItem = SalesInvoiceLine.Create(
                        item.ProductId,
                        item.ProductUnitId,
                        item.Quantity,
                        item.UnitPrice
                    );
                    invoice.AddItem(invoiceItem);
                }

                invoice.SetTaxAmount(quotation.TaxAmount);
                invoice.SetPaidAmount(0);

                await _uow.SalesInvoices.AddAsync(invoice, ct);
                await _uow.SaveChangesAsync(ct);

                // Mark quotation as converted
                quotation.ConvertToInvoice(invoice.Id);
                quotation.SetUpdatedBy(userId);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Sales Quotation converted to invoice: Q={QuotationNo} (ID: {QId}) → Invoice No={InvoiceNo} (ID: {IId})",
                    quotation.QuotationNo, quotation.Id, invoiceNo, invoice.Id);

                return await GetByIdAsync(quotation.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception converting sales quotation to invoice {Id}: {Message}", id, ex.Message);
                return Result<SalesQuotationDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting sales quotation {Id} to invoice", id);
                return Result<SalesQuotationDto>.Failure("حدث خطأ أثناء تحويل عرض السعر إلى فاتورة");
            }
        }, ct);
    }

    public async Task<Result> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var quotation = await _uow.SalesQuotations.FirstOrDefaultAsync(q => q.Id == id, ct);

        if (quotation == null)
            return Result.Failure("عرض السعر غير موجود", ErrorCodes.NotFound);

        try
        {
            quotation.Cancel();
            quotation.SetUpdatedBy(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Sales Quotation cancelled: No={QuotationNo} (ID: {Id})", quotation.QuotationNo, quotation.Id);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception cancelling sales quotation {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
    }

    // ─── Private Helpers ───────────────────────────────────────────────

    private static SalesQuotationDto MapToDto(SalesQuotation q)
    {
        var customerName = q.Customer?.Name ?? "غير معروف";
        var warehouseName = q.Warehouse?.Name ?? "غير معروف";
        var currencyShort = q.CurrencyId > 0 ? q.CurrencyId : (short?)null;

        return new SalesQuotationDto(
            q.Id,
            q.QuotationNo,
            q.CustomerId,
            customerName,
            q.WarehouseId,
            warehouseName,
            currencyShort,
            q.ExchangeRate,
            q.QuotationDate,
            q.ValidUntil ?? default,
            (byte)q.Status,
            q.SubTotal,
            q.DiscountAmount,
            q.TaxAmount,
            q.TotalAmount,
            q.Notes,
            q.TermsAndConditions,
            null, // CreatedByUserName - not tracked on entity
            q.CreatedAt,
            true, // IsActive
            q.Items.Select(MapItemToDto).ToList()
        );
    }

    private static SalesQuotationItemDto MapItemToDto(SalesQuotationItem item)
    {
        return new SalesQuotationItemDto(
            item.Id,
            item.SalesQuotationId,
            item.ProductId,
            item.Product?.Name ?? "غير معروف",
            item.ProductUnitId,
            item.ProductUnit?.Unit?.Name ?? "غير معروفة",
            item.Quantity,
            item.UnitPrice,
            item.DiscountAmount,
            0, // TaxAmount per line not tracked separately
            item.LineTotal,
            item.Notes
        );
    }
}
