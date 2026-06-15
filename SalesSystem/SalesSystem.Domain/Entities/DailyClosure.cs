using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a daily cash box closure record.
/// Computes: ClosingBalance = OpeningBalance + TotalIncome - TotalExpense
/// Difference = ActualCashCount - ClosingBalance
/// </summary>
public class DailyClosure : AuditableEntity
{
    public int CashBoxId { get; private set; }
    public CashBox? CashBox { get; private set; }
    public DateTime ClosureDate { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal TotalIncome { get; private set; }
    public decimal TotalExpense { get; private set; }

    /// <summary>
    /// Computed: OpeningBalance + TotalIncome - TotalExpense
    /// </summary>
    public decimal ClosingBalance { get; private set; }

    /// <summary>
    /// Physical cash counted at the end of the day.
    /// </summary>
    public decimal ActualCashCount { get; private set; }

    /// <summary>
    /// Computed: ActualCashCount - ClosingBalance
    /// </summary>
    public decimal Difference { get; private set; }

    /// <summary>
    /// True if the difference has been reviewed and approved.
    /// Auto-set to true if Difference == 0 on creation.
    /// </summary>
    public bool IsReconciled { get; private set; }

    public string? Notes { get; private set; }

    // ─── EF Core Constructor ────────────────────────────────

    private DailyClosure() { }

    // ─── Factory ────────────────────────────────────────────

    /// <summary>
    /// Creates a new daily closure record with computed values.
    /// </summary>
    /// <param name="cashBoxId">FK to the cash box being closed.</param>
    /// <param name="closureDate">The date of closure (date only).</param>
    /// <param name="openingBalance">Opening balance at the start of the day.</param>
    /// <param name="totalIncome">Total receipts/income during the day.</param>
    /// <param name="totalExpense">Total payments/expenses during the day.</param>
    /// <param name="actualCashCount">Physical cash counted at closure.</param>
    /// <param name="notes">Optional notes about the closure.</param>
    /// <param name="createdByUserId">User who performed the closure.</param>
    /// <returns>The newly created DailyClosure entity.</returns>
    public static DailyClosure Create(
        int cashBoxId,
        DateTime closureDate,
        decimal openingBalance,
        decimal totalIncome,
        decimal totalExpense,
        decimal actualCashCount,
        string? notes,
        int createdByUserId = 0)
    {
        if (cashBoxId <= 0)
            throw new DomainException("الصندوق مطلوب");
        if (closureDate == default)
            throw new DomainException("تاريخ الإغلاق مطلوب");
        if (openingBalance < 0)
            throw new DomainException("الرصيد الافتتاحي لا يمكن أن يكون سالباً");
        if (totalIncome < 0)
            throw new DomainException("إجمالي الإيرادات لا يمكن أن يكون سالباً");
        if (totalExpense < 0)
            throw new DomainException("إجمالي المصروفات لا يمكن أن يكون سالباً");
        if (actualCashCount < 0)
            throw new DomainException("العد الفعلي لا يمكن أن يكون سالباً");

        var closingBalance = openingBalance + totalIncome - totalExpense;
        var difference = actualCashCount - closingBalance;

        var closure = new DailyClosure
        {
            CashBoxId = cashBoxId,
            ClosureDate = closureDate.Date,
            OpeningBalance = openingBalance,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            ClosingBalance = closingBalance,
            ActualCashCount = actualCashCount,
            Difference = difference,
            IsReconciled = difference == 0m,
            Notes = notes?.Trim()
        };
        closure.SetCreatedBy(createdByUserId > 0 ? createdByUserId : null);
        return closure;
    }

    // ─── Domain Methods ────────────────────────────────────

    /// <summary>
    /// Marks this closure as reconciled after reviewing the difference.
    /// </summary>
    /// <param name="notes">Optional reconciliation notes.</param>
    public void Reconcile(string? notes = null)
    {
        IsReconciled = true;
        if (notes != null)
            Notes = notes.Trim();
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the actual cash count and recomputes difference.
    /// Resets IsReconciled to false so a reviewer must re-reconcile.
    /// </summary>
    /// <param name="actualCashCount">Corrected physical cash count.</param>
    /// <param name="notes">Optional notes about the correction.</param>
    public void UpdateActualCashCount(decimal actualCashCount, string? notes = null)
    {
        if (actualCashCount < 0)
            throw new DomainException("العد الفعلي لا يمكن أن يكون سالباً");

        ActualCashCount = actualCashCount;
        Difference = actualCashCount - ClosingBalance;
        IsReconciled = false;

        if (notes != null)
            Notes = notes.Trim();

        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the income/expense totals and recomputes derived values.
    /// Resets IsReconciled to false.
    /// </summary>
    public void UpdateTotals(decimal totalIncome, decimal totalExpense)
    {
        if (totalIncome < 0)
            throw new DomainException("إجمالي الإيرادات لا يمكن أن يكون سالباً");
        if (totalExpense < 0)
            throw new DomainException("إجمالي المصروفات لا يمكن أن يكون سالباً");

        TotalIncome = totalIncome;
        TotalExpense = totalExpense;
        ClosingBalance = OpeningBalance + totalIncome - totalExpense;
        Difference = ActualCashCount - ClosingBalance;
        IsReconciled = false;

        UpdateTimestamp();
    }
}
