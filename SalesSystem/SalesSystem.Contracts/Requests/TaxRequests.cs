namespace SalesSystem.Contracts.Requests;

public record CreateTaxRequest(string Name, string Code, decimal Rate, byte TaxType = 1, bool IsDefault = false);
public record UpdateTaxRequest(string Name, string Code, decimal Rate, byte TaxType = 1, bool IsDefault = false, bool IsActive = true);
