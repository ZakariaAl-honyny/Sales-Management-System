using SalesSystem.Domain.Enums;

namespace SalesSystem.Contracts.Requests;

public record CreateProductPriceRequest(
    int ProductUnitId,
    int CurrencyId,
    PriceLevel PriceLevel,
    decimal Price,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo = null);

public record UpdateProductPriceRequest(
    decimal Price,
    PriceLevel PriceLevel,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null);
