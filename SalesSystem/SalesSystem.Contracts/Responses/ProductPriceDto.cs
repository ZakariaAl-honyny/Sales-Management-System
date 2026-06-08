using SalesSystem.Domain.Enums;

namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Represents a price entry for a specific product unit, currency, and price level.
/// </summary>
public record ProductPriceDto(
    int Id,
    int ProductUnitId,
    string? ProductUnitName,
    int CurrencyId,
    string? CurrencyCode,
    string? CurrencyName,
    PriceLevel PriceLevel,
    decimal Price,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive)
{
    public string PriceLevelDisplay => PriceLevel switch
    {
        PriceLevel.Retail => "تجزئة",
        PriceLevel.Wholesale => "جملة",
        PriceLevel.VIP => "VIP",
        PriceLevel.Distributor => "موزع",
        _ => "غير معروف"
    };

    public bool IsCurrentlyEffective => EffectiveFrom <= DateTime.UtcNow
        && (!EffectiveTo.HasValue || EffectiveTo.Value >= DateTime.UtcNow);
}
