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
    int? AccountId,
    string? AccountName,
    string? Notes,
    bool IsActive
);
