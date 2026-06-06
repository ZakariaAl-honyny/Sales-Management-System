using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents the annual closing of a fiscal year.
/// Records net income and links to the closing journal entry.
/// Only one closure per fiscal year (enforced by unique constraint).
/// </summary>
public class FiscalYearClosure : BaseEntity
{
    public int FiscalYear { get; private set; }
    public DateTime ClosedAt { get; private set; }
    public int ClosedByUserId { get; private set; }
    public decimal NetIncome { get; private set; }
    public int ClosingEntryId { get; private set; }
    public string? Notes { get; private set; }

    // ─── Navigation Properties ──────────────────────────
    public virtual User? ClosedByUser { get; private set; }
    public virtual JournalEntry? ClosingEntry { get; private set; }

    private FiscalYearClosure() { } // EF Core

    public static FiscalYearClosure Create(
        int fiscalYear,
        int closedByUserId,
        decimal netIncome,
        int closingEntryId,
        int? createdByUserId = null,
        string? notes = null)
    {
        if (fiscalYear <= 1900 || fiscalYear > DateTime.UtcNow.Year + 1)
            throw new DomainException(
                $"السنة المالية غير صالحة — يجب أن تكون بين 1901 و{DateTime.UtcNow.Year + 1}");

        if (closedByUserId <= 0)
            throw new DomainException("مستخدم الإغلاق المالي مطلوب");

        if (closingEntryId <= 0)
            throw new DomainException("قيد الإغلاق المالي مطلوب");

        var closure = new FiscalYearClosure
        {
            FiscalYear = fiscalYear,
            ClosedAt = DateTime.UtcNow,
            ClosedByUserId = closedByUserId,
            NetIncome = netIncome,
            ClosingEntryId = closingEntryId,
            Notes = notes?.Trim(),
            IsActive = true
        };
        closure.SetCreatedBy(createdByUserId);
        return closure;
    }
}
