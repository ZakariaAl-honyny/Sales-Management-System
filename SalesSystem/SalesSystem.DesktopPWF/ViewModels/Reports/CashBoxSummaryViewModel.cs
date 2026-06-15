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
/// ViewModel for Cash Box Summary Report — displays balances of all cash boxes.
/// </summary>
public class CashBoxSummaryViewModel : ViewModelBase
{
    private ICashBoxReportApiService? _cashBoxReportApiService;
    private ICashBoxReportApiService CashBoxReportApiService => _cashBoxReportApiService ??= App.GetService<ICashBoxReportApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private DateTime _asOfDate;
    private string? _errorMessage;
    private decimal _totalIncome;
    private decimal _totalExpense;
    private bool _hasData;

    public CashBoxSummaryViewModel()
    {
        _asOfDate = DateTime.Today;

        Entries = new ObservableCollection<CashBoxSummaryDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));
    }

    #region Properties

    public DateTime AsOfDate
    {
        get => _asOfDate;
        set => SetProperty(ref _asOfDate, value);
    }

    public ObservableCollection<CashBoxSummaryDto> Entries { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalIncome
    {
        get => _totalIncome;
        private set
        {
            if (SetProperty(ref _totalIncome, value))
                OnPropertyChanged(nameof(FormattedTotalIncome));
        }
    }

    public decimal TotalExpense
    {
        get => _totalExpense;
        private set
        {
            if (SetProperty(ref _totalExpense, value))
                OnPropertyChanged(nameof(FormattedTotalExpense));
        }
    }

    public string FormattedTotalIncome => TotalIncome.ToString("N2");
    public string FormattedTotalExpense => TotalExpense.ToString("N2");

    public decimal TotalNetBalance => TotalIncome - TotalExpense;

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
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        Log.Information("Loading cash box summary as of {AsOfDate}", AsOfDate);

        var result = await CashBoxReportApiService.GetCashBoxSummaryAsync(AsOfDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderBy(x => x.CashBoxName))
                {
                    Entries.Add(item);
                }

                TotalIncome = result.Value.Sum(x => x.TotalIncome);
                TotalExpense = result.Value.Sum(x => x.TotalExpense);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Cash box summary loaded: {Count} boxes, TotalIncome={TotalIncome}, TotalExpense={TotalExpense}",
                Entries.Count, TotalIncome, TotalExpense);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل ملخص الصناديق", "CashBoxSummaryViewModel.LoadAsync");
            Log.Warning("Failed to load cash box summary: {Error}", result.Error);
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
                FileName = $"CashBoxSummary_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("ملخص الصناديق");

                    worksheet.Cell(1, 1).Value = "اسم الصندوق";
                    worksheet.Cell(1, 2).Value = "إجمالي الإيرادات";
                    worksheet.Cell(1, 3).Value = "إجمالي المصروفات";
                    worksheet.Cell(1, 4).Value = "صافي الرصيد";

                    for (int i = 0; i < Entries.Count; i++)
                    {
                        var item = Entries[i];
                        worksheet.Cell(i + 2, 1).Value = item.CashBoxName;
                        worksheet.Cell(i + 2, 2).Value = item.TotalIncome;
                        worksheet.Cell(i + 2, 3).Value = item.TotalExpense;
                        worksheet.Cell(i + 2, 4).Value = item.NetBalance;
                    }

                    worksheet.Cell(Entries.Count + 2, 1).Value = "الإجمالي";
                    worksheet.Cell(Entries.Count + 2, 2).Value = TotalIncome;
                    worksheet.Cell(Entries.Count + 2, 3).Value = TotalExpense;
                    worksheet.Cell(Entries.Count + 2, 1).Style.Font.Bold = true;

                    var headerRange = worksheet.Range(1, 1, 1, 4);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير ملخص أرصدة الصناديق إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير ملخص أرصدة الصناديق إلى Excel", "CashBoxSummaryViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("اسم الصندوق", typeof(string));
            dataTable.Columns.Add("إجمالي الإيرادات", typeof(decimal));
            dataTable.Columns.Add("إجمالي المصروفات", typeof(decimal));
            dataTable.Columns.Add("صافي الرصيد", typeof(decimal));

            foreach (var item in Entries)
                dataTable.Rows.Add(item.CashBoxName, item.TotalIncome,
                    item.TotalExpense, item.NetBalance);

            await PdfExportService.ExportToPdfAsync("ملخص أرصدة الصناديق", dataTable, TotalNetBalance,
                $"CashBoxSummary_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير ملخص أرصدة الصناديق إلى PDF", "CashBoxSummaryViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
