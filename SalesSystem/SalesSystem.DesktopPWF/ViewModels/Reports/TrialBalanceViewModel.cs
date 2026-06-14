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
/// ViewModel for the Trial Balance Report — displays debits and credits.
/// </summary>
public class TrialBalanceViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private DateTime _asOfDate;
    private string? _errorMessage;
    private decimal _totalDebit;
    private decimal _totalCredit;
    private bool _isBalanced;
    private bool _hasData;

    public TrialBalanceViewModel()
    {
        _asOfDate = DateTime.Today;

        Entries = new ObservableCollection<TrialBalanceDto>();

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

    public ObservableCollection<TrialBalanceDto> Entries { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalDebit
    {
        get => _totalDebit;
        private set
        {
            if (SetProperty(ref _totalDebit, value))
                OnPropertyChanged(nameof(FormattedTotalDebit));
        }
    }

    public decimal TotalCredit
    {
        get => _totalCredit;
        private set
        {
            if (SetProperty(ref _totalCredit, value))
                OnPropertyChanged(nameof(FormattedTotalCredit));
        }
    }

    public bool IsBalanced
    {
        get => _isBalanced;
        private set
        {
            if (SetProperty(ref _isBalanced, value))
                OnPropertyChanged(nameof(BalanceStatusDisplay));
        }
    }

    public string FormattedTotalDebit => TotalDebit.ToString("N2");
    public string FormattedTotalCredit => TotalCredit.ToString("N2");

    public string BalanceStatusDisplay => IsBalanced ? "🟢 متوازن" : "🔴 غير متوازن";

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

        Log.Information("Loading trial balance as of {AsOfDate}", AsOfDate);

        var result = await ReportApiService.GetTrialBalanceAsync(AsOfDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var entry in result.Value.OrderBy(x => x.AccountCode))
                {
                    Entries.Add(entry);
                }

                TotalDebit = result.Value.Sum(x => x.ClosingDebit);
                TotalCredit = result.Value.Sum(x => x.ClosingCredit);
                IsBalanced = Math.Abs(TotalDebit - TotalCredit) < 0.01m;

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Trial balance loaded: TotalDebit={TotalDebit}, TotalCredit={TotalCredit}",
                TotalDebit, TotalCredit, IsBalanced);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل ميزان المراجعة", "TrialBalanceViewModel.LoadAsync");
            Log.Warning("Failed to load trial balance: {Error}", result.Error);
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
                FileName = $"TrialBalance_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("ميزان المراجعة");

                    worksheet.Cell(1, 1).Value = "كود الحساب";
                    worksheet.Cell(1, 2).Value = "اسم الحساب";
                    worksheet.Cell(1, 3).Value = "مدين افتتاحي";
                    worksheet.Cell(1, 4).Value = "دائن افتتاحي";
                    worksheet.Cell(1, 5).Value = "مدين حركات";
                    worksheet.Cell(1, 6).Value = "دائن حركات";
                    worksheet.Cell(1, 7).Value = "رصيد مدين";
                    worksheet.Cell(1, 8).Value = "رصيد دائن";
                    worksheet.Cell(1, 9).Value = "نوع الحساب";

                    for (int i = 0; i < Entries.Count; i++)
                    {
                        var item = Entries[i];
                        worksheet.Cell(i + 2, 1).Value = item.AccountCode;
                        worksheet.Cell(i + 2, 2).Value = item.AccountName;
                        worksheet.Cell(i + 2, 3).Value = item.OpeningDebit;
                        worksheet.Cell(i + 2, 4).Value = item.OpeningCredit;
                        worksheet.Cell(i + 2, 5).Value = item.TransactionDebit;
                        worksheet.Cell(i + 2, 6).Value = item.TransactionCredit;
                        worksheet.Cell(i + 2, 7).Value = item.ClosingDebit;
                        worksheet.Cell(i + 2, 8).Value = item.ClosingCredit;
                        worksheet.Cell(i + 2, 9).Value = item.AccountTypeLabel ?? "";
                    }

                    worksheet.Cell(Entries.Count + 2, 1).Value = "الإجمالي";
                    worksheet.Cell(Entries.Count + 2, 7).Value = TotalDebit;
                    worksheet.Cell(Entries.Count + 2, 8).Value = TotalCredit;
                    worksheet.Cell(Entries.Count + 2, 1).Style.Font.Bold = true;

                    var headerRange = worksheet.Range(1, 1, 1, 9);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير ميزان المراجعة إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير ميزان المراجعة إلى Excel", "TrialBalanceViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("كود الحساب", typeof(string));
            dataTable.Columns.Add("اسم الحساب", typeof(string));
            dataTable.Columns.Add("مدين افتتاحي", typeof(decimal));
            dataTable.Columns.Add("دائن افتتاحي", typeof(decimal));
            dataTable.Columns.Add("مدين حركات", typeof(decimal));
            dataTable.Columns.Add("دائن حركات", typeof(decimal));
            dataTable.Columns.Add("رصيد مدين", typeof(decimal));
            dataTable.Columns.Add("رصيد دائن", typeof(decimal));
            dataTable.Columns.Add("نوع الحساب", typeof(string));

            foreach (var item in Entries)
                dataTable.Rows.Add(item.AccountCode, item.AccountName,
                    item.OpeningDebit, item.OpeningCredit,
                    item.TransactionDebit, item.TransactionCredit,
                    item.ClosingDebit, item.ClosingCredit,
                    item.AccountTypeLabel ?? "");

            await PdfExportService.ExportToPdfAsync("ميزان المراجعة", dataTable, TotalDebit,
                $"TrialBalance_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير ميزان المراجعة إلى PDF", "TrialBalanceViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
