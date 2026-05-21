using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

/// <summary>
/// ViewModel for Product Editor Dialog
/// </summary>
public class ProductEditorViewModel : ViewModelBase
{
    private readonly IProductApiService _productService;
    private readonly ICategoryApiService _categoryService;
    private readonly IUnitApiService _unitService;
    private readonly IEventBus _eventBus;

    private int _productId;
    private string _code = string.Empty;
    private string _barcode = string.Empty;
    private string _name = string.Empty;
    private int? _categoryId;
    private int? _unitId;
    private int? _wholesaleUnitId;
    private int? _retailUnitId;
    private decimal _conversionFactor = 1;
    private decimal _purchasePrice;
    private decimal _salePrice;
    private decimal _wholesalePrice;
    private decimal _retailPrice;
    private decimal _minStock;
    private string _description = string.Empty;
    private bool _isActive = true;
    private bool _isLoading;
    private bool _isEditMode;
    private string? _errorMessage;

    private CategoryDto? _selectedCategory;
    private UnitDto? _selectedUnit;
    private UnitDto? _selectedWholesaleUnit;
    private UnitDto? _selectedRetailUnit;


    public ProductEditorViewModel()
    {
        _productService = App.GetService<IProductApiService>();
        _categoryService = App.GetService<ICategoryApiService>();
        _unitService = App.GetService<IUnitApiService>();
        _eventBus = App.GetService<IEventBus>();

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
        CancelCommand = new RelayCommand(Cancel);
        LoadLookupDataCommand = new AsyncRelayCommand(LoadLookupDataAsync);

        _ = LoadLookupDataAsync();
    }

    public ProductEditorViewModel(
        IProductApiService productService,
        ICategoryApiService categoryService,
        IUnitApiService unitService,
        IEventBus eventBus)
    {
        _productService = productService;
        _categoryService = categoryService;
        _unitService = unitService;
        _eventBus = eventBus;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
        CancelCommand = new RelayCommand(Cancel);
        LoadLookupDataCommand = new AsyncRelayCommand(LoadLookupDataAsync);

        _ = LoadLookupDataAsync();
    }

    public ProductEditorViewModel(ProductDto product)
        : this(
            App.GetService<IProductApiService>(),
            App.GetService<ICategoryApiService>(),
            App.GetService<IUnitApiService>(),
            App.GetService<IEventBus>())
    {
        _productId = product.Id;
        _code = product.Code ?? string.Empty;
        _barcode = product.Barcode ?? string.Empty;
        _name = product.Name;
        _categoryId = product.CategoryId;
        _unitId = product.UnitId;
        _wholesaleUnitId = product.WholesaleUnitId;
        _retailUnitId = product.RetailUnitId;
        _conversionFactor = product.ConversionFactor;
        _purchasePrice = product.PurchasePrice;
        _salePrice = product.SalePrice;
        _wholesalePrice = product.WholesalePrice;
        _retailPrice = product.RetailPrice;
        _minStock = product.MinStock;
        _description = product.Description ?? string.Empty;
        _isActive = product.IsActive;
        _isEditMode = true;
    }

    public ProductEditorViewModel(
        ProductDto product,
        IProductApiService productService,
        ICategoryApiService categoryService,
        IUnitApiService unitService,
        IEventBus eventBus)
        : this(productService, categoryService, unitService, eventBus)
    {
        _productId = product.Id;
        _code = product.Code ?? string.Empty;
        _barcode = product.Barcode ?? string.Empty;
        _name = product.Name;
        _categoryId = product.CategoryId;
        _unitId = product.UnitId;
        _wholesaleUnitId = product.WholesaleUnitId;
        _retailUnitId = product.RetailUnitId;
        _conversionFactor = product.ConversionFactor;
        _purchasePrice = product.PurchasePrice;
        _salePrice = product.SalePrice;
        _wholesalePrice = product.WholesalePrice;
        _retailPrice = product.RetailPrice;
        _minStock = product.MinStock;
        _description = product.Description ?? string.Empty;
        _isActive = product.IsActive;
        _isEditMode = true;
    }

    #region Properties
    public string Title => IsEditMode ? "تعديل منتج" : "إضافة منتج جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string Barcode
    {
        get => _barcode;
        set => SetProperty(ref _barcode, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(HasErrors));
                (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public int? CategoryId
    {
        get => _categoryId;
        set => SetProperty(ref _categoryId, value);
    }

    public int? UnitId
    {
        get => _unitId;
        set => SetProperty(ref _unitId, value);
    }

    public int? WholesaleUnitId
    {
        get => _wholesaleUnitId;
        set => SetProperty(ref _wholesaleUnitId, value);
    }

    public int? RetailUnitId
    {
        get => _retailUnitId;
        set => SetProperty(ref _retailUnitId, value);
    }

    public decimal ConversionFactor
    {
        get => _conversionFactor;
        set => SetProperty(ref _conversionFactor, value);
    }

    public decimal PurchasePrice
    {
        get => _purchasePrice;
        set => SetProperty(ref _purchasePrice, value);
    }

    public decimal SalePrice
    {
        get => _salePrice;
        set => SetProperty(ref _salePrice, value);
    }

    public decimal WholesalePrice
    {
        get => _wholesalePrice;
        set => SetProperty(ref _wholesalePrice, value);
    }

    public decimal RetailPrice
    {
        get => _retailPrice;
        set => SetProperty(ref _retailPrice, value);
    }

    public decimal MinStock
    {
        get => _minStock;
        set => SetProperty(ref _minStock, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public ObservableCollection<UnitDto> Units { get; } = new();

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                CategoryId = value?.Id;
            }
        }
    }

    public UnitDto? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (SetProperty(ref _selectedUnit, value))
            {
                UnitId = value?.Id;
            }
        }
    }

    public UnitDto? SelectedWholesaleUnit
    {
        get => _selectedWholesaleUnit;
        set
        {
            if (SetProperty(ref _selectedWholesaleUnit, value))
            {
                WholesaleUnitId = value?.Id;
            }
        }
    }

    public UnitDto? SelectedRetailUnit
    {
        get => _selectedRetailUnit;
        set
        {
            if (SetProperty(ref _selectedRetailUnit, value))
            {
                RetailUnitId = value?.Id;
            }
        }
    }

    // Validation
    private bool _hasCodeError;
    public bool HasCodeError
    {
        get => _hasCodeError;
        set => SetProperty(ref _hasCodeError, value);
    }

    private bool _hasNameError;
    public bool HasNameError
    {
        get => _hasNameError;
        set
        {
            if (SetProperty(ref _hasNameError, value))
                OnPropertyChanged(nameof(NameError));
        }
    }

    private bool _hasRetailPriceError;
    public bool HasRetailPriceError
    {
        get => _hasRetailPriceError;
        set
        {
            if (SetProperty(ref _hasRetailPriceError, value))
                OnPropertyChanged(nameof(RetailPriceError));
        }
    }

    private bool _hasWholesalePriceError;
    public bool HasWholesalePriceError
    {
        get => _hasWholesalePriceError;
        set
        {
            if (SetProperty(ref _hasWholesalePriceError, value))
                OnPropertyChanged(nameof(WholesalePriceError));
        }
    }

    private bool _hasWholesaleUnitError;
    public bool HasWholesaleUnitError
    {
        get => _hasWholesaleUnitError;
        set
        {
            if (SetProperty(ref _hasWholesaleUnitError, value))
                OnPropertyChanged(nameof(WholesaleUnitError));
        }
    }

    private bool _hasConversionFactorError;
    public bool HasConversionFactorError
    {
        get => _hasConversionFactorError;
        set
        {
            if (SetProperty(ref _hasConversionFactorError, value))
                OnPropertyChanged(nameof(ConversionFactorError));
        }
    }

    private bool _hasCategoryError;
    public bool HasCategoryError
    {
        get => _hasCategoryError;
        set
        {
            if (SetProperty(ref _hasCategoryError, value))
                OnPropertyChanged(nameof(CategoryError));
        }
    }

    private bool _hasUnitError;
    public bool HasUnitError
    {
        get => _hasUnitError;
        set
        {
            if (SetProperty(ref _hasUnitError, value))
                OnPropertyChanged(nameof(UnitError));
        }
    }

    public string? CodeError => HasCodeError ? "الكود مطلوب" : null;
    public string? NameError => HasNameError ? "الاسم مطلوب" : null;
    public bool CanSave => !HasErrors && !string.IsNullOrWhiteSpace(Name);
    public string? RetailPriceError => HasRetailPriceError ? "سعر التجزئة مطلوب (أكبر من 0)" : null;
    public string? WholesalePriceError => HasWholesalePriceError ? "سعر الجملة مطلوب (أكبر من 0)" : null;
    public string? WholesaleUnitError => HasWholesaleUnitError ? "يجب اختيار وحدة الجملة" : null;
    public string? ConversionFactorError => HasConversionFactorError ? "معامل التحويل مطلوب (أكبر من 0)" : null;
    public string? CategoryError => HasCategoryError ? "يجب اختيار فئة" : null;
    public string? UnitError => HasUnitError ? "يجب اختيار وحدة" : null;
    #endregion

    #region Commands
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand LoadLookupDataCommand { get; }
    #endregion

    #region Methods
    private async Task LoadLookupDataAsync()
    {
        try
        {
            var categoriesResult = await _categoryService.GetAllAsync();
            if (categoriesResult.IsSuccess && categoriesResult.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var cat in categoriesResult.Value)
                    {
                        Categories.Add(cat);
                    }

                    if (CategoryId.HasValue)
                    {
                        SelectedCategory = Categories.FirstOrDefault(c => c.Id == CategoryId.Value);
                    }
                });
            }

            var unitsResult = await _unitService.GetAllAsync();
            if (unitsResult.IsSuccess && unitsResult.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Units.Clear();
                    foreach (var unit in unitsResult.Value)
                    {
                        Units.Add(unit);
                    }

                    if (UnitId.HasValue)
                    {
                        SelectedUnit = Units.FirstOrDefault(u => u.Id == UnitId.Value);
                    }

                    if (WholesaleUnitId.HasValue)
                    {
                        SelectedWholesaleUnit = Units.FirstOrDefault(u => u.Id == WholesaleUnitId.Value);
                    }

                    if (RetailUnitId.HasValue)
                    {
                        SelectedRetailUnit = Units.FirstOrDefault(u => u.Id == RetailUnitId.Value);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "ProductEditorViewModel.LoadLookupDataAsync", "[ProductEditorViewModel.LoadLookupDataAsync] Failed to load categories or units for lookup.");
        }
    }

    private bool Validate()
    {
        HasCodeError = false; // Code is auto-generated if empty
        HasNameError = string.IsNullOrWhiteSpace(Name);
        HasRetailPriceError = RetailPrice <= 0;
        HasWholesalePriceError = WholesalePrice <= 0;
        HasConversionFactorError = ConversionFactor <= 0;
        HasCategoryError = !CategoryId.HasValue || CategoryId.Value <= 0;
        HasUnitError = !RetailUnitId.HasValue || RetailUnitId.Value <= 0;
        HasWholesaleUnitError = !WholesaleUnitId.HasValue || WholesaleUnitId.Value <= 0;

        OnPropertyChanged(nameof(NameError));
        OnPropertyChanged(nameof(CategoryError));
        OnPropertyChanged(nameof(UnitError));
        OnPropertyChanged(nameof(WholesaleUnitError));
        OnPropertyChanged(nameof(RetailPriceError));
        OnPropertyChanged(nameof(WholesalePriceError));
        OnPropertyChanged(nameof(ConversionFactorError));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(CanSave));
        (SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();

        return !HasNameError && !HasRetailPriceError && !HasWholesalePriceError && !HasConversionFactorError && !HasCategoryError && !HasUnitError && !HasWholesaleUnitError;
    }

    private async Task SaveAsync()
    {
        if (!Validate())
        {
            var errors = new List<string>();
            if (HasNameError) errors.Add("• " + NameError);
            if (HasCategoryError) errors.Add("• " + CategoryError);
            if (HasUnitError) errors.Add("• " + UnitError);
            if (HasWholesaleUnitError) errors.Add("• " + WholesaleUnitError);
            if (HasConversionFactorError) errors.Add("• " + ConversionFactorError);
            if (HasRetailPriceError) errors.Add("• " + RetailPriceError);
            if (HasWholesalePriceError) errors.Add("• " + WholesalePriceError);
            
            string errorMsg = "يرجى إكمال البيانات الإلزامية التالية:\n\n" + string.Join("\n", errors);
            System.Windows.MessageBox.Show(errorMsg, "بيانات غير مكتملة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            Result<ProductDto> result;

            if (IsEditMode)
            {
                var updateRequest = new UpdateProductRequest(
                    Code, Barcode, Name,
                    CategoryId, UnitId,
                    RetailUnitId, WholesaleUnitId,
                    ConversionFactor,
                    PurchasePrice, SalePrice,
                    RetailPrice, WholesalePrice,
                    MinStock, string.IsNullOrWhiteSpace(Description) ? null : Description,
                    IsActive);

                result = await _productService.UpdateAsync(_productId, updateRequest);
            }
            else
            {
                var createRequest = new CreateProductRequest(
                    Code, Barcode, Name,
                    CategoryId, UnitId,
                    RetailUnitId, WholesaleUnitId,
                    ConversionFactor,
                    PurchasePrice, SalePrice,
                    RetailPrice, WholesalePrice,
                    MinStock, string.IsNullOrWhiteSpace(Description) ? null : Description);

                result = await _productService.CreateAsync(createRequest);
            }

            if (result.IsSuccess && result.Value != null)
            {
                // Publish event to notify other modules
                _eventBus.Publish(new ProductChangedMessage(result.Value.Id));

                System.Windows.MessageBox.Show(
                    IsEditMode ? "تم تحديث المنتج بنجاح" : "تم إضافة المنتج بنجاح",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المنتج", "ProductEditorViewModel.SaveAsync", "[ProductEditorViewModel.SaveAsync] Failed to save product data.");
                System.Windows.MessageBox.Show(ErrorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "ProductEditorViewModel.SaveAsync", "[ProductEditorViewModel.SaveAsync] Failed to save product data.");
            System.Windows.MessageBox.Show(ErrorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
