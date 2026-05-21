using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
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

public class UserListViewModel : ViewModelBase
{
    private readonly IUserApiService _userService;
    private readonly IEventBus _eventBus;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<UserDto> _users = new();
    private ICollectionView? _usersView;
    private UserDto? _selectedUser;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeInactive;

    public UserListViewModel()
    {
        _userService = App.GetService<IUserApiService>();
        _eventBus = App.GetService<IEventBus>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadUsersAsync);
        AddCommand = new RelayCommand(AddUser);
        EditCommand = new RelayCommand(EditUser, () => SelectedUser != null);
        DeleteCommand = new AsyncRelayCommand(DeleteUserAsync, () => SelectedUser != null && SelectedUser.IsActive);
        RestoreCommand = new AsyncRelayCommand(RestoreUserAsync, () => SelectedUser != null && !SelectedUser.IsActive);
        SearchCommand = new RelayCommand(Search);

        // Subscribe to user changes
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
                (DeleteCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RestoreCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
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
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand EditCommand { get; private set; } = null!;
    public ICommand DeleteCommand { get; private set; } = null!;
    public ICommand RestoreCommand { get; private set; } = null!;
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
                Application.Current.Dispatcher.Invoke(() =>
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

public async Task DeleteUserAsync()
    {
        if (SelectedUser == null) return;

        var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المستخدم: {SelectedUser.FullName}");

        if (strategy == DeleteStrategy.Cancel) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            if (strategy == DeleteStrategy.Deactivate)
            {
                var deleteResult = await _userService.DeleteAsync(SelectedUser.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadUsersAsync();
                    _toastService.ShowSuccess("تم إلغاء تنشيط المستخدم بنجاح");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "فشل في إلغاء تنشيط المستخدم";
                }
            }
            else if (strategy == DeleteStrategy.Permanent)
            {
                var deleteResult = await _userService.DeletePermanentlyAsync(SelectedUser.Id);
                if (deleteResult.IsSuccess)
                {
                    await LoadUsersAsync();
                    _toastService.ShowSuccess("تم حذف المستخدم نهائياً");
                }
                else
                {
                    ErrorMessage = deleteResult.Error ?? "فشل في حذف المستخدم";
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
            HandleException(ex, "UserListViewModel.DeleteUserAsync", $"[UserListViewModel.DeleteUserAsync] Failed to delete user with ID {SelectedUser?.Id}.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RestoreUserAsync()
    {
        if (SelectedUser == null) return;

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
                await _dialogService.ShowSuccessAsync("نجاح", "تم استعادة المستخدم بنجاح");
            }
            else
            {
                ErrorMessage = result.Error ?? "فشل في استعادة المستخدم";
                await _dialogService.ShowErrorAsync("خطأ في الاستعادة", ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
            HandleException(ex, "UserListViewModel.RestoreUserAsync", $"[UserListViewModel.RestoreUserAsync] Failed to restore user with ID {SelectedUser?.Id}.");
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
        Application.Current.Dispatcher.InvokeAsync(async () =>
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
