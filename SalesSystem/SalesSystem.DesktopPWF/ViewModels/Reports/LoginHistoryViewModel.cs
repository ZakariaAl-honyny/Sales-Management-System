using System.Collections.ObjectModel;
using ClosedXML.Excel;
using Microsoft.Win32;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
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

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

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

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

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
    public AsyncRelayCommand ExportPdfCommand { get; }

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
        if (Entries.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"LoginHistory_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("سجل تسجيل الدخول");

                    worksheet.Cell(1, 1).Value = "اسم المستخدم";
                    worksheet.Cell(1, 2).Value = "وقت الدخول";
                    worksheet.Cell(1, 3).Value = "الحالة";
                    worksheet.Cell(1, 4).Value = "سبب الفشل";

                    for (int i = 0; i < Entries.Count; i++)
                    {
                        var item = Entries[i];
                        worksheet.Cell(i + 2, 1).Value = item.UserName;
                        worksheet.Cell(i + 2, 2).Value = item.LoginTime.ToString("yyyy/MM/dd HH:mm");
                        worksheet.Cell(i + 2, 3).Value = item.IsSuccess ? "ناجح" : "فاشل";
                        worksheet.Cell(i + 2, 4).Value = item.FailureReason ?? "";
                    }

                    var headerRange = worksheet.Range(1, 1, 1, 4);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير سجل تسجيل الدخول إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير سجل تسجيل الدخول إلى Excel", "LoginHistoryViewModel.ExportToExcel", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    private async Task ExportPdfAsync()
    {
        if (Entries.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("اسم المستخدم", typeof(string));
            dataTable.Columns.Add("وقت الدخول", typeof(string));
            dataTable.Columns.Add("الحالة", typeof(string));
            dataTable.Columns.Add("سبب الفشل", typeof(string));

            foreach (var item in Entries)
                dataTable.Rows.Add(item.UserName,
                    item.LoginTime.ToString("yyyy/MM/dd HH:mm"),
                    item.IsSuccess ? "ناجح" : "فاشل",
                    item.FailureReason ?? "");

            await PdfExportService.ExportToPdfAsync("سجل تسجيل الدخول", dataTable, 0,
                $"LoginHistory_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير سجل تسجيل الدخول إلى PDF", "LoginHistoryViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
