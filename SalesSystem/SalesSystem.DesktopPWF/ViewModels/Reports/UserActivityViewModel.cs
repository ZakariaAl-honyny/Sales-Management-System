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
/// ViewModel for User Activity Report — displays user actions and audit trail.
/// </summary>
public class UserActivityViewModel : ViewModelBase
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
                FileName = $"UserActivity_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("نشاط المستخدمين");

                    worksheet.Cell(1, 1).Value = "المستخدم";
                    worksheet.Cell(1, 2).Value = "الوقت";
                    worksheet.Cell(1, 3).Value = "الإجراء";
                    worksheet.Cell(1, 4).Value = "الكيان";
                    worksheet.Cell(1, 5).Value = "رقم المرجع";
                    worksheet.Cell(1, 6).Value = "التفاصيل";

                    for (int i = 0; i < Entries.Count; i++)
                    {
                        var item = Entries[i];
                        worksheet.Cell(i + 2, 1).Value = item.UserName;
                        worksheet.Cell(i + 2, 2).Value = item.Timestamp.ToString("yyyy/MM/dd HH:mm");
                        worksheet.Cell(i + 2, 3).Value = item.Action;
                        worksheet.Cell(i + 2, 4).Value = item.EntityType;
                        worksheet.Cell(i + 2, 5).Value = item.EntityId?.ToString() ?? "";
                        worksheet.Cell(i + 2, 6).Value = item.Details ?? "";
                    }

                    var headerRange = worksheet.Range(1, 1, 1, 6);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير تقرير نشاط المستخدمين إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير نشاط المستخدمين إلى Excel", "UserActivityViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("المستخدم", typeof(string));
            dataTable.Columns.Add("الوقت", typeof(string));
            dataTable.Columns.Add("الإجراء", typeof(string));
            dataTable.Columns.Add("الكيان", typeof(string));
            dataTable.Columns.Add("رقم المرجع", typeof(string));
            dataTable.Columns.Add("التفاصيل", typeof(string));

            foreach (var item in Entries)
                dataTable.Rows.Add(item.UserName,
                    item.Timestamp.ToString("yyyy/MM/dd HH:mm"),
                    item.Action, item.EntityType,
                    item.EntityId?.ToString() ?? "",
                    item.Details ?? "");

            await PdfExportService.ExportToPdfAsync("نشاط المستخدمين", dataTable, 0,
                $"UserActivity_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير نشاط المستخدمين إلى PDF", "UserActivityViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
