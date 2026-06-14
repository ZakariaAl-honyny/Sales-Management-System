using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Expense;

public class ExpenseEditorViewModel : ViewModelBase
{
    private readonly IExpenseApiService _expenseService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _expenseId;
    private int _expenseNo;
    private DateTime _expenseDate = DateTime.Today;
    private int _expenseAccountId;
    private string? _expenseAccountName;
    private int _cashBoxId;
    private int _currencyId;
    private decimal _amount;
    private string? _notes;
    private bool _isEditMode;
    private string? _errorMessage;

    public ExpenseEditorViewModel()
    {
        _expenseService = App.GetService<IExpenseApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ المصروف...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل مصروف" : "إضافة مصروف جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public int ExpenseNo
    {
        get => _expenseNo;
        set => SetProperty(ref _expenseNo, value);
    }

    public DateTime ExpenseDate
    {
        get => _expenseDate;
        set => SetProperty(ref _expenseDate, value);
    }

    public int ExpenseAccountId
    {
        get => _expenseAccountId;
        set
        {
            if (SetProperty(ref _expenseAccountId, value))
            {
                if (value <= 0)
                    AddError(nameof(ExpenseAccountId), "يجب اختيار حساب المصروف");
                else
                    ClearErrors(nameof(ExpenseAccountId));
            }
        }
    }

    public string? ExpenseAccountName
    {
        get => _expenseAccountName;
        set => SetProperty(ref _expenseAccountName, value);
    }

    public int CashBoxId
    {
        get => _cashBoxId;
        set
        {
            if (SetProperty(ref _cashBoxId, value))
            {
                if (value <= 0)
                    AddError(nameof(CashBoxId), "يجب اختيار الصندوق");
                else
                    ClearErrors(nameof(CashBoxId));
            }
        }
    }

    public int CurrencyId
    {
        get => _currencyId;
        set
        {
            if (SetProperty(ref _currencyId, value))
            {
                if (value <= 0)
                    AddError(nameof(CurrencyId), "يجب اختيار العملة");
                else
                    ClearErrors(nameof(CurrencyId));
            }
        }
    }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                if (value <= 0)
                    AddError(nameof(Amount), "المبلغ يجب أن يكون أكبر من صفر");
                else
                    ClearErrors(nameof(Amount));
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public void LoadExpense(ExpenseDto expense)
    {
        _expenseId = expense.Id;
        _expenseNo = expense.ExpenseNo;
        _expenseDate = expense.ExpenseDate;
        _expenseAccountId = expense.ExpenseAccountId;
        _expenseAccountName = expense.ExpenseAccountName;
        _cashBoxId = expense.CashBoxId;
        _currencyId = expense.CurrencyId;
        _amount = expense.Amount;
        _notes = expense.Notes;
        _isEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (ExpenseAccountId <= 0)
            AddError(nameof(ExpenseAccountId), "يجب اختيار حساب المصروف");

        if (CashBoxId <= 0)
            AddError(nameof(CashBoxId), "يجب اختيار الصندوق");

        if (CurrencyId <= 0)
            AddError(nameof(CurrencyId), "يجب اختيار العملة");

        if (Amount <= 0)
            AddError(nameof(Amount), "المبلغ يجب أن يكون أكبر من صفر");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode)
        {
            var request = new UpdateExpenseRequest(
                ExpenseDate: ExpenseDate,
                ExpenseAccountId: ExpenseAccountId,
                CashBoxId: CashBoxId,
                CurrencyId: CurrencyId,
                Amount: Amount,
                Notes: Notes);

            var result = await _expenseService.UpdateAsync(_expenseId, request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم تحديث المصروف بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث المصروف", "ExpenseEditorViewModel.SaveAsync");
            }
        }
        else
        {
            var request = new CreateExpenseRequest(
                ExpenseDate: ExpenseDate,
                ExpenseAccountId: ExpenseAccountId,
                CashBoxId: CashBoxId,
                CurrencyId: CurrencyId,
                Amount: Amount,
                Notes: Notes);

            var result = await _expenseService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إضافة المصروف بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إضافة المصروف", "ExpenseEditorViewModel.SaveAsync");
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
