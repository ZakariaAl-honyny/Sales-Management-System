namespace SalesSystem.Contracts.Requests.Suppliers;

public record CreateSupplierRequest(string? Code, string Name, string? Phone, string? Email, string? Address, decimal OpeningBalance);

public record UpdateSupplierRequest(int Id, string? Code, string Name, string? Phone, string? Email, string? Address);