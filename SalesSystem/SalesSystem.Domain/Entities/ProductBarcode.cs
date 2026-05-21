using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class ProductBarcode : BaseEntity
{
    public int ProductId { get; private set; }
    public string BarcodeValue { get; private set; } = string.Empty;
    public UnitType UnitType { get; private set; } = UnitType.Retail;
    public bool IsDefault { get; private set; }

    // Navigation property
    public virtual Product? Product { get; private set; }

    private ProductBarcode() { }

    public static ProductBarcode Create(
        int productId,
        string barcodeValue,
        UnitType unitType = UnitType.Retail,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(barcodeValue))
            throw new DomainException("قيمة الباركود مطلوبة.");

        return new ProductBarcode
        {
            ProductId = productId,
            BarcodeValue = barcodeValue.Trim(),
            UnitType = unitType,
            IsDefault = isDefault
        };
    }
}