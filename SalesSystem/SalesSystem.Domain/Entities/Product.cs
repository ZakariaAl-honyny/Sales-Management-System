using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class Product : BaseEntity
{
    public string? Code { get; private set; }
    public string? Barcode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int? CategoryId { get; private set; }
    public int? UnitId { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public decimal SalePrice { get; private set; }
    public decimal MinStock { get; private set; }
    public string? Description { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    // Navigation properties
    public virtual Category? Category { get; private set; }
    public virtual Unit? Unit { get; private set; }
    public virtual ICollection<WarehouseStock> WarehouseStocks { get; private set; } = new List<WarehouseStock>();

    private Product() { }

    public static Product Create(
        string name,
        decimal purchasePrice,
        decimal salePrice,
        decimal minStock = 0,
        string? code = null,
        string? barcode = null,
        int? categoryId = null,
        int? unitId = null,
        string? description = null,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (purchasePrice < 0)
            throw new ArgumentException("PurchasePrice cannot be negative.", nameof(purchasePrice));
        if (salePrice < 0)
            throw new ArgumentException("SalePrice cannot be negative.", nameof(salePrice));
        if (minStock < 0)
            throw new ArgumentException("MinStock cannot be negative.", nameof(minStock));

        return new Product
        {
            Name = name,
            PurchasePrice = purchasePrice,
            SalePrice = salePrice,
            MinStock = minStock,
            Code = code,
            Barcode = barcode,
            CategoryId = categoryId,
            UnitId = unitId,
            Description = description,
            CreatedBy = createdBy
        };
    }

    public void Update(
        string name,
        decimal purchasePrice,
        decimal salePrice,
        decimal minStock,
        string? code,
        string? barcode,
        int? categoryId,
        int? unitId,
        string? description,
        string? updatedBy)
    {
        Name = name;
        PurchasePrice = purchasePrice;
        SalePrice = salePrice;
        MinStock = minStock;
        Code = code;
        Barcode = barcode;
        CategoryId = categoryId;
        UnitId = unitId;
        Description = description;
        UpdatedBy = updatedBy;
        UpdateTimestamp();
    }
}