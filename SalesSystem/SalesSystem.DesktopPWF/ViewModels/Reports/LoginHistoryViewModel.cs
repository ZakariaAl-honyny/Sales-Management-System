using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for Login History Report — displays user login attempts.
/// </summary>
public class LoginHistoryViewModel : ViewModelBase
{
    private IUserReportApiService? _userReportApiService;
    private IUserApiService? _userApiService;

    private IUserReportApiService UserReportApiService => _userReportApiService ??= App.GetService<IUserReportApiService>();
    private IUserApiService UserApiService => _userApiService ??= App.GetService<IUserApiService>();

    private IDialogService D => DialogService!;

    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private int? _selectedUserId;
    private int _successCount;
    private int _failureCount;
    private bool _hasData;

    public LoginHistoryViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<LoginHistoryDto>();
        Users = new ObservableCollection<UserDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));

        LoadUsersCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadUsersAsync)));

        _ = LoadUsersAsync();
    }

    #region Properties

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public int? SelectedUserId
    {
        get => _selectedUserId;
        set => SetProperty(ref _selectedUserId, value);
    }

    public ObservableCollection<LoginHistoryDto> Entries { get; }
    public ObservableCollection<UserDto> Users { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public int SuccessCount
    {
        get => _successCount;
        private set
        {
            if (SetProperty(ref _successCount, value))
                OnPropertyChanged(nameof(SuccessDisplay));
        }
    }

    public int FailureCount
    {
        get => _failureCount;
        private set
        {
            if (SetProperty(ref _failureCount, value))
                OnPropertyChanged(nameof(FailureDisplay));
        }
    }

    public string SuccessDisplay => $"ناجح: {SuccessCount}";
    public string FailureDisplay => $"فاشل: {FailureCount}";

    public bool HasData
    {
        get => _hasData;
        private set => SetProperty(ref _hasData, value);
    }

    public bool IsEmpty => !HasData;

    #endregion

    #region Commands

    public AsyncRelayCommand GenerateReportCommand { get; }
    public AsyncRelayCommand ExportExcelCommand { get; }
    public AsyncRelayCommand LoadUsersCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadUsersAsync()
    {
        try
        {
            var result = await UserApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Users.Clear();
                    foreach (var user in result.Value.Where(u => u.IsActive))
                        Users.Add(user);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة المستخدمين", "LoginHistoryViewModel.LoadUsersAsync", ex);
        }
    }

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading login history report from {FromDate} to {ToDate}, UserId={UserId}",
            FromDate, ToDate, SelectedUserId);

        var result = await UserReportApiService.GetLoginHistoryAsync(SelectedUserId, FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderByDescending(x => x.LoginTime))
                {
                    Entries.Add(item);
                }

                SuccessCount = result.Value.Count(x => x.IsSuccess);
                FailureCount = result.Value.Count(x => !x.IsSuccess);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Login history loaded: {Count} records, Success={SuccessCount}, Failure={FailureCount}",
                Entries.Count, SuccessCount, FailureCount);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل سجل تسجيل الدخول", "LoginHistoryViewModel.LoadAsync");
            Log.Warning("Failed to load login history: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
