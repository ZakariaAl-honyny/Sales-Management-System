using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Accounts;

public class AccountEditorViewModel : ViewModelBase
{
    private readonly IAccountApiService _accountService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly int? _parentAccountId;
    private readonly int? _editAccountId;

    public AccountEditorViewModel(
        IAccountApiService accountService,
        IDialogService dialogService,
        IToastNotificationService toastService,
        int? parentAccountId = null,
        int? editAccountId = null)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? App.GetService<IToastNotificationService>();
        _parentAccountId = parentAccountId;
        _editAccountId = editAccountId;
        SetDialogService(_dialogService); // RULE-227

        SaveCommand = new AsyncRelayCommand(SaveAsync); // ALWAYS enabled (RULE-059)
        CancelCommand = new RelayCommand(() => RequestClose());

        LoadedCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadInitialDataAsync)));
    }

    // ── Properties ──
    public int? AccountId { get; private set; }
    public bool IsSaved { get; private set; }
    public bool IsEditing => _editAccountId.HasValue;

    private string _windowTitle = "إضافة حساب جديد";
    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }

    private bool _isAccountCodeReadOnly;
    public bool IsAccountCodeReadOnly
    {
        get => _isAccountCodeReadOnly;
        set => SetProperty(ref _isAccountCodeReadOnly, value);
    }

    private string _accountCode = string.Empty;
    public string AccountCode
    {
        get => _accountCode;
        set
        {
            if (SetProperty(ref _accountCode, value))
            {
                ClearErrors(nameof(AccountCode));
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(AccountCode), "رمز الحساب مطلوب");
                else if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{4,10}$"))
                    AddError(nameof(AccountCode), "رمز الحساب يجب أن يكون أرقاماً فقط (4-10 خانات)");
            }
        }
    }

    private string _nameAr = string.Empty;
    public string NameAr
    {
        get => _nameAr;
        set
        {
            if (SetProperty(ref _nameAr, value))
            {
                ClearErrors(nameof(NameAr));
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(NameAr), "اسم الحساب بالعربية مطلوب");
            }
        }
    }

    private string _nameEn = string.Empty;
    public string NameEn
    {
        get => _nameEn;
        set => SetProperty(ref _nameEn, value);
    }

    private byte _accountType = 1;
    public byte AccountType
    {
        get => _accountType;
        set => SetProperty(ref _accountType, value);
    }

    public List<KeyValuePair<byte, string>> AccountTypes { get; } = new()
    {
        new(1, "أصل"),
        new(2, "خصم"),
        new(3, "حق ملكية"),
        new(4, "إيراد"),
        new(5, "مصروف")
    };

    public List<KeyValuePair<int, string>> LevelOptions { get; } = new()
    {
        new(1, "1 — رئيسي (مجموعة)"),
        new(2, "2 — فرعي"),
        new(3, "3 — فرعي فرعي"),
        new(4, "4 — تفصيلي")
    };

    private int _level = 4;
    public int Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private string _colorCode = string.Empty;
    public string ColorCode
    {
        get => _colorCode;
        set => SetProperty(ref _colorCode, value);
    }

    private bool _allowTransactions = true;
    public bool AllowTransactions
    {
        get => _allowTransactions;
        set => SetProperty(ref _allowTransactions, value);
    }

    private decimal? _openingBalance;
    public decimal? OpeningBalance
    {
        get => _openingBalance;
        set => SetProperty(ref _openingBalance, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    // ── Commands ──
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand LoadedCommand { get; private set; } = null!;

    // ── Validation (RULE-229) ──
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors(); // RULE-229

        if (string.IsNullOrWhiteSpace(AccountCode))
            AddError(nameof(AccountCode), "رمز الحساب مطلوب");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(AccountCode, @"^\d{4,10}$"))
            AddError(nameof(AccountCode), "رمز الحساب يجب أن يكون أرقاماً فقط (4-10 خانات)");

        if (string.IsNullOrWhiteSpace(NameAr))
            AddError(nameof(NameAr), "اسم الحساب بالعربية مطلوب");

        if (Level < 1 || Level > 10)
            AddError(nameof(Level), "مستوى الحساب يجب أن يكون بين 1 و 10");

        if (Level >= 4 && !AllowTransactions)
            AddError(nameof(AllowTransactions), "الحساب التفصيلي يجب أن يسمح بالحركات");

        return await ValidateAllAsync(); // Shows styled validation dialog automatically
    }

    // ── Save ──
    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            if (IsEditing)
            {
                // ── Update existing account ──
                var updateRequest = new UpdateAccountRequest(
                    NameAr, NameEn, AccountType, Level >= 4,
                    _parentAccountId, null);

                var result = await _accountService.UpdateAsync(_editAccountId!.Value, updateRequest);
                if (result.IsSuccess)
                {
                    IsSaved = true;
                    _toastService.ShowSuccess("تم تعديل الحساب بنجاح");
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في تعديل الحساب", "AccountEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في تعديل الحساب", ErrorMessage);
                }
            }
            else
            {
                // ── Create new account ──
                var createRequest = new CreateAccountRequest(
                    AccountCode, NameAr, NameEn, AccountType, Level >= 4,
                    _parentAccountId, false, null);

                var result = await _accountService.CreateAsync(createRequest);
                if (result.IsSuccess)
                {
                    IsSaved = true;
                    _toastService.ShowSuccess("تم إنشاء الحساب بنجاح");
                    RequestClose();
                }
                else
                {
                    ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الحساب", "AccountEditorViewModel.SaveAsync");
                    await _dialogService.ShowErrorAsync("خطأ في حفظ الحساب", ErrorMessage);
                }
            }
        });
    }

    private async Task LoadInitialDataAsync()
    {
        if (_editAccountId.HasValue)
        {
            // ── Edit mode: load existing account data ──
            var result = await _accountService.GetByIdAsync(_editAccountId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                var account = result.Value;
                AccountId = account.Id;
                AccountCode = account.AccountCode;
                NameAr = account.NameAr;
                NameEn = account.NameEn ?? string.Empty;
                AccountType = account.AccountType;
                Level = account.Level;
                Description = account.Description ?? string.Empty;
                ColorCode = account.ColorCode ?? string.Empty;
                AllowTransactions = account.AllowTransactions;
                OpeningBalance = account.OpeningBalance;
                WindowTitle = "تعديل حساب";
                IsAccountCodeReadOnly = true;

                // Clear validation errors after loading data
                ClearAllErrors();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل بيانات الحساب", "LoadInitialDataAsync");
                await _dialogService.ShowErrorAsync("خطأ في تحميل الحساب", ErrorMessage);
                RequestClose();
            }
        }
        else
        {
            // ── Create mode: auto-set Level based on parent if provided ──
            if (_parentAccountId.HasValue)
            {
                var result = await _accountService.GetByIdAsync(_parentAccountId.Value);
                if (result.IsSuccess && result.Value != null)
                {
                    Level = result.Value.Level + 1;
                }
            }
        }
    }

}
