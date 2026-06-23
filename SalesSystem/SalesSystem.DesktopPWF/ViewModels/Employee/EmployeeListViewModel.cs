using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Employee;

public class EmployeeListViewModel : ViewModelBase
{
    private readonly IEmployeeApiService _employeeService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<EmployeeDto> _employees = new();
    private ICollectionView? _employeesView;
    private EmployeeDto? _selectedEmployee;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public EmployeeListViewModel()
    {
        _employeeService = App.GetService<IEmployeeApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadEmployeesOperationAsync,
                ex => ErrorMessage = HandleException(ex, "EmployeeListViewModel.LoadEmployeesAsync"))));
        AddCommand = new RelayCommand(AddEmployee);
        EditCommand = new RelayCommand(EditEmployee);
        DeleteCommand = new AsyncRelayCommand(DeleteEmployeeAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties

    public ObservableCollection<EmployeeDto> Employees
    {
        get => _employees;
        set => SetProperty(ref _employees, value);
    }

    public ICollectionView? EmployeesView
    {
        get => _employeesView;
        private set => SetProperty(ref _employeesView, value);
    }

    public EmployeeDto? SelectedEmployee
    {
        get => _selectedEmployee;
        set => SetProperty(ref _selectedEmployee, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                EmployeesView?.Refresh();
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

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value))
            {
                _ = LoadEmployeesAsync();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadEmployeesAsync()
    {
        await ExecuteAsync(LoadEmployeesOperationAsync,
            ex => ErrorMessage = HandleException(ex, "EmployeeListViewModel.LoadEmployeesAsync"));
    }

    private async Task LoadEmployeesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _employeeService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Employees.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Employees.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Employees.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الموظفين", "EmployeeListViewModel.LoadEmployeesAsync");
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            IsEmpty = Employees.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        EmployeesView = CollectionViewSource.GetDefaultView(Employees);
        EmployeesView.Filter = FilterEmployees;
    }

    private bool FilterEmployees(object obj)
    {
        if (obj is not EmployeeDto emp) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLower();
        return emp.Name.ToLower().Contains(search) ||
               emp.EmployeeNo.ToString().Contains(search) ||
               (emp.DepartmentName?.ToLower().Contains(search) ?? false);
    }

    private void AddEmployee()
    {
        var editorVm = App.GetService<EmployeeEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "موظف جديد",
            Width = 650,
            Height = 600,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadEmployeesAsync());
            }
        });
    }

    private void EditEmployee()
    {
        if (SelectedEmployee == null) return;

        var editorVm = App.GetService<EmployeeEditorViewModel>();
        editorVm.LoadEmployee(SelectedEmployee);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل موظف",
            Width = 650,
            Height = 600,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadEmployeesAsync());
            }
        });
    }

    public async Task DeleteEmployeeAsync()
    {
        if (SelectedEmployee == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الموظف: {SelectedEmployee.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        var emp = SelectedEmployee;
        await ExecuteAsync(() => DeleteEmployeeOperationAsync(strategy, emp),
            ex => ErrorMessage = HandleException(ex, "EmployeeListViewModel.DeleteEmployeeAsync"));
    }

    private async Task DeleteEmployeeOperationAsync(DeleteStrategy strategy, EmployeeDto emp)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var result = await _employeeService.DeactivateAsync(emp.Id);
            if (result.IsSuccess)
            {
                await LoadEmployeesAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط الموظف بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط الموظف", "EmployeeListViewModel.DeleteEmployeeAsync");
                await _dialogService.ShowErrorAsync("خطأ في الحذف", ErrorMessage!);
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكن حذف الموظف بشكل نهائي. يمكنك إلغاء تنشيطه فقط.");
        }
    }

    private void Search()
    {
        EmployeesView?.Refresh();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
