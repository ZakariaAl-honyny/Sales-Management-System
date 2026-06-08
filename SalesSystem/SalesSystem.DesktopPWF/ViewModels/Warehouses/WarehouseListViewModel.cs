using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Common;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// ViewModel for Warehouses List View
/// </summary>
public class WarehouseListViewModel : ViewModelBase
{
    private IWarehouseApiService? _warehouseService;
    private IEventBus? _eventBus;
    private IToastNotificationService? _toastService;

    private IWarehouseApiService WarehouseService => _warehouseService ??= App.GetService<IWarehouseApiService>();
    private IEventBus EventBus => _eventBus ??= App.GetService<IEventBus>();
    private IToastNotificationService ToastService => _toastService ??= App.GetService<IToastNotificationService>();

    // Uses 'new' to suppress CS0108 (inherited member hiding).
    // DI constructor sets this directly; SetField("_dialogService", mock) also works in tests.
    private new IDialogService DialogService => _dialogService ??= App.GetService<IDialogService>();
    private IDialogService? _dialogService;

    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ICollectionView? _warehousesView;
    private WarehouseDto? _selectedWarehouse;
    private string _searchText = string.Empty;
    private byte? _selectedTypeFilter;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public WarehouseListViewModel()
    {
        InitializeCommands();
    }

    /// <summary>
    /// Constructor with explicit dependencies for testing.
    /// </summary>
    public WarehouseListViewModel(
        IWarehouseApiService warehouseService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        SetDialogService(dialogService ?? throw new ArgumentNullException(nameof(dialogService)));

        InitializeCommands();
    }

    /// <summary>
    /// Initializes commands and subscribes to events. Separated from constructor
    /// to support testing via GetUninitializedObject + reflection.
    /// </summary>
    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadWarehousesOperationAsync)));
        AddCommand = new RelayCommand(AddWarehouse);
        EditCommand = new RelayCommand(EditWarehouse, () => SelectedWarehouse != null);
        DeleteCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(DeleteWarehouseOperationAsync, ex => ErrorMessage = HandleException(ex, "WarehouseListViewModel.DeleteWarehouseAsync"))), () => SelectedWarehouse != null && SelectedWarehouse.IsActive);
        RestoreCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(RestoreWarehouseOperationAsync, ex => ErrorMessage = HandleException(ex, "WarehouseListViewModel.RestoreWarehouseAsync"))), () => SelectedWarehouse != null && !SelectedWarehouse.IsActive);
        SearchCommand = new RelayCommand(Search);

        EventBus.Subscribe<WarehouseChangedMessage>(OnWarehouseChanged);
    }

    #region Properties
    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    public ICollectionView? WarehousesView
    {
        get => _warehousesView;
        private set => SetProperty(ref _warehousesView, value);
    }

    public WarehouseDto? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set
        {
            if (SetProperty(ref _selectedWarehouse, value))
            {
                (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RestoreCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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
                WarehousesView?.Refresh();
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
                _ = ExecuteAsync(LoadWarehousesOperationAsync);
            }
        }
    }

    public int WarehousesCount => Warehouses.Count;

    /// <summary>
    /// Type filter list for the filter ComboBox (null = "الكل").
    /// </summary>
    public List<KeyValuePair<byte?, string>> TypeFilterList { get; } = new()
    {
        new KeyValuePair<byte?, string>(null, "الكل"),
        new KeyValuePair<byte?, string>(1, "رئيسي"),
        new KeyValuePair<byte?, string>(2, "فرعي"),
        new KeyValuePair<byte?, string>(3, "صالة عرض"),
        new KeyValuePair<byte?, string>(4, "تالف"),
    };

    public byte? SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            if (SetProperty(ref _selectedTypeFilter, value))
            {
                WarehousesView?.Refresh();
            }
        }
    }
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand RestoreCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    #endregion

    #region Methods
    /// <summary>
    /// Public method for loading warehouses (used by RefreshCommand and tests).
    /// </summary>
    public Task LoadWarehousesAsync() => ExecuteAsync(LoadWarehousesOperationAsync);

    private async Task LoadWarehousesOperationAsync()
    {
        ErrorMessage = null;

        var result = await WarehouseService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(async () =>
            {
                Warehouses.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Warehouses.Add(item);
                }
                try
                {
                    SetupCollectionView();
                }
                catch (InvalidOperationException)
                {
                    // WPF CollectionView requires a running Dispatcher — silently skip in non-WPF contexts (e.g., tests)
                    WarehousesView = null;
                }
                IsEmpty = Warehouses.Count == 0;
                OnPropertyChanged(nameof(WarehousesCount));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المستودعات", "WarehouseListViewModel.LoadWarehousesAsync", "[WarehouseListViewModel.LoadWarehousesAsync] Failed to load warehouses list.");
            IsEmpty = Warehouses.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        WarehousesView = CollectionViewSource.GetDefaultView(Warehouses);
        WarehousesView.Filter = FilterWarehouses;
    }

    private bool FilterWarehouses(object obj)
    {
        if (obj is not WarehouseDto warehouse) return false;

        // Type filter
        if (SelectedTypeFilter.HasValue && warehouse.Type != SelectedTypeFilter.Value)
            return false;

        // Text search
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return warehouse.Name.ToLower().Contains(searchLower) ||
               (warehouse.Location?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddWarehouse()
    {
        var editorVm = new WarehouseEditorViewModel();
        if (DialogService.ShowDialog(editorVm))
        {
            _ = ExecuteAsync(LoadWarehousesOperationAsync);
        }
    }

    private void EditWarehouse()
    {
        if (SelectedWarehouse == null) return;

        var editorVm = new WarehouseEditorViewModel(SelectedWarehouse);
        if (DialogService.ShowDialog(editorVm))
        {
            _ = ExecuteAsync(LoadWarehousesOperationAsync);
        }
    }

    public void EditWarehouseFromDoubleClick()
    {
        if (SelectedWarehouse != null)
        {
            EditWarehouse();
        }
    }

    private async Task DeleteWarehouseOperationAsync()
    {
        if (SelectedWarehouse == null) return;

        var strategy = await DialogService.ShowDeleteConfirmationAsync($"المستودع: {SelectedWarehouse.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var deleteResult = await WarehouseService.DeleteAsync(SelectedWarehouse.Id);
            if (deleteResult.IsSuccess)
            {
                EventBus.Publish(new WarehouseChangedMessage(SelectedWarehouse.Id));
                await LoadWarehousesOperationAsync();
                ToastService.ShowSuccess("تم إلغاء تنشيط المستودع بنجاح");
            }
            else
            {
                ErrorMessage = deleteResult.Error ?? "فشل في إلغاء تنشيط المستودع";
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            var deleteResult = await WarehouseService.DeletePermanentlyAsync(SelectedWarehouse.Id);
            if (deleteResult.IsSuccess)
            {
                EventBus.Publish(new WarehouseChangedMessage(SelectedWarehouse.Id));
                await LoadWarehousesOperationAsync();
                ToastService.ShowSuccess("تم حذف المستودع نهائياً");
            }
            else
            {
                var error = deleteResult.Error ?? "فشل في حذف المستودع";
                ErrorMessage = error;
                LogSystemError($"Hard delete failed for Warehouse {SelectedWarehouse.Id}: {error}", "WarehouseListViewModel.DeleteWarehouseAsync");
            }
        }
    }

    private async Task RestoreWarehouseOperationAsync()
    {
        if (SelectedWarehouse == null) return;

        ErrorMessage = null;

        var request = new UpdateWarehouseRequest(
            Name: SelectedWarehouse.Name,
            Type: SelectedWarehouse.Type,
            Location: SelectedWarehouse.Location,
            Phone: SelectedWarehouse.Phone,
            Address: SelectedWarehouse.Address,
            ManagerName: SelectedWarehouse.ManagerName,
            IsDefault: SelectedWarehouse.IsDefault,
            IsActive: true,
            Notes: SelectedWarehouse.Notes
        );

        var result = await WarehouseService.UpdateAsync(SelectedWarehouse.Id, request);

        if (result.IsSuccess)
        {
            EventBus.Publish(new WarehouseChangedMessage(SelectedWarehouse.Id));
            await LoadWarehousesOperationAsync();
            await DialogService.ShowSuccessAsync("نجاح", "تم استعادة المستودع بنجاح");
        }
        else
        {
            ErrorMessage = result.Error ?? "فشل في استعادة المستودع";
            await DialogService.ShowErrorAsync("خطأ في الاستعادة", ErrorMessage);
        }
    }

    private void Search()
    {
        WarehousesView?.Refresh();
    }

    private void OnWarehouseChanged(WarehouseChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync((Func<Task>)(async () => await ExecuteAsync(LoadWarehousesOperationAsync)));
    }

    public override void Cleanup()
    {
        EventBus.Unsubscribe<WarehouseChangedMessage>(OnWarehouseChanged);
    }
    #endregion
}
