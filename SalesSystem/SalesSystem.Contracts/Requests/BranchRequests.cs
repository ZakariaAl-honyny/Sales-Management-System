namespace SalesSystem.Contracts.Requests;

public record CreateBranchRequest(string Name, string? Code);
public record UpdateBranchRequest(string Name, string? Code);
