using SalesSystem.DesktopPWF.Messaging.Messages;
using SalesSystem.DesktopPWF.Services.App.Toast;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.ViewModels.Notifications;

/// <summary>
/// ViewModel for Notifications List View — Read-only, system-generated
/// </summary>
public class NotificationListViewModel : ViewModelBase
{
    private readonly INotificationApiService _notificationService;
    private readonly ISessionService _sessionService;
    private readonly IDialogService _dialogService;
    private readonly IToastNotificationService _toastService;

    private readonly IEventBus _eventBus;
    private ObservableCollection<NotificationDto> _notifications = new();
    private ICollectionView? _notificationsView;
    private NotificationDto? _selectedNotification;
    private string _searchText = string.Empty;
    private string? _errorMessage;
    private bool _isEmpty;
    private bool _unreadOnly;

    public NotificationListViewModel()
    {
        _notificationService = App.GetService<INotificationApiService>();
        _sessionService = App.GetService<ISessionService>();
        _dialogService = App.GetService<IDialogService>();
        _toastService = App.GetService<IToastNotificationService>();
        _eventBus = App.GetService<IEventBus>();

        InitializeCommands();
    }

    public NotificationListViewModel(
        INotificationApiService notificationService,
        ISessionService sessionService,
        IDialogService dialogService,
        IToastNotificationService toastService,
        IEventBus eventBus)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(LoadNotificationsAsync);
        MarkAsReadCommand = new AsyncRelayCommand(MarkAsReadAsync);
        MarkAllAsReadCommand = new AsyncRelayCommand(MarkAllAsReadAsync);
        SearchCommand = new RelayCommand(Search);

        _eventBus.Subscribe<NotificationChangedMessage>(OnNotificationChanged);
    }

    #region Properties
    public ObservableCollection<NotificationDto> Notifications
    {
        get => _notifications;
        set => SetProperty(ref _notifications, value);
    }

    public ICollectionView? NotificationsView
    {
        get => _notificationsView;
        private set => SetProperty(ref _notificationsView, value);
    }

    public NotificationDto? SelectedNotification
    {
        get => _selectedNotification;
        set => SetProperty(ref _selectedNotification, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                NotificationsView?.Refresh();
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

    public bool UnreadOnly
    {
        get => _unreadOnly;
        set
        {
            if (SetProperty(ref _unreadOnly, value))
            {
                _ = LoadNotificationsAsync();
            }
        }
    }

    public int NotificationsCount => Notifications.Count;
    public int UnreadCount => Notifications.Count(n => !n.IsRead);
    #endregion

    #region Commands
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand MarkAsReadCommand { get; private set; } = null!;
    public ICommand MarkAllAsReadCommand { get; private set; } = null!;
    public ICommand SearchCommand { get; private set; } = null!;
    #endregion

    #region Methods
    public async Task LoadNotificationsAsync()
    {
        await ExecuteAsync(LoadNotificationsOperationAsync);
    }

    private async Task LoadNotificationsOperationAsync()
    {
        ErrorMessage = null;
        var userId = _sessionService.GetUserId() ?? 0;
        var result = await _notificationService.GetAllAsync(userId: userId, unreadOnly: UnreadOnly);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Notifications.Clear();
                foreach (var item in result.Value.OrderByDescending(x => x.CreatedAt))
                {
                    Notifications.Add(item);
                }
                SetupCollectionView();
                IsEmpty = Notifications.Count == 0;
                OnPropertyChanged(nameof(NotificationsCount));
                OnPropertyChanged(nameof(UnreadCount));
            });
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الإشعارات", "NotificationListViewModel.LoadNotificationsAsync", "[NotificationListViewModel.LoadNotificationsAsync] Failed to load notifications.");
            await _dialogService.ShowErrorAsync("خطأ في تحميل البيانات", ErrorMessage!);
            IsEmpty = Notifications.Count == 0;
        }
    }

    private void SetupCollectionView()
    {
        NotificationsView = new ListCollectionView(Notifications);
        NotificationsView.Filter = FilterNotifications;
    }

    private bool FilterNotifications(object obj)
    {
        if (obj is not NotificationDto notification) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLower();
            if (!notification.Title.ToLower().Contains(searchLower) &&
                !notification.Message.ToLower().Contains(searchLower))
                return false;
        }

        return true;
    }

    public async Task MarkAsReadAsync()
    {
        if (SelectedNotification == null)
        {
            await _dialogService.ShowWarningAsync("تنبيه", "الرجاء اختيار إشعار");
            return;
        }

        if (SelectedNotification.IsRead) return;

        await ExecuteAsync(MarkAsReadOperationAsync);
    }

    private async Task MarkAsReadOperationAsync()
    {
        ErrorMessage = null;
        var result = await _notificationService.MarkAsReadAsync(SelectedNotification!.Id);

        if (result.IsSuccess)
        {
            await LoadNotificationsAsync();
            _toastService.ShowSuccess("تم تحديد الإشعار كمقروء");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث الإشعار", "NotificationListViewModel.MarkAsReadAsync", $"[NotificationListViewModel.MarkAsReadAsync] Failed to mark notification {SelectedNotification.Id} as read.");
            await _dialogService.ShowErrorAsync("خطأ في تحديث الإشعار", ErrorMessage!);
        }
    }

    public async Task MarkAllAsReadAsync()
    {
        await ExecuteAsync(MarkAllAsReadOperationAsync);
    }

    private async Task MarkAllAsReadOperationAsync()
    {
        ErrorMessage = null;
        var userId = _sessionService.GetUserId() ?? 0;
        var result = await _notificationService.MarkAllAsReadAsync(userId);

        if (result.IsSuccess)
        {
            _eventBus?.Publish(new NotificationChangedMessage());
            await LoadNotificationsAsync();
            _toastService.ShowSuccess("تم تحديد جميع الإشعارات كمقروءة");
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحديث الإشعارات", "NotificationListViewModel.MarkAllAsReadAsync", "[NotificationListViewModel.MarkAllAsReadAsync] Failed to mark all notifications as read.");
            await _dialogService.ShowErrorAsync("خطأ في تحديث الإشعارات", ErrorMessage!);
        }
    }

    private void Search()
    {
        NotificationsView?.Refresh();
    }

    private void OnNotificationChanged(NotificationChangedMessage msg)
    {
        _ = InvokeOnUIThreadAsync(async () => await LoadNotificationsAsync());
    }

    public override void Cleanup()
    {
    }
    #endregion
}
