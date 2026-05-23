using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Linq;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.ViewModels.Suppliers;

/// <summary>
/// ViewModel for Suppliers List View
/// </summary>
public class SupplierListViewModel : ViewModelBase
{
    private readonly ISupplierApiService _supplierService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<SupplierDto> _suppliers = new();
    private ICollectionView? _suppliersView;
    private SupplierDto? _selectedSupplier;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public SupplierListViewModel()
    {
        _supplierService = App.GetService<ISupplierApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    public SupplierListViewModel(
        ISupplierApiService supplierService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService toastService)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadSuppliersAsync);
        AddCommand = new RelayCommand(AddSupplier);
        EditCommand = new RelayCommand(EditSupplier, () => SelectedSupplier != null);
        DeleteCommand = new AsyncRelayCommand(DeleteSupplierAsync, () => SelectedSupplier != null && SelectedSupplier.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreSupplierAsync, () => SelectedSupplier != null && !SelectedSupplier.IsActive);
        SearchCommand = new RelayCommand(Search);

        _eventBus.Subscribe<SupplierChangedMessage>(OnSupplierChanged);
    }

    #region Properties
    public ObservableCollection<SupplierDto> Suppliers
    {
        get => _suppliers;
        set => SetProperty(ref _suppliers, value);
    }

    public ICollectionView? SuppliersView
    {
        get => _suppliersView;
        private set => SetProperty(ref _suppliersView, value);
    }

    public SupplierDto? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
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
                SuppliersView?.Refresh();
                OnPropertyChanged(nameof(SuppliersCount));
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
                _ = LoadSuppliersAsync();
            }
        }
    }

    public int SuppliersCount => SuppliersView?.Cast<object>().Count() ?? 0;
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
    public async Task LoadSuppliersAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _supplierService.GetAllAsync(IncludeInactive);
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Suppliers.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
                    {
                        Suppliers.Add(item);
                    }
                    try
                    {
                        SetupCollectionView();
                    }
                    catch (InvalidOperationException)
                    {
                        // WPF ListCollectionView requires a running Dispatcher â€” skip in non-WPF contexts
                        SuppliersView = null;
                    }
                    IsEmpty = !Suppliers.Any();
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ طھط­ظ…ظٹظ„ ط§ظ„ظ…ظˆط±ط¯ظٹظ†", "SupplierListViewModel.LoadSuppliersAsync");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SupplierListViewModel.LoadSuppliersAsync", "Failed to load suppliers.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        SuppliersView = new ListCollectionView(Suppliers);
        SuppliersView.Filter = FilterSuppliers;
        OnPropertyChanged(nameof(SuppliersCount));
    }

    private bool FilterSuppliers(object obj)
    {
        if (obj is not SupplierDto supplier) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return supplier.Name.ToLower().Contains(searchLower) ||
               (supplier.Phone?.ToLower().Contains(searchLower) ?? false) ||
               (supplier.Email?.ToLower().Contains(searchLower) ?? false) ||
               (supplier.Address?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddSupplier()
    {
        var editorVm = new SupplierEditorViewModel();
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadSuppliersAsync();
        }
    }

    private void EditSupplier()
    {
        if (SelectedSupplier == null) return;

        var editorVm = new SupplierEditorViewModel(SelectedSupplier);
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadSuppliersAsync();
        }
    }

    private async Task DeleteSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"ط§ظ„ظ…ظˆط±ط¯: {SelectedSupplier.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _supplierService.DeleteAsync(SelectedSupplier.Id);
                if (deleteResult.IsSuccess)
                {
                    _eventBus.Publish(new SupplierChangedMessage(SelectedSupplier.Id));
                    await LoadSuppliersAsync();
                    _toastService.ShowSuccess("طھظ… ط¥ظ„ط؛ط§ط، طھظ†ط´ظٹط· ط§ظ„ظ…ظˆط±ط¯ ط¨ظ†ط¬ط§ط­");
                }
                else
                {
                    ErrorMessage = HandleFailure(deleteResult.Error ?? "ظپط´ظ„ ظپظٹ ط¥ظ„ط؛ط§ط، طھظ†ط´ظٹط· ط§ظ„ظ…ظˆط±ط¯", "SupplierListViewModel.DeleteSupplierAsync");
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _supplierService.DeletePermanentlyAsync(SelectedSupplier.Id);
                if (deleteResult.IsSuccess)
                {
                    _eventBus.Publish(new SupplierChangedMessage(SelectedSupplier.Id));
                    await LoadSuppliersAsync();
                    _toastService.ShowSuccess("طھظ… ط­ط°ظپ ط§ظ„ظ…ظˆط±ط¯ ظ†ظ‡ط§ط¦ظٹط§ظ‹");
                }
                else
                {
                    var error = deleteResult.Error ?? "ظپط´ظ„ ظپظٹ ط­ط°ظپ ط§ظ„ظ…ظˆط±ط¯";
                    ErrorMessage = HandleFailure(error, "SupplierListViewModel.DeleteSupplierAsync");
                    LogSystemError($"Hard delete failed for Supplier {SelectedSupplier.Id}: {error}", "SupplierListViewModel.DeleteSupplierAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SupplierListViewModel.DeleteSupplierAsync", $"[SupplierListViewModel.DeleteSupplierAsync] Failed to delete supplier with ID {SelectedSupplier?.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new UpdateSupplierRequest(
                Name: SelectedSupplier.Name,
                Phone: SelectedSupplier.Phone,
                Email: SelectedSupplier.Email,
                Address: SelectedSupplier.Address,
                TaxNumber: SelectedSupplier.TaxNumber,
                CreditLimit: SelectedSupplier.CreditLimit,
                IsActive: true
            );

            var result = await _supplierService.UpdateAsync(SelectedSupplier.Id, request);

            if (result.IsSuccess)
            {
                _eventBus.Publish(new SupplierChangedMessage(SelectedSupplier.Id));
                await LoadSuppliersAsync();
                await _dialogService.ShowSuccessAsync("ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ…ظˆط±ط¯", "طھظ… ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ…ظˆط±ط¯ ط¨ظ†ط¬ط§ط­");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ ط§ط³طھط¹ط§ط¯ط© ط§ظ„ظ…ظˆط±ط¯", "SupplierListViewModel.RestoreSupplierAsync", $"[SupplierListViewModel.RestoreSupplierAsync] Failed to restore supplier with ID {SelectedSupplier.Id}.");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SupplierListViewModel.RestoreSupplierAsync", "Failed to restore supplier.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void EditSupplierFromDoubleClick()
    {
        EditSupplier();
    }

    private void Search()
    {
        SuppliersView?.Refresh();
        OnPropertyChanged(nameof(SuppliersCount));
    }

    private void OnSupplierChanged(SupplierChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadSuppliersAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<SupplierChangedMessage>(OnSupplierChanged);
    }
    #endregion
}




