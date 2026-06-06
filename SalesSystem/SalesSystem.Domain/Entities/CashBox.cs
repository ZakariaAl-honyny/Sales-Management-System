using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a cash box for tracking cash flow.
/// Can be assigned to a specific user or shared.
/// </summary>
public class CashBox : BaseEntity
{
    public string BoxName { get; private set; } = string.Empty;
    public decimal OpeningBalance { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public int? BranchId { get; private set; }
    /// <summary>
    /// DEPRECATED: Legacy CurrencyCode field — kept for backwards compatibility.
    /// Use <see cref="CurrencyId"/> and <see cref="Currency"/> navigation instead.
    /// </summary>
    public string CurrencyCode => Currency?.Code ?? string.Empty;
    public int? CurrencyId { get; private set; }
    public Currency? Currency { get; private set; }
    public int? AssignedUserId { get; private set; } // NULL = shared box
    public string? Notes { get; private set; }

    // Navigation
    private readonly List<CashTransaction> _transactions = new();
    public IReadOnlyCollection<CashTransaction> Transactions => _transactions.AsReadOnly();

    private CashBox() { } // EF Core

    /// <summary>
    /// Creates a new cash box.
    /// </summary>
    public static CashBox Create(
        string boxName,
        int? branchId = null,
        int? assignedUserId = null,
        int? currencyId = null,
        decimal initialBalance = 0)
    {
        if (string.IsNullOrWhiteSpace(boxName))
            throw new DomainException("اسم الصندوق مطلوب");

        return new CashBox
        {
            BoxName = boxName.Trim(),
            BranchId = branchId,
            AssignedUserId = assignedUserId,
            CurrencyId = currencyId,
            CurrentBalance = initialBalance,
            IsActive = true
        };
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Deposits cash INTO the box. Returns the transaction for audit.
    /// </summary>
    public CashTransaction Deposit(
        decimal amount,
        CashTransactionType type,
        string? referenceType = null,
        int? referenceId = null,
        int createdBy = 0,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainException("مبلغ الإيداع يجب أن يكون أكبر من صفر");

        var balanceBefore = CurrentBalance;
        CurrentBalance += amount;

        var transaction = CashTransaction.Create(
            Id, type, amount, balanceBefore, CurrentBalance,
            referenceType, referenceId, createdBy, notes, CurrencyId);

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Withdraws cash FROM the box. Returns the transaction for audit.
    /// Throws if insufficient balance.
    /// </summary>
    public CashTransaction Withdraw(
        decimal amount,
        CashTransactionType type,
        string? referenceType = null,
        int? referenceId = null,
        int createdBy = 0,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainException("مبلغ السحب يجب أن يكون أكبر من صفر");

        if (CurrentBalance < amount)
            throw new DomainException(
                $"رصيد الصندوق غير كافٍ. الرصيد الحالي: {CurrentBalance:N2}، " +
                $"المبلغ المطلوب: {amount:N2}");

        var balanceBefore = CurrentBalance;
        CurrentBalance -= amount;

        var transaction = CashTransaction.Create(
            Id, type, -amount, balanceBefore, CurrentBalance,
            referenceType, referenceId, createdBy, notes);

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Validates that a user can access this box.
    /// Shared boxes (AssignedUserId = null) can be accessed by anyone.
    /// </summary>
    public void ValidateUserAccess(int userId)
    {
        if (AssignedUserId.HasValue && AssignedUserId.Value != userId)
            throw new DomainException(
                $"ليس لديك صلاحية الوصول إلى الصندوق '{BoxName}'. " +
                $"تواصل مع المدير لتغيير الصلاحيات.");
    }

    /// <summary>
    /// Updates the box name.
    /// </summary>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("اسم الصندوق مطلوب");

        BoxName = newName.Trim();
    }
}