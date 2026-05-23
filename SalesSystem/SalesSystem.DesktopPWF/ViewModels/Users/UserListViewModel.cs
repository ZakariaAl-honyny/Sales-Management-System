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
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _userService.GetAllAsync(IncludeInactive);

            if (result.IsSuccess && result.Value != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var item in result.Value.OrderByDescending(x => x.Id))
                    {
                        Users.Add(item);
                    }
                    SetupCollectionView();
                    IsEmpty = Users.Count == 0;
                });
            }
            else
            {
                ErrorMessage = HandleFailure(result.Error ?? "ظپط´ظ„ ظپظٹ طھط­ظ…ظٹظ„ ط§ظ„ظ…ط³طھط®ط¯ظ…ظٹظ†", "UserListViewModel.LoadUsersAsync", "[UserListViewModel.LoadUsersAsync] Failed to load users list.");
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
            IsBusy = false;
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
            await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ طھط¹ط·ظٹظ„ ط§ظ„ط­ط³ط§ط¨", "ظ„ط§ ظٹظ…ظƒظ†ظƒ طھط¹ط·ظٹظ„ ط­ط³ط§ط¨ظƒ ط§ظ„ط®ط§طµ. ظٹط±ط¬ظ‰ ط·ظ„ط¨ ظ…ط³ط¤ظˆظ„ ط¢ط®ط± ظ„ظ„ظ‚ظٹط§ظ… ط¨ط°ظ„ظƒ.");
            return;
        }

        if (SelectedUser.IsActive)
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "طھط£ظƒظٹط¯ طھط¹ط·ظٹظ„ ط§ظ„ط­ط³ط§ط¨",
                $"ظ‡ظ„ ط£ظ†طھ ظ…طھط£ظƒط¯ ظ…ظ† طھط¹ط·ظٹظ„ ط­ط³ط§ط¨ ط§ظ„ظ…ط³طھط®ط¯ظ…: {SelectedUser.FullName}طں");
            if (!confirmed) return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var result = await _userService.DeleteAsync(SelectedUser.Id);
                if (result.IsSuccess)
                {
                    await LoadUsersAsync();
                    _toastService.ShowSuccess("طھظ… طھط¹ط·ظٹظ„ ط§ظ„ط­ط³ط§ط¨ ط¨ظ†ط¬ط§ط­");
                }
                else
                {
                    ErrorMessage = result.Error ?? "ظپط´ظ„ ظپظٹ طھط¹ط·ظٹظ„ ط§ظ„ط­ط³ط§ط¨";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = HandleException(ex, "UserListViewModel.ToggleStatusAsync", $"[UserListViewModel.ToggleStatusAsync] Failed to toggle user status for ID {SelectedUser?.Id}.");
            }
            finally
            {
                IsBusy = false;
            }
        }
        else
        {
            var confirmed = await _dialogService.ShowConfirmationAsync(
                "طھط£ظƒظٹط¯ طھظپط¹ظٹظ„ ط§ظ„ط­ط³ط§ط¨",
                $"ظ‡ظ„ ط£ظ†طھ ظ…طھط£ظƒط¯ ظ…ظ† طھظپط¹ظٹظ„ ط­ط³ط§ط¨ ط§ظ„ظ…ط³طھط®ط¯ظ…: {SelectedUser.FullName}طں");
            if (!confirmed) return;

            IsBusy = true;
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
                    _toastService.ShowSuccess("طھظ… طھظپط¹ظٹظ„ ط§ظ„ط­ط³ط§ط¨ ط¨ظ†ط¬ط§ط­");
                }
                else
                {
                    ErrorMessage = result.Error ?? "ظپط´ظ„ ظپظٹ طھظپط¹ظٹظ„ ط§ظ„ط­ط³ط§ط¨";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = HandleException(ex, "UserListViewModel.ToggleStatusAsync", $"[UserListViewModel.ToggleStatusAsync] Failed to restore user with ID {SelectedUser?.Id}.");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public async Task ResetPasswordAsync()
    {
        if (SelectedUser == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "طھط£ظƒظٹط¯ ط¥ط¹ط§ط¯ط© طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±",
            $"ظ‡ظ„ ط£ظ†طھ ظ…طھط£ظƒط¯ ظ…ظ† ط¥ط¹ط§ط¯ط© طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ظ„ظ„ظ…ط³طھط®ط¯ظ…: {SelectedUser.FullName}طں\n\nط³ظٹطھظ… طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ط§ظ„ط§ظپطھط±ط§ط¶ظٹط©: 123456");
        if (!confirmed) return;

        IsBusy = true;
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
                _toastService.ShowSuccess($"طھظ… ط¥ط¹ط§ط¯ط© طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ظ„ظ„ظ…ط³طھط®ط¯ظ…: {SelectedUser.FullName}");
            }
            else
            {
                ErrorMessage = result.Error ?? "ظپط´ظ„ ظپظٹ ط¥ط¹ط§ط¯ط© طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±";
                await _dialogService.ShowErrorAsync("ط®ط·ط£ ظپظٹ ط¥ط¹ط§ط¯ط© طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط±", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = HandleException(ex, "UserListViewModel.ResetPasswordAsync", $"[UserListViewModel.ResetPasswordAsync] Failed to reset password for user ID {SelectedUser?.Id}.");
        }
        finally
        {
            IsBusy = false;
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




