using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

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

    public async Task<Result<PagedResult<PurchaseOrderDto>>> GetAllAsync(
        int? supplierId, int? status, string? search,
        DateTime? from, DateTime? to, int page, int pageSize, bool includeInactive, CancellationToken ct)
    {
        var searchLower = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();
        int? searchId = int.TryParse(searchLower, out var parsedId) ? parsedId : null;

        Expression<Func<PurchaseOrder, bool>> predicate = o =>
            (!supplierId.HasValue || o.SupplierId == supplierId.Value) &&
            (!status.HasValue || (int)o.Status == status.Value) &&
            (status.HasValue || includeInactive || o.Status != PurchaseOrderStatus.Cancelled) &&
            (!from.HasValue || o.OrderDate >= from.Value) &&
            (!to.HasValue || o.OrderDate <= to.Value) &&
            (searchLower == null ||
             (searchId.HasValue && o.Id == searchId.Value) ||
             (o.Supplier != null && o.Supplier.Name.ToLower().Contains(searchLower)) ||
             (o.Notes != null && o.Notes.ToLower().Contains(searchLower)));

        var includes = new[] { "Supplier", "Warehouse" };

        var (items, total) = await _uow.PurchaseOrders.GetPagedAsync(
            predicate, q => q.OrderByDescending(o => o.OrderDate), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<PurchaseOrderDto>>.Success(PagedResult<PurchaseOrderDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<PurchaseOrderDto>> CreateAsync(CreatePurchaseOrderRequest request, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync(async () =>
        {
            // Resolve OrderNo
            int orderNo;
            if (request.OrderNo.HasValue && request.OrderNo.Value > 0)
            {
                var existing = await _uow.PurchaseOrders.AnyAsync(
                    o => o.OrderNo == request.OrderNo.Value, ct);
                if (existing)
                    return Result<PurchaseOrderDto>.Failure("رقم أمر الشراء موجود بالفعل");
                orderNo = request.OrderNo.Value;
            }
            else
            {
                var seqResult = await _documentSequenceService.GetNextIntAsync("PurchaseOrder", ct);
                if (!seqResult.IsSuccess)
                    return Result<PurchaseOrderDto>.Failure("فشل في توليد رقم أمر الشراء");
                orderNo = seqResult.Value;
            }

            if (request.Items == null || request.Items.Count == 0)
                return Result<PurchaseOrderDto>.Failure("يجب إضافة صنف واحد على الأقل");

            var order = PurchaseOrder.Create(
                orderNo,
                request.SupplierId,
                request.WarehouseId,
                orderDate: request.OrderDate,
                expectedDate: request.ExpectedDate,
                discountAmount: 0,
                notes: request.Notes,
                currencyId: request.CurrencyId,
                exchangeRate: request.ExchangeRate,
                createdByUserId: userId
            );

            foreach (var item in request.Items)
            {
                var orderItem = PurchaseOrderItem.Create(
                    productId: item.ProductId,
                    productUnitId: item.ProductUnitId,
                    quantity: item.Quantity,
                    unitCost: item.UnitCost,
                    notes: item.Notes,
                    createdByUserId: userId
                );
                order.AddItem(orderItem);
            }

            await _uow.PurchaseOrders.AddAsync(order, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Purchase Order created: OrderNo {OrderNo} (ID: {Id}) by User {UserId}", orderNo, order.Id, userId);

            return await GetByIdAsync(order.Id, ct);
        }, ct);
    }

    public async Task<Result<PurchaseOrderDto>> UpdateAsync(int id, UpdatePurchaseOrderRequest request, int userId, CancellationToken ct)
    {
        var order = await _uow.PurchaseOrders.FirstOrDefaultAsync(
            o => o.Id == id, ct, "Items");

        if (order == null)
            return Result<PurchaseOrderDto>.Failure("أمر الشراء غير موجود", ErrorCodes.NotFound);

        if (order.Status != PurchaseOrderStatus.Draft)
            return Result<PurchaseOrderDto>.Failure("يمكن تعديل أوامر الشراء المسودة فقط");

        return await _uow.ExecuteTransactionAsync(async () =>
        {
            if (request.Items == null || request.Items.Count == 0)
                return Result<PurchaseOrderDto>.Failure("يجب إضافة صنف واحد على الأقل");

            order.SetCurrency(request.CurrencyId, request.ExchangeRate);
            order.UpdateExpectedDate(request.ExpectedDate);
            order.UpdateNotes(request.Notes);

            // Remove existing items from database
            var existingItems = order.Items.ToList();
            foreach (var existingItem in existingItems)
                order.RemoveItem(existingItem);
            _uow.PurchaseOrderItems.DeleteRange(existingItems);

            // Re-add items through domain
            foreach (var item in request.Items)
            {
                var orderItem = PurchaseOrderItem.Create(
                    productId: item.ProductId,
                    productUnitId: item.ProductUnitId,
                    quantity: item.Quantity,
                    unitCost: item.UnitCost,
                    notes: item.Notes,
                    createdByUserId: userId
                );
                order.AddItem(orderItem);
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Purchase Order updated: ID {Id} by User {UserId}", order.Id, userId);

            return await GetByIdAsync(order.Id, ct);
        }, ct);
    }

    public async Task<Result> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var order = await _uow.PurchaseOrders.GetByIdAsync(id, ct);

        if (order == null)
            return Result.Failure("أمر الشراء غير موجود", ErrorCodes.NotFound);

        if (order.Status == PurchaseOrderStatus.Cancelled)
            return Result.Failure("أمر الشراء ملغى بالفعل", ErrorCodes.InvalidOperation);

        if (order.Status == PurchaseOrderStatus.Received)
            return Result.Failure("لا يمكن إلغاء أمر شراء تم استلامه بالكامل");

        return await _uow.ExecuteTransactionAsync(async () =>
        {
            order.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Purchase Order cancelled: ID {Id} by User {UserId}", id, userId);

            return Result.Success();
        }, ct);
    }

    public async Task<Result<List<PurchaseOrderDto>>> GetPendingOrdersAsync(CancellationToken ct)
    {
        var orders = await _uow.PurchaseOrders.ToListAsync(
            o => o.Status == PurchaseOrderStatus.Draft ||
                 o.Status == PurchaseOrderStatus.Approved ||
                 o.Status == PurchaseOrderStatus.PartiallyReceived,
            q => q.OrderByDescending(o => o.OrderDate),
            ct,
            false,
            "Supplier", "Warehouse");

        var dtos = orders.Select(MapToDto).ToList();
        return Result<List<PurchaseOrderDto>>.Success(dtos);
    }

    private PurchaseOrderDto MapToDto(PurchaseOrder o)
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
            o.SubTotal,
            o.DiscountAmount,
            o.TaxAmount,
            o.TotalAmount,
            o.CurrencyId,
            o.ExchangeRate,
            o.Notes,
            o.Items.Select(i => new PurchaseOrderItemDto(
                i.Id,
                i.ProductId,
                i.Product?.Name ?? "غير معروف",
                i.ProductUnitId,
                i.ProductUnit?.UnitName ?? "غير معروف",
                i.Quantity,
                i.ReceivedQuantity,
                i.PendingReceiveQuantity,
                i.UnitCost,
                i.LineTotal,
                i.Notes
            )).ToList()
        );
    }
}
