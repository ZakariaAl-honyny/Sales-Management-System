using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using System.Linq;
using System.Threading.Tasks;

namespace SalesSystem.DesktopPWF.ViewModels.Suppliers;

public class SupplierSelectionViewModel : ViewModelBase
{
    private readonly ISupplierApiService _supplierService;
    private ObservableCollection<SupplierDto> _suppliers = new();
    private ICollectionView? _suppliersView;
    private SupplierDto? _selectedSupplier;
    private string _searchText = string.Empty;

    public SupplierSelectionViewModel()
    {
        _supplierService = App.GetService<ISupplierApiService>();
        
        SelectCommand = new RelayCommand(Select, () => SelectedSupplier != null);
        CancelCommand = new RelayCommand(Cancel);
        SearchCommand = new RelayCommand(Search);

        _ = LoadSuppliersAsync();
    }

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
                SuppliersView?.Refresh();
            }
        }
    }


    public ICommand SelectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SearchCommand { get; }

    private async Task LoadSuppliersAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _supplierService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                Suppliers = new ObservableCollection<SupplierDto>(result.Value.Where(s => s.IsActive));
                SuppliersView = CollectionViewSource.GetDefaultView(Suppliers);
                SuppliersView.Filter = s => 
                {
                    if (s is not SupplierDto supplier) return false;
                    if (string.IsNullOrWhiteSpace(SearchText)) return true;
                    var search = SearchText.ToLower();
                    return supplier.Name.ToLower().Contains(search) || 
                           (supplier.Phone?.ToLower().Contains(search) ?? false);
                };
            }
        }
        catch
        {
            // Log error
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Search()
    {
        SuppliersView?.Refresh();
    }

    private void Select()
    {
        if (SelectedSupplier != null)
        {
            _dialogResult = true;
            RequestClose();
        }
    }

    private bool _dialogResult;
    public bool DialogResult => _dialogResult;

    private void Cancel()
    {
        SelectedSupplier = null;
        RequestClose();
    }
}




