using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly IDialogService _dialogService;

    private int _productId;
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
        _dialogService = App.GetService<IDialogService>();
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المنتج...")));
        CancelCommand = new RelayCommand(Cancel);
        LoadLookupDataCommand = new AsyncRelayCommand(LoadLookupDataAsync);

        _ = LoadLookupDataAsync();
    }

    public ProductEditorViewModel(
        IProductApiService productService,
        ICategoryApiService categoryService,
        IUnitApiService unitService,
        IEventBus eventBus,
        IDialogService dialogService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _unitService = unitService;
        _eventBus = eventBus;
        _dialogService = dialogService;
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المنتج...")));
        CancelCommand = new RelayCommand(Cancel);
        LoadLookupDataCommand = new AsyncRelayCommand(LoadLookupDataAsync);

        _ = LoadLookupDataAsync();
    }

    public ProductEditorViewModel(ProductDto product)
        : this(
            App.GetService<IProductApiService>(),
            App.GetService<ICategoryApiService>(),
            App.GetService<IUnitApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>())
    {
        _productId = product.Id;
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
        IEventBus eventBus,
        IDialogService dialogService)
        : this(productService, categoryService, unitService, eventBus, dialogService)
    {
        _productId = product.Id;
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
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم المنتج مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public int? CategoryId
    {
        get => _categoryId;
        set
        {
            if (SetProperty(ref _categoryId, value))
            {
                if (!value.HasValue || value.Value <= 0)
                    AddError(nameof(CategoryId), "يجب اختيار فئة");
                else
                    ClearErrors(nameof(CategoryId));
            }
        }
    }

    public int? UnitId
    {
        get => _unitId;
        set => SetProperty(ref _unitId, value);
    }

    public int? WholesaleUnitId
    {
        get => _wholesaleUnitId;
        set
        {
            if (SetProperty(ref _wholesaleUnitId, value))
            {
                if (!value.HasValue || value.Value <= 0)
                    AddError(nameof(WholesaleUnitId), "يجب اختيار وحدة الجملة");
                else
                    ClearErrors(nameof(WholesaleUnitId));
            }
        }
    }

    public int? RetailUnitId
    {
        get => _retailUnitId;
        set
        {
            if (SetProperty(ref _retailUnitId, value))
            {
                if (!value.HasValue || value.Value <= 0)
                    AddError(nameof(RetailUnitId), "يجب اختيار وحدة التجزئة");
                else
                    ClearErrors(nameof(RetailUnitId));
            }
        }
    }

    public decimal ConversionFactor
    {
        get => _conversionFactor;
        set
        {
            if (SetProperty(ref _conversionFactor, value))
            {
                if (value <= 0)
                    AddError(nameof(ConversionFactor), "معامل التحويل يجب أن يكون أكبر من صفر");
                else
                    ClearErrors(nameof(ConversionFactor));
            }
        }
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
        set
        {
            if (SetProperty(ref _wholesalePrice, value))
            {
                if (value <= 0)
                    AddError(nameof(WholesalePrice), "سعر الجملة يجب أن يكون أكبر من صفر");
                else
                    ClearErrors(nameof(WholesalePrice));
            }
        }
    }

    public decimal RetailPrice
    {
        get => _retailPrice;
        set
        {
            if (SetProperty(ref _retailPrice, value))
            {
                if (value <= 0)
                    AddError(nameof(RetailPrice), "سعر التجزئة يجب أن يكون أكبر من صفر");
                else
                    ClearErrors(nameof(RetailPrice));
            }
        }
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

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        // Use INotifyDataErrorInfo to add errors
        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم المنتج مطلوب");
        if (RetailPrice <= 0)
            AddError(nameof(RetailPrice), "سعر التجزئة يجب أن يكون أكبر من صفر");
        if (WholesalePrice <= 0)
            AddError(nameof(WholesalePrice), "سعر الجملة يجب أن يكون أكبر من صفر");
        if (ConversionFactor <= 0)
            AddError(nameof(ConversionFactor), "معامل التحويل يجب أن يكون أكبر من صفر");
        if (!CategoryId.HasValue || CategoryId.Value <= 0)
            AddError(nameof(CategoryId), "يجب اختيار فئة");
        if (!RetailUnitId.HasValue || RetailUnitId.Value <= 0)
            AddError(nameof(RetailUnitId), "يجب اختيار وحدة التجزئة");
        if (!WholesaleUnitId.HasValue || WholesaleUnitId.Value <= 0)
            AddError(nameof(WholesaleUnitId), "يجب اختيار وحدة الجملة");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync())
        {
            return;
        }

        ErrorMessage = null;

        Result<ProductDto> result;

        if (IsEditMode)
        {
            var updateRequest = new UpdateProductRequest(
                Barcode, Name,
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
                Barcode, Name,
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

            await _dialogService.ShowSuccessAsync(
                IsEditMode ? "تحديث المنتج" : "إضافة منتج",
                IsEditMode ? "تم تحديث المنتج بنجاح" : "تم إضافة المنتج بنجاح");

            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ المنتج", "ProductEditorViewModel.SaveAsync", "[ProductEditorViewModel.SaveAsync] Failed to save product data.");
            await _dialogService.ShowErrorAsync("خطأ في حفظ المنتج", ErrorMessage);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }
    #endregion
}
