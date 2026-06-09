using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Purchases;

public class PurchaseOrderListViewModel : ViewModelBase
{
    private readonly IPurchaseOrderApiService _orderService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;
    private ObservableCollection<PurchaseOrderDto> _orders = new();
    private ICollectionView? _ordersView;
    private PurchaseOrderDto? _selectedOrder;
    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private int? _statusFilter;
    private string? _errorMessage;
    private bool _isEmpty;

    public PurchaseOrderListViewModel()
        : this(
            App.GetService<IPurchaseOrderApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            App.GetService<IScreenWindowService>())
    {
    }

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

        RefreshCommand = new AsyncRelayCommand(async () => await ExecuteAsync(LoadOrdersOperationAsync, "جاري تحميل أوامر الشراء..."));
        SearchCommand = new AsyncRelayCommand(async () => await ExecuteAsync(LoadOrdersOperationAsync, "جاري البحث..."));
        NewCommand = new RelayCommand(AddNewOrder);
        ViewCommand = new RelayCommand(ViewOrder, () => SelectedOrder != null);
        EditCommand = new RelayCommand(EditOrder, () => SelectedOrder != null && SelectedOrder.Status == PoStatus.Draft);
        PostCommand = new AsyncRelayCommand(PostOrderAsync, () => SelectedOrder != null && SelectedOrder.Status == PoStatus.Draft);
        CancelOrderCommand = new AsyncRelayCommand(CancelOrderAsync, () => SelectedOrder != null && (SelectedOrder.Status == PoStatus.Draft || SelectedOrder.Status == PoStatus.Approved));

        DateFrom = DateTime.Today.AddDays(-30);
        DateTo = DateTime.Today;

        eventBus.Subscribe<PurchaseOrderChangedMessage>(OnOrderChanged);
    }

    #region Properties
    public ObservableCollection<PurchaseOrderDto> Orders
    {
        get => _orders;
        set => SetProperty(ref _orders, value);
    }

    public ICollectionView? OrdersView
    {
        get => _ordersView;
        private set => SetProperty(ref _ordersView, value);
    }

    public PurchaseOrderDto? SelectedOrder
    {
        get => _selectedOrder;
        set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OrdersView?.Refresh();
            }
        }
    }

    public DateTime? DateFrom
    {
        get => _dateFrom;
        set
        {
            if (SetProperty(ref _dateFrom, value))
            {
                OrdersView?.Refresh();
            }
        }
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set
        {
            if (SetProperty(ref _dateTo, value))
            {
                OrdersView?.Refresh();
            }
        }
    }

    public int? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                OrdersView?.Refresh();
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

    public List<OrderStatusItem> StatusOptions { get; } = new()
    {
        new OrderStatusItem { Value = null, Display = "الكل" },
        new OrderStatusItem { Value = (int)PoStatus.Draft, Display = "مسودة" },
        new OrderStatusItem { Value = (int)PoStatus.Approved, Display = "معتمد" },
        new OrderStatusItem { Value = (int)PoStatus.PartiallyReceived, Display = "مستلم جزئياً" },
        new OrderStatusItem { Value = (int)PoStatus.Received, Display = "مستلم بالكامل" },
        new OrderStatusItem { Value = (int)PoStatus.Cancelled, Display = "ملغي" }
    };
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand ViewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelOrderCommand { get; }
    #endregion

    #region Methods
    private async Task LoadOrdersOperationAsync()
    {
        ErrorMessage = null;
        var result = await _orderService.GetAllAsync(
            search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            from: DateFrom,
            to: DateTo,
            status: StatusFilter.HasValue ? (byte)StatusFilter.Value : null);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Orders.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.OrderDate))
                {
                    Orders.Add(item);
                }
                IsEmpty = Orders.Count == 0;
                SetupCollectionView();
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل أوامر الشراء",
                "PurchaseOrderListViewModel.LoadOrdersOperationAsync",
                "[PurchaseOrderListViewModel.LoadOrdersOperationAsync] Failed to load POs.");
            IsEmpty = Orders.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        OrdersView = new ListCollectionView(Orders);
        OrdersView.Filter = FilterOrders;
    }

    private bool FilterOrders(object obj)
    {
        if (obj is not PurchaseOrderDto order) return false;

        if (DateFrom.HasValue && order.OrderDate < DateFrom.Value) return false;
        if (DateTo.HasValue && order.OrderDate > DateTo.Value.AddDays(1)) return false;

        if (StatusFilter.HasValue && order.Status != StatusFilter.Value) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            return order.OrderNo.ToString().Contains(searchLower) ||
                   (order.SupplierName?.ToLower().Contains(searchLower) ?? false);
        }

        return true;
    }

    private void AddNewOrder()
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
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = ExecuteAsync(LoadOrdersOperationAsync, "جاري التحديث..."));
                }
            }
        });
    }

    private void ViewOrder()
    {
        if (SelectedOrder == null) return;

        var editorVm = new PurchaseOrderEditorViewModel(SelectedOrder.Id, isReadOnly: true);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "عرض أمر شراء"
        });
    }

    private void EditOrder()
    {
        if (SelectedOrder == null) return;

        var editorVm = new PurchaseOrderEditorViewModel(SelectedOrder.Id);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل أمر شراء",
            OnClosed = (vm) =>
            {
                _eventBus.Publish(new PurchaseOrderChangedMessage(SelectedOrder.Id));
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = ExecuteAsync(LoadOrdersOperationAsync, "جاري التحديث..."));
            }
        });
    }

    private async Task PostOrderAsync()
    {
        if (SelectedOrder == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الاعتماد",
            $"هل أنت متأكد من اعتماد أمر الشراء رقم: {SelectedOrder.OrderNo}؟");
        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _orderService.PostAsync(SelectedOrder.Id);
            if (result.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("نجاح", "تم اعتماد أمر الشراء بنجاح");
                _eventBus.Publish(new PurchaseOrderChangedMessage(SelectedOrder.Id));
                await LoadOrdersOperationAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في اعتماد أمر الشراء",
                    "PurchaseOrderListViewModel.PostOrderAsync",
                    $"[PurchaseOrderListViewModel.PostOrderAsync] Failed to post PO ID {SelectedOrder.Id}.");
                await _dialogService.ShowErrorAsync("خطأ في الاعتماد", ErrorMessage);
            }
        });
    }

    private async Task CancelOrderAsync()
    {
        if (SelectedOrder == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء",
            $"هل أنت متأكد من إلغاء أمر الشراء رقم: {SelectedOrder.OrderNo}؟");
        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _orderService.CancelAsync(SelectedOrder.Id);
            if (result.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("نجاح", "تم إلغاء أمر الشراء بنجاح");
                _eventBus.Publish(new PurchaseOrderChangedMessage(SelectedOrder.Id));
                await LoadOrdersOperationAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء أمر الشراء",
                    "PurchaseOrderListViewModel.CancelOrderAsync",
                    $"[PurchaseOrderListViewModel.CancelOrderAsync] Failed to cancel PO ID {SelectedOrder.Id}.");
                await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage);
            }
        });
    }

    private void UpdateCommandStates()
    {
        (ViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PostCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CancelOrderCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnOrderChanged(PurchaseOrderChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await ExecuteAsync(LoadOrdersOperationAsync, "جاري التحديث...");
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<PurchaseOrderChangedMessage>(OnOrderChanged);
    }
    #endregion
}

public class OrderStatusItem
{
    public int? Value { get; set; }
    public string Display { get; set; } = string.Empty;
}
