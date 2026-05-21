using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductUnitRowViewModel : ViewModelBase
{
    private int _id;
    private string _unitName = string.Empty;
    private string _placeholder_UnitName = "مثال: حبة، قطعة";
    private decimal _baseConversionFactor = 1;
    private bool _isBaseUnit;
    private decimal _salesPrice;
    private decimal _purchaseCost;
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

    public decimal BaseConversionFactor
    {
        get => _baseConversionFactor;
        set
        {
            if (SetProperty(ref _baseConversionFactor, value))
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
                if (value) BaseConversionFactor = 1;
                OnPropertyChanged(nameof(IsFactorValid));
            }
        }
    }

    public decimal SalesPrice
    {
        get => _salesPrice;
        set => SetProperty(ref _salesPrice, value);
    }

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

    public bool IsFactorValid => IsBaseUnit || BaseConversionFactor > 1;

    public string ConversionHint => IsBaseUnit
        ? "الوحدة الصغرى = 1"
        : $"كم {_placeholder_UnitName} بداخلها؟";
}