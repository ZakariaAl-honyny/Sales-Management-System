namespace SalesSystem.Contracts.Requests;

public record CreateEmployeeRequest(
    int PartyId,
    int EmployeeNo,
    DateTime HireDate,
    int? DepartmentId = null,
    decimal Salary = 0,
    string? Notes = null
);

public record UpdateEmployeeRequest(
    int? DepartmentId = null,
    decimal? Salary = null,
    string? Notes = null
);
