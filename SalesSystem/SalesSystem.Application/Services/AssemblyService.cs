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
/// Service for managing Bills of Materials and executing assembly production.
/// Components are deducted from inventory using FIFO/FEFO allocation via <see cref="IFifoAllocationService"/>.
/// The finished assembly product is added as a new <see cref="InventoryBatch"/>.
/// All operations are wrapped in a single database transaction via <see cref="IUnitOfWork.ExecuteTransactionAsync"/>.
/// </summary>
public class AssemblyService : IAssemblyService
{
    private readonly IUnitOfWork _uow;
    private readonly IFifoAllocationService _fifoService;
    private readonly ILogger<AssemblyService> _logger;

    public AssemblyService(
        IUnitOfWork uow,
        IFifoAllocationService fifoService,
        ILogger<AssemblyService> logger)
    {
        _uow = uow;
        _fifoService = fifoService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════
    // BOM CRUD
    // ═══════════════════════════════════════════════════════════════════

    public async Task<Result<BillOfMaterialDto>> CreateBomAsync(
        CreateBillOfMaterialRequest request, CancellationToken ct)
    {
        try
        {
            // Validate assembly product exists
            var assemblyProduct = await _uow.Products.GetByIdAsync(request.AssemblyProductId, ct);
            if (assemblyProduct == null)
                return Result<BillOfMaterialDto>.Failure("المنتج المُجمَّع غير موجود", ErrorCodes.NotFound);

            // Validate component product exists
            var componentProduct = await _uow.Products.GetByIdAsync(request.ComponentProductId, ct);
            if (componentProduct == null)
                return Result<BillOfMaterialDto>.Failure("المكوّن غير موجود", ErrorCodes.NotFound);

            // Validate component product unit exists
            var componentUnit = await _uow.ProductUnits.GetByIdAsync(request.ComponentUnitId, ct);
            if (componentUnit == null)
                return Result<BillOfMaterialDto>.Failure("وحدة المكوّن غير موجودة", ErrorCodes.NotFound);

            // Ensure component unit belongs to the component product
            if (componentUnit.ProductId != request.ComponentProductId)
                return Result<BillOfMaterialDto>.Failure("وحدة المكوّن لا تنتمي إلى المنتج المحدد");

            // Check for duplicate BOM entry
            var existingBom = await _uow.BillOfMaterials.FirstOrDefaultAsync(
                b => b.AssemblyProductId == request.AssemblyProductId
                     && b.ComponentProductId == request.ComponentProductId, ct);
            if (existingBom != null)
                return Result<BillOfMaterialDto>.Failure(
                    "فاتورة المواد هذه موجودة بالفعل — هذا المكوّن مضاف مسبقاً للمنتج المُجمَّع",
                    ErrorCodes.DuplicateEntry);

            var bom = BillOfMaterials.Create(
                request.AssemblyProductId,
                request.ComponentProductId,
                request.ComponentUnitId,
                request.QuantityRequired,
                request.WastePercentage);

            await _uow.BillOfMaterials.AddAsync(bom, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "BOM created: AssemblyProduct {AssemblyProductId}, Component {ComponentProductId}, Qty {QuantityRequired}",
                request.AssemblyProductId, request.ComponentProductId, request.QuantityRequired);

            return Result<BillOfMaterialDto>.Success(MapToDto(bom, assemblyProduct.Name, componentProduct.Name, componentUnit.UnitName));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while creating BOM");
            return Result<BillOfMaterialDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating BOM");
            return Result<BillOfMaterialDto>.Failure("حدث خطأ غير متوقع أثناء إضافة فاتورة المواد.");
        }
    }

    public async Task<Result<BillOfMaterialDto>> UpdateBomAsync(
        int id, UpdateBillOfMaterialRequest request, CancellationToken ct)
    {
        try
        {
            var bom = await _uow.BillOfMaterials.FirstOrDefaultAsync(
                b => b.Id == id, ct, "AssemblyProduct", "ComponentProduct", "ComponentUnit");
            if (bom == null)
                return Result<BillOfMaterialDto>.Failure("فاتورة المواد غير موجودة", ErrorCodes.NotFound);

            bom.Update(request.ComponentUnitId, request.QuantityRequired, request.WastePercentage);

            await _uow.BillOfMaterials.UpdateAsync(bom, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "BOM {BomId} updated: ComponentUnit {ComponentUnitId}, Qty {QuantityRequired}, Waste {WastePercentage}%",
                id, request.ComponentUnitId, request.QuantityRequired, request.WastePercentage);

            return Result<BillOfMaterialDto>.Success(MapToDto(
                bom, bom.AssemblyProduct.Name, bom.ComponentProduct.Name, bom.ComponentUnit.UnitName));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while updating BOM {BomId}", id);
            return Result<BillOfMaterialDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating BOM {BomId}", id);
            return Result<BillOfMaterialDto>.Failure("حدث خطأ غير متوقع أثناء تحديث فاتورة المواد.");
        }
    }

    public async Task<Result> DeleteBomAsync(int id, CancellationToken ct)
    {
        try
        {
            var bom = await _uow.BillOfMaterials.GetByIdAsync(id, ct);
            if (bom == null)
                return Result.Failure("فاتورة المواد غير موجودة", ErrorCodes.NotFound);

            await _uow.BillOfMaterials.SoftDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("BOM {BomId} soft-deleted", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting BOM {BomId}", id);
            return Result.Failure("حدث خطأ أثناء حذف فاتورة المواد.");
        }
    }

    public async Task<Result<BillOfMaterialDto>> GetBomByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var bom = await _uow.BillOfMaterials.FirstOrDefaultAsync(
                b => b.Id == id, ct, "AssemblyProduct", "ComponentProduct", "ComponentUnit");
            if (bom == null)
                return Result<BillOfMaterialDto>.Failure("فاتورة المواد غير موجودة", ErrorCodes.NotFound);

            return Result<BillOfMaterialDto>.Success(MapToDto(
                bom, bom.AssemblyProduct.Name, bom.ComponentProduct.Name, bom.ComponentUnit.UnitName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching BOM {BomId}", id);
            return Result<BillOfMaterialDto>.Failure("حدث خطأ أثناء استرجاع فاتورة المواد.");
        }
    }

    public async Task<Result<List<BillOfMaterialDto>>> GetBomsForAssemblyAsync(
        int assemblyProductId, CancellationToken ct)
    {
        try
        {
            var assemblyProduct = await _uow.Products.GetByIdAsync(assemblyProductId, ct);
            if (assemblyProduct == null)
                return Result<List<BillOfMaterialDto>>.Failure("المنتج المُجمَّع غير موجود", ErrorCodes.NotFound);

            var boms = await _uow.BillOfMaterials.ToListAsync(
                b => b.AssemblyProductId == assemblyProductId,
                q => q.OrderBy(b => b.ComponentProduct.Name),
                ct,
                includePaths: new[] { "AssemblyProduct", "ComponentProduct", "ComponentUnit" });

            var dtos = boms.Select(b => MapToDto(
                b, b.AssemblyProduct.Name, b.ComponentProduct.Name, b.ComponentUnit.UnitName)).ToList();

            return Result<List<BillOfMaterialDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching BOMs for assembly product {AssemblyProductId}", assemblyProductId);
            return Result<List<BillOfMaterialDto>>.Failure("حدث خطأ أثناء استرجاع فواتير المواد.");
        }
    }

    public async Task<Result<List<BillOfMaterialDto>>> GetAllBomsAsync(CancellationToken ct)
    {
        try
        {
            var boms = await _uow.BillOfMaterials.ToListAsync(
                null,
                q => q.OrderBy(b => b.AssemblyProduct.Name).ThenBy(b => b.ComponentProduct.Name),
                ct,
                includePaths: new[] { "AssemblyProduct", "ComponentProduct", "ComponentUnit" });

            var dtos = boms.Select(b => MapToDto(
                b, b.AssemblyProduct.Name, b.ComponentProduct.Name, b.ComponentUnit.UnitName)).ToList();

            return Result<List<BillOfMaterialDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all BOMs");
            return Result<List<BillOfMaterialDto>>.Failure("حدث خطأ أثناء استرجاع فواتير المواد.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Assembly Production
    // ═══════════════════════════════════════════════════════════════════

    public async Task<Result<ProduceAssemblyResultDto>> ProduceAsync(
        ProduceAssemblyRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // ─── Pre-transaction Validation ────────────────────────────

            // Validate quantity
            if (request.Quantity <= 0)
                return Result<ProduceAssemblyResultDto>.Failure("الكمية المنتجة يجب أن تكون أكبر من الصفر");

            // Validate assembly product exists
            var assemblyProduct = await _uow.Products.GetByIdAsync(request.AssemblyProductId, ct);
            if (assemblyProduct == null)
                return Result<ProduceAssemblyResultDto>.Failure("المنتج المُجمَّع غير موجود", ErrorCodes.NotFound);

            // Validate warehouse exists
            var warehouse = await _uow.Warehouses.GetByIdAsync(request.WarehouseId, ct);
            if (warehouse == null)
                return Result<ProduceAssemblyResultDto>.Failure("المستودع غير موجود", ErrorCodes.NotFound);

            // Load all active BOM entries for this assembly product with eager-loaded navigation properties
            var boms = await _uow.BillOfMaterials.ToListAsync(
                b => b.AssemblyProductId == request.AssemblyProductId,
                null,
                ct,
                includePaths: new[] { "ComponentProduct", "ComponentUnit" });

            if (boms.Count == 0)
                return Result<ProduceAssemblyResultDto>.Failure(
                    "لا توجد فاتورة مواد لهذا المنتج. يرجى إضافة المكوّنات أولاً.");

            // Verify the assembly product has a base unit (required for batch creation)
            var assemblyBaseUnit = assemblyProduct.GetBaseUnit();

        // ─── Calculate Component Requirements ──────────────────────

        var componentRequirements = new List<(BillOfMaterials Bom, decimal RequiredQty)>();
        foreach (var bom in boms)
        {
            // Effective quantity = QuantityRequired × request.Quantity × (1 + WastePercentage/100)
            var requiredQty = bom.EffectiveQuantityRequired * request.Quantity;

            if (requiredQty <= 0)
            {
                return Result<ProduceAssemblyResultDto>.Failure(
                    $"الكمية المطلوبة للمكوّن '{bom.ComponentProduct.Name}' غير صالحة.");
            }

            componentRequirements.Add((bom, requiredQty));
        }

        // ─── Pre-check component stock availability ────────────────
        foreach (var (bom, requiredQty) in componentRequirements)
        {
            // Get available stock for this component in the warehouse
            var batchesForCheck = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == bom.ComponentProductId
                     && b.WarehouseId == request.WarehouseId
                     && b.Quantity > 0
                     && b.IsActive,
                ct: ct);

            var availableQty = batchesForCheck.Sum(b => b.Quantity);
            if (availableQty < requiredQty)
            {
                _logger.LogWarning(
                    "Insufficient stock for component {ComponentProductId} ({ComponentName}): " +
                    "required {RequiredQty}, available {AvailableQty}",
                    bom.ComponentProductId, bom.ComponentProduct.Name, requiredQty, availableQty);

                return Result<ProduceAssemblyResultDto>.Failure(
                    $"الكمية المتاحة للمكوّن '{bom.ComponentProduct.Name}' غير كافية.\n" +
                    $"المطلوب: {requiredQty:N3}، المتاح: {availableQty:N3}",
                    ErrorCodes.InsufficientStock);
            }
        }

        // ─── Execute Production (Atomic Transaction) ───────────────

        return await _uow.ExecuteTransactionAsync(async () =>
        {
            try
            {
                _logger.LogInformation(
                    "Starting assembly production: Product {AssemblyProductId} ({AssemblyName}), " +
                    "Qty {Quantity}, Warehouse {WarehouseId}",
                    request.AssemblyProductId, assemblyProduct.Name, request.Quantity, request.WarehouseId);

                var componentsConsumed = new List<ComponentConsumedDto>();
                var totalCost = 0m;

                // ── Step 1: Deduct each component from inventory batches ──
                foreach (var (bom, requiredQty) in componentRequirements)
                {
                    var deductionResult = await _fifoService.DeductFromBatchesAsync(
                        bom.ComponentProductId,
                        request.WarehouseId,
                        requiredQty,
                        salesInvoiceItemId: null,
                        createdByUserId: userId,
                        ct);

                    if (!deductionResult.IsSuccess)
                    {
                        _logger.LogError(
                            "FIFO deduction failed for component {ComponentProductId}: {Error}",
                            bom.ComponentProductId, deductionResult.Error);
                        return Result<ProduceAssemblyResultDto>.Failure(
                            $"فشل في صرف المكوّن '{bom.ComponentProduct.Name}': {deductionResult.Error}");
                    }

                    var allocations = deductionResult.Value!;

                    // Calculate total cost of this component based on actual allocation costs
                    var componentTotalCost = allocations.Sum(a => a.Quantity * a.UnitCost);
                    var totalConsumedQty = allocations.Sum(a => a.Quantity);
                    var avgUnitCost = totalConsumedQty > 0
                        ? Math.Round(componentTotalCost / totalConsumedQty, 2)
                        : 0m;

                    totalCost += componentTotalCost;

                    componentsConsumed.Add(new ComponentConsumedDto(
                        bom.ComponentProductId,
                        bom.ComponentProduct.Name,
                        totalConsumedQty,
                        avgUnitCost,
                        Math.Round(componentTotalCost, 2)));

                    _logger.LogInformation(
                        "Component {ComponentName} consumed: {Quantity} from {AllocationCount} batches, cost {Cost}",
                        bom.ComponentProduct.Name, totalConsumedQty, allocations.Count, componentTotalCost);

                    // Note: DeductFromBatchesAsync already creates InventoryMovement records
                    // (SaleOut) for audit trail purposes. These movements accurately track
                    // the batch-level allocations and costs for the consumed components.
                }

                // ── Step 2: Add the finished assembly as a new InventoryBatch ──
                // Use the base unit cost as the per-unit cost of the assembly
                var unitCost = request.Quantity > 0
                    ? Math.Round(totalCost / request.Quantity, 2)
                    : 0m;

                var assemblyBatchNo = $"ASM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

                var assemblyBatch = InventoryBatch.Create(
                    request.AssemblyProductId,
                    request.WarehouseId,
                    request.Quantity,
                    unitCost,
                    assemblyBatchNo,
                    purchaseInvoiceItemId: null,
                    manufactureDate: null,
                    expiryDate: null,
                    createdByUserId: userId);

                await _uow.InventoryBatches.AddAsync(assemblyBatch, ct);

                // ── Step 3: Record inventory movement for the assembly addition ──
                var productionMovement = InventoryMovement.Create(
                    request.AssemblyProductId,
                    request.WarehouseId,
                    MovementType.Adjustment,  // Use Adjustment for production addition
                    request.Quantity,
                    0m,  // Quantity before
                    request.Quantity,  // Quantity after
                    "Assembly",
                    request.AssemblyProductId,
                    unitCost,
                    notes: $"إنتاج تجميعي: {assemblyProduct.Name} (الكمية: {request.Quantity})",
                    createdByUserId: userId);

                await _uow.InventoryMovements.AddAsync(productionMovement, ct);

                _logger.LogInformation(
                    "Assembly production completed: {AssemblyName}, Qty {Quantity}, TotalCost {TotalCost}, BatchNo {BatchNo}",
                    assemblyProduct.Name, request.Quantity, totalCost, assemblyBatchNo);

                var result = new ProduceAssemblyResultDto(
                    request.AssemblyProductId,
                    assemblyProduct.Name,
                    request.Quantity,
                    Math.Round(totalCost, 2),
                    componentsConsumed.AsReadOnly());

                return Result<ProduceAssemblyResultDto>.Success(result);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation during assembly production for Product {ProductId}",
                    request.AssemblyProductId);
                return Result<ProduceAssemblyResultDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during assembly production for Product {ProductId}, Warehouse {WarehouseId}",
                    request.AssemblyProductId, request.WarehouseId);
                return Result<ProduceAssemblyResultDto>.Failure("حدث خطأ أثناء تنفيذ التجميع.");
            }
        }, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation during assembly production for Product {ProductId}",
                request.AssemblyProductId);
            return Result<ProduceAssemblyResultDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during assembly production for Product {ProductId}",
                request.AssemblyProductId);
            return Result<ProduceAssemblyResultDto>.Failure("حدث خطأ غير متوقع أثناء تنفيذ التجميع.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Mapping Helper
    // ═══════════════════════════════════════════════════════════════════

    private static BillOfMaterialDto MapToDto(
        BillOfMaterials bom, string assemblyProductName, string componentProductName, string componentUnitName)
    {
        return new BillOfMaterialDto(
            bom.Id,
            bom.AssemblyProductId,
            assemblyProductName,
            bom.ComponentProductId,
            componentProductName,
            bom.ComponentUnitId,
            componentUnitName,
            bom.QuantityRequired,
            bom.WastePercentage,
            bom.EffectiveQuantityRequired,
            bom.IsActive);
    }
}
