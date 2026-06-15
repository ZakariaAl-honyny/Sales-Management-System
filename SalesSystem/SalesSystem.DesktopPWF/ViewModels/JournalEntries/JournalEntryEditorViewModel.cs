using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.DesktopPWF.ViewModels.JournalEntries;

/// <summary>
/// ViewModel for creating and editing journal entries.
/// Uses INotifyDataErrorInfo (via ViewModelBase) and validates on save.
/// RULE-227: SetDialogService called in constructor.
/// RULE-059: Save button always enabled — validates on click.
/// </summary>
public class JournalEntryEditorViewModel : ViewModelBase
{
    private readonly IJournalEntryApiService _journalEntryService;
    private readonly IAccountApiService _accountService;
    private readonly IDialogService _dialogService;
    private readonly IEventBus _eventBus;
    private readonly IToastNotificationService _toastService;

    private DateTime _transactionDate = DateTime.Today;
    private string _description = string.Empty;
    private readonly ObservableCollection<JournalEntryLineViewModel> _lines = new();
    private string? _errorMessage;
    private ObservableCollection<AccountDto> _availableAccounts = new();

    public JournalEntryEditorViewModel()
        : this(
            App.GetService<IJournalEntryApiService>(),
            App.GetService<IAccountApiService>(),
            App.GetService<IDialogService>(),
            App.GetService<IEventBus>(),
            App.GetService<IToastNotificationService>())
    {
    }

    public JournalEntryEditorViewModel(
        IJournalEntryApiService journalEntryService,
        IAccountApiService accountService,
        IDialogService dialogService,
        IEventBus eventBus,
        IToastNotificationService toastService)
    {
        _journalEntryService = journalEntryService ?? throw new ArgumentNullException(nameof(journalEntryService));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        SetDialogService(dialogService);

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddLineCommand = new RelayCommand(AddLine);
        CloseCommand = new RelayCommand(RequestClose);

        // Load accounts for selection + add first empty line
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteAsync(LoadAccountsOperationAsync);
        if (_lines.Count == 0)
            AddLine(); // Start with one empty line
    }

    #region Properties

    public DateTime TransactionDate
    {
        get => _transactionDate;
        set
        {
            if (SetProperty(ref _transactionDate, value))
            {
                ClearErrors(nameof(TransactionDate));
                if (value == default || value.Year < 2000)
                    AddError(nameof(TransactionDate), "تاريخ القيد غير صالح");
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                ClearErrors(nameof(Description));
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Description), "البيان مطلوب");
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<JournalEntryLineViewModel> Lines => _lines;

    public ObservableCollection<AccountDto> AvailableAccounts
    {
        get => _availableAccounts;
        set => SetProperty(ref _availableAccounts, value);
    }

    #endregion

    #region Computed Totals

    private decimal _totalDebit;
    public decimal TotalDebit
    {
        get => _totalDebit;
        private set => SetProperty(ref _totalDebit, value);
    }

    private decimal _totalCredit;
    public decimal TotalCredit
    {
        get => _totalCredit;
        private set => SetProperty(ref _totalCredit, value);
    }

    private decimal _difference;
    public decimal Difference
    {
        get => _difference;
        private set => SetProperty(ref _difference, value);
    }

    private bool _isBalanced;
    public bool IsBalanced
    {
        get => _isBalanced;
        private set => SetProperty(ref _isBalanced, value);
    }

    /// <summary>
    /// Called by JournalEntryLineViewModel when Debit/Credit changes.
    /// Recomputes totals and balance status.
    /// </summary>
    public void NotifyTotalsChanged()
    {
        TotalDebit = _lines.Sum(l => l.Debit);
        TotalCredit = _lines.Sum(l => l.Credit);
        Difference = TotalDebit - TotalCredit;
        IsBalanced = Math.Abs(Difference) < 0.01m;
        OnPropertyChanged(nameof(TotalDebitFormatted));
        OnPropertyChanged(nameof(TotalCreditFormatted));
        OnPropertyChanged(nameof(DifferenceFormatted));
    }

    public string TotalDebitFormatted => $"{TotalDebit:N2}";
    public string TotalCreditFormatted => $"{TotalCredit:N2}";
    public string DifferenceFormatted => $"{Difference:N2}";

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand AddLineCommand { get; private set; } = null!;
    public ICommand CloseCommand { get; private set; } = null!;

    #endregion

    #region Operations

    private async Task LoadAccountsOperationAsync()
    {
        ErrorMessage = null;
        var result = await _accountService.GetAllAsync();

        if (result.IsSuccess && result.Value != null)
        {
            // Only show leaf accounts (AllowTransactions = true)
            var leafAccounts = result.Value
                .Where(a => a.AllowTransactions && a.IsActive)
                .OrderBy(a => a.AccountCode)
                .ToList();

            await InvokeOnUIThreadAsync(async () =>
            {
                AvailableAccounts = new ObservableCollection<AccountDto>(leafAccounts);
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الحسابات", "LoadAccounts");
        }
    }

    private void AddLine()
    {
        var line = new JournalEntryLineViewModel(this);
        Lines.Add(line);
        NotifyTotalsChanged();
    }

    public void RemoveLine(JournalEntryLineViewModel line)
    {
        if (Lines.Count <= 1)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "يجب أن يحتوي القيد على بند واحد على الأقل.");
            return;
        }

        Lines.Remove(line);
        NotifyTotalsChanged();
    }

    private async Task SaveAsync()
    {
        if (!await ValidateAsync())
            return;

        await ExecuteAsync(SaveOperationAsync);
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        // Manual trigger of property-level validation
        if (string.IsNullOrWhiteSpace(_description))
            AddError(nameof(Description), "البيان مطلوب");

        if (_transactionDate == default || _transactionDate.Year < 2000)
            AddError(nameof(TransactionDate), "تاريخ القيد غير صالح");

        // Line-level validation
        if (_lines.Count == 0)
        {
            await _dialogService.ShowWarningAsync("بيانات غير مكتملة",
                "يجب إضافة بند واحد على الأقل للقيد اليومي.");
            return false;
        }

        var lineErrors = new List<string>();
        foreach (var line in _lines)
        {
            if (line.AccountId == 0)
                lineErrors.Add("• الحساب مطلوب لجميع البنود");
            if (line.Debit <= 0 && line.Credit <= 0)
                lineErrors.Add("• كل بند يجب أن يحتوي على مبلغ مدين أو دائن");
            if (line.Debit > 0 && line.Credit > 0)
                lineErrors.Add("• لا يمكن أن يحتوي البند على مدين ودائن معاً");
        }

        if (lineErrors.Any())
        {
            await _dialogService.ShowWarningAsync("بيانات غير مكتملة",
                "يرجى تصحيح الأخطاء التالية في بنود القيد:\n\n" + string.Join("\n", lineErrors.Distinct()));
            return false;
        }

        // Check balance
        NotifyTotalsChanged();
        if (!IsBalanced)
        {
            await _dialogService.ShowWarningAsync("خطأ في التوازن",
                $"القيد غير متوازن.\nإجمالي المدين: {TotalDebit:N2}\nإجمالي الدائن: {TotalCredit:N2}\nالفرق: {Difference:N2}\n\nيجب أن يتساوى إجمالي المدين مع إجمالي الدائن.");
            return false;
        }

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        ErrorMessage = null;

        var request = new CreateJournalEntryRequest(
            EntryDate: _transactionDate,
            Description: _description.Trim(),
            EntryType: JournalEntryType.Manual,
            ReferenceType: null,
            ReferenceId: null,
            ReferenceNumber: null,
            Lines: _lines.Select(l => new JournalEntryLineRequest(
                AccountId: l.AccountId,
                Debit: l.Debit,
                Credit: l.Credit,
                Description: l.Description?.Trim()
            )).ToList()
        );

        var result = await _journalEntryService.CreateAsync(request);

        if (result.IsSuccess)
        {
            var entryId = result.Value;
            _toastService.ShowSuccess($"تم إنشاء القيد رقم {entryId} بنجاح");
            _eventBus.Publish(new JournalEntryChangedMessage(entryId));
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ القيد", "SaveJournalEntry");
        }
    }

    #endregion

    #region Cleanup

    public override void Cleanup()
    {
        _lines.Clear();
        base.Cleanup();
    }

    #endregion
}
