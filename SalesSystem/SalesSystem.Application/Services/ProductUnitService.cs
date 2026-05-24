using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class ProductUnitService : IProductUnitService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductUnitService> _logger;

    public ProductUnitService(IUnitOfWork uow, ILogger<ProductUnitService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<ProductUnitDto>>> GetByProductIdAsync(int productId, CancellationToken ct)
    {
        try
        {
            var units = await _uow.ProductUnits.ToListAsync(
                u => u.ProductId == productId,
                q => q.OrderBy(u => u.IsBaseUnit ? 0 : 1).ThenBy(u => u.SortOrder),
                ct,
                includePaths: new[] { "Barcodes" });

            var dtos = units.Select(MapToDto).ToList();
            return Result<List<ProductUnitDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load units for product {ProductId}", productId);
            return Result<List<ProductUnitDto>>.Failure("حدث خطأ أثناء تحميل وحدات المنتج.");
        }
    }

    public async Task<Result<ProductUnitDto>> AddUnitAsync(int productId, AddProductUnitRequest req, CancellationToken ct)
    {
        try
        {
            var product = await _uow.Products.FirstOrDefaultAsync(
                p => p.Id == productId, ct, "Units", "Units.Barcodes");
            if (product == null)
                return Result<ProductUnitDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            if (req.Barcodes?.Count > 0)
            {
                foreach (var barcode in req.Barcodes)
                {
                    if (!string.IsNullOrWhiteSpace(barcode))
                    {
                        var existing = await _uow.UnitBarcodes.FirstOrDefaultAsync(
                            b => b.BarcodeValue == barcode, ct);
                        if (existing != null)
                            return Result<ProductUnitDto>.Failure(
                                $"الباركود '{barcode}' مستخدم بالفعل", ErrorCodes.DuplicateBarcode);
                    }
                }
            }

            var unit = req.IsBaseUnit
                ? ProductUnit.CreateBaseUnit(productId, req.UnitName, req.RetailPrice)
                : ProductUnit.CreateDerivedUnit(productId, req.UnitName, req.ConversionFactor, req.RetailPrice);

            if (req.Barcodes?.Count > 0)
            {
                foreach (var barcode in req.Barcodes)
                {
                    if (!string.IsNullOrWhiteSpace(barcode))
                        unit.AddBarcode(barcode);
                }
            }

            product.AddUnit(unit);
            await _uow.ProductUnits.AddAsync(unit, ct);

            var history = ProductPriceHistory.CreateWithDetails(
                unit.Id,
                0, req.RetailPrice,
                0, req.WholesalePrice,
                0, 0,
                "إنشاء وحدة جديدة",
                0);
            await _uow.ProductPriceHistory.AddAsync(history, ct);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Unit {UnitName} added to product {ProductId}", unit.UnitName, productId);

            return Result<ProductUnitDto>.Success(MapToDto(unit));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while adding unit to product {ProductId}", productId);
            return Result<ProductUnitDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while adding unit to product {ProductId}", productId);
            return Result<ProductUnitDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding unit to product {ProductId}", productId);
            return Result<ProductUnitDto>.Failure("حدث خطأ غير متوقع أثناء إضافة الوحدة.");
        }
    }

    public async Task<Result<ProductUnitDto>> UpdateUnitAsync(int productId, int unitId, UpdateProductUnitRequest req, CancellationToken ct)
    {
        try
        {
            var unit = await _uow.ProductUnits.FirstOrDefaultAsync(
                u => u.Id == unitId && u.ProductId == productId, ct, "Barcodes");
            if (unit == null)
                return Result<ProductUnitDto>.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

            unit.Update(req.UnitName, req.RetailPrice, req.WholesalePrice);

            await _uow.ProductUnits.UpdateAsync(unit, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Unit {UnitId} updated for product {ProductId}", unitId, productId);

            return Result<ProductUnitDto>.Success(MapToDto(unit));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while updating unit {UnitId}", unitId);
            return Result<ProductUnitDto>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating unit {UnitId}", unitId);
            return Result<ProductUnitDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating unit {UnitId}", unitId);
            return Result<ProductUnitDto>.Failure("حدث خطأ غير متوقع أثناء تحديث الوحدة.");
        }
    }

    public async Task<Result> DeleteUnitAsync(int productId, int unitId, DeleteStrategy strategy, CancellationToken ct)
    {
        if (strategy == DeleteStrategy.Cancel)
            return Result.Success();

        try
        {
            var product = await _uow.Products.FirstOrDefaultAsync(
                p => p.Id == productId, ct, "Units");
            if (product == null)
                return Result.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            var unit = product.Units.FirstOrDefault(u => u.Id == unitId);
            if (unit == null)
                return Result.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

            if (strategy == DeleteStrategy.Deactivate)
            {
                if (product.Units.Count(u => u.IsActive) <= 1)
                    return Result.Failure("يجب أن يكون للمنتج وحدة قياس واحدة على الأقل");

                unit.MarkAsDeleted();
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Unit {UnitId} deactivated for product {ProductId}", unitId, productId);
                return Result.Success();
            }

            if (strategy == DeleteStrategy.Permanent)
            {
                if (product.Units.Count(u => u.IsActive) <= 1)
                    return Result.Failure("يجب أن يكون للمنتج وحدة قياس واحدة على الأقل");

                var hasSales = await _uow.SalesInvoiceItems.AnyAsync(i => i.ProductId == productId, ct);
                if (hasSales)
                    return Result.Failure("لا يمكن حذف الوحدة نهائياً لأن المنتج مرتبط بعمليات بيع");

                var hasPurchases = await _uow.PurchaseInvoiceItems.AnyAsync(i => i.ProductId == productId, ct);
                if (hasPurchases)
                    return Result.Failure("لا يمكن حذف الوحدة نهائياً لأن المنتج مرتبط بعمليات شراء");

                product.RemoveUnit(unit);
                await _uow.ProductUnits.HardDeleteAsync(unitId, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Unit {UnitId} permanently deleted from product {ProductId}", unitId, productId);
                return Result.Success();
            }

            return Result.Failure("استراتيجية حذف غير معروفة");
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while deleting unit {UnitId}", unitId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogError(ex, "Failed to permanently delete unit {UnitId} due to database constraint", unitId);
            return Result.Failure("لا يمكن حذف الوحدة نهائياً. قد تكون مرتبطة ببيانات أخرى في النظام.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting unit {UnitId}", unitId);
            return Result.Failure("حدث خطأ غير متوقع أثناء حذف الوحدة.");
        }
    }

    public async Task<Result<BarcodeResolutionDto>> ResolveBarcodeAsync(string barcode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return Result<BarcodeResolutionDto>.Failure("الباركود مطلوب");

        try
        {
            var unitBarcode = await _uow.UnitBarcodes.FirstOrDefaultAsync(
                b => b.BarcodeValue == barcode, ct, "ProductUnit", "ProductUnit.Product");
            if (unitBarcode == null)
                return Result<BarcodeResolutionDto>.Failure("الباركود غير موجود", ErrorCodes.NotFound);

            var unit = unitBarcode.ProductUnit;
            var product = unit.Product;

            var dto = new BarcodeResolutionDto(
                product.Id,
                product.Name,
                unit.Id,
                unit.UnitName,
                unit.BaseConversionFactor,
                unit.SalesPrice,
                unit.SalesPrice);

            return Result<BarcodeResolutionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve barcode {Barcode}", barcode);
            return Result<BarcodeResolutionDto>.Failure("حدث خطأ أثناء البحث عن الباركود.");
        }
    }

    public async Task<Result<List<ProductPriceHistoryDto>>> GetPriceHistoryAsync(int productId, CancellationToken ct)
    {
        try
        {
            var historyEntries = await _uow.ProductPriceHistory.ToListAsync(
                h => h.ProductUnit.ProductId == productId,
                q => q.OrderByDescending(h => h.Id),
                ct,
                includePaths: new[] { "ProductUnit" });

            if (historyEntries.Count == 0)
                return Result<List<ProductPriceHistoryDto>>.Success(new List<ProductPriceHistoryDto>());

            var userIds = historyEntries.Select(h => h.ChangedByUserId).Distinct().ToList();
            var users = await _uow.Users.ToListAsync(ct);
            var userMap = users.ToDictionary(u => u.Id, u => u.FullName);

            var dtos = historyEntries.Select(h => new ProductPriceHistoryDto(
                h.Id,
                h.ProductUnitId,
                h.ProductUnit?.UnitName ?? "",
                h.OldRetailPrice,
                h.NewRetailPrice,
                h.OldWholesalePrice,
                h.NewWholesalePrice,
                h.OldAvgCost,
                h.NewAvgCost,
                h.ChangeReason ?? "",
                userMap.GetValueOrDefault(h.ChangedByUserId, ""),
                h.ChangedAt
            )).ToList();

            return Result<List<ProductPriceHistoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load price history for product {ProductId}", productId);
            return Result<List<ProductPriceHistoryDto>>.Failure("حدث خطأ أثناء تحميل سجل الأسعار.");
        }
    }

    private static ProductUnitDto MapToDto(ProductUnit unit)
    {
        var dto = new ProductUnitDto(
            unit.Id,
            unit.ProductId,
            unit.UnitName,
            unit.BaseConversionFactor,
            unit.SalesPrice,
            unit.SalesPrice,
            unit.PurchaseCost,
            unit.IsBaseUnit,
            unit.IsActive)
        {
            Barcodes = unit.Barcodes.Select(b => b.BarcodeValue).ToList()
        };
        return dto;
    }
}
