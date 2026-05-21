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
    private bool _isLoading;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public SupplierListViewModel()
    {
        _supplierService = App.GetService<ISupplierApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadSuppliersAsync);
        AddCommand = new RelayCommand(AddSupplier);
        EditCommand = new RelayCommand(EditSupplier, () => SelectedSupplier != null);
        DeleteCommand = new AsyncRelayCommand(DeleteSupplierAsync, () => SelectedSupplier != null && SelectedSupplier.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreSupplierAsync, () => SelectedSupplier != null && !SelectedSupplier.IsActive);
        SearchCommand = new RelayCommand(Search);

        // Subscribe to supplier changes
        _eventBus.Subscribe<SupplierChangedMessage>(OnSupplierChanged);
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
                _ = LoadSuppliersAsync();
            }
        }
    }

    public int SuppliersCount => SuppliersView?.Cast<object>().Count() ?? 0;
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
    public async Task LoadSuppliersAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _supplierService.GetAllAsync(IncludeInactive);
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Suppliers.Clear();
                    foreach (var item in result.Value)
                    {
                        Suppliers.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = !Suppliers.Any();
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل تحميل الموردين", "SupplierListViewModel.LoadSuppliersAsync");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SupplierListViewModel.LoadSuppliersAsync", "Failed to load suppliers.");
        }
        finally
        {
            IsLoading = false;
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
        return (supplier.Code?.ToLower().Contains(searchLower) ?? false) ||
               supplier.Name.ToLower().Contains(searchLower) ||
               (supplier.Phone?.ToLower().Contains(searchLower) ?? false) ||
               (supplier.Email?.ToLower().Contains(searchLower) ?? false) ||
               (supplier.Address?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddSupplier()
    {
        _dialogService.ShowDialog(new SupplierEditorViewModel());
    }

    private void EditSupplier()
    {
        if (SelectedSupplier != null)
        {
            _dialogService.ShowDialog(new SupplierEditorViewModel(SelectedSupplier));
        }
    }

    private async Task DeleteSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المورد: {SelectedSupplier.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsLoading = true;
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
                    _toastService.ShowSuccess("تم إلغاء تنشيط المورد بنجاح");
                }
                else
                {
                    ErrorMessage = HandleFailure(deleteResult.Error ?? "فشل في إلغاء تنشيط المورد", "SupplierListViewModel.DeleteSupplierAsync");
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _supplierService.DeletePermanentlyAsync(SelectedSupplier.Id);
                if (deleteResult.IsSuccess)
                {
                    _eventBus.Publish(new SupplierChangedMessage(SelectedSupplier.Id));
                    await LoadSuppliersAsync();
                    _toastService.ShowSuccess("تم حذف المورد نهائياً");
                }
                else
                {
                    ErrorMessage = HandleFailure(deleteResult.Error ?? "فشل في حذف المورد", "SupplierListViewModel.DeleteSupplierAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SupplierListViewModel.DeleteSupplierAsync", $"[SupplierListViewModel.DeleteSupplierAsync] Failed to delete supplier with ID {SelectedSupplier?.Id}.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RestoreSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var request = new UpdateSupplierRequest(
                Name: SelectedSupplier.Name,
                Code: SelectedSupplier.Code,
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
                System.Windows.MessageBox.Show("تم استعادة المورد بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في استعادة المورد";
                System.Windows.MessageBox.Show(ErrorMessage, "خطأ في الاستعادة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SupplierListViewModel.RestoreSupplierAsync", "Failed to restore supplier.");
        }
        finally
        {
            IsLoading = false;
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
