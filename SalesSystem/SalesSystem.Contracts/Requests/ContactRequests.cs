namespace SalesSystem.Contracts.Requests;

public record CreateCustomerContactRequest(int CustomerId, string Name, string? Phone = null,
    string? Email = null, string? Position = null, string? Notes = null);

public record UpdateCustomerContactRequest(string Name, string? Phone = null,
    string? Email = null, string? Position = null, string? Notes = null);

public record CreateSupplierContactRequest(int SupplierId, string Name, string? Phone = null,
    string? Email = null, string? Position = null, string? Notes = null);

public record UpdateSupplierContactRequest(string Name, string? Phone = null,
    string? Email = null, string? Position = null, string? Notes = null);
