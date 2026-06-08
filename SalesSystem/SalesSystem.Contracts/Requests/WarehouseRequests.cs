namespace SalesSystem.Contracts.Requests;

public record CreateWarehouseRequest(
    string Name,
    byte Type = 1,
    string? Location = null,
    string? Phone = null,
    string? Address = null,
    string? ManagerName = null,
    bool IsDefault = false,
    int? AccountId = null,
    string? Notes = null);

public record UpdateWarehouseRequest(
    string Name,
    byte Type = 1,
    string? Location = null,
    string? Phone = null,
    string? Address = null,
    string? ManagerName = null,
    bool IsDefault = false,
    bool IsActive = true,
    int? AccountId = null,
    string? Notes = null);
