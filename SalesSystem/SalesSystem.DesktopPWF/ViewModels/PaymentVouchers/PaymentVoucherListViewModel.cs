using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.PaymentVouchers;

/// <summary>
/// ViewModel for Payment Vouchers List View (سندات صرف)
/// </summary>
public class PaymentVoucherListViewModel : ViewModelBase, IDisposable
{
    private readonly IPaymentVoucherApiService _voucherService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<PaymentVoucherDto> _vouchers = new();
    private ICollectionView? _vouchersView;
    private PaymentVoucherDto? _selectedVoucher;
    private string _searchText = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string? _errorMessage;
    private bool _isEmpty;

    public PaymentVoucherListViewModel()
        : this(
            App.GetService<IPaymentVoucherApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public PaymentVoucherListViewModel(
        IPaymentVoucherApiService voucherService,
        IDialogService dialogService,
        IEventBus eventBus,
        IScreenWindowService screenWindowService,
        IToastNotificationService? toastService = null)
    {
        _voucherService = voucherService ?? throw new ArgumentNullException(nameof(voucherService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _screenWindowService = screenWindowService ?? throw new ArgumentNullException(nameof(screenWindowService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadVouchersAsync);
        AddCommand = new RelayCommand(AddVoucher);
        EditCommand = new RelayCommand(EditVoucher);
        PostCommand = new AsyncRelayCommand(PostVoucherAsync);
        CancelCommand = new AsyncRelayCommand(CancelVoucherAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteVoucherAsync);
        SearchCommand = new RelayCommand(Search);
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        _eventBus.Subscribe<PaymentVoucherChangedMessage>(OnVoucherChanged);
    }

    #region Properties

    public ObservableCollection<PaymentVoucherDto> Vouchers
    {
        get => _vouchers;
        set => SetProperty(ref _vouchers, value);
    }

    public ICollectionView? VouchersView
    {
        get => _vouchersView;
        private set => SetProperty(ref _vouchersView, value);
    }

    public PaymentVoucherDto? SelectedVoucher
    {
        get => _selectedVoucher;
        set => SetProperty(ref _selectedVoucher, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                VouchersView?.Refresh();
            }
        }
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
            {
                _ = LoadVouchersAsync();
            }
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value))
            {
                _ = LoadVouchersAsync();
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

    public int VouchersCount => Vouchers.Count;

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand PostCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    public ICommand ClearFiltersCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadVouchersAsync()
    {
        await ExecuteAsync(LoadVouchersOperationAsync);
    }

    private async Task LoadVouchersOperationAsync()
    {
        ErrorMessage = null;

        var result = await _voucherService.GetAllAsync(
            search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            from: FromDate,
            to: ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Vouchers.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Vouchers.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Vouchers.Count == 0;
                OnPropertyChanged(nameof(VouchersCount));
                return Task.CompletedTask;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل سندات الصرف", "PaymentVoucherListViewModel.LoadVouchersOperationAsync");
            IsEmpty = Vouchers.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        VouchersView = new ListCollectionView(Vouchers);
        VouchersView.Filter = FilterVouchers;
    }

    private bool FilterVouchers(object obj)
    {
        if (obj is not PaymentVoucherDto voucher) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            if (!voucher.VoucherNo.ToString().Contains(searchLower) &&
                (voucher.CashBoxName?.ToLower().Contains(searchLower) ?? false) == false &&
                (voucher.AccountName?.ToLower().Contains(searchLower) ?? false) == false &&
                (voucher.Notes?.ToLower().Contains(searchLower) ?? false) == false)
                return false;
        }

        return true;
    }

    private void AddVoucher()
    {
        var editorVm = App.GetService<PaymentVoucherEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "سند صرف جديد",
            Width = 700,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadVouchersAsync());
            }
        });
    }

    private void EditVoucher()
    {
        if (SelectedVoucher == null)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند صرف");
            return;
        }

        var editorVm = new PaymentVoucherEditorViewModel(SelectedVoucher);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل سند صرف",
            Width = 700,
            Height = 650,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadVouchersAsync());
            }
        });
    }

    public void EditVoucherFromDoubleClick()
    {
        if (SelectedVoucher != null)
            EditVoucher();
    }

    public async Task PostVoucherAsync()
    {
        if (SelectedVoucher == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند صرف");
            return;
        }

        if (SelectedVoucher.Status != 1) // Draft only
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن ترحيل السند إلا في حالة مسودة");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الترحيل",
            "هل أنت متأكد من ترحيل سند الصرف؟ سيتم إنشاء القيد المحاسبي.");

        if (!confirmed) return;

        await ExecuteAsync(PostVoucherOperationAsync);
    }

    private async Task PostVoucherOperationAsync()
    {
        ErrorMessage = null;
        var result = await _voucherService.PostAsync(SelectedVoucher!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new PaymentVoucherChangedMessage(SelectedVoucher.Id));
            await LoadVouchersAsync();
            _toastService.ShowSuccess("تم ترحيل سند الصرف بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في ترحيل سند الصرف", "PaymentVoucherListViewModel.PostVoucherOperationAsync");
        }
    }

    public async Task CancelVoucherAsync()
    {
        if (SelectedVoucher == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند صرف");
            return;
        }

        if (SelectedVoucher.Status == 3) // Already cancelled
        {
            await _dialogService.ShowWarningAsync("تنبيه", "السند ملغي بالفعل");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء",
            "هل أنت متأكد من إلغاء سند الصرف؟");

        if (!confirmed) return;

        await ExecuteAsync(CancelVoucherOperationAsync);
    }

    private async Task CancelVoucherOperationAsync()
    {
        ErrorMessage = null;
        var result = await _voucherService.CancelAsync(SelectedVoucher!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new PaymentVoucherChangedMessage(SelectedVoucher.Id));
            await LoadVouchersAsync();
            _toastService.ShowSuccess("تم إلغاء سند الصرف بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء سند الصرف", "PaymentVoucherListViewModel.CancelVoucherOperationAsync");
        }
    }

    public async Task DeleteVoucherAsync()
    {
        if (SelectedVoucher == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند صرف");
            return;
        }

        if (SelectedVoucher.Status != 1) // Only Draft can be deleted
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن حذف السند إلا في حالة مسودة");
            return;
        }

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"سند الصرف رقم: {SelectedVoucher.VoucherNo}");

        if (strategy == DeleteStrategy.Cancel) return;

        var voucherId = SelectedVoucher.Id;
        await ExecuteAsync(() => DeleteVoucherOperationAsync(voucherId));
    }

    private async Task DeleteVoucherOperationAsync(int voucherId)
    {
        ErrorMessage = null;
        var result = await _voucherService.DeleteAsync(voucherId);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new PaymentVoucherChangedMessage(voucherId));
            await LoadVouchersAsync();
            _toastService.ShowSuccess("تم حذف سند الصرف بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حذف سند الصرف", "PaymentVoucherListViewModel.DeleteVoucherOperationAsync");
        }
    }

    private void Search()
    {
        VouchersView?.Refresh();
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        FromDate = null;
        ToDate = null;
        _ = LoadVouchersAsync();
    }

    private void OnVoucherChanged(PaymentVoucherChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () => await LoadVouchersAsync());
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<PaymentVoucherChangedMessage>(OnVoucherChanged);
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    #endregion
}
