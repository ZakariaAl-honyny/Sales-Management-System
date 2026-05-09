namespace SalesSystem.Contracts.Requests;

public record CreateUnitRequest(string Name, string? Symbol);
public record UpdateUnitRequest(string Name, string? Symbol, bool IsActive);

