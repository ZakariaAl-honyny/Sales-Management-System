using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

/// <summary>
/// Handles inventory write-offs: reduces stock and records the transaction.
/// </summary>
public class InventoryWriteOffService : IInventoryWriteOffService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryWriteOffService> _logger;

    public InventoryWriteOffService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        ILogger<InventoryWriteOffService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<Result<StockWriteOffDto>> WriteOffExpiredStockAsync(
        CreateStockWriteOffRequest request, int userId, CancellationToken ct)
    {
        // ─── 1. Validate request ─────────────────────────────────────────────
        if (request.Quantity <= 0)
        {
            _logger.LogWarning("Write-off failed: Quantity must be greater than zero");
            return Result<StockWriteOffDto>.Failure("الكمية يجب أن تكون أكبر من الصفر");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            _logger.LogWarning("Write-off failed: Reason is required");
            return Result<StockWriteOffDto>.Failure("السبب مطلوب");
        }

        try
        {
            // ─── 2. Fetch product ────────────────────────────────────────────
            var product = await _uow.Products.FirstOrDefaultAsync(
                p => p.Id == request.ProductId, ct, "RetailUnit", "WholesaleUnit");

            if (product == null)
            {
                _logger.LogWarning("Write-off failed: Product {ProductId} not found", request.ProductId);
                return Result<StockWriteOffDto>.Failure("المنتج غير موجود");
            }

            // ─── 3. Convert quantity to base unit if UnitId specified ───────
            var quantity = request.Quantity;

            if (request.UnitId.HasValue)
            {
                var sourceUnit = await _uow.ProductUnits.FirstOrDefaultAsync(
                    u => u.Id == request.UnitId.Value && u.ProductId == request.ProductId, ct);

                if (sourceUnit == null)
                {
                    _logger.LogWarning("Write-off failed: Unit {UnitId} not found for product {ProductId}",
                        request.UnitId.Value, request.ProductId);
                    return Result<StockWriteOffDto>.Failure("الوحدة المحددة غير موجودة لهذا المنتج");
                }

                // Convert to base unit: quantity * sourceUnit.BaseConversionFactor
                quantity = sourceUnit.ToBaseUnitQuantity(request.Quantity);
            }

            // ─── 4. Validate stock availability ─────────────────────────────
            var validResult = await _inventoryService.ValidateStockAsync(
                request.ProductId, request.WarehouseId, quantity, false, ct);

            if (!validResult.IsSuccess)
            {
                _logger.LogWarning("Write-off failed: Insufficient stock for Product {ProductId} in Warehouse {WarehouseId}",
                    request.ProductId, request.WarehouseId);
                return Result<StockWriteOffDto>.Failure(validResult.Error ?? "المخزون غير كافٍ");
            }

            // ─── 5. Open transaction ─────────────────────────────────────────
            await using var transaction = await _uow.BeginTransactionAsync(ct);

            // ─── 6. Create StockWriteOff entity ─────────────────────────────
            var writeOff = StockWriteOff.Create(
                request.ProductId,
                request.WarehouseId,
                quantity,
                request.Reason,
                request.UnitId,
                userId);

            await _uow.StockWriteOffs.AddAsync(writeOff, ct);
            await _uow.SaveChangesAsync(ct);

            // ─── 7. Decrease stock (now we have writeOff.Id) ────────────────
            var decResult = await _inventoryService.DecreaseStockAsync(
                request.ProductId,
                request.WarehouseId,
                quantity,
                MovementType.Adjustment,
                "WriteOff",
                writeOff.Id,
                0m, // TODO: unitPrice from ProductUnit.PurchaseCost (Phase 25)
                userId,
                ct);

            if (!decResult.IsSuccess)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning("Write-off failed during stock decrease: {Error}", decResult.Error);
                return Result<StockWriteOffDto>.Failure(decResult.Error ?? "فشل في تحديث المخزون");
            }

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Stock write-off created: Product {ProductId}, Qty {Quantity}, Reason {Reason}, WriteOffId {WriteOffId}",
                request.ProductId, quantity, request.Reason, writeOff.Id);

            // ─── 8. Map to DTO ──────────────────────────────────────────────
            var dto = new StockWriteOffDto(
                writeOff.Id,
                writeOff.ProductId,
                product.Name,
                writeOff.WarehouseId,
                null,
                writeOff.Quantity,
                writeOff.WriteOffDate,
                writeOff.Reason,
                writeOff.UnitId,
                userId,
                writeOff.CreatedAt);

            return Result<StockWriteOffDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing off stock for Product {ProductId}", request.ProductId);
            return Result<StockWriteOffDto>.Failure("حدث خطأ أثناء ترحيل الإتلاف");
        }
    }
}
