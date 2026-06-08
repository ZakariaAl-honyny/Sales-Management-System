using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a single cash transaction for audit trail.
/// The balance of a cash box is now tracked on its linked Account (Chart of Accounts).
/// This record serves as an audit trail of movements.
/// </summary>
public class CashTransaction : BaseEntity
{
    public int CashBoxId { get; private set; }
    public CashTransactionType TransactionType { get; private set; }

    /// <summary>
    /// Amount (positive for in, negative for out).
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Running balance of the cash box computed at transaction time (cumulative sum of Amount).
    /// Captured for audit trail — may differ from Account balance if adjustments were made
    /// directly on the linked Account.
    /// </summary>
    public decimal RunningBalance { get; private set; }
    public string? ReferenceType { get; private set; } // e.g., "SalesInvoice", "PurchaseInvoice"
    public int? ReferenceId { get; private set; }
    public int? CurrencyId { get; private set; }
    public Currency? Currency { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public CashBox CashBox { get; private set; } = null!;

    private CashTransaction() { } // EF Core

    /// <summary>
    /// Creates a new cash transaction with a running balance snapshot.
    /// </summary>
    public static CashTransaction Create(
        int cashBoxId,
        CashTransactionType type,
        decimal amount,
        decimal runningBalance,
        string? referenceType,
        int? referenceId,
        int createdBy,
        string? notes,
        int? currencyId = null)
    {
        var tx = new CashTransaction
        {
            CashBoxId = cashBoxId,
            TransactionType = type,
            Amount = amount,
            RunningBalance = runningBalance,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CurrencyId = currencyId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
        tx.SetCreatedBy(createdBy);
        return tx;
    }
}