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
    private bool _isLoading;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public WarehouseListViewModel()
    {
        _warehouseService = App.GetService<IWarehouseApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadWarehousesAsync);
        AddCommand = new RelayCommand(AddWarehouse);
        EditCommand = new RelayCommand(EditWarehouse, () => SelectedWarehouse != null);
        DeleteCommand = new AsyncRelayCommand(DeleteWarehouseAsync, () => SelectedWarehouse != null && SelectedWarehouse.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreWarehouseAsync, () => SelectedWarehouse != null && !SelectedWarehouse.IsActive);
        SearchCommand = new RelayCommand(Search);

        // Subscribe to warehouse changes
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
                // Update command's CanExecute
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

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
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
                _ = LoadWarehousesAsync();
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
    public async Task LoadWarehousesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _warehouseService.GetAllAsync(IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
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
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "WarehouseListViewModel.LoadWarehousesAsync", "[WarehouseListViewModel.LoadWarehousesAsync] Failed to load warehouses list.");
        }
        finally
        {
            IsLoading = false;
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
            // Warehouse was saved successfully - refresh list
            _ = LoadWarehousesAsync();
        }
    }

    private void EditWarehouse()
    {
        if (SelectedWarehouse == null) return;

        var editorVm = new WarehouseEditorViewModel(SelectedWarehouse);
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadWarehousesAsync();
        }
    }

    public void EditWarehouseFromDoubleClick()
    {
        if (SelectedWarehouse != null)
        {
            EditWarehouse();
        }
    }

private async Task DeleteWarehouseAsync()
    {
        if (SelectedWarehouse == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المستودع: {SelectedWarehouse.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _warehouseService.DeleteAsync(SelectedWarehouse.Id);
                if (deleteResult.IsSuccess)
                {
                    _eventBus.Publish(new WarehouseChangedMessage(SelectedWarehouse.Id));
                    await LoadWarehousesAsync();
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
                    await LoadWarehousesAsync();
                    _toastService.ShowSuccess("تم حذف المستودع نهائياً");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "فشل في حذف المستودع";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
            HandleException(ex, "WarehouseListViewModel.DeleteWarehouseAsync", $"[WarehouseListViewModel.DeleteWarehouseAsync] Failed to delete warehouse with ID {SelectedWarehouse?.Id}.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RestoreWarehouseAsync()
    {
        if (SelectedWarehouse == null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
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
                await LoadWarehousesAsync();
                await _dialogService.ShowSuccessAsync("نجاح", "تم استعادة المستودع بنجاح");
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في استعادة المستودع";
                await _dialogService.ShowErrorAsync("خطأ في الاستعادة", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
            HandleException(ex, "WarehouseListViewModel.RestoreWarehouseAsync", $"[WarehouseListViewModel.RestoreWarehouseAsync] Failed to restore warehouse with ID {SelectedWarehouse?.Id}.");
        }
finally
        {
            IsLoading = false;
        }
    }

    private void Search()
    {
        WarehousesView?.Refresh();
    }

    private void OnWarehouseChanged(WarehouseChangedMessage msg)
    {
        // Reload warehouses when any change happens (from other modules or this module)
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadWarehousesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<WarehouseChangedMessage>(OnWarehouseChanged);
    }
    #endregion
}
