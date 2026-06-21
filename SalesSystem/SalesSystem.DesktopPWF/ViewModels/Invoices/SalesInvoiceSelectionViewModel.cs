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

public class SalesInvoiceSelectionViewModel : ViewModelBase
{
    private readonly ISalesInvoiceApiService _invoiceService;
    private ObservableCollection<SalesInvoiceDto> _invoices = new();
    private ICollectionView? _invoicesView;
    private SalesInvoiceDto? _selectedInvoice;
    private string _searchText = string.Empty;

    public SalesInvoiceSelectionViewModel()
    {
        _invoiceService = App.GetService<ISalesInvoiceApiService>();
        
        SelectCommand = new RelayCommand(Select, () => SelectedInvoice != null);
        CancelCommand = new RelayCommand(Cancel);
        SearchCommand = new RelayCommand(Search);

        _ = LoadInvoicesAsync();
    }

    public ObservableCollection<SalesInvoiceDto> Invoices
    {
        get => _invoices;
        set => SetProperty(ref _invoices, value);
    }

    public ICollectionView? InvoicesView
    {
        get => _invoicesView;
        private set => SetProperty(ref _invoicesView, value);
    }

    public SalesInvoiceDto? SelectedInvoice
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


    public ICommand SelectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SearchCommand { get; }

    private async Task LoadInvoicesAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _invoiceService.GetAllAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                status: 2 // Only Posted invoices
            );

            if (result.IsSuccess && result.Value != null)
            {
                Invoices = new ObservableCollection<SalesInvoiceDto>(result.Value);
                InvoicesView = CollectionViewSource.GetDefaultView(Invoices);
                
                // If there's only one result and we had search text, maybe we should auto-select?
                // But usually better to let the caller handle auto-select if they want.
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل فواتير البيع", "SalesInvoiceSelectionViewModel.LoadInvoicesAsync", ex);
        }
        finally
        {
            IsBusy = false;
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




