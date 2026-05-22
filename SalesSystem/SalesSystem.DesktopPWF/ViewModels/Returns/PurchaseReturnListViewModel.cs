using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Views.Returns;
using SalesSystem.DesktopPWF.ViewModels.Invoices;

namespace SalesSystem.DesktopPWF.ViewModels.Returns;

/// <summary>
/// ViewModel for Purchase Returns List View
/// </summary>
public class PurchaseReturnListViewModel : ViewModelBase
{
    private readonly IPurchaseReturnApiService _returnService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<PurchaseReturnDto> _returns = new();
    private ICollectionView? _returnsView;
    private PurchaseReturnDto? _selectedReturn;
    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private bool _isLoading;
    private string? _errorMessage;

    public PurchaseReturnListViewModel()
    {
        _returnService = App.GetService<IPurchaseReturnApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _screenWindowService = App.GetService<IScreenWindowService>();

        InitializeCommands();

        // Default date range (last 30 days)
        DateFrom = DateTime.Today.AddDays(-30);
        DateTo = DateTime.Today;

        // Subscribe to return changes
        _eventBus.Subscribe<PurchaseReturnChangedMessage>(OnReturnChanged);
    }

    #region Properties
    public ObservableCollection<PurchaseReturnDto> Returns
    {
        get => _returns;
        set => SetProperty(ref _returns, value);
    }

    public ICollectionView? ReturnsView
    {
        get => _returnsView;
        private set => SetProperty(ref _returnsView, value);
    }

    public PurchaseReturnDto? SelectedReturn
    {
        get => _selectedReturn;
        set
        {
            if (SetProperty(ref _selectedReturn, value))
            {
                UpdateCommandStates();
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
                ReturnsView?.Refresh();
            }
        }
    }

    public DateTime? DateFrom
    {
        get => _dateFrom;
        set
        {
            if (SetProperty(ref _dateFrom, value))
            {
                ReturnsView?.Refresh();
            }
        }
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set
        {
            if (SetProperty(ref _dateTo, value))
            {
                ReturnsView?.Refresh();
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

    private bool _includeInactive;
    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = LoadReturnsAsync();
            }
        }
    }

    private bool _isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand NewCommand { get; private set; } = null!;
    public ICommand SearchInvoiceForReturnCommand { get; private set; } = null!;
    public ICommand ViewCommand { get; private set; } = null!;
    #endregion

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadReturnsAsync);
        NewCommand = new RelayCommand(AddNewReturn);
        SearchInvoiceForReturnCommand = new RelayCommand(SearchInvoiceForReturn);
        ViewCommand = new RelayCommand(ViewReturn, () => SelectedReturn != null);
    }

    #region Methods
    public async Task LoadReturnsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _returnService.GetAllAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                from: DateFrom,
                to: DateTo,
                includeInactive: IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Returns.Clear();
                    foreach (var item in result.Value)
                    {
                        Returns.Add(item);
                    }
                    IsEmpty = Returns.Count == 0;
                    SetupCollectionView();
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل مرتجعات المشتريات", "PurchaseReturnListViewModel.LoadReturnsAsync", "[PurchaseReturnListViewModel.LoadReturnsAsync] Failed to load purchase returns list.");
                IsEmpty = Returns.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "PurchaseReturnListViewModel.LoadReturnsAsync", "[PurchaseReturnListViewModel.LoadReturnsAsync] Failed to load purchase returns list.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetupCollectionView()
    {
        ReturnsView = CollectionViewSource.GetDefaultView(Returns);
        ReturnsView.Filter = FilterReturns;
    }

    private bool FilterReturns(object obj)
    {
        if (obj is not PurchaseReturnDto returnItem) return false;

        // Date filter
        if (DateFrom.HasValue && returnItem.ReturnDate < DateFrom.Value) return false;
        if (DateTo.HasValue && returnItem.ReturnDate > DateTo.Value.AddDays(1)) return false;

        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            return returnItem.ReturnNo.ToLower().Contains(searchLower) ||
                   returnItem.SupplierName.ToLower().Contains(searchLower);
        }

        return true;
    }

    private void AddNewReturn()
    {
        var editorVm = App.GetService<PurchaseReturnEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "مرتجع مشتريات جديد",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadReturnsAsync());
            }
        });
    }

    private async void SearchInvoiceForReturn()
    {
        var dialogService = App.GetService<IDialogService>();
        var invoiceVm = new PurchaseInvoiceSelectionViewModel();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            invoiceVm.SearchText = SearchText;
        }

        if (dialogService.ShowDialog(invoiceVm) && invoiceVm.SelectedInvoice != null)
        {
            IsLoading = true;
            try
            {
                var invoiceService = App.GetService<IPurchaseInvoiceApiService>();
                var fullInvoiceResult = await invoiceService.GetByIdAsync(invoiceVm.SelectedInvoice.Id);
                
                if (fullInvoiceResult.IsSuccess && fullInvoiceResult.Value != null)
                {
                    var editorVm = new PurchaseReturnEditorViewModel();
                    editorVm.SelectedInvoice = fullInvoiceResult.Value;
                    
                    _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
                    {
                        Title = "مرتجع مشتريات من فاتورة",
                        OnClosed = (vm) =>
                        {
                            System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadReturnsAsync());
                        }
                    });
                }
                else
                {
                    InvokeOnUIThread(() =>
                    {
                        dialogService.ShowError(fullInvoiceResult.Error ?? "فشل في تحميل تفاصيل الفاتورة");
                    });
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "PurchaseReturnListViewModel.SearchInvoiceForReturn", "[PurchaseReturnListViewModel.SearchInvoiceForReturn] Error starting purchase return from invoice search.");
            }
            finally
            {
                InvokeOnUIThread(() =>
                {
                    IsLoading = false;
                });
            }
        }
    }

    private void ViewReturn()
    {
        if (SelectedReturn == null) return;
        var editorVm = new PurchaseReturnEditorViewModel();
        _ = editorVm.LoadReturnAsync(SelectedReturn.Id);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "عرض مرتجع مشتريات",
            OnClosed = (vm) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadReturnsAsync());
            }
        });
    }

    private void UpdateCommandStates()
    {
        (ViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnReturnChanged(PurchaseReturnChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadReturnsAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<PurchaseReturnChangedMessage>(OnReturnChanged);
    }
    #endregion
}
