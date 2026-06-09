using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class AssemblyProductionViewModel : ViewModelBase
{
    private readonly IBillOfMaterialApiService _bomService;
    private readonly IProductApiService _productService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _assemblyProductId;
    private string _assemblyProductName = string.Empty;
    private int _warehouseId;
    private decimal _quantity;
    private ProduceAssemblyResultDto? _produceResult;
    private string? _errorMessage;
    private bool _hasResult;

    private ObservableCollection<ProductDto> _availableProducts = new();
    private ProductDto? _selectedAssemblyProduct;
    private ObservableCollection<WarehouseDto> _availableWarehouses = new();
    private WarehouseDto? _selectedWarehouse;

    public AssemblyProductionViewModel()
        : this(
            App.GetService<IBillOfMaterialApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public AssemblyProductionViewModel(
        IBillOfMaterialApiService bomService,
        IProductApiService productService,
        IWarehouseApiService warehouseService,
        IDialogService dialogService,
        IToastNotificationService? toastService = null)
    {
        _bomService = bomService ?? throw new ArgumentNullException(nameof(bomService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadLookupDataAsync();
    }

    private void InitializeCommands()
    {
        ProduceCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(ProduceOperationAsync, "جاري تنفيذ أمر الإنتاج...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

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
            }
        }
    }

    public string AssemblyProductName
    {
        get => _assemblyProductName;
        set => SetProperty(ref _assemblyProductName, value);
    }

    public int WarehouseId
    {
        get => _warehouseId;
        set
        {
            if (SetProperty(ref _warehouseId, value))
            {
                ClearErrors(nameof(WarehouseId));
                if (value <= 0)
                    AddError(nameof(WarehouseId), "يجب اختيار المستودع");
            }
        }
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                ClearErrors(nameof(Quantity));
                if (value <= 0)
                    AddError(nameof(Quantity), "الكمية يجب أن تكون أكبر من صفر");
            }
        }
    }

    public ProduceAssemblyResultDto? ProduceResult
    {
        get => _produceResult;
        set
        {
            if (SetProperty(ref _produceResult, value))
            {
                HasResult = value != null;
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasResult
    {
        get => _hasResult;
        private set => SetProperty(ref _hasResult, value);
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

    public ObservableCollection<WarehouseDto> AvailableWarehouses
    {
        get => _availableWarehouses;
        set => SetProperty(ref _availableWarehouses, value);
    }

    public WarehouseDto? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set
        {
            if (SetProperty(ref _selectedWarehouse, value) && value != null)
            {
                WarehouseId = value.Id;
            }
        }
    }

    #endregion

    #region Commands

    public ICommand ProduceCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadLookupDataAsync()
    {
        await ExecuteAsync(LoadLookupDataOperationAsync);
    }

    private async Task LoadLookupDataOperationAsync()
    {
        var productResult = await _productService.GetAllAsync(includeInactive: false);
        if (productResult.IsSuccess && productResult.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                AvailableProducts.Clear();
                foreach (var p in productResult.Value.OrderBy(x => x.Name))
                {
                    AvailableProducts.Add(p);
                }

                // Pre-select if already set
                if (_assemblyProductId > 0)
                {
                    SelectedAssemblyProduct = AvailableProducts.FirstOrDefault(p => p.Id == _assemblyProductId);
                }
            });
        }

        var warehouseResult = await _warehouseService.GetAllAsync(includeInactive: false);
        if (warehouseResult.IsSuccess && warehouseResult.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                AvailableWarehouses.Clear();
                foreach (var w in warehouseResult.Value.OrderBy(x => x.Name))
                {
                    AvailableWarehouses.Add(w);
                }
            });
        }
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (AssemblyProductId <= 0)
            AddError(nameof(AssemblyProductId), "يجب اختيار المنتج المُجمَّع");

        if (WarehouseId <= 0)
            AddError(nameof(WarehouseId), "يجب اختيار المستودع");

        if (Quantity <= 0)
            AddError(nameof(Quantity), "الكمية يجب أن تكون أكبر من صفر");

        return await ValidateAllAsync();
    }

    private async Task ProduceOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ProduceResult = null;

        var request = new ProduceAssemblyRequest(
            AssemblyProductId,
            WarehouseId,
            Quantity);

        var result = await _bomService.ProduceAsync(request);

        if (result.IsSuccess && result.Value != null)
        {
            ProduceResult = result.Value;
            _toastService.ShowSuccess($"تم إنتاج {result.Value.QuantityProduced:N3} وحدة من {result.Value.AssemblyProductName} بنجاح");
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في تنفيذ أمر الإنتاج",
                "AssemblyProductionViewModel.ProduceOperationAsync",
                "[AssemblyProductionViewModel.ProduceOperationAsync] Failed to produce assembly.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في الإنتاج", error);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    public override void Cleanup()
    {
        ProduceResult = null;
        ErrorMessage = null;
        HasResult = false;
        AvailableProducts.Clear();
        AvailableWarehouses.Clear();
        base.Cleanup();
    }

    #endregion
}
