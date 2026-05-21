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
    private readonly IWarehouseApiService _warehouseService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private ObservableCollection<WarehouseDto> _warehouses = new();
    private ICollectionView? _warehousesView;
    private WarehouseDto? _selectedWarehouse;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public WarehouseListViewModel()
    {
        _warehouseService = App.GetService<IWarehouseApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        RefreshCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(LoadWarehousesOperationAsync)));
        AddCommand = new RelayCommand(AddWarehouse);
        EditCommand = new RelayCommand(EditWarehouse, () => SelectedWarehouse != null);
        DeleteCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(DeleteWarehouseOperationAsync, ex => ErrorMessage = HandleException(ex, "WarehouseListViewModel.DeleteWarehouseAsync"))), () => SelectedWarehouse != null && SelectedWarehouse.IsActive);
        RestoreCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(RestoreWarehouseOperationAsync, ex => ErrorMessage = HandleException(ex, "WarehouseListViewModel.RestoreWarehouseAsync"))), () => SelectedWarehouse != null && !SelectedWarehouse.IsActive);
        SearchCommand = new RelayCommand(Search);

        _eventBus.Subscribe<WarehouseChangedMessage>(OnWarehouseChanged);
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
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RestoreCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; }
    #endregion

    #region Methods
    private async Task LoadWarehousesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _warehouseService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(async () =>
            {
                Warehouses.Clear();
                foreach (var item in result.Value)
                {
                    Warehouses.Add(item);
                }
                SetupCollectionView();
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

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return (warehouse.Code?.ToLower().Contains(searchLower) ?? false) ||
               warehouse.Name.ToLower().Contains(searchLower) ||
               (warehouse.Location?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddWarehouse()
    {
        var editorVm = new WarehouseEditorViewModel();
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = ExecuteAsync(LoadWarehousesOperationAsync);
        }
    }

    private void EditWarehouse()
    {
        if (SelectedWarehouse == null) return;

        var editorVm = new WarehouseEditorViewModel(SelectedWarehouse);
        if (_dialogService.ShowDialog(editorVm))
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

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المستودع: {SelectedWarehouse.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var deleteResult = await _warehouseService.DeleteAsync(SelectedWarehouse.Id);
            if (deleteResult.IsSuccess)
            {
                _eventBus.Publish(new WarehouseChangedMessage(SelectedWarehouse.Id));
                await LoadWarehousesOperationAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط المستودع بنجاح");
            }
            else
            {
                ErrorMessage = deleteResult.Error ?? "فشل في إلغاء تنشيط المستودع";
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            var deleteResult = await _warehouseService.DeletePermanentlyAsync(SelectedWarehouse.Id);
            if (deleteResult.IsSuccess)
            {
                _eventBus.Publish(new WarehouseChangedMessage(SelectedWarehouse.Id));
                await LoadWarehousesOperationAsync();
                _toastService.ShowSuccess("تم حذف المستودع نهائياً");
            }
            else
            {
                ErrorMessage = deleteResult.Error ?? "فشل في حذف المستودع";
            }
        }
    }

    private async Task RestoreWarehouseOperationAsync()
    {
        if (SelectedWarehouse == null) return;

        ErrorMessage = null;

        var request = new UpdateWarehouseRequest(
            Name: SelectedWarehouse.Name,
            Code: SelectedWarehouse.Code,
            Location: SelectedWarehouse.Location,
            IsDefault: SelectedWarehouse.IsDefault,
            IsActive: true
        );

        var result = await _warehouseService.UpdateAsync(SelectedWarehouse.Id, request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new WarehouseChangedMessage(SelectedWarehouse.Id));
            await LoadWarehousesOperationAsync();
            await _dialogService.ShowSuccessAsync("نجاح", "تم استعادة المستودع بنجاح");
        }
        else
        {
            ErrorMessage = result.Error ?? "فشل في استعادة المستودع";
            await _dialogService.ShowErrorAsync("خطأ في الاستعادة", ErrorMessage);
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
        _eventBus.Unsubscribe<WarehouseChangedMessage>(OnWarehouseChanged);
    }
    #endregion
}
