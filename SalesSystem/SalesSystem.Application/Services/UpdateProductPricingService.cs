using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
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

    public async Task UpdateFromPurchaseAsync(
        UpdatePricingRequest request,
        CancellationToken ct = default)
    {
        var purchasedUnit = await _uow.ProductUnits.Query()
            .Include(u => u.Product)
                .ThenInclude(p => p.Units)
            .FirstOrDefaultAsync(u => u.Id == request.ProductUnitId, ct)
            ?? throw new InvalidOperationException($"ProductUnit {request.ProductUnitId} not found");

        var product = purchasedUnit.Product;
        var allUnits = product.Units.Where(u => u.IsActive).ToList();
        var baseUnit = allUnits.FirstOrDefault(u => u.IsBaseUnit)
            ?? throw new InvalidOperationException($"Product {product.Name} has no base unit");

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
        var currentStock = await _uow.WarehouseStocks.Query()
            .Where(s => s.ProductId == baseUnit.ProductId)
            .Select(s => s.Quantity)
            .FirstOrDefaultAsync(ct);

        var oldCost = baseUnit.PurchaseCost;

        if (currentStock <= 0) return newBaseUnitCost;

        var weightedAverage =
            ((currentStock * oldCost) + (newQuantityInBaseUnits * newBaseUnitCost))
            / (currentStock + newQuantityInBaseUnits);

        return Math.Round(weightedAverage, 4);
    }
}