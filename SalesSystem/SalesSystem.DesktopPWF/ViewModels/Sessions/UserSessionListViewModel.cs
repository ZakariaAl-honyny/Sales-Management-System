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

namespace SalesSystem.DesktopPWF.ViewModels.Sessions;

public class UserSessionListViewModel : AdminOnlyViewModel
{
    private readonly IUserSessionApiService _sessionService;
    private readonly IUserApiService _userService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<UserSessionDto> _sessions = new();
    private ICollectionView? _sessionsView;
    private UserSessionDto? _selectedSession;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _includeRevoked;
    private int? _filterUserId;
    private ObservableCollection<UserDto> _users = new();
    private UserDto? _selectedUserFilter;

    public UserSessionListViewModel()
        : this(App.GetService<ISessionService>())
    {
    }

    public UserSessionListViewModel(ISessionService sessionService)
        : base(sessionService)
    {
        _sessionService = App.GetService<IUserSessionApiService>();
        _userService = App.GetService<IUserApiService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        SetDialogService(_dialogService);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadSessionsOperationAsync)));
        RevokeCommand = new AsyncRelayCommand(RevokeSessionAsync);
        RevokeAllCommand = new AsyncRelayCommand(RevokeAllSessionsAsync);
        SearchCommand = new RelayCommand(Search);
    }

    #region Properties

    public ObservableCollection<UserSessionDto> Sessions
    {
        get => _sessions;
        set => SetProperty(ref _sessions, value);
    }

    public ICollectionView? SessionsView
    {
        get => _sessionsView;
        private set => SetProperty(ref _sessionsView, value);
    }

    public UserSessionDto? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetProperty(ref _selectedSession, value))
            {
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

    public bool IncludeRevoked
    {
        get => _includeRevoked;
        set
        {
            if (SetProperty(ref _includeRevoked, value))
            {
                _ = LoadSessionsAsync();
            }
        }
    }

    public ObservableCollection<UserDto> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public UserDto? SelectedUserFilter
    {
        get => _selectedUserFilter;
        set
        {
            if (SetProperty(ref _selectedUserFilter, value))
            {
                _filterUserId = value?.Id;
                _ = LoadSessionsAsync();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand RevokeCommand { get; private set; } = null!;
    public ICommand RevokeAllCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;

    #endregion

    #region Methods

    public async Task LoadSessionsAsync() => await ExecuteAsync(LoadSessionsOperationAsync);

    public async Task LoadUsersAsync() => await ExecuteAsync(LoadUsersOperationAsync);

    private async Task LoadUsersOperationAsync()
    {
        var result = await _userService.GetAllAsync();
        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Users.Clear();
                Users.Add(new UserDto(0, "", "الكل", 0, 1, false, null, null, null, null, null, 0, null));
                foreach (var user in result.Value.OrderBy(u => u.FullName))
                {
                    Users.Add(user);
                }
                return Task.CompletedTask;
            });
        }
    }

    private async Task LoadSessionsOperationAsync()
    {
        ErrorMessage = null;

        var result = await _sessionService.GetAllAsync(_filterUserId, IncludeRevoked);

        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Sessions.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.CreatedAt))
                {
                    Sessions.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Sessions.Count == 0;
                return Task.CompletedTask;
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الجلسات", "UserSessionListViewModel.LoadSessionsOperationAsync");
            IsEmpty = Sessions.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        SessionsView = CollectionViewSource.GetDefaultView(Sessions);
    }

    public async Task RevokeSessionAsync()
    {
        if (SelectedSession == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "يرجى تحديد جلسة من القائمة أولاً.");
            return;
        }
        if (!SelectedSession.IsActive)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "هذه الجلسة ملغاة بالفعل.");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "إلغاء جلسة",
            $"هل أنت متأكد من إلغاء جلسة المستخدم {SelectedSession.UserName}؟");

        if (!confirmed) return;

        await ExecuteAsync(RevokeSessionOperationAsync,
            ex => LogSystemError($"Failed to revoke session ID {SelectedSession.Id}", "UserSessionListViewModel.RevokeSessionAsync", ex));
    }

    private async Task RevokeSessionOperationAsync()
    {
        ErrorMessage = null;

        var result = await _sessionService.RevokeAsync(SelectedSession!.Id);
        if (result.IsSuccess)
        {
            await LoadSessionsOperationAsync();
            _toastService.ShowSuccess("تم إلغاء الجلسة بنجاح");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في إلغاء الجلسة", "UserSessionListViewModel.RevokeSessionAsync");
            await _dialogService.ShowErrorAsync("خطأ في إلغاء الجلسة", ErrorMessage!);
        }
    }

    public async Task RevokeAllSessionsAsync()
    {
        var activeSessions = Sessions.Where(s => s.IsActive).ToList();
        if (!activeSessions.Any()) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "إلغاء جميع الجلسات النشطة",
            $"هل أنت متأكد من إلغاء جميع الجلسات النشطة ({activeSessions.Count})؟");

        if (!confirmed) return;

        await ExecuteAsync(RevokeAllOperationAsync,
            ex => LogSystemError("Failed to revoke all sessions", "UserSessionListViewModel.RevokeAllSessionsAsync", ex));
    }

    private async Task RevokeAllOperationAsync()
    {
        ErrorMessage = null;
        var successCount = 0;
        var failCount = 0;

        var activeSessions = Sessions.Where(s => s.IsActive).ToList();
        foreach (var session in activeSessions)
        {
            var result = await _sessionService.RevokeAsync(session.Id);
            if (result.IsSuccess)
                successCount++;
            else
                failCount++;
        }

        await LoadSessionsOperationAsync();

        if (failCount == 0)
        {
            _toastService.ShowSuccess($"تم إلغاء {successCount} جلسات بنجاح");
        }
        else
        {
            await _dialogService.ShowWarningAsync("نتيجة الإلغاء",
                $"تم إلغاء {successCount} جلسات بنجاح.\nفشل إلغاء {failCount} جلسات.");
        }
    }

    private void Search()
    {
        SessionsView?.Refresh();
    }

    public override void Cleanup()
    {
        base.Cleanup();
    }

    #endregion
}
