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
/// ViewModel for the Balance Sheet Report — displays assets, liabilities, and equity.
/// </summary>
public class BalanceSheetViewModel : ViewModelBase
{
    private IFinancialReportApiService? _reportApiService;
    private IFinancialReportApiService ReportApiService => _reportApiService ??= App.GetService<IFinancialReportApiService>();

    private IDialogService D => DialogService!;

    private DateTime _asOfDate;
    private string? _errorMessage;
    private decimal _totalAssets;
    private decimal _totalLiabilities;
    private decimal _totalEquity;
    private bool _isBalanced;
    private bool _hasData;

    public BalanceSheetViewModel()
    {
        _asOfDate = DateTime.Today;

        AssetSections = new ObservableCollection<BalanceSheetSectionDto>();
        LiabilitySections = new ObservableCollection<BalanceSheetSectionDto>();
        EquitySections = new ObservableCollection<BalanceSheetSectionDto>();

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

    public ObservableCollection<BalanceSheetSectionDto> AssetSections { get; }
    public ObservableCollection<BalanceSheetSectionDto> LiabilitySections { get; }
    public ObservableCollection<BalanceSheetSectionDto> EquitySections { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalAssets
    {
        get => _totalAssets;
        private set
        {
            if (SetProperty(ref _totalAssets, value))
                OnPropertyChanged(nameof(FormattedTotalAssets));
        }
    }

    public decimal TotalLiabilities
    {
        get => _totalLiabilities;
        private set
        {
            if (SetProperty(ref _totalLiabilities, value))
                OnPropertyChanged(nameof(FormattedTotalLiabilities));
        }
    }

    public decimal TotalEquity
    {
        get => _totalEquity;
        private set
        {
            if (SetProperty(ref _totalEquity, value))
                OnPropertyChanged(nameof(FormattedTotalEquity));
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

    public string FormattedTotalAssets => TotalAssets.ToString("N2");
    public string FormattedTotalLiabilities => TotalLiabilities.ToString("N2");
    public string FormattedTotalEquity => TotalEquity.ToString("N2");

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

        Log.Information("Loading balance sheet as of {AsOfDate}", AsOfDate);

        var result = await ReportApiService.GetBalanceSheetAsync(AsOfDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                AssetSections.Clear();
                LiabilitySections.Clear();
                EquitySections.Clear();

                if (result.Value.Sections != null)
                    foreach (var section in result.Value.Sections)
                    {
                        if (section.Name.Contains("أصول", StringComparison.Ordinal))
                            AssetSections.Add(section);
                        else if (section.Name.Contains("خصوم", StringComparison.Ordinal))
                            LiabilitySections.Add(section);
                        else
                            EquitySections.Add(section);
                    }

                TotalAssets = result.Value.TotalAssets;
                TotalLiabilities = result.Value.TotalLiabilities;
                TotalEquity = result.Value.TotalEquity;
                IsBalanced = result.Value.IsBalanced;

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Balance sheet loaded: TotalAssets={TotalAssets}, TotalLiabilities={TotalLiabilities}, TotalEquity={TotalEquity}, Balanced={IsBalanced}",
                TotalAssets, TotalLiabilities, TotalEquity, IsBalanced);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل الميزانية العمومية", "BalanceSheetViewModel.LoadAsync");
            Log.Warning("Failed to load balance sheet: {Error}", result.Error);
        }
    }

    private async Task ExportExcelAsync()
    {
        if (!HasData)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BalanceSheet_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("الميزانية العمومية");

                    var sections = new[] {
                        new { Name = "الأصول", Data = AssetSections, Total = TotalAssets },
                        new { Name = "الخصوم", Data = LiabilitySections, Total = TotalLiabilities },
                        new { Name = "حقوق الملكية", Data = EquitySections, Total = TotalEquity }
                    };

                    int currentRow = 1;

                    foreach (var sectionGroup in sections)
                    {
                        worksheet.Cell(currentRow, 1).Value = sectionGroup.Name;
                        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                        worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                        currentRow++;

                        foreach (var section in sectionGroup.Data)
                        {
                            worksheet.Cell(currentRow, 1).Value = section.Name;
                            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
                            currentRow++;

                            if (section.Lines != null)
                            {
                                foreach (var line in section.Lines)
                                {
                                    worksheet.Cell(currentRow, 1).Value = line.AccountName;
                                    worksheet.Cell(currentRow, 2).Value = line.Balance;
                                    currentRow++;
                                }
                            }

                            worksheet.Cell(currentRow, 1).Value = $"إجمالي {section.Name}";
                            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                            worksheet.Cell(currentRow, 2).Value = section.Total;
                            worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
                            currentRow++;
                            currentRow++; // blank row between sections
                        }

                        worksheet.Cell(currentRow, 1).Value = $"إجمالي {sectionGroup.Name}";
                        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                        worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
                        worksheet.Cell(currentRow, 2).Value = sectionGroup.Total;
                        worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
                        currentRow += 2;
                    }

                    // Summary
                    worksheet.Cell(currentRow, 1).Value = "ملخص الميزانية";
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                    currentRow++;

                    worksheet.Cell(currentRow, 1).Value = "إجمالي الأصول";
                    worksheet.Cell(currentRow, 2).Value = TotalAssets;
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = "إجمالي الخصوم";
                    worksheet.Cell(currentRow, 2).Value = TotalLiabilities;
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = "حقوق الملكية";
                    worksheet.Cell(currentRow, 2).Value = TotalEquity;
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = "الحالة";
                    worksheet.Cell(currentRow, 2).Value = IsBalanced ? "متوازن" : "غير متوازن";

                    worksheet.Columns().AdjustToContents();

                    worksheet.Column(2).Style.NumberFormat.Format = "#,##0.00";

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير الميزانية العمومية إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير الميزانية العمومية إلى Excel", "BalanceSheetViewModel.ExportToExcel", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    private async Task ExportPdfAsync()
    {
        if (!HasData)
        {
            await D.ShowWarningAsync("تنبيه", "لا توجد بيانات لتصديرها");
            return;
        }

        try
        {
            var dataTable = new System.Data.DataTable();
            dataTable.Columns.Add("القسم", typeof(string));
            dataTable.Columns.Add("الحساب", typeof(string));
            dataTable.Columns.Add("الرصيد", typeof(decimal));

            foreach (var section in AssetSections)
            {
                if (section.Lines != null)
                    foreach (var line in section.Lines)
                        dataTable.Rows.Add("أصول", line.AccountName, line.Balance);
            }
            foreach (var section in LiabilitySections)
            {
                if (section.Lines != null)
                    foreach (var line in section.Lines)
                        dataTable.Rows.Add("خصوم", line.AccountName, line.Balance);
            }
            foreach (var section in EquitySections)
            {
                if (section.Lines != null)
                    foreach (var line in section.Lines)
                        dataTable.Rows.Add("حقوق ملكية", line.AccountName, line.Balance);
            }

            var exportService = App.GetService<IFinancialReportExportService>();
            await exportService.ExportToPdfAsync("الميزانية العمومية", dataTable, TotalAssets,
                $"BalanceSheet_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير الميزانية العمومية إلى PDF", "BalanceSheetViewModel.ExportToPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
