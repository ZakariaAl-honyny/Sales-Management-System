namespace SalesSystem.Contracts.Responses;

public record EmployeeDto(
    int Id,
    int PartyId,
    string? PartyName,
    int EmployeeNo,
    DateTime HireDate,
    int? DepartmentId,
    string? DepartmentName,
    decimal Salary,
    string? Notes,
    bool IsActive
);
