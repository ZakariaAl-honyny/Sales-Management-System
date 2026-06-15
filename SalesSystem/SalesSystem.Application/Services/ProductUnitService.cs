using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Contracts.Responses;
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
                q => q.OrderBy(u => u.IsBaseUnit ? 0 : 1),
                ct,
                includePaths: new[] { "Unit" });

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
                p => p.Id == productId, ct, "Units");
            if (product == null)
                return Result<ProductUnitDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            // Phase 28: Prices managed via ProductPrices entity (not on ProductUnit).
            // Barcodes managed via Product.Barcode column (single source of truth).
            var unit = req.IsBaseUnit
                ? ProductUnit.CreateBaseUnit(productId, (short)req.UnitId)
                : ProductUnit.CreateDerivedUnit(productId, (short)req.UnitId, req.Factor);

            product.AddUnit(unit);
            await _uow.ProductUnits.AddAsync(unit, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Unit (UnitId={UnitId}) added to product {ProductId}", req.UnitId, productId);

            // Reload with Unit navigation for DTO mapping
            var savedUnit = await _uow.ProductUnits.FirstOrDefaultAsync(
                u => u.Id == unit.Id, ct, "Unit");

            return Result<ProductUnitDto>.Success(MapToDto(savedUnit ?? unit));
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
                u => u.Id == unitId && u.ProductId == productId, ct, "Unit");
            if (unit == null)
                return Result<ProductUnitDto>.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

            unit.ChangeUnit((short)req.UnitId);

            await _uow.ProductUnits.UpdateAsync(unit, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Unit {UnitId} updated for product {ProductId} -> UnitId={NewUnitId}", unitId, productId, req.UnitId);

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

                _logger.LogInformation("Unit {UnitId} deactivated for product {ProductId} (UnitId={DomainUnitId})", unitId, productId, unit.UnitId);
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
            // Include Units and Unit navigation to get unit name
            var product = await _uow.Products.FirstOrDefaultAsync(
                p => p.Barcode == barcode, ct, "Category", "Units", "Units.Unit");
            if (product == null)
                return Result<BarcodeResolutionDto>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

            var baseUnit = product.Units.FirstOrDefault(u => u.IsBaseUnit);
            if (baseUnit == null)
                return Result<BarcodeResolutionDto>.Failure("المنتج لا يحتوي على وحدة أساسية", ErrorCodes.NotFound);

            var dto = new BarcodeResolutionDto(
                product.Id,
                product.Name,
                baseUnit.Id,
                baseUnit.UnitId,
                baseUnit.Unit?.Name ?? "",
                baseUnit.Factor);

            return Result<BarcodeResolutionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve barcode {Barcode}", barcode);
            return Result<BarcodeResolutionDto>.Failure("حدث خطأ أثناء البحث عن الباركود.");
        }
    }

    private static ProductUnitDto MapToDto(ProductUnit unit)
    {
        return new ProductUnitDto(
            unit.Id,
            unit.ProductId,
            unit.UnitId,
            unit.Unit?.Name ?? "",
            unit.Factor,
            unit.IsBaseUnit,
            unit.IsActive);
    }
}
