using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a barcode linked to a specific ProductUnit.
/// Multiple barcodes can exist per product unit (e.g., different supplier codes).
/// </summary>
public class UnitBarcode : BaseEntity
{
    public int ProductUnitId { get; private set; }
    public string BarcodeValue { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public string? SupplierCode { get; private set; }

    // Navigation
    public ProductUnit ProductUnit { get; private set; } = null!;

    private UnitBarcode() { } // EF Core

    /// <summary>
    /// Creates a new barcode for a product unit.
    /// Barcode value is normalized to uppercase.
    /// </summary>
    public static UnitBarcode Create(
        int productUnitId,
        string barcodeValue,
        bool isDefault = false,
        string? supplierCode = null)
    {
        if (string.IsNullOrWhiteSpace(barcodeValue))
            throw new DomainException("قيمة الباركود لا يمكن أن تكون فارغة");

        return new UnitBarcode
        {
            ProductUnitId = productUnitId,
            BarcodeValue = barcodeValue.Trim().ToUpperInvariant(),
            IsDefault = isDefault,
            SupplierCode = supplierCode?.Trim().ToUpperInvariant()
        };
    }

    /// <summary>
    /// Unmarks this barcode as the default.
    /// </summary>
    public void UnmarkDefault() => IsDefault = false;
}