using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a fiscal year with open/close lifecycle.
/// Inherits <see cref="ActivatableEntity"/> for audit, soft-delete, and activation support.
/// PK is smallint (short) — a small lookup table.
/// Schema: §2.9 FiscalYears.
/// </summary>
public class FiscalYear : ActivatableEntity
{
    /// <summary>
    /// smallint PK — overrides base int Id for small lookup tables.
    /// </summary>
    public new short Id { get; private set; }

    /// <summary>
    /// The calendar year number (e.g. 2025).
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// User-facing year name (e.g. "2025", "2025-2026").
    /// </summary>
    public string YearName { get; private set; } = string.Empty;

    /// <summary>
    /// The start date of the fiscal year.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// The end date of the fiscal year.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Indicates whether the fiscal year is currently open.
    /// <c>true</c> means the year is open and active.
    /// <c>false</c> means the year has been closed.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Timestamp of when the year was opened.
    /// </summary>
    public DateTime? OpenedAt { get; private set; }

    /// <summary>
    /// ID of the user who opened the fiscal year.
    /// </summary>
    public int? OpenedByUserId { get; private set; }

    /// <summary>
    /// Timestamp of when the year was closed (null if still open).
    /// </summary>
    public DateTime? ClosedAt { get; private set; }

    /// <summary>
    /// ID of the user who closed the fiscal year.
    /// </summary>
    public int? ClosedByUserId { get; private set; }

    /// <summary>
    /// Private constructor required by EF Core.
    /// </summary>
    private FiscalYear() { }

    /// <summary>
    /// Creates a new fiscal year.
    /// Automatically computes StartDate (Jan 1), EndDate (Dec 31),
    /// and YearName from the provided year.
    /// The year is created in the open state.
    /// </summary>
    /// <param name="year">The calendar year (e.g. 2025).</param>
    /// <param name="openedByUserId">ID of the user opening the year.</param>
    /// <param name="yearName">
    /// Optional custom year name. If null, defaults to <paramref name="year"/>.ToString().
    /// </param>
    /// <returns>A new FiscalYear instance, already opened.</returns>
    public static FiscalYear Create(int year, int openedByUserId = 0, string? yearName = null)
    {
        if (year < 2000 || year > DateTime.UtcNow.Year + 10)
            throw new DomainException(
                $"السنة المالية غير صالحة — يجب أن تكون بين 2000 و{DateTime.UtcNow.Year + 10}");

        var fiscalYear = new FiscalYear
        {
            Year = year,
            YearName = (yearName ?? year.ToString()).Trim(),
            StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(year, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            IsOpen = true,
            OpenedAt = DateTime.UtcNow,
            OpenedByUserId = openedByUserId > 0 ? openedByUserId : null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        fiscalYear.SetCreatedBy(openedByUserId > 0 ? openedByUserId : null);
        return fiscalYear;
    }

    /// <summary>
    /// Opens this fiscal year. Guards against already-open years.
    /// </summary>
    /// <param name="openedByUserId">ID of the user performing the open.</param>
    public void Open(int openedByUserId)
    {
        if (openedByUserId <= 0)
            throw new DomainException("مستخدم فتح السنة المالية مطلوب");

        if (IsOpen)
            throw new DomainException($"السنة المالية {Year} مفتوحة بالفعل");

        IsOpen = true;
        ClosedAt = null;
        ClosedByUserId = null;
        OpenedAt = DateTime.UtcNow;
        OpenedByUserId = openedByUserId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Closes this fiscal year. Guards against already-closed years.
    /// </summary>
    /// <param name="closedByUserId">ID of the user performing the closure.</param>
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
    /// <param name="reopenedByUserId">ID of the user performing the reopen.</param>
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
