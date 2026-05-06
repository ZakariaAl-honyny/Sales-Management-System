namespace SalesSystem.Contracts.Requests.Customers;

public record CreateCustomerRequest(string? Code, string Name, string? Phone, string? Email, string? Address, decimal OpeningBalance);

public record UpdateCustomerRequest(int Id, string? Code, string Name, string? Phone, string? Email, string? Address);