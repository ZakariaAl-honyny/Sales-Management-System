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
/// ViewModel for Sales Returns List View
/// </summary>
public class SalesReturnListViewModel : ViewModelBase
{
    private readonly ISalesReturnApiService _returnService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;

    private ObservableCollection<SalesReturnDto> _returns = new();
    private ICollectionView? _returnsView;
    private SalesReturnDto? _selectedReturn;
    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private bool _isLoading;
    private string? _errorMessage;

    public SalesReturnListViewModel()
    {
        _returnService = App.GetService<ISalesReturnApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadReturnsAsync);
        NewCommand = new RelayCommand(AddNewReturn);
        SearchInvoiceForReturnCommand = new RelayCommand(SearchInvoiceForReturn);
        ViewCommand = new RelayCommand(ViewReturn, () => SelectedReturn != null);

        // Default date range (last 30 days)
        DateFrom = DateTime.Today.AddDays(-30);
        DateTo = DateTime.Today;

        // Subscribe to return changes
        _eventBus.Subscribe<SalesReturnChangedMessage>(OnReturnChanged);
    }


    #region Properties
    public ObservableCollection<SalesReturnDto> Returns
    {
        get => _returns;
        set => SetProperty(ref _returns, value);
    }

    public ICollectionView? ReturnsView
    {
        get => _returnsView;
        private set => SetProperty(ref _returnsView, value);
    }

    public SalesReturnDto? SelectedReturn
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
    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand SearchInvoiceForReturnCommand { get; }
    public ICommand ViewCommand { get; }
    #endregion

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
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل مرتجعات المبيعات", "SalesReturnListViewModel.LoadReturnsAsync", "[SalesReturnListViewModel.LoadReturnsAsync] Failed to load sales returns list.");
                IsEmpty = Returns.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesReturnListViewModel.LoadReturnsAsync", "[SalesReturnListViewModel.LoadReturnsAsync] Failed to load sales returns list.");
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
        if (obj is not SalesReturnDto returnItem) return false;

        // Date filter
        if (DateFrom.HasValue && returnItem.ReturnDate < DateFrom.Value) return false;
        if (DateTo.HasValue && returnItem.ReturnDate > DateTo.Value.AddDays(1)) return false;

        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            return returnItem.ReturnNo.ToLower().Contains(searchLower) ||
                   (returnItem.CustomerName?.ToLower().Contains(searchLower) ?? false);
        }

        return true;
    }

    private void AddNewReturn()
    {
        var editorVm = new SalesReturnEditorViewModel();
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadReturnsAsync();
        }
    }

    private async void SearchInvoiceForReturn()
    {
        var dialogService = App.GetService<IDialogService>();
        var invoiceVm = new SalesInvoiceSelectionViewModel();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            invoiceVm.SearchText = SearchText;
        }

        if (dialogService.ShowDialog(invoiceVm) && invoiceVm.SelectedInvoice != null)
        {
            IsLoading = true;
            try
            {
                var salesInvoiceService = App.GetService<ISalesInvoiceApiService>();
                var fullInvoiceResult = await salesInvoiceService.GetByIdAsync(invoiceVm.SelectedInvoice.Id);
                
                if (fullInvoiceResult.IsSuccess && fullInvoiceResult.Value != null)
                {
                    InvokeOnUIThread(() =>
                    {
                        var editorVm = new SalesReturnEditorViewModel();
                        editorVm.SelectedInvoice = fullInvoiceResult.Value;
                        
                        if (dialogService.ShowDialog(editorVm))
                        {
                            _ = LoadReturnsAsync();
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
                HandleException(ex, "SalesReturnListViewModel.SearchInvoiceForReturn", "[SalesReturnListViewModel.SearchInvoiceForReturn] Error starting sales return from invoice search.");
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
        var editorVm = new SalesReturnEditorViewModel();
        _ = editorVm.LoadReturnAsync(SelectedReturn.Id);
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadReturnsAsync();
        }
    }

    private void UpdateCommandStates()
    {
        (ViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnReturnChanged(SalesReturnChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadReturnsAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<SalesReturnChangedMessage>(OnReturnChanged);
    }
    #endregion
}
