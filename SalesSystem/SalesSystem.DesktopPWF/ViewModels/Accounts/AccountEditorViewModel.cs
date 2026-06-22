using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Accounts;

/// <summary>
/// Editor for creating or modifying chart-of-accounts entries.
/// The parent account selector shows a hierarchical (indented) list.
/// AccountType and Level are auto-filled from the selected parent — the
/// user primarily just enters the account name.
/// </summary>
public class AccountEditorViewModel : ViewModelBase
{
    private readonly IAccountApiService _accountService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly int? _editAccountId;
    private List<AccountDto> _allAccounts = new();

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
        _editAccountId = editAccountId;
        SetDialogService(_dialogService); // RULE-227

        if (parentAccountId.HasValue)
            _selectedParentId = parentAccountId.Value;

        SaveCommand = new AsyncRelayCommand(SaveAsync); // ALWAYS enabled (RULE-059)
        CancelCommand = new RelayCommand(() => RequestClose());

        LoadedCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadInitialDataAsync)));

        _ = ExecuteAsync(LoadInitialDataAsync);
    }

    // ── Properties ──
    public int? AccountId { get; private set; }
    public bool IsSaved { get; private set; }
    public bool IsEditing => _editAccountId.HasValue;
    public bool IsCreating => !IsEditing;

    private string _windowTitle = "إضافة حساب جديد";
    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }

    private string _accountCode = string.Empty;
    public string AccountCode
    {
        get => _accountCode;
        set => SetProperty(ref _accountCode, value);
    }

    // ── Name (the only required user input for creation) ──
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

    // ── Auto-filled from parent ──
    private byte _accountType = 1;
    public byte AccountType
    {
        get => _accountType;
        set => SetProperty(ref _accountType, value);
    }

    /// <summary>Display string for the account type (auto-filled from parent).</summary>
    public string AccountTypeDisplay => AccountType switch
    {
        1 => "أصل",
        2 => "خصم",
        3 => "حق ملكية",
        4 => "إيراد",
        5 => "مصروف",
        _ => "غير معروف"
    };

    private int _level = 1;
    public int Level
    {
        get => _level;
        set
        {
            if (SetProperty(ref _level, value))
            {
                OnPropertyChanged(nameof(LevelDisplay));
                if (value >= 4)
                {
                    AllowTransactions = true;
                    IsAllowTransactionsEditable = false;
                }
                else
                {
                    IsAllowTransactionsEditable = true;
                }
            }
        }
    }

    public string LevelDisplay => Level switch
    {
        1 => "1 — رئيسي (مجموعة)",
        2 => "2 — فرعي",
        3 => "3 — فرعي فرعي",
        _ => "4 — تفصيلي"
    };

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private bool _allowTransactions;
    public bool AllowTransactions
    {
        get => _allowTransactions;
        set => SetProperty(ref _allowTransactions, value);
    }

    private bool _isAllowTransactionsEditable;
    public bool IsAllowTransactionsEditable
    {
        get => _isAllowTransactionsEditable;
        set => SetProperty(ref _isAllowTransactionsEditable, value);
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

    // ── Parent Account ──
    private int? _selectedParentId;
    public int? SelectedParentId
    {
        get => _selectedParentId;
        set
        {
            if (SetProperty(ref _selectedParentId, value))
            {
                AutoFillFromParent();
                OnPropertyChanged(nameof(SelectedParentDisplay));
            }
        }
    }

    public string SelectedParentDisplay
    {
        get
        {
            if (!_selectedParentId.HasValue || _selectedParentId.Value == 0)
                return "— المستوى الأول (بدون حساب أب) —";
            var parent = _allAccounts.FirstOrDefault(a => a.Id == _selectedParentId.Value);
            return parent != null ? $"{parent.AccountCode} — {parent.NameAr}" : "حساب غير معروف";
        }
    }

    /// <summary>
    /// Hierarchical parent-account list.
    /// Accounts are sorted by AccountCode and indented by Level.
    /// A "root" entry (Id=0) is prepended so Level-1 accounts can be created.
    /// </summary>
    public List<KeyValuePair<int, string>> ParentAccountOptions
    {
        get
        {
            var options = _allAccounts
                .Where(a => a.Level < 4 && (!_editAccountId.HasValue || a.Id != _editAccountId.Value))
                .OrderBy(a => a.AccountCode)
                .Select(a => new KeyValuePair<int, string>(
                    a.Id,
                    $"{new string('　', a.Level - 1)}{a.AccountCode} — {a.NameAr}"))
                .ToList();

            options.Insert(0, new KeyValuePair<int, string>(0, "الحساب الرئيسي (المستوى الأول)"));
            return options;
        }
    }

    /// <summary>
    /// Auto-fills AccountType, Level, and AllowTransactions from the selected parent.
    /// </summary>
    private void AutoFillFromParent()
    {
        if (_selectedParentId.HasValue && _selectedParentId.Value > 0)
        {
            var parent = _allAccounts.FirstOrDefault(a => a.Id == _selectedParentId.Value);
            if (parent != null)
            {
                AccountType = parent.AccountType;
                Level = parent.Level + 1;
                OnPropertyChanged(nameof(AccountTypeDisplay));
                OnPropertyChanged(nameof(LevelDisplay));
            }
        }
        else
        {
            // No parent → Level 1, default AccountType = Asset
            Level = 1;
            AccountType = 1;
            AllowTransactions = false;
            IsAllowTransactionsEditable = false;
            OnPropertyChanged(nameof(AccountTypeDisplay));
            OnPropertyChanged(nameof(LevelDisplay));
        }
    }

    // ── Commands ──
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand LoadedCommand { get; private set; } = null!;

    // ── Validation (RULE-229) ──
    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(NameAr))
            AddError(nameof(NameAr), "اسم الحساب بالعربية مطلوب");

        if (Level < 1 || Level > 10)
            AddError(nameof(Level), "مستوى الحساب يجب أن يكون بين 1 و 10");

        if (Level >= 4 && !AllowTransactions)
            AddError(nameof(AllowTransactions), "الحساب التفصيلي يجب أن يسمح بالحركات");

        if (OpeningBalance.HasValue && OpeningBalance.Value < 0)
            AddError(nameof(OpeningBalance), "الرصيد الافتتاحي لا يمكن أن يكون سالباً");

        return await ValidateAllAsync();
    }

    // ── Save ──
    private async Task SaveAsync()
    {
        if (!await ValidateAsync()) return;

        await ExecuteAsync(async () =>
        {
            var parentId = _selectedParentId.GetValueOrDefault(0) > 0 ? _selectedParentId : null;

            if (IsEditing)
            {
                var updateRequest = new UpdateAccountRequest(
                    NameAr, NameEn, AccountType, Level >= 4,
                    parentId, null, Description, null);

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
                var createRequest = new CreateAccountRequest(
                    NameAr, NameEn, AccountType, Level >= 4,
                    parentId, false, null, Description, null, OpeningBalance);

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
        // Load all accounts for the parent dropdown
        var allResult = await _accountService.GetAllAsync();
        if (allResult.IsSuccess && allResult.Value != null)
        {
            _allAccounts = allResult.Value;
            OnPropertyChanged(nameof(ParentAccountOptions));
        }

        if (_editAccountId.HasValue)
        {
            // ── Edit mode ──
            var result = await _accountService.GetByIdAsync(_editAccountId.Value);
            if (result.IsSuccess && result.Value != null)
            {
                var a = result.Value;
                AccountId = a.Id;
                AccountCode = a.AccountCode;
                NameAr = a.NameAr;
                NameEn = a.NameEn ?? string.Empty;
                AccountType = a.AccountType;
                Level = a.Level;
                Description = a.Description ?? string.Empty;
                AllowTransactions = a.AllowTransactions;
                WindowTitle = "تعديل حساب";

                _selectedParentId = a.ParentId;
                OnPropertyChanged(nameof(SelectedParentId));
                OnPropertyChanged(nameof(SelectedParentDisplay));
                OnPropertyChanged(nameof(AccountTypeDisplay));
                OnPropertyChanged(nameof(LevelDisplay));

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
            // ── Create mode ──
            if (_selectedParentId.HasValue)
            {
                AutoFillFromParent();
                OnPropertyChanged(nameof(SelectedParentDisplay));
            }
        }
    }
}
