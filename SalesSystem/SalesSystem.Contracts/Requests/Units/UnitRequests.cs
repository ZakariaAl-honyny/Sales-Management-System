namespace SalesSystem.Contracts.Requests.Units;

public record CreateUnitRequest(string Name, string? Symbol);

public record UpdateUnitRequest(int Id, string Name, string? Symbol);