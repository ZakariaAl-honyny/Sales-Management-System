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

namespace SalesSystem.DesktopPWF.ViewModels.Department;

public class DepartmentListViewModel : ViewModelBase
{
    private readonly IDepartmentApiService _departmentService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<DepartmentDto> _departments = new();
    private ICollectionView? _departmentsView;
    private DepartmentDto? _selectedDepartment;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public DepartmentListViewModel()
    {
        _departmentService = App.GetService<IDepartmentApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDepartmentsOperationAsync,
                ex => ErrorMessage = HandleException(ex, "DepartmentListViewModel.LoadDepartmentsAsync"))));
        AddCommand = new RelayCommand(AddDepartment);
        EditCommand = new RelayCommand(EditDepartment);
        DeleteCommand = new AsyncRelayCommand(DeleteDepartmentAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties

    public ObservableCollection<DepartmentDto> Departments
    {
        get => _departments;
        set => SetProperty(ref _departments, value);
    }

    public ICollectionView? DepartmentsView
    {
        get => _departmentsView;
        private set => SetProperty(ref _departmentsView, value);
    }

    public DepartmentDto? SelectedDepartment
    {
        get => _selectedDepartment;
        set => SetProperty(ref _selectedDepartment, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                DepartmentsView?.Refresh();
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
                _ = LoadDepartmentsAsync();
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

    public async Task LoadDepartmentsAsync()
    {
        await ExecuteAsync(LoadDepartmentsOperationAsync,
            ex => ErrorMessage = HandleException(ex, "DepartmentListViewModel.LoadDepartmentsAsync"));
    }

    private async Task LoadDepartmentsOperationAsync()
    {
        ErrorMessage = null;

        var result = await _departmentService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Departments.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Departments.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Departments.Count == 0;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الأقسام", "DepartmentListViewModel.LoadDepartmentsAsync");
            IsEmpty = Departments.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        DepartmentsView = CollectionViewSource.GetDefaultView(Departments);
        DepartmentsView.Filter = FilterDepartments;
    }

    private bool FilterDepartments(object obj)
    {
        if (obj is not DepartmentDto dept) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var search = SearchText.Trim().ToLower();
        return dept.Name.ToLower().Contains(search);
    }

    private void AddDepartment()
    {
        var editorVm = App.GetService<DepartmentEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "قسم جديد",
            Width = 600,
            Height = 500,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadDepartmentsAsync());
            }
        });
    }

    private void EditDepartment()
    {
        if (SelectedDepartment == null) return;

        var editorVm = App.GetService<DepartmentEditorViewModel>();
        editorVm.LoadDepartment(SelectedDepartment);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل قسم",
            Width = 600,
            Height = 500,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadDepartmentsAsync());
            }
        });
    }

    public async Task DeleteDepartmentAsync()
    {
        if (SelectedDepartment == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"القسم: {SelectedDepartment.Name}");

        if (strategy == DeleteStrategy.Cancel) return;

        var dept = SelectedDepartment;
        await ExecuteAsync(() => DeleteDepartmentOperationAsync(strategy, dept),
            ex => ErrorMessage = HandleException(ex, "DepartmentListViewModel.DeleteDepartmentAsync"));
    }

    private async Task DeleteDepartmentOperationAsync(DeleteStrategy strategy, DepartmentDto dept)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var result = await _departmentService.DeactivateAsync(dept.Id);
            if (result.IsSuccess)
            {
                await LoadDepartmentsAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط القسم بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط القسم", "DepartmentListViewModel.DeleteDepartmentAsync");
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكن حذف القسم بشكل نهائي. يمكنك إلغاء تنشيطه فقط.");
        }
    }

    private void Search()
    {
        DepartmentsView?.Refresh();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
