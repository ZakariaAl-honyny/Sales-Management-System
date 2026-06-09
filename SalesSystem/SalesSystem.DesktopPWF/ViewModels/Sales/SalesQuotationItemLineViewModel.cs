using SalesSystem.Contracts.DTOs;
using System.Collections.ObjectModel;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for a single line item in a Sales Quotation.
/// Handles product selection, quantity, unit price, discount, and computed line total.
/// </summary>
public class SalesQuotationItemLineViewModel : ViewModelBase
{
    private int _productId;
    private ProductDto? _selectedProduct;
    private decimal _quantity = 1;
    private decimal _unitPrice;
    private decimal _discountAmount;
    private decimal _lineTotal;
    private string? _notes;

    public ObservableCollection<ProductDto> AvailableProducts { get; }

    public SalesQuotationItemLineViewModel(ObservableCollection<ProductDto> products)
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
                if (_unitPrice == 0)
                {
                    UnitPrice = value.SalePrice;
                }
                OnPropertyChanged(nameof(ProductName));
                RecalculateLineTotal();
            }
        }
    }

    public string ProductName => SelectedProduct?.Name ?? string.Empty;

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

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value))
            {
                ValidateUnitPrice();
                RecalculateLineTotal();
            }
        }
    }

    public decimal DiscountAmount
    {
        get => _discountAmount;
        set
        {
            if (SetProperty(ref _discountAmount, value))
            {
                ValidateDiscount();
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
        LineTotal = (Quantity * UnitPrice) - DiscountAmount;
        if (LineTotal < 0) LineTotal = 0;
    }

    private void ValidateQuantity()
    {
        ClearErrors(nameof(Quantity));
        if (Quantity <= 0)
            AddError(nameof(Quantity), "الكمية يجب أن تكون أكبر من صفر");
        else if (Quantity > 999999)
            AddError(nameof(Quantity), "الكمية كبيرة جداً");
    }

    private void ValidateUnitPrice()
    {
        ClearErrors(nameof(UnitPrice));
        if (UnitPrice < 0)
            AddError(nameof(UnitPrice), "السعر لا يمكن أن يكون سالباً");
    }

    private void ValidateDiscount()
    {
        ClearErrors(nameof(DiscountAmount));
        if (DiscountAmount < 0)
            AddError(nameof(DiscountAmount), "الخصم لا يمكن أن يكون سالباً");
        else if (DiscountAmount > (Quantity * UnitPrice))
            AddError(nameof(DiscountAmount), "قيمة الخصم أكبر من إجمالي البند");
    }
}
