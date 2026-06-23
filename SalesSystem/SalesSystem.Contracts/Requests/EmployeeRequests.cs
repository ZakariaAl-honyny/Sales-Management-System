namespace SalesSystem.Contracts.Requests;

public record CreateEmployeeRequest(
    string Name,
    int EmployeeNo,
    DateTime HireDate,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    int? DepartmentId = null,
    decimal Salary = 0,
    string? Notes = null
);

public record UpdateEmployeeRequest(
    string Name,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    int? DepartmentId = null,
    decimal? Salary = null,
    string? Notes = null
);
