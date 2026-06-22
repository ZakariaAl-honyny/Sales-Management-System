using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Products;

public class ProductPricesListViewModel : ViewModelBase, IDisposable
{
    private readonly IProductPriceApiService _priceService;
    private readonly IProductUnitApiService _unitService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<ProductPriceDto> _prices = new();
    private ProductPriceDto? _selectedPrice;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _productId;
    private int _productUnitId;
    private string _productUnitName = string.Empty;
    private bool _includeInactive;
    private ObservableCollection<ProductUnitDto> _availableUnits = new();
    private ProductUnitDto? _selectedAvailableUnit;

    public ProductPricesListViewModel()
        : this(
            App.GetService<IProductPriceApiService>(),
            App.GetService<IProductUnitApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ProductPricesListViewModel(
        IProductPriceApiService priceService,
        IProductUnitApiService unitService,
        IDialogService dialogService,
        IEventBus eventBus,
        IScreenWindowService screenWindowService,
        IToastNotificationService? toastService = null)
    {
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
        _unitService = unitService ?? throw new ArgumentNullException(nameof(unitService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadPricesAsync);
        AddCommand = new RelayCommand(AddPrice);
        EditCommand = new RelayCommand(EditPrice);
        DeactivateCommand = new AsyncRelayCommand(DeactivatePriceAsync);
    }

    public void OnNavigatedTo()
    {
        _eventBus.Subscribe<ProductPriceChangedMessage>(OnPriceChanged);
        _ = LoadPricesAsync();
    }

    #region Properties

    public ObservableCollection<ProductPriceDto> Prices
    {
        get => _prices;
        set => SetProperty(ref _prices, value);
    }

    public ProductPriceDto? SelectedPrice
    {
        get => _selectedPrice;
        set => SetProperty(ref _selectedPrice, value);
    }

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

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

    public ObservableCollection<ProductUnitDto> AvailableUnits
    {
        get => _availableUnits;
        set => SetProperty(ref _availableUnits, value);
    }

    public ProductUnitDto? SelectedAvailableUnit
    {
        get => _selectedAvailableUnit;
        set
        {
            if (SetProperty(ref _selectedAvailableUnit, value) && value != null)
            {
                ProductUnitId = value.Id;
                ProductUnitName = value.UnitName ?? string.Empty;
                _ = LoadPricesAsync();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = LoadPricesAsync();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeactivateCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadPricesAsync()
    {
        await ExecuteAsync(LoadPricesOperationAsync);
    }

    private async Task LoadPricesOperationAsync()
    {
        ErrorMessage = null;

        // Load available units on first load
        if (AvailableUnits.Count == 0 && ProductId > 0)
        {
            var unitsResult = await _unitService.GetByProductIdAsync(ProductId);
            if (unitsResult.IsSuccess && unitsResult.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    AvailableUnits.Clear();
                    foreach (var u in unitsResult.Value)
                    {
                        AvailableUnits.Add(u);
                        // Select the unit matching current ProductUnitId
                        if (u.Id == ProductUnitId)
                        {
                            _selectedAvailableUnit = u;
                            OnPropertyChanged(nameof(SelectedAvailableUnit));
                        }
                    }
                });
            }
        }

        // Load prices for current unit
        if (ProductUnitId <= 0) return;

        var result = await _priceService.GetByProductUnitAsync(ProductUnitId);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Prices.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Prices.Add(item);
                }
                IsEmpty = Prices.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل أسعار الوحدة", "ProductPricesListViewModel.LoadPricesOperationAsync", "[ProductPricesListViewModel.LoadPricesOperationAsync] Failed to load product prices from API.");
            IsEmpty = Prices.Count == 0;
        }
    }

    private void AddPrice()
    {
        var editorVm = new ProductPriceEditorViewModel(ProductUnitId);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "إضافة سعر جديد",
            Width = 550,
            Height = 500,
            OnClosed = (vm) =>
            {
                if (vm is ProductPriceEditorViewModel editor && editor.PriceId.HasValue)
                {
                    _eventBus.Publish(new ProductPriceChangedMessage(editor.PriceId.Value));
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadPricesAsync());
                }
            }
        });
    }

    private void EditPrice()
    {
        if (SelectedPrice == null) return;

        var editorVm = new ProductPriceEditorViewModel(ProductUnitId, SelectedPrice);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل السعر",
            Width = 550,
            Height = 500,
            OnClosed = (vm) =>
            {
                _eventBus.Publish(new ProductPriceChangedMessage(SelectedPrice.Id));
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadPricesAsync());
            }
        });
    }

    public async Task DeactivatePriceAsync()
    {
        if (SelectedPrice == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"السعر: {SelectedPrice.Price:N2}");
        if (strategy == DeleteStrategy.Cancel) return;

        var priceId = SelectedPrice.Id;
        await ExecuteAsync(() => DeactivatePriceOperationAsync(priceId));
    }

    private async Task DeactivatePriceOperationAsync(int priceId)
    {
        ErrorMessage = null;
        var deleteResult = await _priceService.DeactivateAsync(priceId);

        if (deleteResult.IsSuccess)
        {
            _eventBus.Publish(new ProductPriceChangedMessage(priceId));
            await LoadPricesAsync();
            _toastService.ShowSuccess("تم إلغاء تنشيط السعر بنجاح");
        }
        else
        {
            var error = deleteResult.Error ?? "فشل في إلغاء تنشيط السعر";
            ErrorMessage = HandleFailure(error, "ProductPricesListViewModel.DeactivatePriceOperationAsync", "[ProductPricesListViewModel.DeactivatePriceOperationAsync] Failed to deactivate product price.");
            await _dialogService.ShowErrorAsync("خطأ في حذف السعر", ErrorMessage!);
        }
    }

    private void OnPriceChanged(ProductPriceChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadPricesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<ProductPriceChangedMessage>(OnPriceChanged);
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    #endregion
}
