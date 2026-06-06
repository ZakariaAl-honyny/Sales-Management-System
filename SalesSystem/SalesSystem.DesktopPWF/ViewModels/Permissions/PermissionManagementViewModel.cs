using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Base;

namespace SalesSystem.DesktopPWF.ViewModels.Permissions;

public class PermissionManagementViewModel : AdminOnlyViewModel
{
    private readonly IPermissionApiService _permissionService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<PermissionCategoryGroup> _categories = new();
    private byte _selectedRole = 1; // Admin default
    private string? _errorMessage;

    public PermissionManagementViewModel()
        : this(App.GetService<ISessionService>())
    {
    }

    public PermissionManagementViewModel(ISessionService sessionService)
        : base(sessionService)
    {
        _permissionService = App.GetService<IPermissionApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadPermissionsAsync();
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الصلاحيات...")));
        SelectAllCommand = new RelayCommand(SelectAll, () => Categories.Any());
        DeselectAllCommand = new RelayCommand(DeselectAll, () => Categories.Any());
    }

    #region Properties

    public ObservableCollection<PermissionCategoryGroup> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public byte SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (SetProperty(ref _selectedRole, value))
            {
                OnPropertyChanged(nameof(IsAdminSelected));
                OnPropertyChanged(nameof(IsManagerSelected));
                OnPropertyChanged(nameof(IsCashierSelected));
                _ = LoadPermissionsAsync();
            }
        }
    }

    public bool IsAdminSelected
    {
        get => _selectedRole == 1;
        set { if (value) SelectedRole = 1; }
    }

    public bool IsManagerSelected
    {
        get => _selectedRole == 2;
        set { if (value) SelectedRole = 2; }
    }

    public bool IsCashierSelected
    {
        get => _selectedRole == 3;
        set { if (value) SelectedRole = 3; }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand SelectAllCommand { get; private set; } = null!;
    public ICommand DeselectAllCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadPermissionsAsync()
    {
        await ExecuteAsync(LoadPermissionsOperationAsync);
    }

    private async Task LoadPermissionsOperationAsync()
    {
        ErrorMessage = null;

        var permissionsResult = await _permissionService.GetAllAsync();
        var rolePermsResult = await _permissionService.GetRolePermissionsAsync();

        if (!permissionsResult.IsSuccess || permissionsResult.Value == null)
        {
            ErrorMessage = HandleFailure(permissionsResult.Error ?? "فشل في تحميل الصلاحيات", "PermissionManagementViewModel.LoadPermissionsAsync");
            return;
        }

        var rolePermissions = rolePermsResult.IsSuccess ? rolePermsResult.Value! : new Dictionary<byte, List<int>>();
        var selectedIds = rolePermissions.TryGetValue(SelectedRole, out var ids) && ids != null ? ids : new List<int>();

        await InvokeOnUIThreadAsync(() =>
        {
            Categories.Clear();
            var grouped = permissionsResult.Value!
                .Where(p => p.IsActive)
                .GroupBy(p => p.Category ?? "عام")
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var category = new PermissionCategoryGroup
                {
                    CategoryName = group.Key,
                    Permissions = new ObservableCollection<PermissionCheckItem>()
                };
                foreach (var perm in group.OrderBy(p => p.DisplayNameAr))
                {
                    category.Permissions.Add(new PermissionCheckItem
                    {
                        Id = perm.Id,
                        Name = perm.Name,
                        DisplayNameAr = perm.DisplayNameAr,
                        IsChecked = selectedIds.Contains(perm.Id)
                    });
                }
                Categories.Add(category);
            }

            (SelectAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeselectAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
            return System.Threading.Tasks.Task.CompletedTask;
        });
    }

    private void SelectAll()
    {
        foreach (var category in Categories)
        {
            foreach (var perm in category.Permissions)
            {
                perm.IsChecked = true;
            }
        }
    }

    private void DeselectAll()
    {
        foreach (var category in Categories)
        {
            foreach (var perm in category.Permissions)
            {
                perm.IsChecked = false;
            }
        }
    }

    private async Task SaveOperationAsync()
    {
        var selectedIds = Categories
            .SelectMany(c => c.Permissions)
            .Where(p => p.IsChecked)
            .Select(p => p.Id)
            .ToList();

        var result = await _permissionService.UpdateRolePermissionsAsync(SelectedRole, selectedIds);

        if (result.IsSuccess)
        {
            _toastService.ShowSuccess("تم حفظ صلاحيات الدور بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الصلاحيات", "PermissionManagementViewModel.SaveOperationAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ الصلاحيات", ErrorMessage!);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}

public class PermissionCategoryGroup : ViewModelBase
{
    private string _categoryName = string.Empty;
    private ObservableCollection<PermissionCheckItem> _permissions = new();

    public string CategoryName
    {
        get => _categoryName;
        set => SetProperty(ref _categoryName, value);
    }

    public ObservableCollection<PermissionCheckItem> Permissions
    {
        get => _permissions;
        set => SetProperty(ref _permissions, value);
    }
}

public class PermissionCheckItem : ViewModelBase
{
    private int _id;
    private string _name = string.Empty;
    private string _displayNameAr = string.Empty;
    private bool _isChecked;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string DisplayNameAr
    {
        get => _displayNameAr;
        set => SetProperty(ref _displayNameAr, value);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }
}
