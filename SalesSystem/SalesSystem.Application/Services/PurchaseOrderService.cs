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
/// تنفيذ خدمة أوامر الشراء — إدارة دورة حياة أمر الشراء مع دعم العملات المتعددة.
/// </summary>
public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _documentSequenceService;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(
        IUnitOfWork uow,
        IDocumentSequenceService documentSequenceService,
        ILogger<PurchaseOrderService> logger)
    {
        _uow = uow;
        _documentSequenceService = documentSequenceService;
        _logger = logger;
    }

    public async Task<Result<PurchaseOrderDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var order = await _uow.PurchaseOrders.FirstOrDefaultAsync(
            o => o.Id == id, ct, "Supplier", "Warehouse", "Items.Product", "Items.ProductUnit");

        if (order == null)
            return Result<PurchaseOrderDto>.Failure("أمر الشراء غير موجود", ErrorCodes.NotFound);

        return Result<PurchaseOrderDto>.Success(MapToDto(order));
    }

    public async Task<Result<List<PurchaseOrderDto>>> GetAllAsync(
        int? supplierId,
        byte? status,
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var searchLower = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        var orders = await _uow.PurchaseOrders.ToListAsync(
            o => (!supplierId.HasValue || o.SupplierId == supplierId.Value) &&
                 (!status.HasValue || (byte)o.Status == status.Value) &&
                 (!from.HasValue || o.OrderDate >= from.Value) &&
                 (!to.HasValue || o.OrderDate <= to.Value) &&
                 (searchLower == null ||
                  (o.Supplier != null && o.Supplier.Name.ToLower().Contains(searchLower)) ||
                  (o.Notes != null && o.Notes.ToLower().Contains(searchLower))),
            q => q.OrderByDescending(o => o.OrderDate),
            ct,
            false,
            "Supplier", "Warehouse", "Items.Product", "Items.ProductUnit");

        var dtos = orders.Select(MapToDto).ToList();
        return Result<List<PurchaseOrderDto>>.Success(dtos);
    }

    public async Task<Result<List<PurchaseOrderDto>>> GetPendingOrdersAsync(CancellationToken ct)
    {
        var orders = await _uow.PurchaseOrders.ToListAsync(
            o => o.Status == PurchaseOrderStatus.Approved ||
                 o.Status == PurchaseOrderStatus.PartiallyReceived,
            q => q.OrderByDescending(o => o.OrderDate),
            ct,
            false,
            "Supplier", "Warehouse", "Items.Product", "Items.ProductUnit");

        var dtos = orders.Select(MapToDto).ToList();
        return Result<List<PurchaseOrderDto>>.Success(dtos);
    }

    public async Task<Result<PurchaseOrderDto>> CreateAsync(
        CreatePurchaseOrderRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Validate supplier exists
            var supplier = await _uow.Suppliers.GetByIdAsync(request.SupplierId, ct);
            if (supplier == null)
                return Result<PurchaseOrderDto>.Failure("المورد غير موجود");

            // Validate warehouse exists
            var warehouse = await _uow.Warehouses.GetByIdAsync(request.WarehouseId, ct);
            if (warehouse == null)
                return Result<PurchaseOrderDto>.Failure("المستودع غير موجود");

            // Generate OrderNo if not provided
            int orderNo;
            if (request.OrderNo.HasValue && request.OrderNo.Value > 0)
            {
                var existing = await _uow.PurchaseOrders.AnyAsync(o => o.OrderNo == request.OrderNo.Value, ct);
                if (existing)
                    return Result<PurchaseOrderDto>.Failure("رقم الأمر موجود بالفعل");
                orderNo = request.OrderNo.Value;
            }
            else
            {
                var seqResult = await _documentSequenceService.GetNextIntAsync("PurchaseOrder", ct);
                if (!seqResult.IsSuccess)
                    return Result<PurchaseOrderDto>.Failure("فشل في توليد رقم الأمر");
                orderNo = seqResult.Value;
            }

            var order = PurchaseOrder.Create(
                request.SupplierId,
                request.WarehouseId,
                orderNo,
                request.OrderDate,
                request.ExpectedDate,
                request.CurrencyId,
                request.ExchangeRate,
                request.Notes,
                userId);

            foreach (var item in request.Items)
            {
                var orderItem = PurchaseOrderItem.Create(
                    item.ProductId,
                    item.ProductUnitId,
                    item.Quantity,
                    item.UnitCost,
                    item.Notes);
                order.AddItem(orderItem);
            }

            await _uow.PurchaseOrders.AddAsync(order, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم إنشاء أمر شراء: رقم {OrderNo} (المعرف {Id}) بواسطة المستخدم {UserId}",
                orderNo, order.Id, userId);

            return await GetByIdAsync(order.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء إنشاء أمر الشراء: {Message}", ex.Message);
            return Result<PurchaseOrderDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء إنشاء أمر الشراء");
            return Result<PurchaseOrderDto>.Failure("حدث خطأ أثناء حفظ أمر الشراء");
        }
    }

    public async Task<Result<PurchaseOrderDto>> UpdateAsync(
        int id, UpdatePurchaseOrderRequest request, int userId, CancellationToken ct)
    {
        var order = await _uow.PurchaseOrders.FirstOrDefaultAsync(
            o => o.Id == id, ct, "Items");

        if (order == null)
            return Result<PurchaseOrderDto>.Failure("أمر الشراء غير موجود", ErrorCodes.NotFound);

        if (order.Status != PurchaseOrderStatus.Draft)
            return Result<PurchaseOrderDto>.Failure("يمكن تعديل أوامر الشراء المسودة فقط");

        try
        {
            // Update OrderNo if provided
            if (request.OrderNo.HasValue && request.OrderNo.Value > 0 && request.OrderNo.Value != order.OrderNo)
            {
                var existing = await _uow.PurchaseOrders.AnyAsync(o => o.OrderNo == request.OrderNo.Value && o.Id != id, ct);
                if (existing)
                    return Result<PurchaseOrderDto>.Failure("رقم الأمر موجود بالفعل");
                order.SetOrderNo(request.OrderNo.Value);
            }

            // Remove existing items
            foreach (var existingItem in order.Items.ToList())
            {
                order.RemoveItem(existingItem);
            }

            // Re-add items from request
            foreach (var item in request.Items)
            {
                var orderItem = PurchaseOrderItem.Create(
                    item.ProductId,
                    item.ProductUnitId,
                    item.Quantity,
                    item.UnitCost,
                    item.Notes);
                order.AddItem(orderItem);
            }

            // Reset order total calculations
            order.RecalculateTotals();

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم تحديث أمر الشراء: المعرف {Id} بواسطة المستخدم {UserId}", id, userId);

            return await GetByIdAsync(id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء تحديث أمر الشراء {Id}: {Message}", id, ex.Message);
            return Result<PurchaseOrderDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء تحديث أمر الشراء {Id}", id);
            return Result<PurchaseOrderDto>.Failure("حدث خطأ أثناء تحديث أمر الشراء");
        }
    }

    public async Task<Result> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var order = await _uow.PurchaseOrders.FirstOrDefaultAsync(
            o => o.Id == id, ct);

        if (order == null)
            return Result.Failure("أمر الشراء غير موجود", ErrorCodes.NotFound);

        if (order.Status == PurchaseOrderStatus.Cancelled)
            return Result.Failure("أمر الشراء ملغي بالفعل", ErrorCodes.InvalidOperation);

        try
        {
            order.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم إلغاء أمر الشراء: المعرف {Id} بواسطة المستخدم {UserId}", id, userId);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء إلغاء أمر الشراء {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء إلغاء أمر الشراء {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء أمر الشراء");
        }
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder o)
    {
        return new PurchaseOrderDto(
            o.Id,
            o.OrderNo,
            o.SupplierId,
            o.Supplier?.Name ?? "غير معروف",
            o.WarehouseId,
            o.Warehouse?.Name ?? "غير معروف",
            o.OrderDate,
            o.ExpectedDate,
            (byte)o.Status,
            o.CurrencyId,
            o.Currency?.Code,
            o.ExchangeRate,
            o.SubTotal,
            o.DiscountAmount,
            o.TaxAmount,
            o.TotalAmount,
            o.Notes,
            o.Items.Select(it => new PurchaseOrderItemDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.ProductUnitId,
                it.ProductUnit?.Unit?.Name ?? "غير معروف",
                it.Quantity,
                it.ReceivedQuantity,
                it.PendingReceiveQuantity,
                it.UnitCost,
                it.LineTotal,
                it.Notes
            )).ToList()
        );
    }
}
