using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Accounting;

/// <summary>
/// ViewModel for Receipt Vouchers List View (سندات قبض).
/// Manages loading, filtering, adding, editing, posting, and cancelling receipt vouchers.
/// </summary>
public class ReceiptVoucherListViewModel : ViewModelBase, IDisposable
{
    private readonly IReceiptVoucherApiService _voucherService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IScreenWindowService _screenWindowService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<ReceiptVoucherDto> _vouchers = new();
    private ICollectionView? _vouchersView;
    private ReceiptVoucherDto? _selectedVoucher;
    private string _searchText = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string? _errorMessage;
    private bool _isEmpty;
    private int _totalCount;
    private int _currentPage = 1;
    private const int PageSize = 100;

    public ReceiptVoucherListViewModel()
        : this(
            App.GetService<IReceiptVoucherApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IScreenWindowService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ReceiptVoucherListViewModel(
        IReceiptVoucherApiService voucherService,
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
        CancelVoucherCommand = new AsyncRelayCommand(CancelVoucherAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteVoucherAsync);

        _eventBus.Subscribe<ReceiptVoucherChangedMessage>(OnVoucherChanged);
    }

    #region Properties

    public ObservableCollection<ReceiptVoucherDto> Vouchers
    {
        get => _vouchers;
        set => SetProperty(ref _vouchers, value);
    }

    public ICollectionView? VouchersView
    {
        get => _vouchersView;
        private set => SetProperty(ref _vouchersView, value);
    }

    public ReceiptVoucherDto? SelectedVoucher
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

    public int TotalCount
    {
        get => _totalCount;
        private set => SetProperty(ref _totalCount, value);
    }

    public int VouchersCount => Vouchers.Count;

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand PostCommand { get; private set; } = null!;
    public ICommand CancelVoucherCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;

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
            string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            FromDate,
            ToDate,
            _currentPage,
            PageSize);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Vouchers.Clear();
                foreach (var item in result.Value.Items.OrderByDescending(x => x.Id))
                {
                    Vouchers.Add(item);
                }
                SetupCollectionView();
                TotalCount = result.Value.TotalCount;
                IsEmpty = Vouchers.Count == 0;
                OnPropertyChanged(nameof(VouchersCount));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل سندات القبض",
                "ReceiptVoucherListViewModel.LoadVouchersOperationAsync",
                "[ReceiptVoucherListViewModel.LoadVouchersOperationAsync] Failed to load vouchers.");
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
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
        if (obj is not ReceiptVoucherDto voucher) return false;

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
        var editorVm = App.GetService<ReceiptVoucherEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "سند قبض جديد",
            Width = 650,
            Height = 600,
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
            _ = _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند قبض");
            return;
        }

        if (SelectedVoucher.Status != 1) // Draft only
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "لا يمكن تعديل السند إلا في حالة مسودة");
            return;
        }

        var editorVm = new ReceiptVoucherEditorViewModel(SelectedVoucher);
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل سند قبض",
            Width = 650,
            Height = 600,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadVouchersAsync());
            }
        });
    }

    public async Task PostVoucherAsync()
    {
        if (SelectedVoucher == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند قبض");
            return;
        }

        if (SelectedVoucher.Status != 1) // Draft only
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن ترحيل السند إلا في حالة مسودة");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "تأكيد الترحيل",
            $"هل أنت متأكد من ترحيل سند القبض رقم {SelectedVoucher.VoucherNo}؟\nسيتم إنشاء قيد محاسبي عند الترحيل.");
        if (!confirmed) return;

        await ExecuteAsync(PostVoucherOperationAsync);
    }

    private async Task PostVoucherOperationAsync()
    {
        ErrorMessage = null;
        var result = await _voucherService.PostAsync(SelectedVoucher!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ReceiptVoucherChangedMessage(SelectedVoucher.Id));
            await LoadVouchersAsync();
            _toastService.ShowSuccess("تم ترحيل سند القبض بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في ترحيل سند القبض",
                "ReceiptVoucherListViewModel.PostVoucherOperationAsync",
                $"[ReceiptVoucherListViewModel.PostVoucherOperationAsync] Failed to post voucher {SelectedVoucher.Id}.");
            await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
        }
    }

    public async Task CancelVoucherAsync()
    {
        if (SelectedVoucher == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند قبض");
            return;
        }

        if (SelectedVoucher.Status == 3) // Already cancelled
        {
            await _dialogService.ShowWarningAsync("تنبيه", "السند ملغي بالفعل");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "تأكيد الإلغاء",
            $"هل أنت متأكد من إلغاء سند القبض رقم {SelectedVoucher.VoucherNo}؟\nسيتم عكس القيد المحاسبي إذا كان مرحلة.");
        if (!confirmed) return;

        await ExecuteAsync(CancelVoucherOperationAsync);
    }

    private async Task CancelVoucherOperationAsync()
    {
        ErrorMessage = null;
        var result = await _voucherService.CancelAsync(SelectedVoucher!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ReceiptVoucherChangedMessage(SelectedVoucher.Id));
            await LoadVouchersAsync();
            _toastService.ShowSuccess("تم إلغاء سند القبض بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء سند القبض",
                "ReceiptVoucherListViewModel.CancelVoucherOperationAsync",
                $"[ReceiptVoucherListViewModel.CancelVoucherOperationAsync] Failed to cancel voucher {SelectedVoucher.Id}.");
            await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage!);
        }
    }

    public async Task DeleteVoucherAsync()
    {
        if (SelectedVoucher == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار سند قبض");
            return;
        }

        if (SelectedVoucher.Status != 1) // Draft only
        {
            await _dialogService.ShowWarningAsync("تنبيه", "لا يمكن حذف السند إلا في حالة مسودة");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "تأكيد الحذف",
            $"هل أنت متأكد من حذف سند القبض رقم {SelectedVoucher.VoucherNo}؟");
        if (!confirmed) return;

        await ExecuteAsync(DeleteVoucherOperationAsync);
    }

    private async Task DeleteVoucherOperationAsync()
    {
        ErrorMessage = null;
        var result = await _voucherService.DeleteAsync(SelectedVoucher!.Id);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ReceiptVoucherChangedMessage(SelectedVoucher.Id));
            await LoadVouchersAsync();
            _toastService.ShowSuccess("تم حذف سند القبض بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حذف سند القبض",
                "ReceiptVoucherListViewModel.DeleteVoucherOperationAsync",
                $"[ReceiptVoucherListViewModel.DeleteVoucherOperationAsync] Failed to delete voucher {SelectedVoucher.Id}.");
            await _dialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage!);
        }
    }

    private void OnVoucherChanged(ReceiptVoucherChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () => await LoadVouchersAsync());
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<ReceiptVoucherChangedMessage>(OnVoucherChanged);
        base.Cleanup();
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    #endregion
}
