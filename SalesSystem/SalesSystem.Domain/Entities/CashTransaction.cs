using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a single cash transaction for audit trail.
/// Balance snapshots (Before/After) are captured at transaction time.
/// </summary>
public class CashTransaction : BaseEntity
{
    public int CashBoxId { get; private set; }
    public CashTransactionType TransactionType { get; private set; }

    /// <summary>
    /// Amount (positive for in, negative for out).
    /// </summary>
    public decimal Amount { get; private set; }

    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string? ReferenceType { get; private set; } // e.g., "SalesInvoice", "PurchaseInvoice"
    public int? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public CashBox CashBox { get; private set; } = null!;

    private CashTransaction() { } // EF Core

    /// <summary>
    /// Creates a new cash transaction. Called internally by CashBox.
    /// </summary>
    internal static CashTransaction Create(
        int cashBoxId,
        CashTransactionType type,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string? referenceType,
        int? referenceId,
        int createdBy,
        string? notes)
    {
        return new CashTransaction
        {
            CashBoxId = cashBoxId,
            TransactionType = type,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedBy = createdBy,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}