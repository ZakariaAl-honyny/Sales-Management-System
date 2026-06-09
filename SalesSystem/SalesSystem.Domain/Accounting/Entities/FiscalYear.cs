using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a fiscal year with open/close lifecycle.
/// Only one fiscal year per calendar year (enforced by unique filtered index).
/// </summary>
public class FiscalYear : BaseEntity
{
    public int Year { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsOpen { get; private set; }
    public DateTime? OpenedAt { get; private set; }
    public int? OpenedByUserId { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public int? ClosedByUserId { get; private set; }

    private FiscalYear() { } // EF Core

    /// <summary>
    /// Creates a new fiscal year. Automatically computes StartDate (Jan 1) and EndDate (Dec 31).
    /// </summary>
    public static FiscalYear Create(int year, int openedByUserId = 0)
    {
        if (year < 2000 || year > DateTime.UtcNow.Year + 10)
            throw new DomainException(
                $"السنة المالية غير صالحة — يجب أن تكون بين 2000 و{DateTime.UtcNow.Year + 10}");

        return new FiscalYear
        {
            Year = year,
            StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            IsOpen = true,
            OpenedAt = DateTime.UtcNow,
            OpenedByUserId = openedByUserId > 0 ? openedByUserId : null,
            IsActive = true
        };
    }

    /// <summary>
    /// Closes this fiscal year. Guards against already-closed years.
    /// </summary>
    public void Close(int closedByUserId)
    {
        if (closedByUserId <= 0)
            throw new DomainException("مستخدم الإغلاق المالي مطلوب");

        if (!IsOpen)
            throw new DomainException($"السنة المالية {Year} مغلقة بالفعل");

        IsOpen = false;
        ClosedAt = DateTime.UtcNow;
        ClosedByUserId = closedByUserId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Reopens a closed fiscal year. Guards against already-open years.
    /// </summary>
    public void Reopen(int reopenedByUserId)
    {
        if (reopenedByUserId <= 0)
            throw new DomainException("مستخدم إعادة الفتح المالي مطلوب");

        if (IsOpen)
            throw new DomainException($"السنة المالية {Year} مفتوحة بالفعل");

        IsOpen = true;
        ClosedAt = null;
        ClosedByUserId = null;
        OpenedAt = DateTime.UtcNow;
        OpenedByUserId = reopenedByUserId;
        UpdateTimestamp();
    }
}
