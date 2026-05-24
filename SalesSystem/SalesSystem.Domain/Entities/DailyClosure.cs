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
    public decimal ClosingBalance { get; private set; }
    public int ClosedByUserId { get; private set; }

    public CashBox CashBox { get; private set; } = null!;

    private DailyClosure() { }

    public static DailyClosure Create(
        int cashBoxId,
        DateOnly closureDate,
        decimal openingBalance,
        decimal totalIncome,
        decimal totalExpense,
        decimal closingBalance,
        int closedByUserId)
    {
        if (cashBoxId <= 0)
            throw new DomainException("معرف الصندوق غير صالح");

        if (totalIncome < 0)
            throw new DomainException("إجمالي الإيرادات لا يمكن أن يكون سالباً");

        if (totalExpense < 0)
            throw new DomainException("إجمالي المصروفات لا يمكن أن يكون سالباً");

        if (closedByUserId <= 0)
            throw new DomainException("معرف المستخدم الذي أغلق الصندوق غير صالح");

        var computedClosing = openingBalance + totalIncome - totalExpense;
        if (computedClosing != closingBalance)
            throw new DomainException(
                "المبالغ غير متطابقة: الرصيد الختامي لا يساوي الرصيد الافتتاحي + الإيرادات - المصروفات");

        var entity = new DailyClosure
        {
            CashBoxId = cashBoxId,
            ClosureDate = closureDate,
            OpeningBalance = openingBalance,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            ClosingBalance = closingBalance,
            ClosedByUserId = closedByUserId,
            IsActive = true
        };
        entity.SetCreatedBy(closedByUserId);
        return entity;
    }
}
