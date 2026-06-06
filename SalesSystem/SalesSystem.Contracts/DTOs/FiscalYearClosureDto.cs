namespace SalesSystem.Contracts.DTOs;

/// <summary>
/// DTO for fiscal year closure records.
/// </summary>
public record FiscalYearClosureDto(
    int Id,
    int FiscalYear,
    DateTime ClosedAt,
    int ClosedByUserId,
    decimal NetIncome,
    int ClosingEntryId,
    string? NetIncomeFormatted = null);
