using System.Collections.ObjectModel;
using System.Windows.Input;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Base;
using SalesSystem.DesktopPWF.ViewModels.Permissions;

namespace SalesSystem.DesktopPWF.ViewModels.Roles;

public class RolePermissionViewModel : AdminOnlyViewModel
{
    private readonly IPermissionApiService _permissionService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<PermissionCategoryGroup> _categories = new();
    private int _selectedRoleId = 1;
    private string? _errorMessage;
    private string _windowTitle = "صلاحيات الدور";

    public RolePermissionViewModel()
        : this(App.GetService<ISessionService>())
    {
    }

    public RolePermissionViewModel(ISessionService sessionService)
        : base(sessionService)
    {
        _permissionService = App.GetService<IPermissionApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
        _ = LoadPermissionsAsync();
    }

    public void LoadRole(int roleId, string roleName)
    {
        _selectedRoleId = roleId;
        WindowTitle = $"صلاحيات دور: {roleName}";
    }

    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(SaveOperationAsync, "جاري حفظ الصلاحيات...")));
        CancelCommand = new RelayCommand(() => RequestClose());
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
    }

    #region Properties

    public ObservableCollection<PermissionCategoryGroup> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
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
            ErrorMessage = HandleFailure(permissionsResult.Error ?? "فشل في تحميل الصلاحيات", "RolePermissionViewModel.LoadPermissionsAsync");
            return;
        }

        var rolePermissions = rolePermsResult.IsSuccess ? rolePermsResult.Value! : new Dictionary<byte, List<int>>();
        var selectedIds = rolePermissions.TryGetValue((byte)_selectedRoleId, out var ids) && ids != null
            ? ids
            : new List<int>();

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
            return Task.CompletedTask;
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

        var result = await _permissionService.UpdateRolePermissionsAsync((byte)_selectedRoleId, selectedIds);

        if (result.IsSuccess)
        {
            _toastService.ShowSuccess("تم حفظ صلاحيات الدور بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في حفظ الصلاحيات", "RolePermissionViewModel.SaveOperationAsync");
            await _dialogService.ShowErrorAsync("خطأ في حفظ الصلاحيات", ErrorMessage!);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
