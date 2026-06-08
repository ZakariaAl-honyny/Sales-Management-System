using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.InventoryOperations;

/// <summary>
/// ViewModel for Inventory Operations List View
/// Supports three operation types: صرف مخزني (1), توريد مخزني (2), تسوية مخزنية (3)
/// </summary>
public class InventoryOperationListViewModel : ViewModelBase
{
    private IInventoryOperationApiService? _operationService;
    private IWarehouseApiService? _warehouseService;
    private IEventBus? _eventBus;
    private IToastNotificationService? _toastService;
    private IDialogService? _dialogService;

    private IInventoryOperationApiService OperationService => _operationService ??= App.GetService<IInventoryOperationApiService>();
    private IWarehouseApiService WarehouseService => _warehouseService ??= App.GetService<IWarehouseApiService>();
    private IEventBus EventBusService => _eventBus ??= App.GetService<IEventBus>();
    private IToastNotificationService ToastService => _toastService ??= App.GetService<IToastNotificationService>();

    private new IDialogService DialogService => _dialogService ??= App.GetService<IDialogService>();

    private ObservableCollection<InventoryOperationDto> _operations = new();
    private ICollectionView? _operationsView;
    private InventoryOperationDto? _selectedOperation;
    private string _searchText = string.Empty;
    private byte? _selectedTypeFilter = 1; // Default to StockIssue
    private int? _selectedWarehouseFilter;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;
    private ObservableCollection<WarehouseDto> _warehouses = new();

    public InventoryOperationListViewModel()
    {
        InitializeCommands();
    }

    public InventoryOperationListViewModel(
        IInventoryOperationApiService operationService,
        IWarehouseApiService warehouseService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _operationService = operationService ?? throw new ArgumentNullException(nameof(operationService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        SetDialogService(dialogService ?? throw new ArgumentNullException(nameof(dialogService)));

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadOperationsOperationAsync)));
        AddCommand = new RelayCommand(AddOperation);
        EditCommand = new RelayCommand(EditOperation);
        PostCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(PostOperationOperationAsync, ex => ErrorMessage = HandleException(ex, "InventoryOperationListViewModel.PostOperationAsync"))));
        CancelOperationCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(CancelOperationOperationAsync, ex => ErrorMessage = HandleException(ex, "InventoryOperationListViewModel.CancelOperationAsync"))));
        SearchCommand = new RelayCommand(Search);
        ViewCommand = new RelayCommand(ViewOperation);

        EventBusService.Subscribe<InventoryOperationChangedMessage>(OnInventoryOperationChanged);
    }

    #region Properties

    public ObservableCollection<InventoryOperationDto> Operations
    {
        get => _operations;
        set => SetProperty(ref _operations, value);
    }

    public ICollectionView? OperationsView
    {
        get => _operationsView;
        private set => SetProperty(ref _operationsView, value);
    }

    public InventoryOperationDto? SelectedOperation
    {
        get => _selectedOperation;
        set => SetProperty(ref _selectedOperation, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OperationsView?.Refresh();
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
                _ = ExecuteAsync(LoadOperationsOperationAsync);
            }
        }
    }

    public int OperationsCount => Operations.Count;

    /// <summary>
    /// Type filter list for the filter ComboBox.
    /// null = "الكل", 1 = "صرف مخزني", 2 = "توريد مخزني", 3 = "تسوية مخزنية"
    /// </summary>
    public List<KeyValuePair<byte?, string>> TypeFilterList { get; } = new()
    {
        new KeyValuePair<byte?, string>(null, "الكل"),
        new KeyValuePair<byte?, string>(1, "صرف مخزني"),
        new KeyValuePair<byte?, string>(2, "توريد مخزني"),
        new KeyValuePair<byte?, string>(3, "تسوية مخزنية"),
    };

    public byte? SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            if (SetProperty(ref _selectedTypeFilter, value))
            {
                _ = ExecuteAsync(LoadOperationsOperationAsync);
            }
        }
    }

    /// <summary>
    /// Warehouse filter (null = all warehouses).
    /// </summary>
    public int? SelectedWarehouseFilter
    {
        get => _selectedWarehouseFilter;
        set
        {
            if (SetProperty(ref _selectedWarehouseFilter, value))
            {
                _ = ExecuteAsync(LoadOperationsOperationAsync);
            }
        }
    }

    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand PostCommand { get; private set; } = null!;
    public ICommand CancelOperationCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    public ICommand ViewCommand { get; private set; } = null!;

    #endregion

    #region Methods

    /// <summary>
    /// Public method for loading operations (used by RefreshCommand and tests).
    /// </summary>
    public Task LoadOperationsAsync() => ExecuteAsync(LoadOperationsOperationAsync);

    private async Task LoadOperationsOperationAsync()
    {
        ErrorMessage = null;

        var result = await OperationService.GetAllAsync(
            SelectedWarehouseFilter,
            SelectedTypeFilter);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(async () =>
            {
                Operations.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Operations.Add(item);
                }
                try
                {
                    SetupCollectionView();
                }
                catch (InvalidOperationException)
                {
                    // WPF CollectionView requires a running Dispatcher — silently skip in non-WPF contexts (e.g., tests)
                    OperationsView = null;
                }
                IsEmpty = Operations.Count == 0;
                OnPropertyChanged(nameof(OperationsCount));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل العمليات المخزنية", "InventoryOperationListViewModel.LoadOperationsAsync", "[InventoryOperationListViewModel.LoadOperationsAsync] Failed to load inventory operations list.");
            IsEmpty = Operations.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        OperationsView = CollectionViewSource.GetDefaultView(Operations);
        OperationsView.Filter = FilterOperations;
    }

    private bool FilterOperations(object obj)
    {
        if (obj is not InventoryOperationDto operation) return false;

        // Search text filter on OperationNo, WarehouseName
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return operation.OperationNo.ToLower().Contains(searchLower) ||
               (operation.WarehouseName?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddOperation()
    {
        var screenWindowService = App.GetService<IScreenWindowService>();
        var editorVm = new InventoryOperationEditorViewModel(SelectedTypeFilter ?? 1);
        screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = SelectedTypeFilter switch
            {
                1 => "صرف مخزني جديد",
                2 => "توريد مخزني جديد",
                3 => "تسوية مخزنية جديدة",
                _ => "عملية مخزنية جديدة"
            },
            OnClosed = (vm) =>
            {
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadOperationsAsync());
            }
        });
    }

    private void EditOperation()
    {
        if (SelectedOperation == null) return;

        var screenWindowService = App.GetService<IScreenWindowService>();
        var editorVm = new InventoryOperationEditorViewModel(SelectedOperation.OperationType, SelectedOperation.Id);
        screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = SelectedOperation.OperationType switch
            {
                1 => "تعديل صرف مخزني",
                2 => "تعديل توريد مخزني",
                3 => "تعديل تسوية مخزنية",
                _ => "تعديل العملية المخزنية"
            },
            OnClosed = (vm) =>
            {
                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadOperationsAsync());
            }
        });
    }

    private void ViewOperation()
    {
        if (SelectedOperation == null) return;

        var screenWindowService = App.GetService<IScreenWindowService>();
        var editorVm = new InventoryOperationEditorViewModel(SelectedOperation.OperationType, SelectedOperation.Id, isReadOnly: true);
        screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = SelectedOperation.OperationType switch
            {
                1 => "عرض صرف مخزني",
                2 => "عرض توريد مخزني",
                3 => "عرض تسوية مخزنية",
                _ => "عرض العملية المخزنية"
            }
        });
    }

    private async Task PostOperationOperationAsync()
    {
        if (SelectedOperation == null) return;

        if (SelectedOperation.Status != 1) // Only draft can be posted
        {
            await DialogService.ShowWarningAsync("تنبيه", "لا يمكن ترحيل هذه العملية لأنها ليست في حالة مسودة.");
            return;
        }

        var confirmed = await DialogService.ShowConfirmationAsync("تأكيد الترحيل",
            "هل أنت متأكد من ترحيل هذه العملية المخزنية؟\n\n" +
            "• سيتم تحديث المخزون في المستودع.\n" +
            "• لا يمكن التراجع عن الترحيل بعد إتمامه.");

        if (!confirmed) return;

        ErrorMessage = null;
        var result = await OperationService.PostAsync(SelectedOperation.Id);

        if (result.IsSuccess)
        {
            EventBusService.Publish(new InventoryOperationChangedMessage(result.Value!.Id));
            await LoadOperationsOperationAsync();
            ToastService.ShowSuccess("تم ترحيل العملية المخزنية بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في ترحيل العملية", "InventoryOperationListViewModel.PostOperationAsync");
            await DialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage ?? "حدث خطأ غير متوقع");
        }
    }

    private async Task CancelOperationOperationAsync()
    {
        if (SelectedOperation == null) return;

        if (SelectedOperation.Status == 3) // Already cancelled
        {
            await DialogService.ShowWarningAsync("تنبيه", "هذه العملية ملغية بالفعل.");
            return;
        }

        var confirmed = await DialogService.ShowConfirmationAsync("تأكيد الإلغاء",
            "هل أنت متأكد من إلغاء هذه العملية المخزنية؟\n\n" +
            "• سيتم عكس تأثير العملية على المخزون.\n" +
            "• لا يمكن التراجع عن الإلغاء بعد إتمامه.");

        if (!confirmed) return;

        ErrorMessage = null;
        var result = await OperationService.CancelAsync(SelectedOperation.Id);

        if (result.IsSuccess)
        {
            EventBusService.Publish(new InventoryOperationChangedMessage(result.Value!.Id));
            await LoadOperationsOperationAsync();
            ToastService.ShowSuccess("تم إلغاء العملية المخزنية بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء العملية", "InventoryOperationListViewModel.CancelOperationAsync");
            await DialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage ?? "حدث خطأ غير متوقع");
        }
    }

    private void Search()
    {
        OperationsView?.Refresh();
    }

    private void OnInventoryOperationChanged(InventoryOperationChangedMessage msg)
    {
        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await ExecuteAsync(LoadOperationsOperationAsync));
    }

    public override void Cleanup()
    {
        if (_eventBus != null)
        {
            _eventBus.Unsubscribe<InventoryOperationChangedMessage>(OnInventoryOperationChanged);
        }
    }

    /// <summary>
    /// Public method for loading warehouses (used by init).
    /// </summary>
    public async Task LoadWarehousesAsync()
    {
        var result = await WarehouseService.GetAllAsync();
        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
        {
            Warehouses.Clear();
            Warehouses.Add(new WarehouseDto(0, "--- الكل ---", 0, null, null, null, null, false, true, null, null));
            foreach (var wh in result.Value.OrderBy(x => x.Name))
            {
                Warehouses.Add(wh);
            }
        });
        }
    }

    #endregion
}
