namespace SalesSystem.Contracts.Requests;

public record CreateDepartmentRequest(int BranchId, string Name);
public record UpdateDepartmentRequest(string Name);
