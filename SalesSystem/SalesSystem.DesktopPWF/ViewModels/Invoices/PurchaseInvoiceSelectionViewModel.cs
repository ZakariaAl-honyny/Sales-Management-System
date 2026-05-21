using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Linq;
using System.Threading.Tasks;

namespace SalesSystem.DesktopPWF.ViewModels.Invoices;

public class PurchaseInvoiceSelectionViewModel : ViewModelBase
{
    private readonly IPurchaseInvoiceApiService _invoiceService;
    private ObservableCollection<PurchaseInvoiceDto> _invoices = new();
    private ICollectionView? _invoicesView;
    private PurchaseInvoiceDto? _selectedInvoice;
    private string _searchText = string.Empty;
    private bool _isLoading;

    public PurchaseInvoiceSelectionViewModel()
    {
        _invoiceService = App.GetService<IPurchaseInvoiceApiService>();
        
        SelectCommand = new RelayCommand(Select, () => SelectedInvoice != null);
        CancelCommand = new RelayCommand(Cancel);
        SearchCommand = new RelayCommand(Search);

        _ = LoadInvoicesAsync();
    }

    public ObservableCollection<PurchaseInvoiceDto> Invoices
    {
        get => _invoices;
        set => SetProperty(ref _invoices, value);
    }

    public ICollectionView? InvoicesView
    {
        get => _invoicesView;
        private set => SetProperty(ref _invoicesView, value);
    }

    public PurchaseInvoiceDto? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
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
                InvoicesView?.Refresh();
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

    private async Task LoadInvoicesAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _invoiceService.GetAllAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                status: 2 // Only Posted invoices
            );

            if (result.IsSuccess && result.Value != null)
            {
                Invoices = new ObservableCollection<PurchaseInvoiceDto>(result.Value);
                InvoicesView = CollectionViewSource.GetDefaultView(Invoices);
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

    private async void Search()
    {
        await LoadInvoicesAsync();
    }

    private void Select()
    {
        if (SelectedInvoice != null)
        {
            _dialogResult = true;
            RequestClose();
        }
    }

    private bool _dialogResult;
    public bool DialogResult => _dialogResult;

    private void Cancel()
    {
        SelectedInvoice = null;
        RequestClose();
    }
}
