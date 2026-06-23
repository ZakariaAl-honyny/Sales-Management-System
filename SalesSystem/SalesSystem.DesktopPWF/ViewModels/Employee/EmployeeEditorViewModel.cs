using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;

namespace SalesSystem.DesktopPWF.ViewModels.Employee;

public class EmployeeEditorViewModel : ViewModelBase
{
    private readonly IEmployeeApiService _employeeService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private int _employeeId;
    private string _name = string.Empty;
    private int _employeeNo;
    private DateTime _hireDate = DateTime.Today;
    private int? _departmentId;
    private decimal _salary;
    private string? _notes;
    private bool _isActive = true;
    private bool _isEditMode;
    private string? _errorMessage;

    public EmployeeEditorViewModel()
    {
        _employeeService = App.GetService<IEmployeeApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الموظف...")));
        CancelCommand = new RelayCommand(Cancel);
    }

    #region Properties

    public string Title => IsEditMode ? "تعديل موظف" : "إضافة موظف جديد";

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                    AddError(nameof(Name), "اسم الموظف مطلوب");
                else
                    ClearErrors(nameof(Name));
            }
        }
    }

    public int EmployeeNo
    {
        get => _employeeNo;
        set => SetProperty(ref _employeeNo, value);
    }

    public DateTime HireDate
    {
        get => _hireDate;
        set => SetProperty(ref _hireDate, value);
    }

    public int? DepartmentId
    {
        get => _departmentId;
        set => SetProperty(ref _departmentId, value);
    }

    public decimal Salary
    {
        get => _salary;
        set => SetProperty(ref _salary, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

    public void LoadEmployee(EmployeeDto employee)
    {
        _employeeId = employee.Id;
        _name = employee.Name;
        _employeeNo = employee.EmployeeNo;
        _hireDate = employee.HireDate;
        _departmentId = employee.DepartmentId;
        _salary = employee.Salary;
        _notes = employee.Notes;
        _isActive = employee.IsActive;
        _isEditMode = true;
    }

    private async Task<bool> ValidateAsync()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(Name))
            AddError(nameof(Name), "اسم الموظف مطلوب");
        if (_hireDate == default || _hireDate > DateTime.Today)
            AddError(nameof(HireDate), "تاريخ التوظيف غير صالح");

        return await ValidateAllAsync();
    }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        ErrorMessage = null;

        if (IsEditMode)
        {
            var request = new UpdateEmployeeRequest(
                Name: _name,
                Phone: null,
                Email: null,
                Address: null,
                DepartmentId: _departmentId,
                Salary: _salary > 0 ? _salary : null,
                Notes: _notes);

            var result = await _employeeService.UpdateAsync(_employeeId, request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم تحديث الموظف بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث الموظف", "EmployeeEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في تحديث الموظف", ErrorMessage!);
            }
        }
        else
        {
            var request = new CreateEmployeeRequest(
                Name: _name,
                EmployeeNo: _employeeNo > 0 ? _employeeNo : 0,
                HireDate: _hireDate,
                Phone: null,
                Email: null,
                Address: null,
                DepartmentId: _departmentId,
                Salary: _salary,
                Notes: _notes);

            var result = await _employeeService.CreateAsync(request);

            if (result.IsSuccess)
            {
                _toastService.ShowSuccess("تم إضافة الموظف بنجاح");
                RequestClose();
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إضافة الموظف", "EmployeeEditorViewModel.SaveAsync");
                await _dialogService.ShowErrorAsync("خطأ في إضافة الموظف", ErrorMessage!);
            }
        }
    }

    private void Cancel()
    {
        RequestClose();
    }

    #endregion
}
