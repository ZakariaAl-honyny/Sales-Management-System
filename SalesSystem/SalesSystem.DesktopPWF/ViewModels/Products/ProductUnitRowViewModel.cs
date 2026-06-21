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
    private int _sortOrder;
    private bool _isActive = true;

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

    public bool IsFactorValid => IsBaseUnit || Factor > 1;

    public string ConversionHint => IsBaseUnit
        ? "الوحدة الصغرى = 1"
        : $"كم {_placeholder_UnitName} بداخلها؟";
}