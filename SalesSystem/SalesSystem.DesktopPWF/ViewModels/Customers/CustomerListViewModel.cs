using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Customers;

/// <summary>
/// ViewModel for Customers List View
/// </summary>
public class CustomerListViewModel : ViewModelBase
{
    private readonly ICustomerApiService _customerService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<CustomerDto> _customers = new();
    private ICollectionView? _customersView;
    private CustomerDto? _selectedCustomer;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public CustomerListViewModel()
    {
        _customerService = App.GetService<ICustomerApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();

        InitializeCommands();
    }

    /// <summary>    
    /// Constructor for dependency injection (used in tests)    
    /// </summary>
    public CustomerListViewModel(
        ICustomerApiService customerService,
        IEventBus eventBus,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IToastNotificationService toastService)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadCustomersAsync);
        AddCommand = new RelayCommand(AddCustomer);
        EditCommand = new RelayCommand(EditCustomer, () => SelectedCustomer != null);
        DeleteCommand = new AsyncRelayCommand(DeleteCustomerAsync, () => SelectedCustomer != null && SelectedCustomer.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreCustomerAsync, () => SelectedCustomer != null && !SelectedCustomer.IsActive);
        SearchCommand = new RelayCommand(Search);

        // Subscribe to customer changes
        _eventBus.Subscribe<CustomerChangedMessage>(OnCustomerChanged);
    }

    #region Properties
    public ObservableCollection<CustomerDto> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    public ICollectionView? CustomersView
    {
        get => _customersView;
        private set => SetProperty(ref _customersView, value);
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
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
                CustomersView?.Refresh();
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
                _ = LoadCustomersAsync();
            }
        }
    }

    public int CustomersCount => Customers.Count;
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
    public async Task LoadCustomersAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _customerService.GetAllAsync(IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Customers.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
                    {
                        Customers.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Customers.Count == 0;
                    OnPropertyChanged(nameof(CustomersCount));
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل العملاء", "CustomerListViewModel.LoadCustomersAsync", "[CustomerListViewModel.LoadCustomersAsync] Failed to load customers list.");
                IsEmpty = Customers.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CustomerListViewModel.LoadCustomersAsync", "[CustomerListViewModel.LoadCustomersAsync] Failed to load customers list.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        CustomersView = new ListCollectionView(Customers);
        CustomersView.Filter = FilterCustomers;
    }

    private bool FilterCustomers(object obj)
    {
        if (obj is not CustomerDto customer) return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return customer.Name.ToLower().Contains(searchLower) ||
               (customer.Phone?.ToLower().Contains(searchLower) ?? false) ||
               (customer.Email?.ToLower().Contains(searchLower) ?? false) ||
               (customer.Address?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddCustomer()
    {
        var editorVm = App.GetService<CustomerEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "عميل جديد",
            Width = 900,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCustomersAsync());
            }
        });
    }

    private void EditCustomer()
    {
        if (SelectedCustomer == null) return;

        var editorVm = new CustomerEditorViewModel(SelectedCustomer);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل عميل",
            Width = 900,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadCustomersAsync());
            }
        });
    }

    public void EditCustomerFromDoubleClick()
    {
        if (SelectedCustomer != null)
        {
            EditCustomer();
        }
    }

    public async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"العميل: {SelectedCustomer.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _customerService.DeleteAsync(SelectedCustomer.Id);
                if (deleteResult.IsSuccess)
                {
                    _eventBus.Publish(new CustomerChangedMessage(SelectedCustomer.Id));
                    await LoadCustomersAsync();
                    _toastService.ShowSuccess("تم إلغاء تنشيط العميل بنجاح");
                }
                else
                {
                    ErrorMessage = HandleFailure(deleteResult.Error ?? "فشل في إلغاء تنشيط العميل", "CustomerListViewModel.DeleteCustomerAsync", $"[CustomerListViewModel.DeleteCustomerAsync] Failed to delete customer with ID {SelectedCustomer.Id}.");
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _customerService.DeletePermanentlyAsync(SelectedCustomer.Id);
                if (deleteResult.IsSuccess)
                {
                    _eventBus.Publish(new CustomerChangedMessage(SelectedCustomer.Id));
                    await LoadCustomersAsync();
                    _toastService.ShowSuccess("تم حذف العميل نهائياً");
                }
                else
                {
                    var error = deleteResult.Error ?? "فشل في حذف العميل";
                    ErrorMessage = HandleFailure(error, "CustomerListViewModel.DeleteCustomerAsync", $"[CustomerListViewModel.DeleteCustomerAsync] Failed to delete customer with ID {SelectedCustomer.Id}.");
                    LogSystemError($"Hard delete failed for Customer {SelectedCustomer.Id}: {error}", "CustomerListViewModel.DeleteCustomerAsync");
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CustomerListViewModel.DeleteCustomerAsync", $"[CustomerListViewModel.DeleteCustomerAsync] Failed to delete customer with ID {SelectedCustomer?.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RestoreCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var request = new UpdateCustomerRequest(
                Name: SelectedCustomer.Name,
                Phone: SelectedCustomer.Phone,
                Email: SelectedCustomer.Email,
                Address: SelectedCustomer.Address,
                TaxNumber: SelectedCustomer.TaxNumber,
                CreditLimit: SelectedCustomer.CreditLimit,
                IsActive: true
            );

            var result = await _customerService.UpdateAsync(SelectedCustomer.Id, request);

            if (result.IsSuccess)
            {
                _eventBus.Publish(new CustomerChangedMessage(SelectedCustomer.Id));
                await LoadCustomersAsync();
                await _dialogService.ShowSuccessAsync("استعادة العميل", "تم استعادة العميل بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في استعادة العميل", "CustomerListViewModel.RestoreCustomerAsync", $"[CustomerListViewModel.RestoreCustomerAsync] Failed to restore customer with ID {SelectedCustomer.Id}.");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "CustomerListViewModel.RestoreCustomerAsync", $"[CustomerListViewModel.RestoreCustomerAsync] Failed to restore customer with ID {SelectedCustomer.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Search()
    {
        CustomersView?.Refresh();
    }

    private void OnCustomerChanged(CustomerChangedMessage msg)
    {
        // Reload customers when any change happens (from other modules or this module)
        _ = InvokeOnUIThreadAsync(async () => await LoadCustomersAsync());
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<CustomerChangedMessage>(OnCustomerChanged);
    }
    #endregion
}




