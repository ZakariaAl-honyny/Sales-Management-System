using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class UpdateProductPricingService : IUpdateProductPricingService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateProductPricingService> _logger;

    public UpdateProductPricingService(
        IUnitOfWork uow,
        ILogger<UpdateProductPricingService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result> UpdateFromPurchaseAsync(
        UpdatePricingRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Phase 25: Costing now routes through InventoryBatches instead of ProductUnit fields.
            // The ProductUnit no longer stores PurchaseCost, Cost, or SalesPrice.
            // Cost allocation is done via InventoryBatch records during purchase receipt.

            var purchasedUnit = await _uow.ProductUnits.FirstOrDefaultAsync(
                u => u.Id == request.ProductUnitId, ct, "Product");

            if (purchasedUnit == null)
                return Result.Failure("وحدة المنتج غير موجودة", "PRODUCT_UNIT_NOT_FOUND");

            var product = purchasedUnit.Product;

            if (product == null)
                return Result.Failure("المنتج غير موجود", "PRODUCT_NOT_FOUND");

            _logger.LogInformation(
                "UpdateProductPricing: Product {ProductId}, Unit {UnitId}, " +
                "NewCost={NewCost}, Qty={Qty}, Invoice={InvoiceId} — " +
                "costing routed through InventoryBatches (Phase 25)",
                product.Id, request.ProductUnitId,
                request.NewPurchaseCost, request.NewQuantityPurchased, request.InvoiceId);

            // ─── Update Product.Cost based on InventoryBatches ────────────
            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == product.Id && b.Quantity > 0 && b.IsActive, null, ct);

            if (batches.Count > 0)
            {
                var totalValue = batches.Sum(b => b.Quantity * b.UnitCost);
                var totalQuantity = batches.Sum(b => b.Quantity);
                var newCost = totalQuantity > 0
                    ? Math.Round(totalValue / totalQuantity, 2)
                    : request.NewPurchaseCost;

                product.UpdateCost(newCost);

                _logger.LogInformation(
                    "Updated Cost for Product {ProductId}: {Cost} " +
                    "(from {BatchCount} batches, total value={TotalValue}, total qty={TotalQty})",
                    product.Id, newCost, batches.Count, totalValue, totalQuantity);
            }
            else
            {
                // No existing batches — set Cost to the incoming purchase cost
                product.UpdateCost(request.NewPurchaseCost);

                _logger.LogInformation(
                    "Updated Cost for Product {ProductId} to {Cost} (no existing batches)",
                    product.Id, request.NewPurchaseCost);
            }

            // ─── Record ProductPriceHistory for audit trail ───────────────
            var history = ProductPriceHistory.Create(
                request.ProductUnitId,
                "PurchaseCost",
                0m, // OldCost — not tracked at purchase level; recalculated from batches
                product.Cost,
                $"Purchase invoice #{request.InvoiceId}",
                request.InvoiceId,
                request.ChangedBy);

            await _uow.ProductPriceHistory.AddAsync(history, ct);
            await _uow.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation while updating pricing for ProductUnit {ProductUnitId}",
                request.ProductUnitId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating pricing for ProductUnit {ProductUnitId}, Invoice {InvoiceId}",
                request.ProductUnitId, request.InvoiceId);
            return Result.Failure("حدث خطأ أثناء تحديث تكلفة المنتج");
        }
    }
}