using System.Collections.ObjectModel;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.ViewModels.Purchases;

/// <summary>
/// ViewModel for a single line item in a Purchase Order.
/// Handles product selection, quantity, unit cost, and computed line total.
/// </summary>
public class PurchaseOrderItemLineViewModel : ViewModelBase
{
    private int _productId;
    private ProductDto? _selectedProduct;
    private int _productUnitId;
    private string _productUnitName = string.Empty;
    private decimal _quantity = 1;
    private decimal _unitCost;
    private decimal _lineTotal;
    private string? _notes;

    public ObservableCollection<ProductDto> AvailableProducts { get; }

    public PurchaseOrderItemLineViewModel(ObservableCollection<ProductDto> products)
    {
        AvailableProducts = products;
    }

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value) && value != null)
            {
                ProductId = value.Id;
                ClearErrors(nameof(ProductName));
                if (UnitCost == 0)
                {
                    UnitCost = value.Cost;
                }
                OnPropertyChanged(nameof(ProductName));
                RecalculateLineTotal();
            }
        }
    }

    public string ProductName => SelectedProduct?.Name ?? string.Empty;

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    public string ProductUnitName
    {
        get => _productUnitName;
        set => SetProperty(ref _productUnitName, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                ValidateQuantity();
                RecalculateLineTotal();
            }
        }
    }

    public decimal UnitCost
    {
        get => _unitCost;
        set
        {
            if (SetProperty(ref _unitCost, value))
            {
                ValidateUnitCost();
                RecalculateLineTotal();
            }
        }
    }

    public decimal LineTotal
    {
        get => _lineTotal;
        private set => SetProperty(ref _lineTotal, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public void RecalculateLineTotal()
    {
        LineTotal = Quantity * UnitCost;
    }

    private void ValidateQuantity()
    {
        ClearErrors(nameof(Quantity));
        if (Quantity <= 0)
            AddError(nameof(Quantity), "الكمية يجب أن تكون أكبر من صفر");
        else if (Quantity > 999999)
            AddError(nameof(Quantity), "الكمية كبيرة جداً");
    }

    private void ValidateUnitCost()
    {
        ClearErrors(nameof(UnitCost));
        if (UnitCost < 0)
            AddError(nameof(UnitCost), "التكلفة لا يمكن أن تكون سالبة");
    }
}
