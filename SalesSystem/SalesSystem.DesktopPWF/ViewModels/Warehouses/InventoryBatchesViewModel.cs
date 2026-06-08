using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Warehouses;

public class InventoryBatchesViewModel : ViewModelBase, IDisposable
{
    private readonly IInventoryBatchApiService _batchService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<InventoryBatchDto> _batches = new();
    private InventoryBatchDto? _selectedBatch;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _productId;
    private int? _warehouseId;
    private string _productName = string.Empty;

    public InventoryBatchesViewModel()
        : this(
            App.GetService<IInventoryBatchApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public InventoryBatchesViewModel(
        IInventoryBatchApiService batchService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadBatchesAsync);
    }

    public void OnNavigatedTo()
    {
        _eventBus.Subscribe<InventoryBatchChangedMessage>(OnBatchChanged);
        _eventBus.Subscribe<StockChangedMessage>(OnStockChanged);
        _ = LoadBatchesAsync();
    }

    #region Properties

    public ObservableCollection<InventoryBatchDto> Batches
    {
        get => _batches;
        set => SetProperty(ref _batches, value);
    }

    public InventoryBatchDto? SelectedBatch
    {
        get => _selectedBatch;
        set => SetProperty(ref _selectedBatch, value);
    }

    public int ProductId
    {
        get => _productId;
        set => SetProperty(ref _productId, value);
    }

    public int? WarehouseId
    {
        get => _warehouseId;
        set => SetProperty(ref _warehouseId, value);
    }

    public string ProductName
    {
        get => _productName;
        set => SetProperty(ref _productName, value);
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

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadBatchesAsync()
    {
        await ExecuteAsync(LoadBatchesOperationAsync);
    }

    private async Task LoadBatchesOperationAsync()
    {
        ErrorMessage = null;
        var result = await _batchService.GetByProductAsync(ProductId, WarehouseId);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Batches.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Batches.Add(item);
                }
                IsEmpty = Batches.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل دفعات المخزون", "InventoryBatchesViewModel.LoadBatchesOperationAsync", "[InventoryBatchesViewModel.LoadBatchesOperationAsync] Failed to load inventory batches from API.");
            IsEmpty = Batches.Count == 0;
        }
    }

    private void OnBatchChanged(InventoryBatchChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadBatchesAsync();
        });
    }

    private void OnStockChanged(StockChangedMessage msg)
    {
        if (msg.ProductId == ProductId)
        {
            _ = InvokeOnUIThreadAsync(async () =>
            {
                await LoadBatchesAsync();
            });
        }
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<InventoryBatchChangedMessage>(OnBatchChanged);
        _eventBus.Unsubscribe<StockChangedMessage>(OnStockChanged);
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    #endregion
}
