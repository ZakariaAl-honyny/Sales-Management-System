using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class DailyClosure : BaseEntity
{
    public int CashBoxId { get; private set; }
    public DateOnly ClosureDate { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal TotalIncome { get; private set; }
    public decimal TotalExpense { get; private set; }

    /// <summary>
    /// Computed: OpeningBalance + TotalIncome - TotalExpense.
    /// Represents the balance that should be in the cash box at closing.
    /// </summary>
    public decimal ExpectedClosingBalance { get; private set; }

    /// <summary>
    /// The actual physical cash count entered by the user during reconciliation.
    /// </summary>
    public decimal ActualCashCount { get; private set; }

    /// <summary>
    /// Computed: ActualCashCount - ExpectedClosingBalance.
    /// Zero when the actual count matches the expected balance.
    /// </summary>
    public decimal Difference { get; private set; }

    /// <summary>
    /// Whether the closure has been reconciled by comparing actual count with expected balance.
    /// </summary>
    public bool IsReconciled { get; private set; }

    /// <summary>
    /// The ID of the user who performed the closure.
    /// </summary>
    public int ClosedByUserId { get; private set; }

    public string? Notes { get; private set; }

    public CashBox CashBox { get; private set; } = null!;

    private DailyClosure() { }

    public static DailyClosure Create(
        int cashBoxId,
        DateOnly closureDate,
        decimal openingBalance,
        decimal totalIncome,
        decimal totalExpense,
        int closedByUserId,
        int? createdByUserId = null)
    {
        if (cashBoxId <= 0)
            throw new DomainException("معرف الصندوق غير صالح");

        if (totalIncome < 0)
            throw new DomainException("إجمالي الإيرادات لا يمكن أن يكون سالباً");

        if (totalExpense < 0)
            throw new DomainException("إجمالي المصروفات لا يمكن أن يكون سالباً");

        if (closedByUserId <= 0)
            throw new DomainException("معرف المستخدم الذي أغلق الصندوق غير صالح");

        var expectedClosingBalance = openingBalance + totalIncome - totalExpense;

        var entity = new DailyClosure
        {
            CashBoxId = cashBoxId,
            ClosureDate = closureDate,
            OpeningBalance = openingBalance,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            ExpectedClosingBalance = expectedClosingBalance,
            ActualCashCount = 0,
            Difference = 0,
            IsReconciled = false,
            ClosedByUserId = closedByUserId,
            Notes = null,
            IsActive = true
        };
        entity.SetCreatedBy(createdByUserId ?? closedByUserId);
        return entity;
    }

    /// <summary>
    /// Reconciles the closure by recording the actual cash count.
    /// Sets IsReconciled = true and computes the Difference.
    /// </summary>
    public void Reconcile(decimal actualCashCount, string? notes = null)
    {
        if (actualCashCount < 0)
            throw new DomainException("العدد الفعلي للنقدية لا يمكن أن يكون سالباً");

        if (IsReconciled)
            throw new DomainException("تمت تسوية هذه الجردية بالفعل");

        ActualCashCount = actualCashCount;
        Difference = actualCashCount - ExpectedClosingBalance;
        IsReconciled = true;

        if (notes != null)
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        UpdateTimestamp();
    }
}
