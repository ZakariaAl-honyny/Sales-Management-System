using System.Collections.ObjectModel;
using ClosedXML.Excel;
using Microsoft.Win32;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using SalesSystem.DesktopPWF.Services.Export;
using Serilog;

namespace SalesSystem.DesktopPWF.ViewModels.Reports;

public class ProductProfitabilityViewModel : ViewModelBase
{
    private ISalesReportApiService? _salesReportApiService;
    private IProductApiService? _productApiService;
    private IProductCategoryApiService? _categoryApiService;

    private ISalesReportApiService SalesReportApiService => _salesReportApiService ??= App.GetService<ISalesReportApiService>();
    private IProductApiService ProductApiService => _productApiService ??= App.GetService<IProductApiService>();
    private IProductCategoryApiService CategoryApiService => _categoryApiService ??= App.GetService<IProductCategoryApiService>();

    private IDialogService D => DialogService!;

    private IFinancialReportExportService? _pdfExportService;
    private IFinancialReportExportService PdfExportService => _pdfExportService ??= App.GetService<IFinancialReportExportService>();

    private int? _selectedProductId;
    private int? _selectedCategoryId;
    private DateTime _fromDate;
    private DateTime _toDate;
    private string? _errorMessage;
    private bool _hasSearched;
    private ObservableCollection<ProductProfitabilityDto> _reportData = new();

    public ProductProfitabilityViewModel()
    {
        _fromDate = DateTime.Today.AddDays(-30);
        _toDate = DateTime.Today;

        Products = new ObservableCollection<ProductDto>();
        Categories = new ObservableCollection<ProductCategoryDto>();

        SetDialogService(App.GetService<IDialogService>());

        LoadCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExecuteAsync(LoadDataAsync)));

        ExportCommand = new RelayCommand(ExportToExcel);
        ExportPdfCommand = new AsyncRelayCommand(
            (Func<Task>)(async () => await ExportPdfAsync()));

        _ = LoadProductsAsync();
        _ = LoadCategoriesAsync();
        _ = LoadDataAsync();
    }

    #region Properties

    public ObservableCollection<ProductDto> Products { get; }
    public ObservableCollection<ProductCategoryDto> Categories { get; }

    public int? SelectedProductId
    {
        get => _selectedProductId;
        set => SetProperty(ref _selectedProductId, value);
    }

    public int? SelectedCategoryId
    {
        get => _selectedCategoryId;
        set => SetProperty(ref _selectedCategoryId, value);
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
                _ = LoadDataAsync();
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value))
                _ = LoadDataAsync();
        }
    }

    public ObservableCollection<ProductProfitabilityDto> ReportData
    {
        get => _reportData;
        set
        {
            if (SetProperty(ref _reportData, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(TotalSales));
                OnPropertyChanged(nameof(TotalProfit));
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

    public decimal TotalSales => ReportData.Sum(x => x.TotalSalesAmount);
    public decimal TotalProfit => ReportData.Sum(x => x.GrossProfit);
    public string SummaryText => $"إجمالي المبيعات: {TotalSales:N2} — إجمالي الربح: {TotalProfit:N2}";

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

        Log.Information("Loading product profitability report (From: {FromDate}, To: {ToDate})", FromDate, ToDate);

        var result = await SalesReportApiService.GetProductProfitabilityAsync(SelectedProductId, SelectedCategoryId, FromDate, ToDate);

        if (result.IsSuccess && result.Value != null)
        {
            var sorted = result.Value
                .OrderByDescending(x => x.GrossProfit)
                .ToList();

            ReportData = new ObservableCollection<ProductProfitabilityDto>(sorted);
            HasSearched = true;

            Log.Information("Product profitability loaded: {Count} products, TotalSales={TotalSales}, TotalProfit={TotalProfit}",
                ReportData.Count, TotalSales, TotalProfit);
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير أرباح الأصناف", "ProductProfitabilityViewModel.LoadDataAsync");
            Log.Warning("Failed to load product profitability: {Error}", result.Error);
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
                    Products.Add(new ProductDto(0, "جميع المنتجات", 0, null, null, null, 0, false, null, null, null, null, true));
                    foreach (var p in result.Value)
                        Products.Add(p);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة المنتجات", "ProductProfitabilityViewModel.LoadProductsAsync", ex);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var result = await CategoryApiService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Categories.Clear();
                    Categories.Add(new ProductCategoryDto(0, "جميع التصنيفات", null, null, true));
                    foreach (var c in result.Value)
                        Categories.Add(c);
                });
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تحميل قائمة التصنيفات", "ProductProfitabilityViewModel.LoadCategoriesAsync", ex);
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
                FileName = $"ProductProfitability_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("أرباح الأصناف");

                    worksheet.Cell(1, 1).Value = "المنتج";
                    worksheet.Cell(1, 2).Value = "التصنيف";
                    worksheet.Cell(1, 3).Value = "الكمية المباعة";
                    worksheet.Cell(1, 4).Value = "إجمالي المبيعات";
                    worksheet.Cell(1, 5).Value = "إجمالي التكلفة";
                    worksheet.Cell(1, 6).Value = "صافي الربح";
                    worksheet.Cell(1, 7).Value = "نسبة الربح";

                    for (int i = 0; i < ReportData.Count; i++)
                    {
                        var item = ReportData[i];
                        worksheet.Cell(i + 2, 1).Value = item.ProductName;
                        worksheet.Cell(i + 2, 2).Value = item.Category;
                        worksheet.Cell(i + 2, 3).Value = item.TotalSoldQty;
                        worksheet.Cell(i + 2, 4).Value = item.TotalSalesAmount;
                        worksheet.Cell(i + 2, 5).Value = item.TotalCOGS;
                        worksheet.Cell(i + 2, 6).Value = item.GrossProfit;
                        worksheet.Cell(i + 2, 7).Value = item.ProfitMargin.ToString("P1");
                    }

                    var headerRange = worksheet.Range(1, 1, 1, 7);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E8EE");
                    worksheet.Columns().AdjustToContents();

                    workbook.SaveAs(saveFileDialog.FileName);
                }

                await D.ShowInfoAsync("نجاح", "تم تصدير تقرير أرباح الأصناف إلى Excel بنجاح");
            }
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير أرباح الأصناف إلى Excel", "ProductProfitabilityViewModel.ExportToExcel", ex);
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
            dataTable.Columns.Add("المنتج", typeof(string));
            dataTable.Columns.Add("التصنيف", typeof(string));
            dataTable.Columns.Add("الكمية المباعة", typeof(decimal));
            dataTable.Columns.Add("إجمالي المبيعات", typeof(decimal));
            dataTable.Columns.Add("إجمالي التكلفة", typeof(decimal));
            dataTable.Columns.Add("صافي الربح", typeof(decimal));
            dataTable.Columns.Add("نسبة الربح", typeof(decimal));

            foreach (var item in ReportData)
                dataTable.Rows.Add(item.ProductName, item.Category,
                    item.TotalSoldQty, item.TotalSalesAmount,
                    item.TotalCOGS, item.GrossProfit, item.ProfitMargin);

            await PdfExportService.ExportToPdfAsync("أرباح الأصناف", dataTable, TotalProfit,
                $"ProductProfitability_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
        catch (Exception ex)
        {
            LogSystemError("فشل في تصدير تقرير أرباح الأصناف إلى PDF", "ProductProfitabilityViewModel.ExportPdf", ex);
            await D.ShowErrorAsync("خطأ في تصدير الملف", "حدث خطأ غير متوقع أثناء تصدير الملف. يرجى المحاولة مرة أخرى.");
        }
    }

    #endregion
}
