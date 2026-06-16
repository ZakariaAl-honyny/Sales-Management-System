using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents an inventory transaction (purchase, sale, transfer, adjustment, etc.).
/// Replaces old InventoryMovement entity.
/// Maps to "InventoryTransactions" table.
/// Schema: nvarchar(50) TransactionNo (unique), tinyint MovementType,
/// nvarchar(500) Notes, int? ReferenceId, nvarchar(50) ReferenceType,
/// smallint WarehouseId FK, int CreatedByUserId, datetime2 CreatedAt.
/// BaseEntity with CreatedAt only — written once, immutable, no Status lifecycle.
/// </summary>
public class InventoryTransaction : Entity
{
    /// <summary>
    /// Unique sequential transaction number (nvarchar(50)).
    /// </summary>
    public string TransactionNo { get; private set; } = string.Empty;

    /// <summary>
    /// The type of inventory movement.
    /// Maps to MovementType (tinyint):
    /// 1=Purchase, 2=PurchaseReturn, 3=Sale, 4=SaleReturn,
    /// 5=TransferOut, 6=TransferIn, 7=Count, 8=Adjustment,
    /// 9=Damage, 10=OpeningBalance, 11=InternalIssue, 12=InternalReceipt
    /// </summary>
    public InventoryTransactionType MovementType { get; private set; }

    /// <summary>
    /// FK to the warehouse where the transaction occurred.
    /// </summary>
    public short WarehouseId { get; private set; }

    /// <summary>
    /// Optional reference document type.
    /// 1=PurchaseInvoice, 2=SalesInvoice, 3=PurchaseReturn, 4=SalesReturn,
    /// 5=Transfer, 6=Count, 7=Adjustment
    /// </summary>
    public InventoryReferenceType? ReferenceType { get; private set; }

    /// <summary>
    /// FK to the reference document (e.g., PurchaseInvoice.Id, SalesInvoice.Id).
    /// </summary>
    public int? ReferenceId { get; private set; }

    /// <summary>
    /// Optional notes for this transaction.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// FK to the user who created this transaction.
    /// </summary>
    public int CreatedByUserId { get; private set; }

    // Navigation properties
    public virtual Warehouse? Warehouse { get; private set; }
    private readonly List<InventoryTransactionLine> _lines = new();
    public IReadOnlyCollection<InventoryTransactionLine> Lines => _lines.AsReadOnly();

    private InventoryTransaction() { } // EF Core

    /// <summary>
    /// Creates a new inventory transaction (immutable — written once, no lifecycle).
    /// </summary>
    public static InventoryTransaction Create(
        string transactionNo,
        InventoryTransactionType movementType,
        short warehouseId,
        InventoryReferenceType? referenceType = null,
        int? referenceId = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(transactionNo))
            throw new DomainException("رقم المعاملة مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");

        var tx = new InventoryTransaction
        {
            TransactionNo = transactionNo.Trim(),
            MovementType = movementType,
            WarehouseId = warehouseId,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId ?? 0
        };
        return tx;
    }

    /// <summary>
    /// Adds a transaction line (product/quantity/cost).
    /// </summary>
    public void AddLine(InventoryTransactionLine line)
    {
        if (line == null)
            throw new DomainException("الصنف مطلوب.");
        _lines.Add(line);
    }
}
