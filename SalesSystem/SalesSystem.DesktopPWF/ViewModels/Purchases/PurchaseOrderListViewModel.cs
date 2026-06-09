using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.Views.Purchases;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Purchases;

/// <summary>
/// ViewModel for Purchase Orders List View.
/// Supports filtering by status, supplier search, and CRUD operations.
/// </summary>
public class PurchaseOrderListViewModel : ViewModelBase, IDisposable
{
    private readonly IPurchaseOrderApiService _orderService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<PurchaseOrderDto> _orders = new();
    private PurchaseOrderDto? _selectedOrder;
    private string? _searchText;
    private int? _filterStatus;
    private string? _errorMessage;
    private bool _isEmpty;

    public PurchaseOrderListViewModel(
        IPurchaseOrderApiService orderService,
        IEventBus eventBus,
        IDialogService dialogService,
        IScreenWindowService screenWindowService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        SetDialogService(dialogService);

        RefreshCommand = new AsyncRelayCommand(LoadOrdersAsync);
        AddCommand = new RelayCommand(AddOrder);
        EditCommand = new AsyncRelayCommand(EditOrderAsync);
        CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync);

        _eventBus.Subscribe<PurchaseOrderChangedMessage>(OnOrderChanged);
        _ = LoadOrdersAsync();
    }

    #region Properties

    public ObservableCollection<PurchaseOrderDto> Orders
    {
        get => _orders;
        set => SetProperty(ref _orders, value);
    }

    public PurchaseOrderDto? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public int? FilterStatus
    {
        get => _filterStatus;
        set
        {
            if (SetProperty(ref _filterStatus, value))
                _ = ApplyFiltersAsync();
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

    public List<PurchaseOrderStatusItem> StatusOptions { get; } = new()
    {
        new PurchaseOrderStatusItem { Value = null, Display = "الكل" },
        new PurchaseOrderStatusItem { Value = (int)PurchaseOrderStatus.Draft, Display = "مسودة" },
        new PurchaseOrderStatusItem { Value = (int)PurchaseOrderStatus.Approved, Display = "معتمد" },
        new PurchaseOrderStatusItem { Value = (int)PurchaseOrderStatus.PartiallyReceived, Display = "مستلم جزئياً" },
        new PurchaseOrderStatusItem { Value = (int)PurchaseOrderStatus.Received, Display = "مستلم بالكامل" },
        new PurchaseOrderStatusItem { Value = (int)PurchaseOrderStatus.Cancelled, Display = "ملغي" }
    };

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand CancelOrderCommand { get; }

    #endregion

    #region Methods

    public async Task LoadOrdersAsync()
    {
        await ExecuteAsync(LoadOrdersOperationAsync, "جاري تحميل أوامر الشراء...");
    }

    private async Task LoadOrdersOperationAsync()
    {
        ErrorMessage = null;
        var result = await _orderService.GetAllAsync(
            search: SearchText,
            status: FilterStatus.HasValue ? (byte)FilterStatus.Value : null);

        if (result.IsSuccess && result.Value != null)
        {
            Orders = new ObservableCollection<PurchaseOrderDto>(
                result.Value.OrderByDescending(x => x.Id));
            IsEmpty = Orders.Count == 0;
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل أوامر الشراء", "PurchaseOrderListViewModel.LoadOrdersAsync");
            IsEmpty = Orders.Count == 0;
        }
    }

    private async Task ApplyFiltersAsync()
    {
        await LoadOrdersAsync();
    }

    private void AddOrder()
    {
        var editorVm = App.GetService<PurchaseOrderEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "أمر شراء جديد",
            OnClosed = (vm) =>
            {
                if (vm is PurchaseOrderEditorViewModel editor && editor.OrderId.HasValue)
                {
                    _eventBus.Publish(new PurchaseOrderChangedMessage(editor.OrderId.Value));
                    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadOrdersAsync());
                }
            }
        });
    }

    private async Task EditOrderAsync()
    {
        if (SelectedOrder == null) return;

        var editorVm = new PurchaseOrderEditorViewModel(SelectedOrder.Id);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = $"تعديل أمر الشراء رقم {SelectedOrder.OrderNo}",
            OnClosed = (vm) =>
            {
                if (vm is PurchaseOrderEditorViewModel editor && editor.OrderId.HasValue)
                {
                    _eventBus.Publish(new PurchaseOrderChangedMessage(editor.OrderId.Value));
                    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadOrdersAsync());
                }
            }
        });
    }

    private async Task CancelOrderAsync()
    {
        if (SelectedOrder == null) return;

        if (SelectedOrder.Status >= (byte)PurchaseOrderStatus.Received)
        {
            await _dialogService.ShowWarningAsync("إلغاء أمر شراء", "لا يمكن إلغاء أمر شراء تم استلامه بالكامل.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "تأكيد إلغاء أمر الشراء",
            $"هل أنت متأكد من إلغاء أمر الشراء رقم {SelectedOrder.OrderNo}؟");
        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _orderService.CancelAsync(SelectedOrder!.Id);
            if (result.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("نجاح", "تم إلغاء أمر الشراء بنجاح");
                _eventBus.Publish(new PurchaseOrderChangedMessage(SelectedOrder.Id));
                await LoadOrdersAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء أمر الشراء", "PurchaseOrderListViewModel.CancelOrderAsync");
                await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage!);
            }
        });
    }

    private void OnOrderChanged(PurchaseOrderChangedMessage msg)
    {
        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadOrdersAsync());
    }

    public void Dispose()
    {
        Cleanup();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}

/// <summary>
/// Helper class for status combo box items
/// </summary>
public class PurchaseOrderStatusItem
{
    public int? Value { get; set; }
    public string Display { get; set; } = string.Empty;
}
