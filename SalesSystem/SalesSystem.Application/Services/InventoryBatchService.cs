using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class InventoryBatchService : IInventoryBatchService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<InventoryBatchService> _logger;

    public InventoryBatchService(
        IUnitOfWork uow,
        IDocumentSequenceService sequenceService,
        ILogger<InventoryBatchService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<List<InventoryBatchDto>>> GetByProductAsync(int productId, int? warehouseId, CancellationToken ct)
    {
        try
        {
            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId && (!warehouseId.HasValue || b.WarehouseId == warehouseId.Value),
                q => q.OrderByDescending(b => b.CreatedAt),
                ct,
                includePaths: new[] { "Product", "Warehouse" });

            var dtos = batches.Select(MapToDto).ToList();
            return Result<List<InventoryBatchDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches for product {ProductId}", productId);
            return Result<List<InventoryBatchDto>>.Failure("حدث خطأ أثناء استرجاع الدفعات");
        }
    }

    public async Task<Result<InventoryBatchDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var batch = await _uow.InventoryBatches.FirstOrDefaultAsync(
                b => b.Id == id, ct, "Product", "Warehouse");

            if (batch == null)
                return Result<InventoryBatchDto>.Failure("الدفعة غير موجودة", ErrorCodes.NotFound);

            return Result<InventoryBatchDto>.Success(MapToDto(batch));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory batch {Id}", id);
            return Result<InventoryBatchDto>.Failure("حدث خطأ أثناء استرجاع الدفعة");
        }
    }

    public async Task<Result<InventoryBatchDto>> CreateAsync(CreateInventoryBatchRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Validate product exists
            var product = await _uow.Products.GetByIdAsync(request.ProductId, ct);
            if (product == null)
                return Result<InventoryBatchDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            // Validate warehouse exists
            var warehouse = await _uow.Warehouses.GetByIdAsync(request.WarehouseId, ct);
            if (warehouse == null)
                return Result<InventoryBatchDto>.Failure("المستودع غير موجود", ErrorCodes.NotFound);

            // Generate system batch number
            var seqResult = await _sequenceService.GetNextIntAsync("InventoryBatch", ct);
            if (!seqResult.IsSuccess)
                return Result<InventoryBatchDto>.Failure(seqResult.Error ?? "فشل في توليد رقم الدفعة.");

            var batch = InventoryBatch.Create(
                batchNo: seqResult.Value.ToString("D6"),
                productId: request.ProductId,
                warehouseId: request.WarehouseId,
                quantityReceived: request.QuantityReceived,
                unitCost: request.UnitCost,
                purchaseInvoiceId: request.PurchaseInvoiceId,
                purchaseInvoiceLineId: request.PurchaseInvoiceLineId,
                supplierBatchNo: request.SupplierBatchNo,
                expiryDate: request.ExpiryDate,
                createdByUserId: userId);

            await _uow.InventoryBatches.AddAsync(batch, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Inventory batch created: Product={ProductId}, Warehouse={WarehouseId}, Qty={Quantity}, BatchNo={BatchNo} by User {UserId}",
                request.ProductId, request.WarehouseId, request.QuantityReceived, request.BatchNo, userId);

            return await GetByIdAsync(batch.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating inventory batch: {Message}", ex.Message);
            return Result<InventoryBatchDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory batch");
            return Result<InventoryBatchDto>.Failure("حدث خطأ أثناء إنشاء الدفعة");
        }
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct)
    {
        try
        {
            var batch = await _uow.InventoryBatches.GetByIdAsync(id, ct);
            if (batch == null)
                return Result.Failure("الدفعة غير موجودة", ErrorCodes.NotFound);

            _uow.InventoryBatches.DeleteRange(new[] { batch });
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Inventory batch {Id} permanently deleted", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating inventory batch {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء تنشيط الدفعة");
        }
    }

    // ─── Mapping ─────────────────────────────────

    private static InventoryBatchDto MapToDto(InventoryBatch batch) => new(
        batch.Id,
        batch.ProductId,
        batch.Product?.Name,
        batch.PurchaseInvoiceId,
        batch.PurchaseInvoiceLineId,
        batch.WarehouseId,
        batch.Warehouse?.Name,
        batch.BatchNo,
        batch.QuantityReceived,
        batch.QuantityRemaining,
        batch.UnitCost,
        batch.ExpiryDate,
        batch.SupplierBatchNo);
}
