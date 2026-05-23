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
using SalesSystem.DesktopPWF.Views.Purchases;

namespace SalesSystem.DesktopPWF.ViewModels.Purchases;

/// <summary>
/// ViewModel for Purchase Invoice List View
/// </summary>
public class PurchaseInvoiceListViewModel : ViewModelBase
{
    private readonly IPurchaseInvoiceApiService _invoiceService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IPrintApiService _printService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<PurchaseInvoiceDto> _invoices = new();
    private ICollectionView? _invoicesView;
    private PurchaseInvoiceDto? _selectedInvoice;
    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private int? _statusFilter;
    private string? _errorMessage;

    public PurchaseInvoiceListViewModel()
        : this(
            App.GetService<IPurchaseInvoiceApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            App.GetService<IPrintApiService>(),
            App.GetService<IScreenWindowService>())
    {
    }

    public PurchaseInvoiceListViewModel(
        IPurchaseInvoiceApiService invoiceService,
        IEventBus eventBus,
        IDialogService dialogService,
        IPrintApiService printService,
        IScreenWindowService screenWindowService)
    {
        _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _printService = printService ?? throw new ArgumentNullException(nameof(printService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadInvoicesAsync);
        SearchCommand = new AsyncRelayCommand(LoadInvoicesAsync);
        NewCommand = new RelayCommand(AddNewInvoice);
        ViewCommand = new RelayCommand(ViewInvoice, () => SelectedInvoice != null);
        EditCommand = new RelayCommand(EditInvoice, () => SelectedInvoice != null && SelectedInvoice.Status == (byte)InvoiceStatus.Draft);
        PostCommand = new AsyncRelayCommand(PostInvoiceAsync, () => SelectedInvoice != null && SelectedInvoice.Status == (byte)InvoiceStatus.Draft);
        CancelCommand = new AsyncRelayCommand(CancelInvoiceAsync, () => SelectedInvoice != null && SelectedInvoice.Status == (byte)InvoiceStatus.Posted);
        PrintPreviewCommand = new AsyncRelayCommand(PrintPreviewAsync, () => IsPrintAllowed());
        PrintA4Command = new AsyncRelayCommand(PrintA4Async, () => IsPrintAllowed());
        PrintThermalCommand = new AsyncRelayCommand(PrintThermalAsync, () => IsPrintAllowed());

        // Default date range (last 30 days)
        DateFrom = DateTime.Today.AddDays(-30);
        DateTo = DateTime.Today;

        // Subscribe to invoice changes
        _eventBus.Subscribe<PurchaseInvoiceChangedMessage>(OnInvoiceChanged);
    }

    #region Properties
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
                InvoicesView?.Refresh();
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
                InvoicesView?.Refresh();
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
                InvoicesView?.Refresh();
            }
        }
    }

    public int? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                InvoicesView?.Refresh();
            }
        }
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
                _ = LoadInvoicesAsync();
            }
        }
    }

    private bool _isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    // Status options for ComboBox
    public List<PurchaseStatusItem> StatusOptions { get; } = new()
    {
        new PurchaseStatusItem { Value = null, Display = "الكل" },
        new PurchaseStatusItem { Value = (int)InvoiceStatus.Draft, Display = "مسودة" },
        new PurchaseStatusItem { Value = (int)InvoiceStatus.Posted, Display = "مرحلة" },
        new PurchaseStatusItem { Value = (int)InvoiceStatus.Cancelled, Display = "ملغاة" }
    };
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand ViewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand PostCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand PrintPreviewCommand { get; }
    public ICommand PrintA4Command { get; }
    public ICommand PrintThermalCommand { get; }
    #endregion

    #region Methods
    public async Task LoadInvoicesAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _invoiceService.GetAllAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                from: DateFrom,
                to: DateTo,
                status: StatusFilter.HasValue ? (byte)StatusFilter.Value : null,
                includeInactive: IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Invoices.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.InvoiceDate))
                    {
                        Invoices.Add(item);
                    }
                    IsEmpty = Invoices.Count == 0;
                    SetupCollectionView();
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ طھط­ظ…ظٹظ„ ظپظˆط§طھظٹط± ط§ظ„ط´ط±ط§ط،", "PurchaseInvoiceListViewModel.LoadInvoicesAsync", "[PurchaseInvoiceListViewModel.LoadInvoicesAsync] Failed to load purchase invoices list.");
                IsEmpty = Invoices.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "PurchaseInvoiceListViewModel.LoadInvoicesAsync", "[PurchaseInvoiceListViewModel.LoadInvoicesAsync] Failed to load purchase invoices list.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetupCollectionView()
    {
        InvoicesView = new ListCollectionView(Invoices);
        InvoicesView.Filter = FilterInvoices;
    }

    private bool FilterInvoices(object obj)
    {
        if (obj is not PurchaseInvoiceDto invoice) return false;

        // Date filter
        if (DateFrom.HasValue && invoice.InvoiceDate < DateFrom.Value) return false;
        if (DateTo.HasValue && invoice.InvoiceDate > DateTo.Value.AddDays(1)) return false;

        // Status filter
        if (StatusFilter.HasValue && invoice.Status != StatusFilter.Value) return false;

        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            return invoice.Id.ToString().ToLower().Contains(searchLower) ||
                   (invoice.SupplierName?.ToLower().Contains(searchLower) ?? false);
        }

        return true;
    }

    private void AddNewInvoice()
    {
        var editorVm = App.GetService<PurchaseInvoiceEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "ظپط§طھظˆط±ط© ط´ط±ط§ط، ط¬ط¯ظٹط¯ط©",
            OnClosed = (vm) =>
            {
                if (vm is PurchaseInvoiceEditorViewModel editor && editor.InvoiceId.HasValue)
                {
                    _eventBus.Publish(new PurchaseInvoiceChangedMessage(editor.InvoiceId.Value));
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadInvoicesAsync());
                }
            }
        });
    }

    private void ViewInvoice()
    {
        if (SelectedInvoice == null) return;

        var editorVm = new PurchaseInvoiceEditorViewModel(SelectedInvoice.Id, isReadOnly: true);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "ط¹ط±ط¶ ظپط§طھظˆط±ط© ط´ط±ط§ط،"
        });
    }

    private void EditInvoice()
    {
        if (SelectedInvoice == null) return;

        var editorVm = new PurchaseInvoiceEditorViewModel(SelectedInvoice.Id);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "طھط¹ط¯ظٹظ„ ظپط§طھظˆط±ط© ط´ط±ط§ط،",
            OnClosed = (vm) =>
            {
                _eventBus.Publish(new PurchaseInvoiceChangedMessage(SelectedInvoice.Id));
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadInvoicesAsync());
            }
        });
    }

    private async Task PostInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        var result = await _dialogService.ShowConfirmationAsync("طھط£ظƒظٹط¯ ط§ظ„طھط±ط­ظٹظ„", $"ظ‡ظ„ ط£ظ†طھ ظ…طھط£ظƒط¯ ظ…ظ† طھط±ط­ظٹظ„ ط§ظ„ظپط§طھظˆط±ط© ط±ظ‚ظ…: {SelectedInvoice.Id}طں\nط³ظٹطھظ… ط¥ط¶ط§ظپط© ط§ظ„ظƒظ…ظٹط§طھ ظ„ظ„ظ…ط®ط²ظˆظ†.");

        if (!result) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var postResult = await _invoiceService.PostAsync(SelectedInvoice.Id);

            if (postResult.IsSuccess)
            {
                IsBusy = false;
                await _dialogService.ShowSuccessAsync("ظ†ط¬ط§ط­", "طھظ… طھط±ط­ظٹظ„ ط§ظ„ظپط§طھظˆط±ط© ط¨ظ†ط¬ط§ط­");
                _eventBus.Publish(new PurchaseInvoiceChangedMessage(SelectedInvoice.Id));
                await LoadInvoicesAsync();
            }
            else
            {
                IsBusy = false;
                ErrorMessage = HandleFailure(postResult.Error ?? "ظپط´ظ„ ظپظٹ طھط±ط­ظٹظ„ ط§ظ„ظپط§طھظˆط±ط©", "PurchaseInvoiceListViewModel.PostInvoiceAsync", $"[PurchaseInvoiceListViewModel.PostInvoiceAsync] Failed to post/confirm purchase invoice ID {SelectedInvoice.Id}.");
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„طھط±ط­ظٹظ„", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "PurchaseInvoiceListViewModel.PostInvoiceAsync", $"[PurchaseInvoiceListViewModel.PostInvoiceAsync] Failed to post/confirm purchase invoice ID {SelectedInvoice.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CancelInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        var result = await _dialogService.ShowConfirmationAsync("طھط£ظƒظٹط¯ ط§ظ„ط¥ظ„ط؛ط§ط،", $"ظ‡ظ„ ط£ظ†طھ ظ…طھط£ظƒط¯ ظ…ظ† ط¥ظ„ط؛ط§ط، ط§ظ„ظپط§طھظˆط±ط© ط±ظ‚ظ…: {SelectedInvoice.Id}طں\nط³ظٹطھظ… ط®طµظ… ط§ظ„ظƒظ…ظٹط§طھ ظ…ظ† ط§ظ„ظ…ط®ط²ظˆظ†.");

        if (!result) return;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var cancelResult = await _invoiceService.CancelAsync(SelectedInvoice.Id);

            if (cancelResult.IsSuccess)
            {
                IsBusy = false;
                await _dialogService.ShowSuccessAsync("ظ†ط¬ط§ط­", "طھظ… ط¥ظ„ط؛ط§ط، ط§ظ„ظپط§طھظˆط±ط© ط¨ظ†ط¬ط§ط­");
                _eventBus.Publish(new PurchaseInvoiceChangedMessage(SelectedInvoice.Id));
                await LoadInvoicesAsync();
            }
            else
            {
                IsBusy = false;
                ErrorMessage = HandleFailure(cancelResult.Error ?? "ظپط´ظ„ ظپظٹ ط¥ظ„ط؛ط§ط، ط§ظ„ظپط§طھظˆط±ط©", "PurchaseInvoiceListViewModel.CancelInvoiceAsync", $"[PurchaseInvoiceListViewModel.CancelInvoiceAsync] Failed to cancel purchase invoice ID {SelectedInvoice.Id}.");
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط¥ظ„ط؛ط§ط،", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "PurchaseInvoiceListViewModel.CancelInvoiceAsync", $"[PurchaseInvoiceListViewModel.CancelInvoiceAsync] Failed to cancel purchase invoice ID {SelectedInvoice.Id}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // â”€â”€â”€ Print Methods â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private bool IsPrintAllowed() => SelectedInvoice != null
        && SelectedInvoice.Status == (byte)InvoiceStatus.Posted
        && !IsBusy;

    private async Task PrintPreviewAsync()
    {
        if (SelectedInvoice == null) return;
        try
        {
            IsBusy = true;
            var result = await _printService.GetPurchasePreviewDataAsync(SelectedInvoice.Id);
            if (result.IsSuccess && result.Value != null)
            {
                var preview = new Views.Common.PdfPreviewWindow(result.Value.TempFilePath, result.Value.InvoiceNumber);
                preview.ShowDialog();
            }
            else
            {
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط·ط¨ط§ط¹ط©",
                    result.Error ?? "طھط¹ط°ط± ظپطھط­ ظ…ط¹ط§ظٹظ†ط© ط§ظ„ط·ط¨ط§ط¹ط©");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PrintA4Async()
    {
        if (SelectedInvoice == null) return;
        try
        {
            IsBusy = true;
            var result = await _printService.PrintPurchaseA4Async(SelectedInvoice.Id);
            if (!result.IsSuccess)
            {
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط·ط¨ط§ط¹ط©",
                    result.Error ?? "ظپط´ظ„طھ ط§ظ„ط·ط¨ط§ط¹ط©");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PrintThermalAsync()
    {
        if (SelectedInvoice == null) return;
        try
        {
            IsBusy = true;
            var result = await _printService.PrintPurchaseThermalAsync(SelectedInvoice.Id);
            if (!result.IsSuccess)
            {
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط§ظ„ط·ط¨ط§ط¹ط©",
                    result.Error ?? "ظپط´ظ„طھ ط§ظ„ط·ط¨ط§ط¹ط© ط§ظ„ط­ط±ط§ط±ظٹط©");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCommandStates()
    {
        (ViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PostCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CancelCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrintPreviewCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrintA4Command as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (PrintThermalCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnInvoiceChanged(PurchaseInvoiceChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadInvoicesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<PurchaseInvoiceChangedMessage>(OnInvoiceChanged);
    }
    #endregion
}

/// <summary>
/// Helper class for status combo box
/// </summary>
public class PurchaseStatusItem
{
    public int? Value { get; set; }
    public string Display { get; set; } = string.Empty;
}




