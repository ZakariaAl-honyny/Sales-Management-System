using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class BillOfMaterialEditorViewModel : ViewModelBase
{
    private readonly IBillOfMaterialApiService _bomService;
    private readonly IProductApiService _productService;
    private readonly IProductUnitApiService _productUnitService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int? _bomId;
    private int _assemblyProductId;
    private int _componentProductId;
    private int _componentUnitId;
    private decimal _quantityRequired;
    private decimal _wastePercentage;
    private bool _isEditMode;
    private string? _errorMessage;
    private string _assemblyProductName = string.Empty;
    private string _componentProductName = string.Empty;

    private ObservableCollection<ProductDto> _availableProducts = new();
    private ProductDto? _selectedAssemblyProduct;
    private ProductDto? _selectedComponentProduct;
    private ObservableCollection<ProductUnitDto> _availableUnits = new();
    private ProductUnitDto? _selectedComponentUnit;

    public BillOfMaterialEditorViewModel()
        : this(
            App.GetService<IBillOfMaterialApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IProductUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public BillOfMaterialEditorViewModel(
        IBillOfMaterialApiService bomService,
        IProductApiService productService,
        IProductUnitApiService productUnitService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _bomService = bomService ?? throw new ArgumentNullException(nameof(bomService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _productUnitService = productUnitService ?? throw new ArgumentNullException(nameof(productUnitService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadLookupDataAsync();
    }

    /// <summary>
    /// Constructor for editing an existing BOM.
    /// </summary>
    public BillOfMaterialEditorViewModel(BillOfMaterialDto existing)
        : this()
    {
        LoadForEdit(existing);
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المكون...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public int? BomId
    {
        get => _bomId;
        private set => SetProperty(ref _bomId, value);
    }

    public int AssemblyProductId
    {
        get => _assemblyProductId;
        set
        {
            if (SetProperty(ref _assemblyProductId, value))
            {
                ClearErrors(nameof(AssemblyProductId));
                if (value <= 0)
                    AddError(nameof(AssemblyProductId), "يجب اختيار المنتج المُجمَّع");
                // Load units for the selected assembly product
                if (value > 0)
                    _ = LoadUnitsForComponentAsync(value);
            }
        }
    }

    public int ComponentProductId
    {
        get => _componentProductId;
        set
        {
            if (SetProperty(ref _componentProductId, value))
            {
                ClearErrors(nameof(ComponentProductId));
                if (value <= 0)
                    AddError(nameof(ComponentProductId), "يجب اختيار المكوّن");
                // Load units for the selected component product
                if (value > 0)
                    _ = LoadUnitsForComponentAsync(value);
            }
        }
    }

    public int ComponentUnitId
    {
        get => _componentUnitId;
        set
        {
            if (SetProperty(ref _componentUnitId, value))
            {
                ClearErrors(nameof(ComponentUnitId));
                if (value <= 0 && ComponentProductId > 0)
                    AddError(nameof(ComponentUnitId), "يجب اختيار الوحدة");
            }
        }
    }

    public decimal QuantityRequired
    {
        get => _quantityRequired;
        set
        {
            if (SetProperty(ref _quantityRequired, value))
            {
                ClearErrors(nameof(QuantityRequired));
                if (value <= 0)
                    AddError(nameof(QuantityRequired), "الكمية المطلوبة يجب أن تكون أكبر من صفر");
            }
        }
    }

    public decimal WastePercentage
    {
        get => _wastePercentage;
        set => SetProperty(ref _wastePercentage, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        private set => SetProperty(ref _isEditMode, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string AssemblyProductName
    {
        get => _assemblyProductName;
        set => SetProperty(ref _assemblyProductName, value);
    }

    public string ComponentProductName
    {
        get => _componentProductName;
        set => SetProperty(ref _componentProductName, value);
    }

    public ObservableCollection<ProductDto> AvailableProducts
    {
        get => _availableProducts;
        set => SetProperty(ref _availableProducts, value);
    }

    public ProductDto? SelectedAssemblyProduct
    {
        get => _selectedAssemblyProduct;
        set
        {
            if (SetProperty(ref _selectedAssemblyProduct, value) && value != null)
            {
                AssemblyProductId = value.Id;
                AssemblyProductName = value.Name;
            }
        }
    }

    public ProductDto? SelectedComponentProduct
    {
        get => _selectedComponentProduct;
        set
        {
            if (SetProperty(ref _selectedComponentProduct, value) && value != null)
            {
                ComponentProductId = value.Id;
                ComponentProductName = value.Name;
            }
        }
    }

    public ObservableCollection<ProductUnitDto> AvailableUnits
    {
        get => _availableUnits;
        set => SetProperty(ref _availableUnits, value);
    }

    public ProductUnitDto? SelectedComponentUnit
    {
        get => _selectedComponentUnit;
        set
        {
            if (SetProperty(ref _selectedComponentUnit, value) && value != null)
            {
                ComponentUnitId = value.Id;
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadForEdit(BillOfMaterialDto existing)
    {
        _bomId = existing.Id;
        _assemblyProductId = existing.AssemblyProductId;
        _assemblyProductName = existing.AssemblyProductName;
        _componentProductId = existing.ComponentProductId;
        _componentProductName = existing.ComponentProductName;
        _componentUnitId = existing.ComponentUnitId;
        _quantityRequired = existing.QuantityRequired;
        _wastePercentage = existing.WastePercentage;
        IsEditMode = true;

        // Pre-select products and units when loaded
        OnPropertyChanged(nameof(AssemblyProductId));
        OnPropertyChanged(nameof(AssemblyProductName));
        OnPropertyChanged(nameof(ComponentProductId));
        OnPropertyChanged(nameof(ComponentProductName));
        OnPropertyChanged(nameof(ComponentUnitId));
        OnPropertyChanged(nameof(QuantityRequired));
        OnPropertyChanged(nameof(WastePercentage));
    }

    public async Task LoadLookupDataAsync()
    {
        await ExecuteAsync(LoadLookupDataOperationAsync);
    }

    private async Task LoadLookupDataOperationAsync()
    {
        var result = await _productService.GetAllAsync(includeInactive: false);
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                AvailableProducts.Clear();
                foreach (var p in result.Value.OrderBy(x => x.Name))
                {
                    AvailableProducts.Add(p);
                }

                // Pre-select existing products in edit mode
                if (IsEditMode)
                {
                    SelectedAssemblyProduct = AvailableProducts.FirstOrDefault(p => p.Id == _assemblyProductId);
                    SelectedComponentProduct = AvailableProducts.FirstOrDefault(p => p.Id == _componentProductId);
                }
            });
        }
    }

    private async Task LoadUnitsForComponentAsync(int productId)
    {
        var result = await _productUnitService.GetByProductIdAsync(productId);
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                AvailableUnits.Clear();
                foreach (var u in result.Value.Where(x => x.IsActive).OrderBy(x => x.UnitName))
                {
                    AvailableUnits.Add(u);
                }

                // Pre-select existing unit in edit mode
                if (IsEditMode && _componentUnitId > 0)
                {
                    SelectedComponentUnit = AvailableUnits.FirstOrDefault(u => u.Id == _componentUnitId);
                }
            });
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (AssemblyProductId <= 0)
            AddError(nameof(AssemblyProductId), "يجب اختيار المنتج المُجمَّع");

        if (ComponentProductId <= 0)
            AddError(nameof(ComponentProductId), "يجب اختيار المكوّن");

        if (AssemblyProductId > 0 && ComponentProductId > 0 && AssemblyProductId == ComponentProductId)
            AddError(nameof(ComponentProductId), "لا يمكن أن يكون المنتج المُجمَّع هو نفسه المكوّن");

        if (ComponentUnitId <= 0 && ComponentProductId > 0)
            AddError(nameof(ComponentUnitId), "يجب اختيار وحدة المكوّن");

        if (QuantityRequired <= 0)
            AddError(nameof(QuantityRequired), "الكمية المطلوبة يجب أن تكون أكبر من صفر");

        if (WastePercentage < 0)
            AddError(nameof(WastePercentage), "نسبة الهالك لا يمكن أن تكون سالبة");

        if (WastePercentage > 100)
            AddError(nameof(WastePercentage), "نسبة الهالك لا يمكن أن تتجاوز 100%");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        if (IsEditMode && BomId.HasValue)
        {
            await UpdateBomAsync();
        }
        else
        {
            await CreateBomAsync();
        }
    }

    private async Task CreateBomAsync()
    {
        var request = new CreateBillOfMaterialRequest(
            AssemblyProductId,
            ComponentProductId,
            ComponentUnitId,
            QuantityRequired,
            WastePercentage);

        var result = await _bomService.CreateAsync(request);

        if (result.IsSuccess)
        {
            BomId = result.Value?.Id;
            _eventBus.Publish(new BillOfMaterialChangedMessage(BomId ?? 0));
            _toastService.ShowSuccess("تم إضافة المكون بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في إضافة المكون",
                "BillOfMaterialEditorViewModel.CreateBomAsync",
                "[BillOfMaterialEditorViewModel.CreateBomAsync] Failed to create BOM.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في حفظ المكون", error);
        }
    }

    private async Task UpdateBomAsync()
    {
        if (!BomId.HasValue) return;

        var request = new UpdateBillOfMaterialRequest(
            ComponentUnitId,
            QuantityRequired,
            WastePercentage);

        var result = await _bomService.UpdateAsync(BomId.Value, request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new BillOfMaterialChangedMessage(BomId.Value));
            _toastService.ShowSuccess("تم تحديث المكون بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في تحديث المكون",
                "BillOfMaterialEditorViewModel.UpdateBomAsync",
                "[BillOfMaterialEditorViewModel.UpdateBomAsync] Failed to update BOM.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في تحديث المكون", error);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    public override void Cleanup()
    {
        _bomId = null;
        IsEditMode = false;
        ErrorMessage = null;
        AvailableProducts.Clear();
        AvailableUnits.Clear();
        base.Cleanup();
    }

    #endregion
}
