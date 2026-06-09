using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for Sales Quotations List View.
/// Supports filtering by status, customer search, and CRUD operations.
/// </summary>
public class SalesQuotationListViewModel : ViewModelBase, IDisposable
{
    private readonly ISalesQuotationApiService _quotationService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<SalesQuotationDto> _quotations = new();
    private SalesQuotationDto? _selectedQuotation;
    private string? _searchText;
    private int? _filterStatus;
    private string? _errorMessage;
    private bool _isEmpty;

    public SalesQuotationListViewModel(
        ISalesQuotationApiService quotationService,
        IEventBus eventBus,
        IDialogService dialogService,
        IScreenWindowService screenWindowService,
        IToastNotificationService toastService)
    {
        _quotationService = quotationService ?? throw new ArgumentNullException(nameof(quotationService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        SetDialogService(dialogService);

        RefreshCommand = new AsyncRelayCommand(LoadQuotationsAsync);
        SearchCommand = new AsyncRelayCommand(LoadQuotationsAsync);
        AddCommand = new RelayCommand(AddQuotation);
        EditCommand = new AsyncRelayCommand(EditQuotationAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteQuotationAsync);
        ConfirmCommand = new AsyncRelayCommand(ConfirmQuotationAsync);
        ExpireCommand = new AsyncRelayCommand(ExpireQuotationAsync);
        ConvertToInvoiceCommand = new AsyncRelayCommand(ConvertToInvoiceAsync);

        _eventBus.Subscribe<SalesQuotationChangedMessage>(OnQuotationChanged);
        _ = LoadQuotationsAsync();
    }

    #region Properties

    public ObservableCollection<SalesQuotationDto> Quotations
    {
        get => _quotations;
        set => SetProperty(ref _quotations, value);
    }

    public SalesQuotationDto? SelectedQuotation
    {
        get => _selectedQuotation;
        set => SetProperty(ref _selectedQuotation, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public int? FilterStatus
    {
        get => _filterStatus;
        set
        {
            if (SetProperty(ref _filterStatus, value))
                _ = ApplyFiltersAsync();
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

    public List<QuotationStatusItem> StatusOptions { get; } = new()
    {
        new QuotationStatusItem { Value = null, Display = "الكل" },
        new QuotationStatusItem { Value = 1, Display = "مسودة" },
        new QuotationStatusItem { Value = 2, Display = "مؤكد" },
        new QuotationStatusItem { Value = 3, Display = "منتهي" },
        new QuotationStatusItem { Value = 4, Display = "محول" }
    };

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand ExpireCommand { get; }
    public ICommand ConvertToInvoiceCommand { get; }

    #endregion

    #region Methods

    public async Task LoadQuotationsAsync()
    {
        await ExecuteAsync(LoadQuotationsOperationAsync, "جاري تحميل عروض الأسعار...");
    }

    private async Task LoadQuotationsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _quotationService.GetAllAsync(
            search: SearchText,
            status: FilterStatus.HasValue ? (byte)FilterStatus.Value : null);

        if (result.IsSuccess && result.Value != null)
        {
            Quotations = new ObservableCollection<SalesQuotationDto>(
                result.Value.OrderByDescending(x => x.Id));
            IsEmpty = Quotations.Count == 0;
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل عروض الأسعار",
                "SalesQuotationListViewModel.LoadQuotationsAsync");
            IsEmpty = Quotations.Count == 0;
        }
    }

    private async Task ApplyFiltersAsync()
    {
        await LoadQuotationsAsync();
    }

    private void AddQuotation()
    {
        var editorVm = App.GetService<SalesQuotationEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "عرض سعر جديد",
            OnClosed = (vm) =>
            {
                if (vm is SalesQuotationEditorViewModel editor && editor.QuotationId.HasValue)
                {
                    _eventBus.Publish(new SalesQuotationChangedMessage(editor.QuotationId.Value));
                    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadQuotationsAsync());
                }
            }
        });
    }

    private async Task EditQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        if (SelectedQuotation.Status > 1)
        {
            await _dialogService.ShowWarningAsync("تعديل عرض السعر",
                "لا يمكن تعديل عرض سعر تم تأكيده أو تحويله أو انتهاء صلاحيته.");
            return;
        }

        var editorVm = new SalesQuotationEditorViewModel(SelectedQuotation.Id);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = $"تعديل عرض السعر {SelectedQuotation.QuotationNo}",
            OnClosed = (vm) =>
            {
                if (vm is SalesQuotationEditorViewModel editor && editor.QuotationId.HasValue)
                {
                    _eventBus.Publish(new SalesQuotationChangedMessage(editor.QuotationId.Value));
                    _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadQuotationsAsync());
                }
            }
        });
    }

    private async Task DeleteQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync(
            "تأكيد حذف عرض السعر",
            $"هل أنت متأكد من حذف عرض السعر {SelectedQuotation.QuotationNo}؟");
        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _quotationService.DeleteAsync(SelectedQuotation!.Id);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم حذف عرض السعر بنجاح");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حذف عرض السعر",
                    "SalesQuotationListViewModel.DeleteQuotationAsync");
                await _dialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage!);
            }
        });
    }

    private async Task ConfirmQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        if (SelectedQuotation.Status != 1)
        {
            await _dialogService.ShowWarningAsync("تأكيد عرض السعر",
                "يمكن تأكيد عروض الأسعار في حالة مسودة فقط.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "تأكيد عرض السعر",
            $"هل أنت متأكد من تأكيد عرض السعر {SelectedQuotation.QuotationNo}؟");
        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _quotationService.ConfirmAsync(SelectedQuotation!.Id);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم تأكيد عرض السعر بنجاح");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تأكيد عرض السعر",
                    "SalesQuotationListViewModel.ConfirmQuotationAsync");
                await _dialogService.ShowErrorAsync("خطأ في التأكيد", ErrorMessage!);
            }
        });
    }

    private async Task ExpireQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        if (SelectedQuotation.Status != 1 && SelectedQuotation.Status != 2)
        {
            await _dialogService.ShowWarningAsync("إنهاء عرض السعر",
                "يمكن إنهاء عروض الأسعار في حالة مسودة أو مؤكد فقط.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "إنهاء عرض السعر",
            $"هل أنت متأكد من إنهاء عرض السعر {SelectedQuotation.QuotationNo}؟");
        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _quotationService.ExpireAsync(SelectedQuotation!.Id);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إنهاء عرض السعر بنجاح");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إنهاء عرض السعر",
                    "SalesQuotationListViewModel.ExpireQuotationAsync");
                await _dialogService.ShowErrorAsync("خطأ في الإنهاء", ErrorMessage!);
            }
        });
    }

    private async Task ConvertToInvoiceAsync()
    {
        if (SelectedQuotation == null) return;

        if (SelectedQuotation.Status != 2)
        {
            await _dialogService.ShowWarningAsync("تحويل عرض السعر",
                "يمكن تحويل عروض الأسعار المؤكدة فقط إلى فاتورة بيع.");
            return;
        }

        var confirm = await _dialogService.ShowConfirmationAsync(
            "تحويل عرض السعر إلى فاتورة بيع",
            $"هل أنت متأكد من تحويل عرض السعر {SelectedQuotation.QuotationNo} إلى فاتورة بيع؟");
        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var request = new ConvertQuotationToInvoiceRequest(
                CustomerId: SelectedQuotation!.CustomerId,
                WarehouseId: SelectedQuotation.WarehouseId,
                CashBoxId: null,
                PaymentType: 1,
                DiscountAmount: SelectedQuotation.DiscountAmount,
                TaxAmount: 0,
                PaidAmount: 0,
                Notes: SelectedQuotation.Notes,
                CurrencyId: null,
                ExchangeRate: null);

            var result = await _quotationService.ConvertToInvoiceAsync(SelectedQuotation.Id, request);
            if (result.IsSuccess)
            {
                await _dialogService.ShowSuccessAsync("نجاح",
                    $"تم تحويل عرض السعر {SelectedQuotation.QuotationNo} إلى فاتورة بيع بنجاح.");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحويل عرض السعر",
                    "SalesQuotationListViewModel.ConvertToInvoiceAsync");
                await _dialogService.ShowErrorAsync("خطأ في التحويل", ErrorMessage!);
            }
        });
    }

    private void OnQuotationChanged(SalesQuotationChangedMessage msg)
    {
        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadQuotationsAsync());
    }

    public void Dispose()
    {
        Cleanup();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}

/// <summary>
/// Helper class for status combo box items in quotations
/// </summary>
public class QuotationStatusItem
{
    public int? Value { get; set; }
    public string Display { get; set; } = string.Empty;
}
