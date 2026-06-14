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
/// ViewModel for the General Ledger Report — displays account movements.
/// </summary>
public class GeneralLedgerViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private IAccountApiService? _accountApiService;

    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();
    private IAccountApiService AccountApiService => _accountApiService ??= App.GetService<IAccountApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private int? _selectedAccountId;
    private string? _selectedAccountName;
    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private decimal _openingBalance;
    private decimal _closingBalance;
    private decimal _totalDebit;
    private decimal _totalCredit;
    private bool _hasData;

    public GeneralLedgerViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Lines = new ObservableCollection<AccountLedgerLineDto>();
        AvailableAccounts = new ObservableCollection<AccountDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        LoadAccountsCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAccountsAsync)));

        _ = LoadAccountsAsync();
    }

    #region Properties

    public int? SelectedAccountId
    {
        get => _selectedAccountId;
        set => SetProperty(ref _selectedAccountId, value);
    }

    public string? SelectedAccountName
    {
        get => _selectedAccountName;
        set => SetProperty(ref _selectedAccountName, value);
    }

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

    public ObservableCollection<AccountLedgerLineDto> Lines { get; }
    public ObservableCollection<AccountDto> AvailableAccounts { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal OpeningBalance
    {
        get => _openingBalance;
        private set
        {
            if (SetProperty(ref _openingBalance, value))
                OnPropertyChanged(nameof(FormattedOpeningBalance));
        }
    }

    public decimal ClosingBalance
    {
        get => _closingBalance;
        private set
        {
            if (SetProperty(ref _closingBalance, value))
                OnPropertyChanged(nameof(FormattedClosingBalance));
        }
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

    public string FormattedOpeningBalance => OpeningBalance.ToString("N2");
    public string FormattedClosingBalance => ClosingBalance.ToString("N2");
    public string FormattedTotalDebit => TotalDebit.ToString("N2");
    public string FormattedTotalCredit => TotalCredit.ToString("N2");

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
    public AsyncRelayCommand LoadAccountsCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadAccountsAsync()
    {
        try
        {
            var result = await AccountApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    AvailableAccounts.Clear();
                    foreach (var acc in result.Value.Where(a => a.IsActive))
                        AvailableAccounts.Add(acc);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة الحسابات", "GeneralLedgerViewModel.LoadAccountsAsync", ex);
        }
    }

    private async Task LoadAsync()
    {
        ErrorMessage = null;

        if (SelectedAccountId == null || SelectedAccountId == 0)
        {
            await D.ShowWarningAsync("تنبيه", "يرجى اختيار حساب لعرض دفتر الأستاذ");
            return;
        }

        Log.Information("Loading general ledger for account {AccountId} from {FromDate} to {ToDate}",
            SelectedAccountId, FromDate, ToDate);

        var result = await ReportApiService.GetGeneralLedgerAsync(SelectedAccountId.Value, FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Lines.Clear();

                SelectedAccountName = $"{result.Value.AccountCode} - {result.Value.AccountNameAr}";

                if (result.Value.Lines != null)
                {
                    foreach (var line in result.Value.Lines.OrderBy(x => x.Date))
                    {
                        Lines.Add(line);
                    }
                }

                OpeningBalance = result.Value.OpeningBalance;
                TotalDebit = result.Value.TotalDebit;
                TotalCredit = result.Value.TotalCredit;
                ClosingBalance = result.Value.ClosingBalance;

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("General ledger loaded: OpeningBalance={OpeningBalance}, TotalDebit={TotalDebit}, TotalCredit={TotalCredit}, ClosingBalance={ClosingBalance}",
                OpeningBalance, TotalDebit, TotalCredit, ClosingBalance);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل دفتر الأستاذ", "GeneralLedgerViewModel.LoadAsync");
            Log.Warning("Failed to load general ledger: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        if (Lines.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"GeneralLedger_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("دفتر الأستاذ");

                    // Header info
                    worksheet.Cell(1, 1).Value = "الحساب:";
                    worksheet.Cell(1, 2).Value = SelectedAccountName ?? "";
                    worksheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
                    worksheet.Cell(2, 1).Value = "الرصيد الافتتاحي:";
                    worksheet.Cell(2, 2).Value = OpeningBalance;
                    worksheet.Range(2, 1, 2, 2).Style.Font.Bold = true;

                    // Column headers
                    worksheet.Cell(4, 1).Value = "التاريخ";
                    worksheet.Cell(4, 2).Value = "رقم القيد";
                    worksheet.Cell(4, 3).Value = "البيان";
                    worksheet.Cell(4, 4).Value = "رقم المرجع";
                    worksheet.Cell(4, 5).Value = "مدين";
                    worksheet.Cell(4, 6).Value = "دائن";
                    worksheet.Cell(4, 7).Value = "الرصيد";

                    for (int i = 0; i < Lines.Count; i++)
                    {
                        var item = Lines[i];
                        worksheet.Cell(i + 5, 1).Value = item.Date.ToString("yyyy/MM/dd");
                        worksheet.Cell(i + 5, 2).Value = item.EntryNumber;
                        worksheet.Cell(i + 5, 3).Value = item.Description;
                        worksheet.Cell(i + 5, 4).Value = item.ReferenceNumber ?? "";
                        worksheet.Cell(i + 5, 5).Value = item.Debit;
                        worksheet.Cell(i + 5, 6).Value = item.Credit;
                        worksheet.Cell(i + 5, 7).Value = item.RunningBalance;
                    }

                    // Totals
                    var totalRow = Lines.Count + 5;
                    worksheet.Cell(totalRow, 1).Value = "الإجمالي";
                    worksheet.Cell(totalRow, 5).Value = TotalDebit;
                    worksheet.Cell(totalRow, 6).Value = TotalCredit;
                    worksheet.Cell(totalRow, 7).Value = ClosingBalance;
                    worksheet.Cell(totalRow, 1).Style.Font.Bold = true;

                    // Footer info
                    worksheet.Cell(totalRow + 2, 1).Value = "الرصيد الختامي:";
                    worksheet.Cell(totalRow + 2, 2).Value = ClosingBalance;
                    worksheet.Range(totalRow + 2, 1, totalRow + 2, 2).Style.Font.Bold = true;

                    var headerRange = worksheet.Range(4, 1, 4, 7);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير دفتر الأستاذ إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير دفتر الأستاذ إلى Excel", "GeneralLedgerViewModel.ExportToExcel", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    private async Task ExportPdfAsync()
    {
        if (Lines.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("التاريخ", typeof(string));
            dataTable.Columns.Add("رقم القيد", typeof(string));
            dataTable.Columns.Add("البيان", typeof(string));
            dataTable.Columns.Add("رقم المرجع", typeof(string));
            dataTable.Columns.Add("مدين", typeof(decimal));
            dataTable.Columns.Add("دائن", typeof(decimal));
            dataTable.Columns.Add("الرصيد", typeof(decimal));

            foreach (var item in Lines)
                dataTable.Rows.Add(item.Date.ToString("yyyy/MM/dd"),
                    item.EntryNumber, item.Description,
                    item.ReferenceNumber ?? "", item.Debit,
                    item.Credit, item.RunningBalance);

            await PdfExportService.ExportToPdfAsync("دفتر الأستاذ", dataTable, ClosingBalance,
                $"GeneralLedger_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير دفتر الأستاذ إلى PDF", "GeneralLedgerViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
