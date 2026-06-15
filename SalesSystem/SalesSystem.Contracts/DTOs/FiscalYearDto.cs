namespace SalesSystem.Contracts.DTOs;

/// <summary>
/// DTO for fiscal year records.
/// </summary>
public record FiscalYearDto(
    int Id,
    int Year,
    DateTime StartDate,
    DateTime EndDate,
    bool IsOpen,
    DateTime? OpenedAt,
    int? OpenedByUserId,
    DateTime? ClosedAt,
    int? ClosedByUserId)
{
    public string StatusDisplay => IsOpen ? "مفتوحة" : "مغلقة";
}
