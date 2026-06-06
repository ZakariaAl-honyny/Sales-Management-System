namespace SalesSystem.Contracts.Requests;

public record CreateCurrencyRequest(
    string Name,
    string Code,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    string? FractionName);

public record UpdateCurrencyRequest(
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    string? FractionName,
    bool IsActive);

public record UpdateExchangeRateRequest(decimal NewRate);
