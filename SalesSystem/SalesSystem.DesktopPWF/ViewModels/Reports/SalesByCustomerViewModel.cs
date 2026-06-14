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
/// ViewModel for Sales by Customer Report.
/// </summary>
public class SalesByCustomerViewModel : ViewModelBase
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
    private bool _hasData;

    public SalesByCustomerViewModel()
    {
        _toDate = DateTime.Today;
        _fromDate = DateTime.Today.AddDays(-30);

        Entries = new ObservableCollection<SalesByCustomerDto>();

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

    public ObservableCollection<SalesByCustomerDto> Entries { get; }

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

    public string FormattedTotalAmount => TotalAmount.ToString("N2");

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

        Log.Information("Loading sales by customer report from {FromDate} to {ToDate}", FromDate, ToDate);

        var result = await SalesReportApiService.GetSalesByCustomerAsync(FromDate, ToDate);

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

                HasData = true;
                OnPropertyChanged(nameof(IsEmpty));
            });

            Log.Information("Sales by customer loaded: {Count} customers, Total={TotalAmount}", Entries.Count, TotalAmount);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المبيعات حسب العميل", "SalesByCustomerViewModel.LoadAsync");
            Log.Warning("Failed to load sales by customer: {Error}", result.Error);
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
                FileName = $"SalesByCustomer_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("المبيعات حسب العميل");

                    worksheet.Cell(1, 1).Value = "اسم العميل";
                    worksheet.Cell(1, 2).Value = "عدد الفواتير";
                    worksheet.Cell(1, 3).Value = "إجمالي المبيعات";
                    worksheet.Cell(1, 4).Value = "المدفوع";
                    worksheet.Cell(1, 5).Value = "المتبقي";

                    for (int i = 0; i < Entries.Count; i++)
                    {
                        var item = Entries[i];
                        worksheet.Cell(i + 2, 1).Value = item.CustomerName;
                        worksheet.Cell(i + 2, 2).Value = item.InvoiceCount;
                        worksheet.Cell(i + 2, 3).Value = item.TotalAmount;
                        worksheet.Cell(i + 2, 4).Value = item.PaidAmount;
                        worksheet.Cell(i + 2, 5).Value = item.DueAmount;
                    }

                    worksheet.Cell(Entries.Count + 2, 1).Value = "الإجمالي";
                    worksheet.Cell(Entries.Count + 2, 3).Value = TotalAmount;
                    worksheet.Cell(Entries.Count + 2, 1).Style.Font.Bold = true;

                    var headerRange = worksheet.Range(1, 1, 1, 5);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير تقرير المبيعات حسب العميل إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير المبيعات حسب العميل إلى Excel", "SalesByCustomerViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("اسم العميل", typeof(string));
            dataTable.Columns.Add("عدد الفواتير", typeof(int));
            dataTable.Columns.Add("إجمالي المبيعات", typeof(decimal));
            dataTable.Columns.Add("المدفوع", typeof(decimal));
            dataTable.Columns.Add("المتبقي", typeof(decimal));

            foreach (var item in Entries)
                dataTable.Rows.Add(item.CustomerName,
                    item.InvoiceCount, item.TotalAmount,
                    item.PaidAmount, item.DueAmount);

            await PdfExportService.ExportToPdfAsync("المبيعات حسب العميل", dataTable, TotalAmount,
                $"SalesByCustomer_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير المبيعات حسب العميل إلى PDF", "SalesByCustomerViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
