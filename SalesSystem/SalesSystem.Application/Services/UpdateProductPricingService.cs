using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class UpdateProductPricingService : IUpdateProductPricingService
{
    private readonly IUnitOfWork _uow;
    private readonly ISystemSettingsRepository _settings;
    private readonly ILogger<UpdateProductPricingService> _logger;

    public UpdateProductPricingService(
        IUnitOfWork uow,
        ISystemSettingsRepository settings,
        ILogger<UpdateProductPricingService> logger)
    {
        _uow = uow;
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result> UpdateFromPurchaseAsync(
        UpdatePricingRequest request,
        CancellationToken ct = default)
    {
        var purchasedUnit = await _uow.ProductUnits.FirstOrDefaultAsync(
            u => u.Id == request.ProductUnitId, ct, "Product.Units");
        
        if (purchasedUnit == null)
            return Result.Failure("وحدة المنتج غير موجودة", "PRODUCT_UNIT_NOT_FOUND");

        var product = purchasedUnit.Product;
        var allUnits = product.Units.Where(u => u.IsActive).ToList();
        var baseUnit = allUnits.FirstOrDefault(u => u.IsBaseUnit);
        
        if (baseUnit == null)
            return Result.Failure($"المنتج '{product.Name}' لا يحتوي على وحدة أساسية", "NO_BASE_UNIT");

        var costingMethod = await _settings.GetCostingMethodAsync(ct);

        var newBaseUnitCost = await CalculateNewBaseUnitCostAsync(
            costingMethod,
            baseUnit,
            purchasedUnit,
            request.NewPurchaseCost,
            request.NewQuantityPurchased,
            ct);

        _logger.LogInformation(
            "Updating costs for Product {ProductId} using {Method}. New base unit cost: {Cost}",
            product.Id, costingMethod, newBaseUnitCost);

        var historyEntries = new List<ProductPriceHistory>();

        foreach (var unit in allUnits)
        {
            var newUnitCost = unit.CalculateCostFromBaseUnitCost(newBaseUnitCost);
            var oldCost = unit.UpdatePurchaseCost(newUnitCost);

            historyEntries.Add(ProductPriceHistory.Create(
                unit.Id,
                "PurchaseCost",
                oldCost,
                newUnitCost,
                costingMethod.ToString(),
                request.InvoiceId,
                request.ChangedBy));
        }

        if (request.NewSalesPrice.HasValue && request.NewSalesPrice.Value > 0)
        {
            var oldSalesPrice = purchasedUnit.UpdateSalesPrice(request.NewSalesPrice.Value);

            historyEntries.Add(ProductPriceHistory.Create(
                purchasedUnit.Id,
                "SalesPrice",
                oldSalesPrice,
                request.NewSalesPrice.Value,
                null,
                request.InvoiceId,
                request.ChangedBy));
        }

        foreach (var entry in historyEntries)
        {
            await _uow.ProductPriceHistory.AddAsync(entry, ct);
        }
        await _uow.SaveChangesAsync(ct);
        
        return Result.Success();
    }

    private async Task<decimal> CalculateNewBaseUnitCostAsync(
        CostingMethod method,
        ProductUnit baseUnit,
        ProductUnit purchasedUnit,
        decimal invoiceCostForPurchasedUnit,
        decimal quantityPurchased,
        CancellationToken ct)
    {
        var newBaseCostFromInvoice = purchasedUnit.IsBaseUnit
            ? invoiceCostForPurchasedUnit
            : invoiceCostForPurchasedUnit / purchasedUnit.BaseConversionFactor;

        return method switch
        {
            CostingMethod.LastPurchasePrice =>
                newBaseCostFromInvoice,

            CostingMethod.SupplierPrice =>
                baseUnit.SupplierPrice > 0
                    ? baseUnit.SupplierPrice
                    : newBaseCostFromInvoice,

            CostingMethod.WeightedAverage =>
                await CalculateWeightedAverageAsync(baseUnit, newBaseCostFromInvoice, quantityPurchased * purchasedUnit.BaseConversionFactor, ct),

            _ => newBaseCostFromInvoice
        };
    }

    private async Task<decimal> CalculateWeightedAverageAsync(
        ProductUnit baseUnit,
        decimal newBaseUnitCost,
        decimal newQuantityInBaseUnits,
        CancellationToken ct)
    {
        var stockRecord = await _uow.WarehouseStocks.FirstOrDefaultAsync(
            s => s.ProductId == baseUnit.ProductId, ct);
        var currentStock = stockRecord?.Quantity ?? 0m;

        var oldCost = baseUnit.PurchaseCost;

        if (currentStock <= 0) return newBaseUnitCost;

        var weightedAverage =
            ((currentStock * oldCost) + (newQuantityInBaseUnits * newBaseUnitCost))
            / (currentStock + newQuantityInBaseUnits);

        return Math.Round(weightedAverage, 2);
    }
}