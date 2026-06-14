using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductUnitRowViewModel : ViewModelBase
{
    private int _id;
    private string _unitName = string.Empty;
    private string _placeholder_UnitName = "مثال: حبة، قطعة";
    private decimal _factor = 1;
    private bool _isBaseUnit;
    [Obsolete("Prices moved to ProductPrices table")]
    private decimal _salesPrice;
    [Obsolete("Cost tracked via InventoryBatches")]
    private decimal _purchaseCost;
    [Obsolete("SupplierPrice replaced by ProductPrices table")]
    private decimal _supplierPrice;
    private int _sortOrder;
    private bool _isActive = true;
    private bool _isDefaultBarcode;
    private string _barcodeValue = string.Empty;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string UnitName
    {
        get => _unitName;
        set => SetProperty(ref _unitName, value);
    }

    public string Placeholder_UnitName
    {
        get => _placeholder_UnitName;
        set => SetProperty(ref _placeholder_UnitName, value);
    }

    public decimal Factor
    {
        get => _factor;
        set
        {
            if (SetProperty(ref _factor, value))
                OnPropertyChanged(nameof(IsFactorValid));
        }
    }

    public bool IsBaseUnit
    {
        get => _isBaseUnit;
        set
        {
            if (SetProperty(ref _isBaseUnit, value))
            {
                if (value) Factor = 1;
                OnPropertyChanged(nameof(IsFactorValid));
            }
        }
    }

    [Obsolete("Use ProductPrices instead — pricing moved to ProductPrices table.")]
    public decimal SalesPrice
    {
        get => _salesPrice;
        set => SetProperty(ref _salesPrice, value);
    }

#pragma warning disable CS0618 // Obsolete — kept for backward compatibility during Phase 25 transition
    public decimal PurchaseCost
    {
        get => _purchaseCost;
        set => SetProperty(ref _purchaseCost, value);
    }

    public decimal SupplierPrice
    {
        get => _supplierPrice;
        set => SetProperty(ref _supplierPrice, value);
    }
#pragma warning restore CS0618

    public int SortOrder
    {
        get => _sortOrder;
        set => SetProperty(ref _sortOrder, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool IsDefaultBarcode
    {
        get => _isDefaultBarcode;
        set => SetProperty(ref _isDefaultBarcode, value);
    }

    public string BarcodeValue
    {
        get => _barcodeValue;
        set => SetProperty(ref _barcodeValue, value);
    }

    public bool IsFactorValid => IsBaseUnit || Factor > 1;

    public string ConversionHint => IsBaseUnit
        ? "الوحدة الصغرى = 1"
        : $"كم {_placeholder_UnitName} بداخلها؟";
}