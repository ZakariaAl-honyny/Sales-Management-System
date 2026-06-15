using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Accounting;

/// <summary>
/// ViewModel for Receipt Voucher Editor (إضافة/تعديل سند قبض).
/// Supports create and edit modes with full validation.
/// </summary>
public class ReceiptVoucherEditorViewModel : ViewModelBase
{
    private readonly IReceiptVoucherApiService _voucherService;
    private readonly ICashBoxApiService _cashBoxService;
    private readonly IAccountApiService _accountService;
    private readonly ICurrencyApiService _currencyService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private int? _voucherId;
    private int _voucherNo;
    private DateTime _voucherDate = DateTime.Today;
    private int _cashBoxId;
    private string? _cashBoxName;
    private int _accountId;
    private string? _accountName;
    private short _currencyId;
    private string? _currencyCode;
    private decimal _totalAmount;
    private string _notes = string.Empty;
    private byte _status = 1; // Draft
    private bool _isEditMode;
    private string? _errorMessage;
    private bool _isReadOnly;

    private ObservableCollection<CashBoxDto> _cashBoxes = new();
    private ObservableCollection<AccountDto> _accounts = new();
    private ObservableCollection<CurrencyDto> _currencies = new();

    private CashBoxDto? _selectedCashBox;
    private AccountDto? _selectedAccount;
    private CurrencyDto? _selectedCurrency;

    public ReceiptVoucherEditorViewModel()
        : this(
            App.GetService<IReceiptVoucherApiService>(),
            App.GetService<ICashBoxApiService>(),
            App.GetService<IAccountApiService>(),
            App.GetService<ICurrencyApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public ReceiptVoucherEditorViewModel(
        IReceiptVoucherApiService voucherService,
        ICashBoxApiService cashBoxService,
        IAccountApiService accountService,
        ICurrencyApiService currencyService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService? toastService = null)
    {
        _voucherService = voucherService ?? throw new ArgumentNullException(nameof(voucherService));
        _cashBoxService = cashBoxService ?? throw new ArgumentNullException(nameof(cashBoxService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadLookupsAsync();
    }

    /// <summary>
    /// Constructor for editing an existing voucher (from list double-click or edit button).
    /// </summary>
    public ReceiptVoucherEditorViewModel(ReceiptVoucherDto voucher)
        : this()
    {
        LoadFromDto(voucher);
    }

    private void LoadFromDto(ReceiptVoucherDto voucher)
    {
        _voucherId = voucher.Id;
        _voucherNo = voucher.VoucherNo;
        VoucherDate = voucher.VoucherDate;
        CashBoxId = voucher.CashBoxId;
        CashBoxName = voucher.CashBoxName;
        AccountId = voucher.AccountId;
        AccountName = voucher.AccountName;
        CurrencyId = voucher.CurrencyId;
        CurrencyCode = voucher.CurrencyCode;
        TotalAmount = voucher.TotalAmount;
        Notes = voucher.Notes ?? string.Empty;
        Status = voucher.Status;
        IsEditMode = true;
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ سند القبض...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public bool IsEditMode
    {
        get => _isEditMode;
        private set => SetProperty(ref _isEditMode, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    public int? VoucherId => _voucherId;

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

    public short CurrencyId
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

    public string? CurrencyCode
    {
        get => _currencyCode;
        set => SetProperty(ref _currencyCode, value);
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

    // Lookup collections for dropdowns

    public ObservableCollection<CashBoxDto> CashBoxes
    {
        get => _cashBoxes;
        set => SetProperty(ref _cashBoxes, value);
    }

    public ObservableCollection<AccountDto> Accounts
    {
        get => _accounts;
        set => SetProperty(ref _accounts, value);
    }

    public ObservableCollection<CurrencyDto> Currencies
    {
        get => _currencies;
        set => SetProperty(ref _currencies, value);
    }

    public CashBoxDto? SelectedCashBox
    {
        get => _selectedCashBox;
        set
        {
            if (SetProperty(ref _selectedCashBox, value) && value != null)
            {
                CashBoxId = value.Id;
                CashBoxName = value.Name;
            }
        }
    }

    public AccountDto? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (SetProperty(ref _selectedAccount, value) && value != null)
            {
                AccountId = value.Id;
                AccountName = value.NameAr;
            }
        }
    }

    public CurrencyDto? SelectedCurrency
    {
        get => _selectedCurrency;
        set
        {
            if (SetProperty(ref _selectedCurrency, value) && value != null)
            {
                CurrencyId = (short)value.Id;
                CurrencyCode = value.Code;
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    private async Task LoadLookupsAsync()
    {
        try
        {
            var cashBoxTask = _cashBoxService.GetAllAsync();
            var accountTask = _accountService.GetAllAsync();
            var currencyTask = _currencyService.GetAllAsync();

            await Task.WhenAll(cashBoxTask, accountTask, currencyTask);

            if (cashBoxTask.Result.IsSuccess && cashBoxTask.Result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    CashBoxes.Clear();
                    foreach (var box in cashBoxTask.Result.Value.Where(b => b.IsActive).OrderBy(b => b.Name))
                    {
                        CashBoxes.Add(box);
                    }

                    // Select the matching cash box if in edit mode
                    if (IsEditMode && CashBoxId > 0)
                    {
                        SelectedCashBox = CashBoxes.FirstOrDefault(b => b.Id == CashBoxId);
                    }
                });
            }

            if (accountTask.Result.IsSuccess && accountTask.Result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Accounts.Clear();
                    foreach (var acc in accountTask.Result.Value.OrderBy(a => a.NameAr))
                    {
                        Accounts.Add(acc);
                    }

                    if (IsEditMode && AccountId > 0)
                    {
                        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == AccountId);
                    }
                });
            }

            if (currencyTask.Result.IsSuccess && currencyTask.Result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Currencies.Clear();
                    foreach (var cur in currencyTask.Result.Value.Where(c => c.IsActive).OrderBy(c => c.Code))
                    {
                        Currencies.Add(cur);
                    }

                    if (IsEditMode && CurrencyId > 0)
                    {
                        SelectedCurrency = Currencies.FirstOrDefault(c => c.Id == CurrencyId);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "[ReceiptVoucherEditorViewModel.LoadLookupsAsync] Failed to load lookup data.");
            ErrorMessage = "فشل في تحميل بيانات القوائم المساعدة";
        }
    }

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
        var request = new CreateReceiptVoucherRequest(
            VoucherDate,
            CurrencyId,
            CashBoxId,
            AccountId,
            TotalAmount,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());

        var result = await _voucherService.CreateAsync(request);

        if (result.IsSuccess && result.Value != null)
        {
            _eventBus.Publish(new ReceiptVoucherChangedMessage(result.Value.Id));
            _toastService.ShowSuccess("تم إضافة سند القبض بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في إضافة سند القبض",
                "ReceiptVoucherEditorViewModel.CreateVoucherAsync",
                "[ReceiptVoucherEditorViewModel.CreateVoucherAsync] Failed to create voucher.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في حفظ سند القبض", error);
        }
    }

    private async Task UpdateVoucherAsync()
    {
        if (!_voucherId.HasValue) return;

        var request = new UpdateReceiptVoucherRequest(
            VoucherDate,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());

        var result = await _voucherService.UpdateAsync(_voucherId.Value, request);

        if (result.IsSuccess)
        {
            _eventBus.Publish(new ReceiptVoucherChangedMessage(_voucherId.Value));
            _toastService.ShowSuccess("تم تحديث سند القبض بنجاح");
            RequestClose();
        }
        else
        {
            var error = HandleFailure(result.Error ?? "فشل في تحديث سند القبض",
                "ReceiptVoucherEditorViewModel.UpdateVoucherAsync",
                "[ReceiptVoucherEditorViewModel.UpdateVoucherAsync] Failed to update voucher.");
            ErrorMessage = error;
            await _dialogService.ShowErrorAsync("خطأ في تحديث سند القبض", error);
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    public override void Cleanup()
    {
        _voucherId = null;
        IsEditMode = false;
        ErrorMessage = null;
        base.Cleanup();
    }

    #endregion
}
