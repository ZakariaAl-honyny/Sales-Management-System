using SalesSystem.Domain.Enums;

namespace SalesSystem.Contracts.Responses;

/// <summary>
/// Result of the effective price lookup with fallback chain information.
/// Indicates which price level, currency, and whether conversion was applied.
/// </summary>
public record EffectivePriceDto(
    int ProductUnitId,
    int CurrencyId,
    string? CurrencyCode,
    string? CurrencyName,
    PriceLevel PriceLevel,
    decimal Price,
    decimal? OriginalPrice,
    int? OriginalCurrencyId,
    string? OriginalCurrencyCode,
    string? OriginalCurrencyName,
    string? FallbackDescription)
{
    /// <summary>
    /// True if the price was converted from another currency via exchange rate.
    /// </summary>
    public bool IsConverted => OriginalCurrencyId.HasValue && OriginalCurrencyId.Value != CurrencyId;

    /// <summary>
    /// True if the price level differs from the originally requested level (retail fallback).
    /// </summary>
    public bool IsFallbackLevel => FallbackDescription != null && !IsConverted;
}
