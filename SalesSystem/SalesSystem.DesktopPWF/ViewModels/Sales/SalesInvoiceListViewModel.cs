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
using SalesSystem.DesktopPWF.Views.Sales;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for Sales Invoice List View
/// </summary>
public class SalesInvoiceListViewModel : ViewModelBase
{
    private readonly ISalesInvoiceApiService _invoiceService;
    private readonly IEventBus _eventBus;
    private readonly ICustomerApiService _customerService;
    private readonly IWarehouseApiService _warehouseService;
    private readonly IProductApiService _productService;
    private readonly IDialogService _dialogService;
    private readonly IPrintApiService _printService;

    private ObservableCollection<SalesInvoiceDto> _invoices = new();
    private ICollectionView? _invoicesView;
    private SalesInvoiceDto? _selectedInvoice;
    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private int? _statusFilter;
    private bool _isLoading;
    private string? _errorMessage;
    private bool _isEmpty;

    public SalesInvoiceListViewModel()
        : this(
            App.GetService<ISalesInvoiceApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<ICustomerApiService>(),
            App.GetService<IWarehouseApiService>(),
            App.GetService<IProductApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IPrintApiService>())
    {
    }

    public SalesInvoiceListViewModel(
        ISalesInvoiceApiService invoiceService,
        IEventBus eventBus,
        ICustomerApiService customerService,
        IWarehouseApiService warehouseService,
        IProductApiService productService,
        IDialogService dialogService,
        IPrintApiService printService)
    {
        _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _printService = printService ?? throw new ArgumentNullException(nameof(printService));

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadInvoicesAsync);
        SearchCommand = RefreshCommand;
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
        _eventBus.Subscribe<SaleInvoiceChangedMessage>(OnInvoiceChanged);
    }

    #region Properties
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
                _ = LoadInvoicesAsync();
            }
        }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    // Status options for ComboBox
    public List<StatusItem> StatusOptions { get; } = new()
    {
        new StatusItem { Value = null, Display = "الكل" },
        new StatusItem { Value = (int)InvoiceStatus.Draft, Display = "مسودة" },
        new StatusItem { Value = (int)InvoiceStatus.Posted, Display = "مرحلة" },
        new StatusItem { Value = (int)InvoiceStatus.Cancelled, Display = "ملغاة" }
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
        IsLoading = true;
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
                    foreach (var item in result.Value)
                    {
                        Invoices.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Invoices.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل فواتير البيع", "SalesInvoiceListViewModel.LoadInvoicesAsync", "[SalesInvoiceListViewModel.LoadInvoicesAsync] Failed to load sales invoices list.");
                IsEmpty = Invoices.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesInvoiceListViewModel.LoadInvoicesAsync", "[SalesInvoiceListViewModel.LoadInvoicesAsync] Failed to load sales invoices list.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetupCollectionView()
    {
        InvoicesView = new ListCollectionView(Invoices);
        InvoicesView.Filter = FilterInvoices;
    }

    private bool FilterInvoices(object obj)
    {
        if (obj is not SalesInvoiceDto invoice) return false;

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
                   (invoice.CustomerName?.ToLower().Contains(searchLower) ?? false);
        }

        return true;
    }

    private void AddNewInvoice()
    {
        var editorVm = new SalesInvoiceEditorViewModel();
        var editorWindow = new SalesInvoiceEditorView { DataContext = editorVm };
        editorVm.CloseRequested += () => editorWindow.DialogResult = true;

        if (editorWindow.ShowDialog() == true)
        {
            if (editorVm.InvoiceId.HasValue)
                _eventBus.Publish(new SaleInvoiceChangedMessage(editorVm.InvoiceId.Value));
            _ = LoadInvoicesAsync();
        }
    }

    private void ViewInvoice()
    {
        if (SelectedInvoice == null) return;

        var editorVm = new SalesInvoiceEditorViewModel(SelectedInvoice.Id, isReadOnly: true);
        _dialogService.ShowDialog(editorVm);
    }

    private void EditInvoice()
    {
        if (SelectedInvoice == null) return;

        var editorVm = new SalesInvoiceEditorViewModel(SelectedInvoice.Id);
        if (_dialogService.ShowDialog(editorVm))
        {
            _eventBus.Publish(new SaleInvoiceChangedMessage(SelectedInvoice.Id));
            _ = LoadInvoicesAsync();
        }
    }

    private async Task PostInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد الترحيل", $"هل أنت متأكد من ترحيل الفاتورة رقم: {SelectedInvoice.Id}؟\nسيتم خصم الكميات من المخزون.");

        if (!result) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var postResult = await _invoiceService.PostAsync(SelectedInvoice.Id);

            if (postResult.IsSuccess)
            {
                IsLoading = false;
                await _dialogService.ShowSuccessAsync("نجاح", "تم ترحيل الفاتورة بنجاح");
                _eventBus.Publish(new SaleInvoiceChangedMessage(SelectedInvoice.Id));
                await LoadInvoicesAsync();
            }
            else
            {
                IsLoading = false;
                ErrorMessage = HandleFailure(postResult.Error ?? "فشل في ترحيل الفاتورة", "SalesInvoiceListViewModel.PostInvoiceAsync", $"[SalesInvoiceListViewModel.PostInvoiceAsync] Failed to post/confirm sales invoice ID {SelectedInvoice.Id}.");
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesInvoiceListViewModel.PostInvoiceAsync", $"[SalesInvoiceListViewModel.PostInvoiceAsync] Failed to post/confirm sales invoice ID {SelectedInvoice.Id}.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CancelInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", $"هل أنت متأكد من إلغاء الفاتورة رقم: {SelectedInvoice.Id}؟\nسيتم إعادة الكميات للمخزون.");

        if (!result) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var cancelResult = await _invoiceService.CancelAsync(SelectedInvoice.Id);

            if (cancelResult.IsSuccess)
            {
                IsLoading = false;
                await _dialogService.ShowSuccessAsync("نجاح", "تم إلغاء الفاتورة بنجاح");
                _eventBus.Publish(new SaleInvoiceChangedMessage(SelectedInvoice.Id));
                await LoadInvoicesAsync();
            }
            else
            {
                IsLoading = false;
                ErrorMessage = HandleFailure(cancelResult.Error ?? "فشل في إلغاء الفاتورة", "SalesInvoiceListViewModel.CancelInvoiceAsync", $"[SalesInvoiceListViewModel.CancelInvoiceAsync] Failed to cancel sales invoice ID {SelectedInvoice.Id}.");
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "SalesInvoiceListViewModel.CancelInvoiceAsync", $"[SalesInvoiceListViewModel.CancelInvoiceAsync] Failed to cancel sales invoice ID {SelectedInvoice.Id}.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ─── Print Methods ────────────────────────────

    private bool IsPrintAllowed() => SelectedInvoice != null
        && SelectedInvoice.Status == (byte)InvoiceStatus.Posted
        && !IsLoading;

    private async Task PrintPreviewAsync()
    {
        if (SelectedInvoice == null) return;
        try
        {
            IsLoading = true;
            var result = await _printService.GetSalesPreviewDataAsync(SelectedInvoice.Id);
            if (result.IsSuccess && result.Value != null)
            {
                var preview = new Views.Common.PdfPreviewWindow(result.Value.TempFilePath, result.Value.InvoiceNumber);
                preview.ShowDialog();
            }
            else
            {
                await _dialogService.ShowErrorAsync("خطأ في الطباعة",
                    result.Error ?? "تعذر فتح معاينة الطباعة");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PrintA4Async()
    {
        if (SelectedInvoice == null) return;
        try
        {
            IsLoading = true;
            var result = await _printService.PrintSalesA4Async(SelectedInvoice.Id);
            if (!result.IsSuccess)
            {
                await _dialogService.ShowErrorAsync("خطأ في الطباعة",
                    result.Error ?? "فشلت الطباعة");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PrintThermalAsync()
    {
        if (SelectedInvoice == null) return;
        try
        {
            IsLoading = true;
            var result = await _printService.PrintSalesThermalAsync(SelectedInvoice.Id);
            if (!result.IsSuccess)
            {
                await _dialogService.ShowErrorAsync("خطأ في الطباعة",
                    result.Error ?? "فشلت الطباعة الحرارية");
            }
        }
        finally
        {
            IsLoading = false;
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

    private void OnInvoiceChanged(SaleInvoiceChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadInvoicesAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<SaleInvoiceChangedMessage>(OnInvoiceChanged);
    }
    #endregion
}

/// <summary>
/// Helper class for status combo box
/// </summary>
public class StatusItem
{
    public int? Value { get; set; }
    public string Display { get; set; } = string.Empty;
}
