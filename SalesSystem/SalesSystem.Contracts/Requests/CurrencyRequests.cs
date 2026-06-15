namespace SalesSystem.Contracts.Requests;

public record CreateCurrencyRequest(
    string Name,
    string Code,
    string Symbol,
    bool IsBaseCurrency = false,
    string? FractionName = null,
    int DecimalPlaces = 2);

public record UpdateCurrencyRequest(
    string Name,
    string Symbol,
    string? FractionName = null,
    int DecimalPlaces = 2);
