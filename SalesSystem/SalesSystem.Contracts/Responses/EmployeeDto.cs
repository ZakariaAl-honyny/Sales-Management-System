namespace SalesSystem.Contracts.Responses;

public record EmployeeDto(
    int Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    int EmployeeNo,
    DateTime HireDate,
    int? DepartmentId,
    string? DepartmentName,
    decimal Salary,
    int? AccountId,
    string? AccountName,
    string? Notes,
    bool IsActive
);
