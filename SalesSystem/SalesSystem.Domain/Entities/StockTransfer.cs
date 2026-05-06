using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

public class StockTransferItem
{
    public int StockTransferItemId { get; private set; }
    public int StockTransferId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }

    public virtual StockTransfer? StockTransfer { get; private set; }
    public virtual Product? Product { get; private set; }

    private StockTransferItem() { }

    public static StockTransferItem Create(int productId, decimal quantity, string? notes = null)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        return new StockTransferItem
        {
            ProductId = productId,
            Quantity = quantity,
            Notes = notes
        };
    }
}

public class StockTransfer : BaseEntity
{
    public string TransferNo { get; private set; } = string.Empty;
    public int FromWarehouseId { get; private set; }
    public int ToWarehouseId { get; private set; }
    public DateTime TransferDate { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual Warehouse? FromWarehouse { get; private set; }
    public virtual Warehouse? ToWarehouse { get; private set; }
    public virtual List<StockTransferItem> Items { get; private set; } = new();

    private StockTransfer() { }

    public static StockTransfer Create(
        string transferNo,
        int fromWarehouseId,
        int toWarehouseId,
        string? notes = null,
        DateTime? transferDate = null)
    {
        if (string.IsNullOrWhiteSpace(transferNo))
            throw new ArgumentException("TransferNo is required.", nameof(transferNo));
        if (fromWarehouseId <= 0)
            throw new ArgumentException("FromWarehouseId is required.", nameof(fromWarehouseId));
        if (toWarehouseId <= 0)
            throw new ArgumentException("ToWarehouseId is required.", nameof(toWarehouseId));
        if (fromWarehouseId == toWarehouseId)
            throw new ArgumentException("Cannot transfer to the same warehouse.");

        return new StockTransfer
        {
            TransferNo = transferNo,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Notes = notes,
            TransferDate = transferDate ?? DateTime.UtcNow,
            Status = InvoiceStatus.Draft
        };
    }

    public void AddItem(StockTransferItem item)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot add items to a non-draft transfer.");
        Items.Add(item);
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft transfers can be posted.");
        if (!Items.Any())
            throw new InvalidOperationException("Cannot post a transfer with no items.");
        Status = InvoiceStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Already cancelled.");
        Status = InvoiceStatus.Cancelled;
    }
}