using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

public interface IUpdateProductPricingService
{
    Task<Result> UpdateFromPurchaseAsync(
        UpdatePricingRequest request,
        CancellationToken ct = default);
}

public record UpdatePricingRequest(
    int ProductUnitId,
    decimal NewPurchaseCost,
    decimal NewQuantityPurchased,
    decimal? NewSalesPrice,
    int InvoiceId,
    int ChangedBy
);