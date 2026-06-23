using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.Views.Sales;

namespace SalesSystem.DesktopPWF.ViewModels.Sales;

/// <summary>
/// ViewModel for Sales Quotations List View
/// </summary>
public class SalesQuotationListViewModel : ViewModelBase
{
    private readonly ISalesQuotationApiService _quotationService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<SalesQuotationDto> _quotations = new();
    private ICollectionView? _quotationsView;
    private SalesQuotationDto? _selectedQuotation;
    private string _searchText = string.Empty;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private int? _statusFilter;
    private string? _errorMessage;
    private bool _isEmpty;

    public SalesQuotationListViewModel()
        : this(
            App.GetService<ISalesQuotationApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
    {
    }

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

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(LoadQuotationsAsync);
        SearchCommand = RefreshCommand;
        NewCommand = new RelayCommand(AddNewQuotation);
        ViewCommand = new RelayCommand(ViewQuotation, () => SelectedQuotation != null);
        EditCommand = new RelayCommand(EditQuotation, () => SelectedQuotation != null && SelectedQuotation.Status == 1);
        SendCommand = new AsyncRelayCommand(SendQuotationAsync, () => SelectedQuotation != null && SelectedQuotation.Status == 1);
        AcceptCommand = new AsyncRelayCommand(AcceptQuotationAsync, () => SelectedQuotation != null && SelectedQuotation.Status == 2);
        RejectCommand = new AsyncRelayCommand(RejectQuotationAsync, () => SelectedQuotation != null && (SelectedQuotation.Status == 1 || SelectedQuotation.Status == 2));
        ConvertCommand = new AsyncRelayCommand(ConvertQuotationAsync, () => SelectedQuotation != null && SelectedQuotation.Status == 3);
        CancelCommand = new AsyncRelayCommand(CancelQuotationAsync, () => SelectedQuotation != null && SelectedQuotation.Status == 1);

        // Default date range (last 30 days)
        DateFrom = DateTime.Today.AddDays(-30);
        DateTo = DateTime.Today;

        // Subscribe to quotation changes
        _eventBus.Subscribe<SalesQuotationChangedMessage>(OnQuotationChanged);
    }

    #region Properties

    public ObservableCollection<SalesQuotationDto> Quotations
    {
        get => _quotations;
        set => SetProperty(ref _quotations, value);
    }

    public ICollectionView? QuotationsView
    {
        get => _quotationsView;
        private set => SetProperty(ref _quotationsView, value);
    }

    public SalesQuotationDto? SelectedQuotation
    {
        get => _selectedQuotation;
        set
        {
            if (SetProperty(ref _selectedQuotation, value))
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
                QuotationsView?.Refresh();
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
                QuotationsView?.Refresh();
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
                QuotationsView?.Refresh();
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
                QuotationsView?.Refresh();
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

    // Status options for ComboBox
    public List<StatusItem> StatusOptions { get; } = new()
    {
        new StatusItem { Value = null, Display = "الكل" },
        new StatusItem { Value = (byte)1, Display = "مسودة" },
        new StatusItem { Value = (byte)2, Display = "مرسلة" },
        new StatusItem { Value = (byte)3, Display = "مقبولة" },
        new StatusItem { Value = (byte)4, Display = "محولة لفاتورة" },
        new StatusItem { Value = (byte)5, Display = "مرفوضة" }
    };

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand ViewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SendCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand RejectCommand { get; }
    public ICommand ConvertCommand { get; }
    public ICommand CancelCommand { get; }

    #endregion

    #region Methods

    public async Task LoadQuotationsAsync()
    {
        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var result = await _quotationService.GetAllAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                from: DateFrom,
                to: DateTo,
                status: StatusFilter.HasValue ? (byte)StatusFilter.Value : null);

            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Quotations.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.QuotationDate))
                    {
                        Quotations.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Quotations.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل عروض الأسعار", "SalesQuotationListViewModel.LoadQuotationsAsync", "[SalesQuotationListViewModel.LoadQuotationsAsync] Failed to load sales quotations list.");
                await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
                IsEmpty = Quotations.Count == 0;
            }
        });
    }

    private void SetupCollectionView()
    {
        QuotationsView = new ListCollectionView(Quotations);
        QuotationsView.Filter = FilterQuotations;
    }

    private bool FilterQuotations(object obj)
    {
        if (obj is not SalesQuotationDto quotation) return false;

        // Date filter
        if (DateFrom.HasValue && quotation.QuotationDate < DateFrom.Value) return false;
        if (DateTo.HasValue && quotation.QuotationDate > DateTo.Value.AddDays(1)) return false;

        // Status filter
        if (StatusFilter.HasValue && quotation.Status != StatusFilter.Value) return false;

        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            return quotation.QuotationNo.ToString().Contains(searchLower) ||
                   (quotation.CustomerName?.ToLower().Contains(searchLower) ?? false);
        }

        return true;
    }

    private void AddNewQuotation()
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
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadQuotationsAsync());
                }
            }
        });
    }

    private void ViewQuotation()
    {
        if (SelectedQuotation == null) return;

        var editorVm = new SalesQuotationEditorViewModel(SelectedQuotation.Id, isReadOnly: true);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "عرض عرض السعر"
        });
    }

    private void EditQuotation()
    {
        if (SelectedQuotation == null) return;

        var editorVm = new SalesQuotationEditorViewModel(SelectedQuotation.Id);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل عرض السعر",
            OnClosed = (vm) =>
            {
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadQuotationsAsync());
            }
        });
    }

    private async Task SendQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد الإرسال", $"هل أنت متأكد من إرسال عرض السعر رقم: {SelectedQuotation.QuotationNo}؟");

        if (!result) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var sendResult = await _quotationService.SendAsync(SelectedQuotation.Id);

            if (sendResult.IsSuccess)
            {
                _toastService.ShowSuccess("تم إرسال عرض السعر بنجاح");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(sendResult.Error ?? "فشل في إرسال عرض السعر", "SalesQuotationListViewModel.SendQuotationAsync", $"[SalesQuotationListViewModel.SendQuotationAsync] Failed to send quotation ID {SelectedQuotation.Id}.");
                await _dialogService.ShowErrorAsync("خطأ في الإرسال", ErrorMessage);
            }
        });
    }

    private async Task AcceptQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد القبول", $"هل أنت متأكد من قبول عرض السعر رقم: {SelectedQuotation.QuotationNo}؟");

        if (!result) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var acceptResult = await _quotationService.AcceptAsync(SelectedQuotation.Id);

            if (acceptResult.IsSuccess)
            {
                _toastService.ShowSuccess("تم قبول عرض السعر بنجاح");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(acceptResult.Error ?? "فشل في قبول عرض السعر", "SalesQuotationListViewModel.AcceptQuotationAsync", $"[SalesQuotationListViewModel.AcceptQuotationAsync] Failed to accept quotation ID {SelectedQuotation.Id}.");
                await _dialogService.ShowErrorAsync("خطأ في القبول", ErrorMessage);
            }
        });
    }

    private async Task RejectQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الرفض", $"هل أنت متأكد من رفض عرض السعر رقم: {SelectedQuotation.QuotationNo}؟");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var rejectResult = await _quotationService.RejectAsync(SelectedQuotation.Id, null);

            if (rejectResult.IsSuccess)
            {
                _toastService.ShowSuccess("تم رفض عرض السعر");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(rejectResult.Error ?? "فشل في رفض عرض السعر", "SalesQuotationListViewModel.RejectQuotationAsync", $"[SalesQuotationListViewModel.RejectQuotationAsync] Failed to reject quotation ID {SelectedQuotation.Id}.");
                await _dialogService.ShowErrorAsync("خطأ في الرفض", ErrorMessage);
            }
        });
    }

    private async Task ConvertQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد التحويل", $"هل أنت متأكد من تحويل عرض السعر رقم: {SelectedQuotation.QuotationNo} إلى فاتورة بيع؟\nسيتم إنشاء فاتورة بيع جديدة.");

        if (!result) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var convertResult = await _quotationService.ConvertToInvoiceAsync(SelectedQuotation.Id);

            if (convertResult.IsSuccess)
            {
                _toastService.ShowSuccess("تم تحويل عرض السعر إلى فاتورة بيع بنجاح");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(convertResult.Error ?? "فشل في تحويل عرض السعر", "SalesQuotationListViewModel.ConvertQuotationAsync", $"[SalesQuotationListViewModel.ConvertQuotationAsync] Failed to convert quotation ID {SelectedQuotation.Id}.");
                await _dialogService.ShowErrorAsync("خطأ في التحويل", ErrorMessage);
            }
        });
    }

    private async Task CancelQuotationAsync()
    {
        if (SelectedQuotation == null) return;

        var result = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء", $"هل أنت متأكد من إلغاء عرض السعر رقم: {SelectedQuotation.QuotationNo}؟");

        if (!result) return;

        await ExecuteAsync(async () =>
        {
            ErrorMessage = null;
            var cancelResult = await _quotationService.CancelAsync(SelectedQuotation.Id);

            if (cancelResult.IsSuccess)
            {
                _toastService.ShowSuccess("تم إلغاء عرض السعر بنجاح");
                _eventBus.Publish(new SalesQuotationChangedMessage(SelectedQuotation.Id));
                await LoadQuotationsAsync();
            }
            else
            {
                ErrorMessage = HandleFailure(cancelResult.Error ?? "فشل في إلغاء عرض السعر", "SalesQuotationListViewModel.CancelQuotationAsync", $"[SalesQuotationListViewModel.CancelQuotationAsync] Failed to cancel quotation ID {SelectedQuotation.Id}.");
                await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage);
            }
        });
    }

    private void UpdateCommandStates()
    {
        (ViewCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SendCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (AcceptCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (RejectCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (ConvertCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        (CancelCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnQuotationChanged(SalesQuotationChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () =>
        {
            await LoadQuotationsAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<SalesQuotationChangedMessage>(OnQuotationChanged);
    }

    #endregion
}
