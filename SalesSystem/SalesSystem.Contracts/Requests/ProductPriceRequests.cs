namespace SalesSystem.Contracts.Requests;

public record CreateProductPriceRequest(
    int ProductUnitId,
    short CurrencyId,
    decimal Price,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo = null);

public record UpdateProductPriceRequest(
    decimal Price,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null);
