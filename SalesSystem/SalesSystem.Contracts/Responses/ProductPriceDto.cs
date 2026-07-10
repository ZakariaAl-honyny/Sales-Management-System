namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Represents a price entry for a specific product unit and currency combination.
/// </summary>
public record ProductPriceDto(
    int Id,
    int ProductUnitId,
    string? ProductUnitName,
    decimal Price,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo)
{
    public bool IsCurrentlyEffective => EffectiveFrom <= DateTime.UtcNow
        && (!EffectiveTo.HasValue || EffectiveTo.Value >= DateTime.UtcNow);
}
