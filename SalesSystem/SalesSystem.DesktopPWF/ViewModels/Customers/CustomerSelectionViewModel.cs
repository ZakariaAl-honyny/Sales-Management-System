using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Linq;
using System.Threading.Tasks;

namespace SalesSystem.DesktopPWF.ViewModels.Customers;

public class CustomerSelectionViewModel : ViewModelBase
{
    private readonly ICustomerApiService _customerService;
    private ObservableCollection<CustomerDto> _customers = new();
    private ICollectionView? _customersView;
    private CustomerDto? _selectedCustomer;
    private string _searchText = string.Empty;
    private bool _isLoading;

    public CustomerSelectionViewModel()
    {
        _customerService = App.GetService<ICustomerApiService>();
        
        SelectCommand = new RelayCommand(Select, () => SelectedCustomer != null);
        CancelCommand = new RelayCommand(Cancel);
        SearchCommand = new RelayCommand(Search);
        ConfirmSelectionCommand = new RelayCommand(() => Select());

        _ = LoadCustomersAsync();
    }

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
                (SelectCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand SelectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ConfirmSelectionCommand { get; }

    private async Task LoadCustomersAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _customerService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                Customers = new ObservableCollection<CustomerDto>(result.Value.Where(c => c.IsActive));
                CustomersView = CollectionViewSource.GetDefaultView(Customers);
                CustomersView.Filter = c => 
                {
                    if (c is not CustomerDto customer) return false;
                    if (string.IsNullOrWhiteSpace(SearchText)) return true;
                    var search = SearchText.ToLower();
                    return customer.Name.ToLower().Contains(search) || 
                           (customer.Phone?.ToLower().Contains(search) ?? false);
                };
            }
        }
        catch
        {
            // Log error
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Search()
    {
        CustomersView?.Refresh();
    }

    private void Select()
    {
        if (SelectedCustomer != null)
        {
            // DialogResult = true signals the dialog closed with a selection
            _dialogResult = true;
            RequestClose();
        }
    }

    // Used by IDialogService to know the result
    private bool _dialogResult;
    public bool DialogResult => _dialogResult;

    private void Cancel()
    {
        SelectedCustomer = null;
        RequestClose();
    }
}
