namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create a new currency.
/// Code must be exactly 3 characters (ISO 4217), FractionName is required.
/// </summary>
public record CreateCurrencyRequest(
    string Name,
    string Code,
    string Symbol,
    bool IsBaseCurrency = false,
    string FractionName = "",
    byte DecimalPlaces = 2);

/// <summary>
/// Request to update an existing currency.
/// Code is read-only after creation — not included in update request.
/// </summary>
public record UpdateCurrencyRequest(
    string Name,
    string Symbol,
    string FractionName = "",
    byte DecimalPlaces = 2);
