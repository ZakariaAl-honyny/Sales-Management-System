namespace SalesSystem.Contracts.Requests.Warehouses;

public record CreateWarehouseRequest(string? Code, string Name, string? Location, bool IsDefault);

public record UpdateWarehouseRequest(int Id, string? Code, string Name, string? Location, bool IsDefault);