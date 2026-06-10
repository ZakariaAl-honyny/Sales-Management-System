namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Represents a price entry for a specific product unit and currency combination.
/// </summary>
public record ProductPriceDto(
    int Id,
    int ProductUnitId,
    string? ProductUnitName,
    int CurrencyId,
    string? CurrencyCode,
    string? CurrencyName,
    decimal Price,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive)
{
    public bool IsCurrentlyEffective => EffectiveFrom <= DateTime.UtcNow
        && (!EffectiveTo.HasValue || EffectiveTo.Value >= DateTime.UtcNow);
}
