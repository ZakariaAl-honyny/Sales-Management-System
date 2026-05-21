using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using SalesSystem.DesktopPWF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Users;

public class UserListViewModel : AdminOnlyViewModel
{
    private readonly IUserApiService _userService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;
    private readonly ISessionService _sessionService;

    private ObservableCollection<UserDto> _users = new();
    private ICollectionView? _usersView;
    private UserDto? _selectedUser;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public UserListViewModel()
        : this(App.GetService<ISessionService>())
    {
    }

    public UserListViewModel(ISessionService sessionService)
        : base(sessionService)
    {
        _userService = App.GetService<IUserApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _sessionService = sessionService;

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadUsersAsync);
        AddCommand = new RelayCommand(AddUser);
        EditCommand = new RelayCommand(EditUser, () => SelectedUser != null);
        ToggleStatusCommand = new AsyncRelayCommand(ToggleStatusAsync, () => SelectedUser != null);
        ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync, () => SelectedUser != null);
        SearchCommand = new RelayCommand(Search);

        _eventBus.Subscribe<UserChangedMessage>(OnUserChanged);
    }

    #region Properties
    public ObservableCollection<UserDto> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public ICollectionView? UsersView
    {
        get => _usersView;
        private set => SetProperty(ref _usersView, value);
    }

    public UserDto? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (ToggleStatusCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (ResetPasswordCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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
                UsersView?.Refresh();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
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
                _ = LoadUsersAsync();
            }
        }
    }

    public int CurrentUserId => _sessionService.GetUserId() ?? 0;
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand ToggleStatusCommand { get; private set; } = null!;
    public ICommand ResetPasswordCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    #endregion

    #region Methods
    public async Task LoadUsersAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _userService.GetAllAsync(IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var item in result.Value)
                    {
                        Users.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Users.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل المستخدمين", "UserListViewModel.LoadUsersAsync", "[UserListViewModel.LoadUsersAsync] Failed to load users list.");
                IsEmpty = Users.Count == 0;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "UserListViewModel.LoadUsersAsync", "[UserListViewModel.LoadUsersAsync] Failed to load users list.");
            IsEmpty = Users.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetupCollectionView()
    {
        UsersView = CollectionViewSource.GetDefaultView(Users);
        UsersView.Filter = FilterUsers;
    }

    private bool FilterUsers(object obj)
    {
        if (obj is not UserDto user) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var searchLower = SearchText.Trim().ToLower();
        return user.UserName.ToLower().Contains(searchLower) ||
               user.FullName.ToLower().Contains(searchLower) ||
               user.Role.ToString().ToLower().Contains(searchLower);
    }

    private void AddUser()
    {
        var editorVm = new UserEditorViewModel();
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadUsersAsync();
        }
    }

    private void EditUser()
    {
        if (SelectedUser == null) return;

        var editorVm = new UserEditorViewModel(SelectedUser);
        if (_dialogService.ShowDialog(editorVm))
        {
            _ = LoadUsersAsync();
        }
    }

    public async Task ToggleStatusAsync()
    {
        if (SelectedUser == null) return;

        if (SelectedUser.Id == CurrentUserId)
        {
            await _dialogService.ShowErrorAsync("خطأ", "لا يمكنك تعطيل حسابك الخاص. يرجى طلب مسؤول آخر للقيام بذلك.");
            return;
        }

        if (SelectedUser.IsActive)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "تأكيد تعطيل الحساب",
                $"هل أنت متأكد من تعطيل حساب المستخدم: {SelectedUser.FullName}؟");
            if (!confirmed) return;

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var result = await _userService.DeleteAsync(SelectedUser.Id);
                if (result.IsSuccess)
                {
                    await LoadUsersAsync();
                    _toastService.ShowSuccess("تم تعطيل الحساب بنجاح");
                }
                else
                {
                    ErrorMessage = result.Error ?? "فشل في تعطيل الحساب";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = HandleException(ex, "UserListViewModel.ToggleStatusAsync", $"[UserListViewModel.ToggleStatusAsync] Failed to toggle user status for ID {SelectedUser?.Id}.");
            }
            finally
            {
                IsLoading = false;
            }
        }
        else
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "تأكيد تفعيل الحساب",
                $"هل أنت متأكد من تفعيل حساب المستخدم: {SelectedUser.FullName}؟");
            if (!confirmed) return;

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var request = new UpdateUserRequest(
                    FullName: SelectedUser.FullName,
                    Role: SelectedUser.Role,
                    IsActive: true,
                    Password: null
                );

                var result = await _userService.UpdateAsync(SelectedUser.Id, request);
                if (result.IsSuccess)
                {
                    await LoadUsersAsync();
                    _toastService.ShowSuccess("تم تفعيل الحساب بنجاح");
                }
                else
                {
                    ErrorMessage = result.Error ?? "فشل في تفعيل الحساب";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = HandleException(ex, "UserListViewModel.ToggleStatusAsync", $"[UserListViewModel.ToggleStatusAsync] Failed to restore user with ID {SelectedUser?.Id}.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public async Task ResetPasswordAsync()
    {
        if (SelectedUser == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "تأكيد إعادة تعيين كلمة المرور",
            $"هل أنت متأكد من إعادة تعيين كلمة المرور للمستخدم: {SelectedUser.FullName}؟\n\nسيتم تعيين كلمة المرور الافتراضية: 123456");
        if (!confirmed) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var request = new UpdateUserRequest(
                FullName: SelectedUser.FullName,
                Role: SelectedUser.Role,
                IsActive: SelectedUser.IsActive,
                Password: "123456"
            );

            var result = await _userService.UpdateAsync(SelectedUser.Id, request);
            if (result.IsSuccess)
            {
                _toastService.ShowSuccess($"تم إعادة تعيين كلمة المرور للمستخدم: {SelectedUser.FullName}");
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في إعادة تعيين كلمة المرور";
                await _dialogService.ShowErrorAsync("خطأ", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "UserListViewModel.ResetPasswordAsync", $"[UserListViewModel.ResetPasswordAsync] Failed to reset password for user ID {SelectedUser?.Id}.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Search()
    {
        UsersView?.Refresh();
    }

    private void OnUserChanged(UserChangedMessage msg)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await LoadUsersAsync();
        });
    }

    public override void Cleanup()
    {
        _eventBus.Unsubscribe<UserChangedMessage>(OnUserChanged);
    }
    #endregion
}
