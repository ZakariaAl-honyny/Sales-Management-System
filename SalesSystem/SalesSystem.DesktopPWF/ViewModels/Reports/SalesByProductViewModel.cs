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
/// ViewModel for Sales by Product Report.
/// </summary>
public class SalesByProductViewModel : ViewModelBase
{
    private ISalesReportApiService? _salesReportApiService;
    private ISalesReportApiService SalesReportApiService => _salesReportApiService ??= App.GetService<ISalesReportApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private decimal _totalAmount;
    private decimal _totalProfit;
    private bool _hasData;

    public SalesByProductViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<SalesByProductDto>();

        SetDialogService(App.GetService<IDialogService>());

        GenerateReportCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadAsync)));

        ExportExcelCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportExcelAsync()));

        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));
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

    public ObservableCollection<SalesByProductDto> Entries { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public decimal TotalAmount
    {
        get => _totalAmount;
        private set
        {
            if (SetProperty(ref _totalAmount, value))
                OnPropertyChanged(nameof(FormattedTotalAmount));
        }
    }

    public decimal TotalProfit
    {
        get => _totalProfit;
        private set
        {
            if (SetProperty(ref _totalProfit, value))
                OnPropertyChanged(nameof(FormattedTotalProfit));
        }
    }

    public string FormattedTotalAmount => TotalAmount.ToString("N2");
    public string FormattedTotalProfit => TotalProfit.ToString("N2");

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

        Log.Information("Loading sales by product report from {FromDate} to {ToDate}", FromDate, ToDate);

        var result = await SalesReportApiService.GetSalesByProductAsync(FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            InvokeOnUIThread(() =>
            {
                Entries.Clear();

                foreach (var item in result.Value.OrderByDescending(x => x.TotalAmount))
                {
                    Entries.Add(item);
                }

                TotalAmount = result.Value.Sum(x => x.TotalAmount);
                TotalProfit = result.Value.Sum(x => x.TotalProfit);

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Sales by product loaded: {Count} products, Total={TotalAmount}, Profit={TotalProfit}",
                Entries.Count, TotalAmount, TotalProfit);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المبيعات حسب المنتج", "SalesByProductViewModel.LoadAsync");
            Log.Warning("Failed to load sales by product: {Error}", result.Error);
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
                FileName = $"SalesByProduct_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("المبيعات حسب المنتج");

                    worksheet.Cell(1, 1).Value = "اسم المنتج";
                    worksheet.Cell(1, 2).Value = "الكمية";
                    worksheet.Cell(1, 3).Value = "إجمالي المبيعات";
                    worksheet.Cell(1, 4).Value = "إجمالي التكلفة";
                    worksheet.Cell(1, 5).Value = "الربح";
                    worksheet.Cell(1, 6).Value = "نسبة الربح";

                    for (int i = 0; i < Entries.Count; i++)
                    {
                        var item = Entries[i];
                        worksheet.Cell(i + 2, 1).Value = item.ProductName;
                        worksheet.Cell(i + 2, 2).Value = item.Quantity;
                        worksheet.Cell(i + 2, 3).Value = item.TotalAmount;
                        worksheet.Cell(i + 2, 4).Value = item.TotalCost;
                        worksheet.Cell(i + 2, 5).Value = item.TotalProfit;
                        worksheet.Cell(i + 2, 6).Value = item.ProfitMargin;
                    }

                    worksheet.Cell(Entries.Count + 2, 1).Value = "الإجمالي";
                    worksheet.Cell(Entries.Count + 2, 3).Value = TotalAmount;
                    worksheet.Cell(Entries.Count + 2, 5).Value = TotalProfit;
                    worksheet.Cell(Entries.Count + 2, 1).Style.Font.Bold = true;

                    var headerRange = worksheet.Range(1, 1, 1, 6);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير تقرير المبيعات حسب المنتج إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير المبيعات حسب المنتج إلى Excel", "SalesByProductViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("اسم المنتج", typeof(string));
            dataTable.Columns.Add("الكمية", typeof(decimal));
            dataTable.Columns.Add("إجمالي المبيعات", typeof(decimal));
            dataTable.Columns.Add("إجمالي التكلفة", typeof(decimal));
            dataTable.Columns.Add("الربح", typeof(decimal));
            dataTable.Columns.Add("نسبة الربح", typeof(decimal));

            foreach (var item in Entries)
                dataTable.Rows.Add(item.ProductName, item.Quantity,
                    item.TotalAmount, item.TotalCost, item.TotalProfit, item.ProfitMargin);

            await PdfExportService.ExportToPdfAsync("المبيعات حسب المنتج", dataTable, TotalAmount,
                $"SalesByProduct_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير المبيعات حسب المنتج إلى PDF", "SalesByProductViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
