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

public class WorkingCapitalViewModel : ViewModelBase
{
    private IFinancialReportApiService? _financialReportApiService;

    private IFinancialReportApiService FinancialReportApiService
        => _financialReportApiService ??= App.GetService<IFinancialReportApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private DateTime _asOfDate;
    private string? _errorMessage;
    private bool _hasSearched;
    private decimal _currentAssets;
    private decimal _currentLiabilities;
    private decimal _workingCapital;
    private decimal _currentRatio;
    private ObservableCollection<WorkingCapitalAccountDto> _accounts = new();

    public WorkingCapitalViewModel()
    {
        _asOfDate = DateTime.Today;

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        ExportCommand = new RelayCommand(ExportToExcel);
        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        _ = LoadDataAsync();
    }

    #region Properties

    public DateTime AsOfDate
    {
        get => _asOfDate;
        set
        {
            if (SetProperty(ref _asOfDate, value))
                _ = LoadDataAsync();
        }
    }

    public ObservableCollection<WorkingCapitalAccountDto> Accounts
    {
        get => _accounts;
        set
        {
            if (SetProperty(ref _accounts, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasData));
            }
        }
    }

    public bool HasSearched
    {
        get => _hasSearched;
        set => SetProperty(ref _hasSearched, value);
    }

    public bool HasData => Accounts.Count > 0;
    public bool IsEmpty => Accounts.Count == 0 && HasSearched;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal CurrentAssets
    {
        get => _currentAssets;
        set
        {
            if (SetProperty(ref _currentAssets, value))
                OnPropertyChanged(nameof(FormattedCurrentAssets));
        }
    }

    public decimal CurrentLiabilities
    {
        get => _currentLiabilities;
        set
        {
            if (SetProperty(ref _currentLiabilities, value))
                OnPropertyChanged(nameof(FormattedCurrentLiabilities));
        }
    }

    public decimal WorkingCapital
    {
        get => _workingCapital;
        set
        {
            if (SetProperty(ref _workingCapital, value))
            {
                OnPropertyChanged(nameof(FormattedWorkingCapital));
                OnPropertyChanged(nameof(WorkingCapitalStatus));
            }
        }
    }

    public decimal CurrentRatio
    {
        get => _currentRatio;
        set
        {
            if (SetProperty(ref _currentRatio, value))
                OnPropertyChanged(nameof(FormattedCurrentRatio));
        }
    }

    public string FormattedCurrentAssets => CurrentAssets.ToString("N2");
    public string FormattedCurrentLiabilities => CurrentLiabilities.ToString("N2");
    public string FormattedWorkingCapital => WorkingCapital.ToString("N2");
    public string FormattedCurrentRatio => CurrentRatio.ToString("N2");

    public string WorkingCapitalStatus => WorkingCapital >= 0 ? "إيجابي" : "سلبي";
    public string RatioStatus => CurrentRatio >= 2 ? "ممتاز" : CurrentRatio >= 1 ? "مقبول" : "غير كافٍ";

    #endregion

    #region Commands

    public AsyncRelayCommand LoadCommand { get; }
    public RelayCommand ExportCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadDataAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading working capital report as of {AsOfDate}", AsOfDate);

        var result = await FinancialReportApiService.GetWorkingCapitalAsync(AsOfDate);

        if (result.IsSuccess && result.Value != null)
        {
            var data = result.Value;

            InvokeOnUIThread(() =>
            {
                CurrentAssets = data.CurrentAssets;
                CurrentLiabilities = data.CurrentLiabilities;
                WorkingCapital = data.WorkingCapital;
                CurrentRatio = data.CurrentRatio;

                Accounts.Clear();
                if (data.Accounts != null)
                {
                    foreach (var acct in data.Accounts)
                        Accounts.Add(acct);
                }

                HasSearched = true;
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Working capital loaded: Assets={CurrentAssets}, Liabilities={CurrentLiabilities}, Ratio={CurrentRatio}",
                CurrentAssets, CurrentLiabilities, CurrentRatio);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير رأس المال العامل", "WorkingCapitalViewModel.LoadDataAsync");
            Log.Warning("Failed to load working capital: {Error}", result.Error);
        }
    }

    #endregion

    #region Export

    private async void ExportToExcel()
    {
        if (Accounts.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"WorkingCapital_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("رأس المال العامل");

                    worksheet.Cell(1, 1).Value = "ملخص رأس المال العامل";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 14;

                    worksheet.Cell(3, 1).Value = "الأصول المتداولة";
                    worksheet.Cell(3, 2).Value = CurrentAssets;

                    worksheet.Cell(4, 1).Value = "الخصوم المتداولة";
                    worksheet.Cell(4, 2).Value = CurrentLiabilities;

                    worksheet.Cell(5, 1).Value = "رأس المال العامل";
                    worksheet.Cell(5, 2).Value = WorkingCapital;

                    worksheet.Cell(6, 1).Value = "النسبة الحالية";
                    worksheet.Cell(6, 2).Value = CurrentRatio;

                    worksheet.Cell(8, 1).Value = "الحسابات";
                    worksheet.Cell(8, 1).Style.Font.Bold = true;

                    worksheet.Cell(9, 1).Value = "اسم الحساب";
                    worksheet.Cell(9, 2).Value = "رمز الحساب";
                    worksheet.Cell(9, 3).Value = "الرصيد";
                    worksheet.Cell(9, 4).Value = "النوع";

                    for (int i = 0; i < Accounts.Count; i++)
                    {
                        var item = Accounts[i];
                        worksheet.Cell(i + 10, 1).Value = item.AccountName;
                        worksheet.Cell(i + 10, 2).Value = item.AccountCode;
                        worksheet.Cell(i + 10, 3).Value = item.Balance;
                        worksheet.Cell(i + 10, 4).Value = item.Type;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير تقرير رأس المال العامل إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير رأس المال العامل إلى Excel", "WorkingCapitalViewModel.ExportToExcel", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    private async Task ExportPdfAsync()
    {
        if (Accounts.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("الحساب", typeof(string));
            dataTable.Columns.Add("الرمز", typeof(string));
            dataTable.Columns.Add("الرصيد", typeof(decimal));
            dataTable.Columns.Add("النوع", typeof(string));

            foreach (var item in Accounts)
                dataTable.Rows.Add(item.AccountName, item.AccountCode,
                    item.Balance, item.Type);

            await PdfExportService.ExportToPdfAsync("رأس المال العامل", dataTable, WorkingCapital,
                $"WorkingCapital_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير رأس المال العامل إلى PDF", "WorkingCapitalViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
