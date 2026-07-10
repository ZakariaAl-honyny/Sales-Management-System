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
    private readonly IRoleApiService _roleService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<PermissionCategoryGroup> _categories = new();
    private ObservableCollection<RoleItem> _roles = new();
    private RoleItem? _selectedRoleItem;
    private string? _errorMessage;

    public PermissionManagementViewModel()
        : this(App.GetService<ISessionService>())
    {
    }

    public PermissionManagementViewModel(ISessionService sessionService)
        : base(sessionService)
    {
        _permissionService = App.GetService<IPermissionApiService>();
        _roleService = App.GetService<IRoleApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadRolesAsync();
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

    public ObservableCollection<RoleItem> Roles
    {
        get => _roles;
        set => SetProperty(ref _roles, value);
    }

    public RoleItem? SelectedRoleItem
    {
        get => _selectedRoleItem;
        set
        {
            if (SetProperty(ref _selectedRoleItem, value) && value != null)
            {
                _ = LoadPermissionsAsync();
            }
        }
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

    public async Task LoadRolesAsync()
    {
        await ExecuteAsync(LoadRolesOperationAsync);
    }

    private async Task LoadRolesOperationAsync()
    {
        var result = await _roleService.GetAllAsync();
        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Roles.Clear();
                foreach (var role in result.Value.Where(r => r.IsActive).OrderBy(r => r.Id))
                {
                    Roles.Add(new RoleItem { Id = role.Id, Name = role.Name });
                }
                SelectedRoleItem = Roles.FirstOrDefault();
                return System.Threading.Tasks.Task.CompletedTask;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الأدوار", "PermissionManagementViewModel.LoadRolesOperationAsync");
        }
    }

    public async Task LoadPermissionsAsync()
    {
        await ExecuteAsync(LoadPermissionsOperationAsync);
    }

    private async Task LoadPermissionsOperationAsync()
    {
        ErrorMessage = null;

        if (SelectedRoleItem == null) return;

        var permissionsResult = await _permissionService.GetAllAsync();
        var rolePermsResult = await _permissionService.GetRolePermissionsAsync();

        if (!permissionsResult.IsSuccess || permissionsResult.Value == null)
        {
            ErrorMessage = HandleFailure(permissionsResult.Error ?? "فشل في تحميل الصلاحيات", "PermissionManagementViewModel.LoadPermissionsAsync");
            return;
        }

        var rolePermissions = rolePermsResult.IsSuccess ? rolePermsResult.Value! : new Dictionary<byte, List<int>>();
        var selectedIds = rolePermissions.TryGetValue((byte)SelectedRoleItem.Id, out var ids) && ids != null ? ids : new List<int>();

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
                foreach (var perm in group.OrderBy(p => p.DisplayName))
                {
                    category.Permissions.Add(new PermissionCheckItem
                    {
                        Id = perm.Id,
                        Name = perm.Code,
                        DisplayNameAr = perm.DisplayName,
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
        if (SelectedRoleItem == null) return;

        var selectedIds = Categories
            .SelectMany(c => c.Permissions)
            .Where(p => p.IsChecked)
            .Select(p => p.Id)
            .ToList();

        var result = await _permissionService.UpdateRolePermissionsAsync((byte)SelectedRoleItem.Id, selectedIds);

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

public class RoleItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public override string ToString() => Name;
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
