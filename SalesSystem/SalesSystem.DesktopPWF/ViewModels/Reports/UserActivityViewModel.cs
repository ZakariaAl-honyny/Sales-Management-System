using System.Collections.ObjectModel;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

/// <summary>
/// ViewModel for User Activity Report — displays user actions and audit trail.
/// </summary>
public class UserActivityViewModel : ViewModelBase
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
    private bool _hasData;

    public UserActivityViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<UserActivityReportDto>();
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

    public ObservableCollection<UserActivityReportDto> Entries { get; }
    public ObservableCollection<UserDto> Users { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

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
            LogSystemError("فشل في تحميل قائمة المستخدمين", "UserActivityViewModel.LoadUsersAsync", ex);
        }
    }

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading user activity report from {FromDate} to {ToDate}, UserId={UserId}",
            FromDate, ToDate, SelectedUserId);

        var result = await UserReportApiService.GetUserActivityAsync(SelectedUserId, FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderByDescending(x => x.Timestamp))
                {
                    Entries.Add(item);
                }

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("User activity report loaded: {Count} records", Entries.Count);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير نشاط المستخدمين", "UserActivityViewModel.LoadAsync");
            Log.Warning("Failed to load user activity report: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        await D.ShowInfoAsync("تصدير Excel", "سيتم تفعيل تصدير Excel قريباً");
    }

    #endregion
}
