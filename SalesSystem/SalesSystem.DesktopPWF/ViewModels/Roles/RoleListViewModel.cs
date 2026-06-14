using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Base;

namespace SalesSystem.DesktopPWF.ViewModels.Roles;

public class RoleListViewModel : AdminOnlyViewModel
{
    private readonly IRoleApiService _roleService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly IScreenWindowService _screenWindowService;

    private ObservableCollection<RoleDto> _roles = new();
    private ICollectionView? _rolesView;
    private RoleDto? _selectedRole;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public RoleListViewModel()
        : this(App.GetService<ISessionService>())
    {
    }

    public RoleListViewModel(ISessionService sessionService)
        : base(sessionService)
    {
        _roleService = App.GetService<IRoleApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _screenWindowService = App.GetService<IScreenWindowService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadRolesOperationAsync)));
        AddCommand = new RelayCommand(AddRole);
        EditCommand = new RelayCommand(EditRole);
        DeleteCommand = new AsyncRelayCommand(DeleteRoleAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties

    public ObservableCollection<RoleDto> Roles
    {
        get => _roles;
        set => SetProperty(ref _roles, value);
    }

    public ICollectionView? RolesView
    {
        get => _rolesView;
        private set => SetProperty(ref _rolesView, value);
    }

    public RoleDto? SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (SetProperty(ref _selectedRole, value))
            {
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RolesView?.Refresh();
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
                _ = LoadRolesAsync();
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

    public async Task LoadRolesAsync() => await ExecuteAsync(LoadRolesOperationAsync);

    private async Task LoadRolesOperationAsync()
    {
        ErrorMessage = null;

        var result = await _roleService.GetAllAsync(IncludeInactive);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Roles.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.Id))
                {
                    Roles.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Roles.Count == 0;
                return Task.CompletedTask;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الأدوار", "RoleListViewModel.LoadRolesOperationAsync");
            IsEmpty = Roles.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        RolesView = CollectionViewSource.GetDefaultView(Roles);
        RolesView.Filter = FilterRoles;
    }

    private bool FilterRoles(object obj)
    {
        if (obj is not RoleDto role) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return role.Name.ToLower().Contains(searchLower) ||
               (role.Description?.ToLower().Contains(searchLower) ?? false);
    }

    private void AddRole()
    {
        var editorVm = App.GetService<RoleEditorViewModel>();
        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "دور جديد",
            Width = 600,
            Height = 450,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadRolesAsync());
            }
        });
    }

    private void EditRole()
    {
        if (SelectedRole == null)
        {
            _ = _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد دور من القائمة أولاً.");
            return;
        }

        var editorVm = App.GetService<RoleEditorViewModel>();
        editorVm.LoadRole(SelectedRole);

        _screenWindowService.OpenScreen(editorVm, new ScreenWindowOptions
        {
            Title = "تعديل الدور",
            Width = 600,
            Height = 450,
            OnClosed = (_) =>
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() => _ = LoadRolesAsync());
            }
        });
    }

    public async Task DeleteRoleAsync()
    {
        if (SelectedRole == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد دور من القائمة أولاً.");
            return;
        }

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"الدور: {SelectedRole.Name}");
        if (strategy == DeleteStrategy.Cancel) return;

        await ExecuteAsync(() => DeleteRoleOperationAsync(strategy),
            ex => LogSystemError($"Failed to delete role ID {SelectedRole.Id}", "RoleListViewModel.DeleteRoleAsync", ex));
    }

    private async Task DeleteRoleOperationAsync(DeleteStrategy strategy)
    {
        ErrorMessage = null;

        if (strategy == DeleteStrategy.Deactivate)
        {
            var result = await _roleService.DeleteAsync(SelectedRole!.Id);
            if (result.IsSuccess)
            {
                await LoadRolesAsync();
                _toastService.ShowSuccess("تم إلغاء تنشيط الدور بنجاح");
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء تنشيط الدور", "RoleListViewModel.DeleteRoleAsync");
                await _dialogService.ShowErrorAsync("خطأ في حذف الدور", ErrorMessage!);
            }
        }
        else if (strategy == DeleteStrategy.Permanent)
        {
            // Roles can only be soft-deleted
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكن حذف الأدوار بشكل نهائي. يمكنك إلغاء تنشيطها فقط.");
        }
    }

    private void Search()
    {
        RolesView?.Refresh();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
