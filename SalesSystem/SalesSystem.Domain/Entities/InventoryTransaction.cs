using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents an inventory transaction (purchase, sale, transfer, adjustment, etc.).
/// Replaces old InventoryMovement and InventoryOperation entities.
/// Maps to "InventoryTransactions" table.
/// </summary>
public class InventoryTransaction : DocumentEntity
{
    /// <summary>
    /// Transaction status (Draft=1, Posted=2, Cancelled=3).
    /// </summary>
    public InvoiceStatus Status { get; private set; }

    /// <summary>
    /// Unique sequential transaction number (int).
    /// </summary>
    public int TransactionNo { get; private set; }

    /// <summary>
    /// The date this transaction was recorded.
    /// </summary>
    public DateTime TransactionDate { get; private set; }

    /// <summary>
    /// The type of inventory transaction.
    /// </summary>
    public InventoryTransactionType TransactionType { get; private set; }

    /// <summary>
    /// Optional reference document type (e.g., PurchaseInvoice, SalesInvoice).
    /// </summary>
    public InventoryReferenceType? ReferenceType { get; private set; }

    /// <summary>
    /// FK to the reference document (e.g., PurchaseInvoice.Id, SalesInvoice.Id).
    /// </summary>
    public int? ReferenceId { get; private set; }

    /// <summary>
    /// FK to the warehouse where the transaction occurred.
    /// </summary>
    public short WarehouseId { get; private set; }

    /// <summary>
    /// Optional notes for this transaction.
    /// </summary>
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual Warehouse? Warehouse { get; private set; }
    private readonly List<InventoryTransactionLine> _lines = new();
    public IReadOnlyCollection<InventoryTransactionLine> Lines => _lines.AsReadOnly();

    private InventoryTransaction() { } // EF Core

    /// <summary>
    /// Creates a new inventory transaction.
    /// </summary>
    public static InventoryTransaction Create(
        int transactionNo,
        InventoryTransactionType transactionType,
        short warehouseId,
        DateTime? transactionDate = null,
        InventoryReferenceType? referenceType = null,
        int? referenceId = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (transactionNo <= 0)
            throw new DomainException("رقم المعاملة مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");

        var tx = new InventoryTransaction
        {
            Status = InvoiceStatus.Draft,
            TransactionNo = transactionNo,
            TransactionType = transactionType,
            WarehouseId = warehouseId,
            TransactionDate = transactionDate ?? DateTime.UtcNow,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes
        };
        tx.SetCreatedBy(createdByUserId);
        return tx;
    }

    /// <summary>
    /// Adds a transaction line (product/quantity/cost).
    /// Only allowed in Draft status.
    /// </summary>
    public void AddLine(InventoryTransactionLine line)
    {
        if (line == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لمعاملة غير مسودة.");
        _lines.Add(line);
        UpdateTimestamp();
    }

    /// <summary>
    /// Posts the transaction — transitions from Draft to Posted.
    /// </summary>
    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط المعاملات المسودة يمكن ترحيلها.");
        if (!_lines.Any())
            throw new DomainException("لا يمكن ترحيل معاملة بدون أصناف.");
        Status = InvoiceStatus.Posted;
        PostedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the transaction — transitions to Cancelled.
    /// </summary>
    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("المعاملة ملغاة بالفعل.");
        Status = InvoiceStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
}
