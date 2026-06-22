using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Expense;

public class ExpenseListViewModel : ViewModelBase
{
    private readonly IExpenseApiService _expenseService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<ExpenseDto> _expenses = new();
    private ICollectionView? _expensesView;
    private ExpenseDto? _selectedExpense;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;

    public ExpenseListViewModel()
    {
        _expenseService = App.GetService<IExpenseApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadExpensesOperationAsync,
                ex => ErrorMessage = HandleException(ex, "ExpenseListViewModel.LoadExpensesAsync"))));
        AddCommand = new RelayCommand(AddExpense);
        EditCommand = new RelayCommand(EditExpense);
        DeleteCommand = new AsyncRelayCommand(DeleteExpenseAsync);
        PostCommand = new AsyncRelayCommand(PostExpenseAsync);
        CancelCommand = new AsyncRelayCommand(CancelExpenseAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties

    public ObservableCollection<ExpenseDto> Expenses
    {
        get => _expenses;
        set => SetProperty(ref _expenses, value);
    }

    public ICollectionView? ExpensesView
    {
        get => _expensesView;
        private set => SetProperty(ref _expensesView, value);
    }

    public ExpenseDto? SelectedExpense
    {
        get => _selectedExpense;
        set => SetProperty(ref _selectedExpense, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ExpensesView?.Refresh();
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

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand PostCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadExpensesAsync()
    {
        await ExecuteAsync(LoadExpensesOperationAsync,
            ex => ErrorMessage = HandleException(ex, "ExpenseListViewModel.LoadExpensesAsync"));
    }

    private async Task LoadExpensesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _expenseService.GetAllAsync(
            search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Expenses.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Expenses.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Expenses.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المصروفات", "ExpenseListViewModel.LoadExpensesAsync");
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            IsEmpty = Expenses.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        ExpensesView = CollectionViewSource.GetDefaultView(Expenses);
        ExpensesView.Filter = FilterExpenses;
    }

    private bool FilterExpenses(object obj)
    {
        if (obj is not ExpenseDto expense) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLower();
        return (expense.Notes?.ToLower().Contains(search) ?? false) ||
               expense.ExpenseNo.ToString().Contains(search);
    }

    private void AddExpense()
    {
        var editorVm = App.GetService<ExpenseEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "مصروف جديد",
            Width = 700,
            Height = 600,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadExpensesAsync());
            }
        });
    }

    private void EditExpense()
    {
        if (SelectedExpense == null) return;

        var editorVm = App.GetService<ExpenseEditorViewModel>();
        editorVm.LoadExpense(SelectedExpense);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل مصروف",
            Width = 700,
            Height = 600,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadExpensesAsync());
            }
        });
    }

    public async Task DeleteExpenseAsync()
    {
        if (SelectedExpense == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المصروف رقم: {SelectedExpense.ExpenseNo}");

        if (strategy == DeleteStrategy.Cancel) return;

        var expense = SelectedExpense;
        await ExecuteAsync(() => DeleteExpenseOperationAsync(strategy, expense),
            ex => ErrorMessage = HandleException(ex, "ExpenseListViewModel.DeleteExpenseAsync"));
    }

    private async Task DeleteExpenseOperationAsync(DeleteStrategy strategy, ExpenseDto expense)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var result = await _expenseService.DeleteAsync(expense.Id);
            if (result.IsSuccess)
            {
                await LoadExpensesAsync();
                _toastService.ShowSuccess("تم حذف المصروف بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في حذف المصروف", "ExpenseListViewModel.DeleteExpenseAsync");
                await _dialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage!);
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكن حذف المصروف بشكل نهائي.");
        }
    }

    public async Task PostExpenseAsync()
    {
        if (SelectedExpense == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync("تأكيد الترحيل",
            "هل أنت متأكد من ترحيل هذا المصروف؟ لن يمكن تعديله بعد الترحيل.");

        if (!confirm) return;

        var expense = SelectedExpense;
        await ExecuteAsync(() => PostExpenseOperationAsync(expense),
            ex => ErrorMessage = HandleException(ex, "ExpenseListViewModel.PostExpenseAsync"));
    }

    private async Task PostExpenseOperationAsync(ExpenseDto expense)
    {
        ErrorMessage = null;
        var result = await _expenseService.PostAsync(expense.Id);

        if (result.IsSuccess)
        {
            await LoadExpensesAsync();
            _toastService.ShowSuccess("تم ترحيل المصروف بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في ترحيل المصروف", "ExpenseListViewModel.PostExpenseAsync");
            await _dialogService.ShowErrorAsync("خطأ في الترحيل", ErrorMessage!);
        }
    }

    public async Task CancelExpenseAsync()
    {
        if (SelectedExpense == null) return;

        var confirm = await _dialogService.ShowConfirmationAsync("تأكيد الإلغاء",
            "هل أنت متأكد من إلغاء هذا المصروف؟");

        if (!confirm) return;

        var expense = SelectedExpense;
        await ExecuteAsync(() => CancelExpenseOperationAsync(expense),
            ex => ErrorMessage = HandleException(ex, "ExpenseListViewModel.CancelExpenseAsync"));
    }

    private async Task CancelExpenseOperationAsync(ExpenseDto expense)
    {
        ErrorMessage = null;
        var result = await _expenseService.CancelAsync(expense.Id);

        if (result.IsSuccess)
        {
            await LoadExpensesAsync();
            _toastService.ShowSuccess("تم إلغاء المصروف بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء المصروف", "ExpenseListViewModel.CancelExpenseAsync");
            await _dialogService.ShowErrorAsync("خطأ في الإلغاء", ErrorMessage!);
        }
    }

    private void Search()
    {
        ExpensesView?.Refresh();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
