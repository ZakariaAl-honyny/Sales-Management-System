using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Contracts.Requests;

/// <summary>
/// Request to create or update a system account mapping.
/// </summary>
public record CreateSystemAccountMappingRequest(
    SystemAccountKey MappingKey,
    int AccountId,
    int BranchId = 0,
    string? DescriptionAr = null,
    string? DescriptionEn = null
);

/// <summary>
/// Request to update an existing system account mapping.
/// </summary>
public record UpdateSystemAccountMappingRequest(
    int AccountId,
    string? DescriptionAr = null,
    string? DescriptionEn = null
);
