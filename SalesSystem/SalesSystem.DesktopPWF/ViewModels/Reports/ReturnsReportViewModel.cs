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

public class ReturnsReportViewModel : ViewModelBase
{
    private IReportApiService? _reportApiService;
    private IProductApiService? _productApiService;

    private IReportApiService ReportApiService => _reportApiService ??= App.GetService<IReportApiService>();
    private IProductApiService ProductApiService => _productApiService ??= App.GetService<IProductApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private string? _selectedReturnType;
    private int? _selectedProductId;
    private DateTime _dateFrom;
    private DateTime _dateTo;
    private string? _errorMessage;
    private bool _hasSearched;
    private ObservableCollection<ReturnsReportDto> _reportData = new();

    public ReturnsReportViewModel()
    {
        _dateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _dateTo = DateTime.Today;

        ReturnTypes = new ObservableCollection<string> { "الكل", "مبيعات", "مشتريات" };
        _selectedReturnType = "الكل";
        Products = new ObservableCollection<ProductDto>();

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        ExportCommand = new RelayCommand(ExportToExcel);
        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        _ = LoadProductsAsync();
        _ = LoadDataAsync();
    }

    #region Properties

    public ObservableCollection<string> ReturnTypes { get; }
    public ObservableCollection<ProductDto> Products { get; }

    public string? SelectedReturnType
    {
        get => _selectedReturnType;
        set
        {
            if (SetProperty(ref _selectedReturnType, value))
                _ = LoadDataAsync();
        }
    }

    public int? SelectedProductId
    {
        get => _selectedProductId;
        set => SetProperty(ref _selectedProductId, value);
    }

    public DateTime DateFrom
    {
        get => _dateFrom;
        set
        {
            if (SetProperty(ref _dateFrom, value))
                _ = LoadDataAsync();
        }
    }

    public DateTime DateTo
    {
        get => _dateTo;
        set
        {
            if (SetProperty(ref _dateTo, value))
                _ = LoadDataAsync();
        }
    }

    public ObservableCollection<ReturnsReportDto> ReportData
    {
        get => _reportData;
        set
        {
            if (SetProperty(ref _reportData, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(TotalReturns));
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

    public int TotalReturns => ReportData.Count;
    public string SummaryText => $"إجمالي المرتجعات: {TotalReturns}";

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

        var returnType = SelectedReturnType switch
        {
            "مبيعات" => "Sales",
            "مشتريات" => "Purchases",
            _ => null
        };

        Log.Information("Loading returns report (Type: {ReturnType}, From: {DateFrom}, To: {DateTo})",
            returnType ?? "All", DateFrom, DateTo);

        var result = await ReportApiService.GetReturnsReportAsync(returnType, DateFrom, DateTo, SelectedProductId);

        if (result.IsSuccess && result.Value != null)
        {
            var sorted = result.Value
                .OrderByDescending(x => x.Date)
                .ToList();

            ReportData = new ObservableCollection<ReturnsReportDto>(sorted);
            HasSearched = true;

            Log.Information("Returns report loaded: {Count} returns", ReportData.Count);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المرتجعات", "ReturnsReportViewModel.LoadDataAsync");
            Log.Warning("Failed to load returns report: {Error}", result.Error);
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var result = await ProductApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Products.Clear();
                    Products.Add(new ProductDto(0, "جميع المنتجات", 0, null, null, null, 0, false, null, true));
                    foreach (var p in result.Value)
                        Products.Add(p);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة المنتجات", "ReturnsReportViewModel.LoadProductsAsync", ex);
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
                FileName = $"ReturnsReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("المرتجعات");

                    worksheet.Cell(1, 1).Value = "رقم المرتجع";
                    worksheet.Cell(1, 2).Value = "التاريخ";
                    worksheet.Cell(1, 3).Value = "النوع";
                    worksheet.Cell(1, 4).Value = "الطرف";
                    worksheet.Cell(1, 5).Value = "المنتج";
                    worksheet.Cell(1, 6).Value = "الكمية";
                    worksheet.Cell(1, 7).Value = "المبلغ";
                    worksheet.Cell(1, 8).Value = "السبب";
                    worksheet.Cell(1, 9).Value = "الحالة";

                    for (int i = 0; i < ReportData.Count; i++)
                    {
                        var item = ReportData[i];
                        worksheet.Cell(i + 2, 1).Value = item.ReturnNo;
                        worksheet.Cell(i + 2, 2).Value = item.Date.ToString("yyyy/MM/dd");
                        worksheet.Cell(i + 2, 3).Value = item.Type;
                        worksheet.Cell(i + 2, 4).Value = item.PartyName;
                        worksheet.Cell(i + 2, 5).Value = item.ProductName;
                        worksheet.Cell(i + 2, 6).Value = item.Quantity;
                        worksheet.Cell(i + 2, 7).Value = item.Amount;
                        worksheet.Cell(i + 2, 8).Value = item.Reason;
                        worksheet.Cell(i + 2, 9).Value = item.Status;
                    }

                    var headerRange = worksheet.Range(1, 1, 1, 9);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير تقرير المرتجعات إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير المرتجعات إلى Excel", "ReturnsReportViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("رقم المرتجع", typeof(string));
            dataTable.Columns.Add("التاريخ", typeof(string));
            dataTable.Columns.Add("النوع", typeof(string));
            dataTable.Columns.Add("الطرف", typeof(string));
            dataTable.Columns.Add("المنتج", typeof(string));
            dataTable.Columns.Add("الكمية", typeof(decimal));
            dataTable.Columns.Add("المبلغ", typeof(decimal));
            dataTable.Columns.Add("السبب", typeof(string));
            dataTable.Columns.Add("الحالة", typeof(string));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.ReturnNo,
                    item.Date.ToString("yyyy/MM/dd"),
                    item.Type, item.PartyName, item.ProductName,
                    item.Quantity, item.Amount, item.Reason, item.Status);

            await PdfExportService.ExportToPdfAsync("تقرير المرتجعات", dataTable, ReportData.Sum(x => x.Amount),
                $"ReturnsReport_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير المرتجعات إلى PDF", "ReturnsReportViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
