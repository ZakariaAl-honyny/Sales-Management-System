using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.PaymentVouchers;

/// <summary>
/// ViewModel for Payment Voucher Editor (Create/Edit)
/// </summary>
public class PaymentVoucherEditorViewModel : ViewModelBase
{
    private readonly IPaymentVoucherApiService _voucherService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int? _voucherId;
    private int _voucherNo;
    private DateTime _voucherDate = DateTime.Today;
    private int _cashBoxId;
    private string? _cashBoxName;
    private int _accountId;
    private string? _accountName;
    private int _currencyId;
    private decimal _totalAmount;
    private string _notes = string.Empty;
    private byte _status = 1; // Draft
    private bool _isEditMode;
    private string? _errorMessage;

    public PaymentVoucherEditorViewModel()
        : this(
            App.GetService<IPaymentVoucherApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public PaymentVoucherEditorViewModel(
        IPaymentVoucherApiService voucherService,
        IEventBus eventBus,
        IDialogService dialogService,
        IToastNotificationService? toastService = null)
    {
        _voucherService = voucherService ?? throw new ArgumentNullException(nameof(voucherService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ سند الصرف...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>
    /// Constructor for editing an existing voucher
    /// </summary>
    public PaymentVoucherEditorViewModel(PaymentVoucherDto voucher)
        : this(
            App.GetService<IPaymentVoucherApiService>(),
            App.GetService<IEventBus>(),
            App.GetService<IDialogService>(),
            App.GetService<IToastNotificationService>())
    {
        _voucherId = voucher.Id;
        _voucherNo = voucher.VoucherNo;
        _voucherDate = voucher.VoucherDate;
        _cashBoxId = voucher.CashBoxId;
        _cashBoxName = voucher.CashBoxName;
        _accountId = voucher.AccountId;
        _accountName = voucher.AccountName;
        _currencyId = voucher.CurrencyId;
        _totalAmount = voucher.TotalAmount;
        _notes = voucher.Notes ?? string.Empty;
        _status = voucher.Status;
        _isEditMode = true;
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل سند صرف" : "سند صرف جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public int VoucherNo
    {
        get => _voucherNo;
        set => SetProperty(ref _voucherNo, value);
    }

    public DateTime VoucherDate
    {
        get => _voucherDate;
        set => SetProperty(ref _voucherDate, value);
    }

    public int CashBoxId
    {
        get => _cashBoxId;
        set
        {
            if (SetProperty(ref _cashBoxId, value))
            {
                if (value <= 0)
                    AddError(nameof(CashBoxId), "الصندوق مطلوب");
                else
                    ClearErrors(nameof(CashBoxId));
            }
        }
    }

    public string? CashBoxName
    {
        get => _cashBoxName;
        set => SetProperty(ref _cashBoxName, value);
    }

    public int AccountId
    {
        get => _accountId;
        set
        {
            if (SetProperty(ref _accountId, value))
            {
                if (value <= 0)
                    AddError(nameof(AccountId), "الحساب مطلوب");
                else
                    ClearErrors(nameof(AccountId));
            }
        }
    }

    public string? AccountName
    {
        get => _accountName;
        set => SetProperty(ref _accountName, value);
    }

    public int CurrencyId
    {
        get => _currencyId;
        set
        {
            if (SetProperty(ref _currencyId, value))
            {
                if (value <= 0)
                    AddError(nameof(CurrencyId), "العملة مطلوبة");
                else
                    ClearErrors(nameof(CurrencyId));
            }
        }
    }

    public decimal TotalAmount
    {
        get => _totalAmount;
        set
        {
            if (SetProperty(ref _totalAmount, value))
            {
                if (value <= 0)
                    AddError(nameof(TotalAmount), "المبلغ يجب أن يكون أكبر من صفر");
                else
                    ClearErrors(nameof(TotalAmount));
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public byte Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Whether the voucher can be edited (Draft status only)
    /// </summary>
    public bool CanEdit => Status == 1;

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    #endregion

    #region Methods

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (CashBoxId <= 0)
            AddError(nameof(CashBoxId), "الصندوق مطلوب");
        if (AccountId <= 0)
            AddError(nameof(AccountId), "الحساب مطلوب");
        if (CurrencyId <= 0)
            AddError(nameof(CurrencyId), "العملة مطلوبة");
        if (TotalAmount <= 0)
            AddError(nameof(TotalAmount), "المبلغ يجب أن يكون أكبر من صفر");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode && _voucherId.HasValue)
        {
            await UpdateVoucherAsync();
        }
        else
        {
            await CreateVoucherAsync();
        }
    }

    private async Task CreateVoucherAsync()
    {
        var request = new CreatePaymentVoucherRequest(
            VoucherDate,
            (short)CurrencyId,
            CashBoxId,
            AccountId,
            TotalAmount,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());

        var result = await _voucherService.CreateAsync(request);

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new PaymentVoucherChangedMessage(result.Value.Id));
            _toastService.ShowSuccess("تم إضافة سند الصرف بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ سند الصرف", "PaymentVoucherEditorViewModel.CreateVoucherAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ سند الصرف", ErrorMessage!);
        }
    }

    private async Task UpdateVoucherAsync()
    {
        if (!_voucherId.HasValue) return;

        var request = new UpdatePaymentVoucherRequest(
            VoucherDate,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());

        var result = await _voucherService.UpdateAsync(_voucherId.Value, request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new PaymentVoucherChangedMessage(_voucherId.Value));
            _toastService.ShowSuccess("تم تحديث سند الصرف بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث سند الصرف", "PaymentVoucherEditorViewModel.UpdateVoucherAsync");
            await _dialogService.ShowErrorAsync("خطأ في تحديث سند الصرف", ErrorMessage!);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
