using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Services;

/// <summary>
/// Phase 25: Costing is handled via InventoryBatches (FIFO/weighted average).
/// This service is retained for backward compatibility but is essentially a no-op.
/// All cost updates occur when InventoryBatch records are created/modified.
/// </summary>
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
        // Phase 25: Costing is handled entirely via InventoryBatches.
        // The Product entity no longer has a Cost property.
        // ProductUnit no longer has PurchaseCost.
        // This method is a no-op — cost allocation occurs via InventoryBatch.Create()
        // during purchase invoice receipt.
        _logger.LogInformation(
            "UpdateProductPricing called for ProductUnit {ProductUnitId} (Invoice {InvoiceId}) — " +
            "delegated to InventoryBatch costing (Phase 25).",
            request.ProductUnitId, request.InvoiceId);

        return Result.Success();
    }
}
