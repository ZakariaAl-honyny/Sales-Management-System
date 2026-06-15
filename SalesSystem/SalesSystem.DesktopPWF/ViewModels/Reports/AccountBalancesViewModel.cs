using System.Collections.ObjectModel;
using System.Windows.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

public class AccountBalancesViewModel : ViewModelBase
{
    private IFinancialReportApiService? _financialReportApiService;

    private IFinancialReportApiService FinancialReportApiService
        => _financialReportApiService ??= App.GetService<IFinancialReportApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private int? _selectedAccountType;
    private string? _searchText;
    private string? _errorMessage;
    private bool _hasSearched;
    private ObservableCollection<AccountBalanceReportDto> _reportData = new();
    private decimal _totalDebit;
    private decimal _totalCredit;

    public AccountBalancesViewModel()
    {
        AccountTypes = new ObservableCollection<AccountTypeOption>
        {
            new(0, "جميع الحسابات"),
            new(1, "أصول"),
            new(2, "خصوم"),
            new(3, "حقوق ملكية"),
            new(4, "إيرادات"),
            new(5, "مصروفات")
        };
        _selectedAccountType = 0;

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        ExportCommand = new RelayCommand(ExportToExcel);
        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        SearchCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        _ = LoadDataAsync();
    }

    #region Properties

    public ObservableCollection<AccountTypeOption> AccountTypes { get; }

    public int? SelectedAccountType
    {
        get => _selectedAccountType;
        set
        {
            if (SetProperty(ref _selectedAccountType, value))
                _ = LoadDataAsync();
        }
    }

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public ObservableCollection<AccountBalanceReportDto> ReportData
    {
        get => _reportData;
        set
        {
            if (SetProperty(ref _reportData, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(TotalDebit));
                OnPropertyChanged(nameof(TotalCredit));
                OnPropertyChanged(nameof(SummaryText));
            }
        }
    }

    public bool HasSearched
    {
        get => _hasSearched;
        set => SetProperty(ref _hasSearched, value);
    }

    public bool HasData => ReportData.Count > 0;
    public bool IsEmpty => ReportData.Count == 0 && HasSearched;

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

    public string FormattedTotalDebit => TotalDebit.ToString("N2");
    public string FormattedTotalCredit => TotalCredit.ToString("N2");
    public string SummaryText => $"إجمالي المدين: {FormattedTotalDebit} — إجمالي الدائن: {FormattedTotalCredit}";

    #endregion

    #region Commands

    public AsyncRelayCommand LoadCommand { get; }
    public RelayCommand ExportCommand { get; }
    public AsyncRelayCommand SearchCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    #endregion

    #region Data Loading

    private async Task LoadDataAsync()
    {
        ErrorMessage = null;

        byte? accountType = SelectedAccountType > 0 ? (byte)SelectedAccountType : null;

        Log.Information("Loading account balances (Type: {AccountType}, Search: {SearchText})",
            accountType, SearchText);

        var result = await FinancialReportApiService.GetAccountBalancesAsync(accountType, SearchText);

        if (result.IsSuccess && result.Value != null)
        {
            var sorted = result.Value
                .OrderBy(x => x.AccountCode)
                .ToList();

            ReportData = new ObservableCollection<AccountBalanceReportDto>(sorted);
            HasSearched = true;
            TotalDebit = result.Value.Sum(x => x.DebitBalance);
            TotalCredit = result.Value.Sum(x => x.CreditBalance);

            Log.Information("Account balances loaded: {Count} accounts, Debit={TotalDebit}, Credit={TotalCredit}",
                ReportData.Count, TotalDebit, TotalCredit);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل أرصدة الحسابات", "AccountBalancesViewModel.LoadDataAsync");
            Log.Warning("Failed to load account balances: {Error}", result.Error);
        }
    }

    #endregion

    #region Export

    private async void ExportToExcel()
    {
        if (ReportData.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"AccountBalances_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("أرصدة الحسابات");

                    worksheet.Cell(1, 1).Value = "رمز الحساب";
                    worksheet.Cell(1, 2).Value = "اسم الحساب";
                    worksheet.Cell(1, 3).Value = "النوع";
                    worksheet.Cell(1, 4).Value = "المستوى";
                    worksheet.Cell(1, 5).Value = "مدين";
                    worksheet.Cell(1, 6).Value = "دائن";
                    worksheet.Cell(1, 7).Value = "صافي الرصيد";

                    for (int i = 0; i < ReportData.Count; i++)
                    {
                        var item = ReportData[i];
                        worksheet.Cell(i + 2, 1).Value = item.AccountCode;
                        worksheet.Cell(i + 2, 2).Value = item.AccountName;
                        worksheet.Cell(i + 2, 3).Value = item.AccountTypeDisplay;
                        worksheet.Cell(i + 2, 4).Value = item.Level;
                        worksheet.Cell(i + 2, 5).Value = item.DebitBalance;
                        worksheet.Cell(i + 2, 6).Value = item.CreditBalance;
                        worksheet.Cell(i + 2, 7).Value = item.NetBalance;
                    }

                    var headerRange = worksheet.Range(1, 1, 1, 7);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير أرصدة الحسابات إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير أرصدة الحسابات إلى Excel", "AccountBalancesViewModel.ExportToExcel", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    private async Task ExportPdfAsync()
    {
        if (ReportData.Count == 0)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("الرمز", typeof(string));
            dataTable.Columns.Add("اسم الحساب", typeof(string));
            dataTable.Columns.Add("النوع", typeof(string));
            dataTable.Columns.Add("المستوى", typeof(int));
            dataTable.Columns.Add("رصيد مدين", typeof(decimal));
            dataTable.Columns.Add("رصيد دائن", typeof(decimal));
            dataTable.Columns.Add("صافي الرصيد", typeof(decimal));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.AccountCode, item.AccountName,
                    item.AccountTypeDisplay, item.Level,
                    item.DebitBalance, item.CreditBalance, item.NetBalance);

            await PdfExportService.ExportToPdfAsync("أرصدة الحسابات", dataTable, ReportData.Sum(x => x.NetBalance),
                $"AccountBalances_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير أرصدة الحسابات إلى PDF", "AccountBalancesViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}

public record AccountTypeOption(int Id, string Name);
