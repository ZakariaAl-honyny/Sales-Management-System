namespace SalesSystem.Contracts.Requests;

public record CreateCurrencyRequest(
    string Name,
    string Code,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    string? FractionName,
    int DecimalPlaces = 2);

public record UpdateCurrencyRequest(
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    string? FractionName,
    int DecimalPlaces = 2);

public record UpdateExchangeRateRequest(decimal NewRate);
