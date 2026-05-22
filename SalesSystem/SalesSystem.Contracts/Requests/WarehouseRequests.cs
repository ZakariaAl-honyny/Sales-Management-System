namespace SalesSystem.Contracts.Requests;

public record CreateWarehouseRequest(string Name, string? Location, bool IsDefault);
public record UpdateWarehouseRequest(string Name, string? Location, bool IsDefault, bool IsActive);

