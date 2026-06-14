using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Contracts.Responses;

/// <summary>
/// DTO for a key-value system account mapping.
/// </summary>
public record SystemAccountMappingDto(
    int Id,
    SystemAccountKey MappingKey,
    string? MappingKeyName,
    int AccountId,
    string? AccountName,
    string? AccountCode,
    int BranchId,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsActive
);
