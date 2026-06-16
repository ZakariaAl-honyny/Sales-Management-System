using System.Linq.Expressions;
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
/// Product service aligned to the new 65-table schema.
/// - Prices via ProductPrices (multi-currency, effective dates)
/// - Cost via InventoryBatches (FIFO/FEFO, weighted average)
/// - Barcode on Product entity (primary barcode only)
/// - ImagePath on Product (single image, no separate ProductImages table)
/// - Expiry tracked via InventoryBatches, not Product
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductService> _logger;
    private readonly IDocumentSequenceService _documentSequenceService;
    private readonly IAccountingIntegrationService _accountingService;

    public ProductService(
        IUnitOfWork uow,
        ILogger<ProductService> logger,
        IDocumentSequenceService documentSequenceService,
        IAccountingIntegrationService accountingService)
    {
        _uow = uow;
        _logger = logger;
        _documentSequenceService = documentSequenceService;
        _accountingService = accountingService;
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Id == id, ct, "ProductCategory");

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<PagedResult<ProductDto>>> GetAllAsync(
        string? search, int? categoryId, int page, int pageSize,
        bool includeInactive = false, CancellationToken ct = default)
    {
        Expression<Func<Product, bool>> predicate = p =>
            (string.IsNullOrWhiteSpace(search) || p.Name.Contains(search)) &&
            (!categoryId.HasValue || p.CategoryId == categoryId.Value);

        var includes = new[] { "ProductCategory" };

        var (items, total) = await _uow.Products.GetPagedAsync(
            predicate, q => q.OrderBy(p => p.Name), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<ProductDto>>.Success(
            PagedResult<ProductDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        try
        {
            // ─── Guard: validate opening stock prerequisites ──────────────
            var openingQuantity = request.OpeningQuantity ?? 0m;
            var openingUnitCost = request.OpeningUnitCost ?? 0m;

            if (openingQuantity > 0 && openingUnitCost <= 0)
                return Result<ProductDto>.Failure(
                    "يجب تحديد تكلفة الوحدة للكمية الافتتاحية.");

            if (request.TrackExpiry && openingQuantity > 0 && request.OpeningExpiryDate == null)
                return Result<ProductDto>.Failure(
                    "يجب تحديد تاريخ انتهاء الصلاحية للمنتجات التي لها صلاحية.");

            // ─── Create product entity ────────────────────────────────────
            var product = Product.Create(
                name: request.Name,
                categoryId: request.CategoryId,
                description: request.Description,
                barcode: request.Barcode,
                taxId: request.TaxId,
                reorderLevel: request.ReorderLevel,
                trackExpiry: request.TrackExpiry,
                imagePath: request.ImagePath
            );

            // ─── Execute everything inside a transaction ───────────────────
            await _uow.ExecuteTransactionAsync(async () =>
            {
                await _uow.Products.AddAsync(product, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Product created: {ProductName} (ID: {ProductId})", product.Name, product.Id);

                if (openingQuantity > 0)
                {
                    // ── Ensure base unit exists ─────────────────────────
                    var hasBaseUnit = await _uow.ProductUnits.AnyAsync(
                        pu => pu.ProductId == product.Id && pu.IsBaseUnit, ct);
                    if (!hasBaseUnit)
                    {
                        var defaultUnit = (await _uow.Units.ToListAsync(ct)).FirstOrDefault();
                        if (defaultUnit == null)
                            throw new InvalidOperationException(
                                "لا توجد وحدات قياس في النظام. يرجى إضافة وحدة قياس أولاً.");

                        var baseUnit = ProductUnit.CreateBaseUnit(product.Id, defaultUnit.Id);
                        await _uow.ProductUnits.AddAsync(baseUnit, ct);
                        await _uow.SaveChangesAsync(ct);
                    }

                    // ── Resolve warehouse ──────────────────────────────
                    var warehouses = await _uow.Warehouses.ToListAsync(ct);
                    var warehouse = warehouses.FirstOrDefault()
                        ?? throw new InvalidOperationException(
                            "لا توجد مستودعات في النظام. يرجى إضافة مستودع أولاً.");

                    // ── Generate batch number ──────────────────────────
                    var batchSeqResult = await _documentSequenceService.GetNextIntAsync("InventoryBatch", ct);
                    if (!batchSeqResult.IsSuccess)
                        throw new InvalidOperationException(
                            batchSeqResult.Error ?? "فشل في توليد رقم الدفعة.");

                    // ── Create InventoryBatch (OPENING) ────────────────
                    DateOnly? openingExpiryDateOnly = request.OpeningExpiryDate.HasValue
                        ? DateOnly.FromDateTime(request.OpeningExpiryDate.Value)
                        : null;
                    var batch = InventoryBatch.Create(
                        batchNo: batchSeqResult.Value.ToString(),
                        productId: product.Id,
                        warehouseId: (short)warehouse.Id,
                        quantityReceived: openingQuantity,
                        unitCost: openingUnitCost,
                        expiryDate: openingExpiryDateOnly);
                    await _uow.InventoryBatches.AddAsync(batch, ct);

                    // ── Update/create WarehouseStock ────────────────────
                    var existingStock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
                        ws => ws.ProductId == product.Id && ws.WarehouseId == warehouse.Id, ct);

                    if (existingStock != null)
                    {
                        existingStock.IncreaseQuantity(openingQuantity);
                    }
                    else
                    {
                        var newStock = WarehouseStock.Create(
                            warehouseId: (short)warehouse.Id,
                            productId: product.Id,
                            quantity: openingQuantity);
                        await _uow.WarehouseStocks.AddAsync(newStock, ct);
                    }

                    // ── Generate transaction number ────────────────────
                    var seqResult = await _documentSequenceService.GetNextIntAsync("InventoryTransaction", ct);
                    if (!seqResult.IsSuccess)
                        throw new InvalidOperationException(
                            seqResult.Error ?? "فشل في توليد رقم المعاملة.");

                    // ── Create InventoryTransaction ─────────────────────
                    var invTx = InventoryTransaction.Create(
                        transactionNo: seqResult.Value.ToString(),
                        movementType: InventoryTransactionType.OpeningBalance,
                        warehouseId: (short)warehouse.Id,
                        referenceType: null,
                        referenceId: null,
                        notes: $"الرصيد الافتتاحي للمنتج: {product.Name}");
                    await _uow.InventoryTransactions.AddAsync(invTx, ct);
                    await _uow.SaveChangesAsync(ct);

                    // ── Get base unit for the transaction line ──────────
                    var baseProductUnit = await _uow.ProductUnits.FirstOrDefaultAsync(
                        pu => pu.ProductId == product.Id && pu.IsBaseUnit, ct);
                    if (baseProductUnit == null)
                        throw new InvalidOperationException(
                            "لم يتم العثور على الوحدة الأساسية للمنتج.");

                    // ── Create InventoryTransactionLine ─────────────────
                    var txLine = InventoryTransactionLine.Create(
                        inventoryTransactionId: invTx.Id,
                        productUnitId: baseProductUnit.Id,
                        quantity: openingQuantity,
                        unitCost: openingUnitCost,
                        batchNo: batch.BatchNo);
                    invTx.AddLine(txLine);

                    // ── Create journal entry for opening stock ──────────
                    var totalValue = openingQuantity * openingUnitCost;
                    var accountingResult = await _accountingService.CreateProductOpeningEntryAsync(
                        productId: product.Id,
                        productName: product.Name,
                        totalOpeningValue: totalValue,
                        createdByUserId: 0,
                        transactionDate: DateTime.UtcNow,
                        ct: ct);
                    if (!accountingResult.IsSuccess)
                        throw new InvalidOperationException(
                            accountingResult.Error ?? "فشل في إنشاء قيد اليومية للرصيد الافتتاحي.");

                    await _uow.SaveChangesAsync(ct);
                }
            }, ct);

            return await GetByIdAsync(product.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while creating product");
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating product");
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation while creating product");
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating product");
            return Result<ProductDto>.Failure("حدث خطأ غير متوقع أثناء إضافة المنتج.");
        }
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct)
    {
        try
        {
            var product = await _uow.Products.FirstOrDefaultIgnoreFiltersAsync(p => p.Id == id, ct);
            if (product == null)
                return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            product.Update(
                name: request.Name,
                categoryId: request.CategoryId,
                description: request.Description,
                barcode: request.Barcode,
                taxId: request.TaxId,
                reorderLevel: request.ReorderLevel,
                trackExpiry: request.TrackExpiry,
                imagePath: request.ImagePath
            );

            if (request.IsActive != product.IsActive)
            {
                if (request.IsActive) product.Restore();
                else product.MarkAsDeleted();
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product updated: {ProductName} (ID: {ProductId})", product.Name, product.Id);

            return await GetByIdAsync(product.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while updating product {Id}", id);
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating product {Id}", id);
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating product {Id}", id);
            return Result<ProductDto>.Failure("حدث خطأ غير متوقع أثناء تحديث بيانات المنتج.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.GetByIdAsync(id, ct);
        if (product == null)
            return Result.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        await _uow.Products.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Product soft-deleted: {ProductId}", id);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var product = await _uow.Products.FirstOrDefaultIgnoreFiltersAsync(p => p.Id == id, ct);
        if (product == null)
            return Result.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        var hasSalesItems = await _uow.SalesInvoiceLines.AnyAsync(i => i.ProductId == id, ct);
        if (hasSalesItems)
            return Result.Failure("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات بيع");

        var hasPurchaseItems = await _uow.PurchaseInvoiceLines.AnyAsync(i => i.ProductId == id, ct);
        if (hasPurchaseItems)
            return Result.Failure("لا يمكن حذف المنتج نهائياً لأنه مرتبط بعمليات شراء");

        try
        {
            await _uow.Products.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Product permanently deleted: {ProductId}", id);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogError(ex, "Failed to permanently delete product {ProductId} due to database constraint", id);
            return Result.Failure("لا يمكن حذف المنتج نهائياً. قد يكون مرتبطاً ببيانات أخرى في النظام.");
        }
    }

    public async Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result<ProductDto>.Failure("الباركود مطلوب");

        var product = await _uow.Products.FirstOrDefaultAsync(
            p => p.Barcode == barcode, ct, "ProductCategory");

        if (product == null)
            return Result<ProductDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        return Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<ProductDto>> UploadImageAsync(int id, byte[] imageBytes, string fileName, CancellationToken ct)
    {
        // Phase 25 simplification: Product has a single ImagePath field.
        // UploadImageAsync saves the file and updates ImagePath on the Product entity.
        throw new NotImplementedException("Use Desktop file upload that updates Product.ImagePath directly");
    }

    public async Task<Result<List<ProductDto>>> GetExpiringProductsAsync(int thresholdDays, CancellationToken ct)
    {
        // Expiry tracking moved to InventoryBatches — query batches directly.
        throw new NotImplementedException("Use InventoryBatchService.GetExpiringBatchesAsync instead");
    }

    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto(
            p.Id,
            p.Name,
            p.CategoryId,
            p.ProductCategory?.Name,
            p.Barcode,
            p.Description,
            p.ReorderLevel,
            p.TrackExpiry,
            p.ImagePath,
            p.IsActive
        );
    }
}
