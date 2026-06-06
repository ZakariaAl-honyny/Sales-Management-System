namespace SalesSystem.Contracts.Requests;

public record CreateTaxRequest(string Name, decimal Rate, bool IsDefault);
public record UpdateTaxRequest(string Name, decimal Rate, bool IsDefault, bool IsActive);
