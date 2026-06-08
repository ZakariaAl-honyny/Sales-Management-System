using SalesSystem.DesktopPWF.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using ClosedXML.Excel;
using Microsoft.Win32;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.DesktopPWF.Services.App;
using Serilog;
using SalesSystem.DesktopPWF.Models;

namespace SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Report types enumeration for Arabic labels
/// </summary>
public enum ReportType
{
    Sales,
    Purchases,
    Inventory,
    Customers,
    Suppliers,
    ProfitLoss,
    LowStock
}

/// <summary>
/// Report type item for ComboBox binding
/// </summary>
public class ReportTypeItem
{
    public ReportType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Reports ViewModel - handles report generation and export
/// </summary>
public class ReportsViewModel : ViewModelBase
{
    private IReportApiService? _reportApiService;
    private IWarehouseApiService? _warehouseService;
    private IReportApiService ReportApiService => _reportApiService ??= App.GetService<IReportApiService>();
    private IWarehouseApiService WarehouseService => _warehouseService ??= App.GetService<IWarehouseApiService>();

    // Uses 'new' to suppress CS0108 (inherited member hiding).
    // Test uses SetField("_dialogService", mock) before property is accessed.
    private new IDialogService DialogService => _dialogService ??= App.GetService<IDialogService>();
    private IDialogService? _dialogService;

    private ReportType _selectedReportType;
    private DateTime _dateFrom;
    private DateTime _dateTo;
    private decimal _reportTotal;
    private bool _isEmpty = true;
    private bool _hasSearched;
    private int? _selectedWarehouseId;
    private ObservableCollection<WarehouseDto> _warehouses = new();

    public ReportsViewModel()
    {
        ReportTypes = new ObservableCollection<ReportTypeItem>
        {
            new() { Type = ReportType.Sales, DisplayName = "تقرير المبيعات" },
            new() { Type = ReportType.Purchases, DisplayName = "تقرير المشتريات" },
            new() { Type = ReportType.Inventory, DisplayName = "تقرير المخزون" },
            new() { Type = ReportType.Customers, DisplayName = "تقرير العملاء" },
            new() { Type = ReportType.Suppliers, DisplayName = "تقرير الموردين" },
            new() { Type = ReportType.ProfitLoss, DisplayName = "تقرير الأرباح والخسائر" },
            new() { Type = ReportType.LowStock, DisplayName = "تقرير النواقص (المخزون المنخفض)" }
        };

        _selectedReportType = ReportType.Sales;
        _dateTo = DateTime.Today;
        _dateFrom = DateTime.Today.AddDays(-30);

        ReportData = new DataTable();
        ReportColumns = new ObservableCollection<DataColumn>();

        GenerateReportCommand = new AsyncRelayCommand((Func<Task>)(async () => await ExecuteAsync(GenerateReportOperationAsync, ex => ShowReportError(ex))), () => CanGenerateReport());
        ExportExcelCommand = new AsyncRelayCommand(async () => await ExportToExcelAsync(), () => ReportData != null && ReportData.Rows.Count > 0);
        ExportCsvCommand = new AsyncRelayCommand(async () => await ExportToCsvAsync(), () => ReportData != null && ReportData.Rows.Count > 0);
        ClearReportCommand = new RelayCommand(_ => ClearReport());

        _ = LoadWarehousesAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var result = await WarehouseService.GetAllAsync();
            if (result.IsSuccess && result.Value != null)
            {
                InvokeOnUIThread(() =>
                {
                    Warehouses.Clear();
                    Warehouses.Add(new WarehouseDto(0, "كل المخازن", 1, string.Empty, null, null, null, true, true, null, null));
                    foreach (var wh in result.Value)
                        Warehouses.Add(wh);
                    
                    var defaultOrFirst = result.Value.FirstOrDefault(w => w.IsDefault) ?? result.Value.FirstOrDefault();
                    if (defaultOrFirst != null)
                    {
                        SelectedWarehouseId = defaultOrFirst.Id;
                    }
                    else
                    {
                        SelectedWarehouseId = null;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            HandleException(ex, "ReportsViewModel.LoadWarehousesAsync", "[ReportsViewModel.LoadWarehousesAsync] Error loading warehouses in ReportsViewModel.");
        }
    }

    #region Properties
    public ObservableCollection<ReportTypeItem> ReportTypes { get; }
    public ObservableCollection<WarehouseDto> Warehouses
    {
        get => _warehouses;
        set => SetProperty(ref _warehouses, value);
    }

    public int? SelectedWarehouseId
    {
        get => _selectedWarehouseId;
        set => SetProperty(ref _selectedWarehouseId, value == 0 ? null : value);
    }

    public bool WarehouseFilterVisible => SelectedReportType is
        ReportType.Sales or ReportType.Purchases or
        ReportType.Inventory or ReportType.LowStock;

    public ReportType SelectedReportType
    {
        get => _selectedReportType;
        set
        {
            if (SetProperty(ref _selectedReportType, value))
            {
                OnPropertyChanged(nameof(ReportTitle));
                OnPropertyChanged(nameof(WarehouseFilterVisible));
            }
        }
    }

    public DateTime DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    public DateTime DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private DataTable _reportData = new();
    public DataTable ReportData
    {
        get => _reportData;
        set => SetProperty(ref _reportData, value);
    }

    public ObservableCollection<DataColumn> ReportColumns { get; }

    public string ReportTitle
    {
        get
        {
            return SelectedReportType switch
            {
                ReportType.Sales => "تقرير المبيعات",
                ReportType.Purchases => "تقرير المشتريات",
                ReportType.Inventory => "تقرير المخزون",
                ReportType.Customers => "تقرير أرصدة العملاء",
                ReportType.Suppliers => "تقرير أرصدة الموردين",
                ReportType.ProfitLoss => "تقرير الأرباح والخسائر",
                ReportType.LowStock => "تقرير النواقص والمخزون المنخفض",
                _ => "تقرير"
            };
        }
    }

    public decimal ReportTotal
    {
        get => _reportTotal;
        set
        {
            if (SetProperty(ref _reportTotal, value))
            {
                OnPropertyChanged(nameof(FormattedReportTotal));
            }
        }
    }

    public string FormattedReportTotal => ReportTotal.ToString("N2");
    public string FormattedDateFrom => DateFrom.ToString("yyyy/MM/dd");
    public string FormattedDateTo => DateTo.ToString("yyyy/MM/dd");

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public bool HasSearched
    {
        get => _hasSearched;
        private set => SetProperty(ref _hasSearched, value);
    }

    private bool CanGenerateReport()
    {
        return !IsBusy && DateFrom <= DateTo;
    }
    #endregion

    #region Commands
    public AsyncRelayCommand GenerateReportCommand { get; }
    public AsyncRelayCommand ExportExcelCommand { get; }
    public AsyncRelayCommand ExportCsvCommand { get; }
    public RelayCommand ClearReportCommand { get; }
    #endregion

    #region Report Generation
    private void ShowReportError(Exception ex)
    {
        var userMsg = HandleException(ex, "ReportsViewModel.GenerateReportAsync", $"[ReportsViewModel.GenerateReportAsync] Failed to generate report of type {SelectedReportType}.");
        _ = DialogService.ShowErrorAsync("خطأ في التقرير", userMsg);
    }

    private async Task GenerateReportOperationAsync()
    {
        ErrorMessage = null;
        HasSearched = false;

        Log.Information("Generating report: {ReportType} from {DateFrom} to {DateTo}", SelectedReportType, DateFrom, DateTo);

        DataTable table = new DataTable();
        
        InvokeOnUIThread(() => ReportColumns.Clear());

        switch (SelectedReportType)
        {
            case ReportType.Sales: await GenerateSalesReportAsync(table); break;
            case ReportType.Purchases: await GeneratePurchasesReportAsync(table); break;
            case ReportType.Inventory: await GenerateInventoryReportAsync(table); break;
            case ReportType.Customers: await GenerateCustomersReportAsync(table); break;
            case ReportType.Suppliers: await GenerateSuppliersReportAsync(table); break;
            case ReportType.ProfitLoss: await GenerateProfitLossReportAsync(table); break;
            case ReportType.LowStock: await GenerateLowStockReportAsync(table); break;
        }

        InvokeOnUIThread(() =>
        {
            ReportData = table;
            HasSearched = true;
            IsEmpty = table.Rows.Count == 0;
            
            GenerateReportCommand.RaiseCanExecuteChanged();
            ExportExcelCommand.RaiseCanExecuteChanged();
            ExportCsvCommand.RaiseCanExecuteChanged();
        });

        Log.Information("Report generated successfully. Total rows: {RowCount}", table.Rows.Count);
    }

    private void ClearReport()
    {
        ReportData = new DataTable();
        ReportColumns.Clear();
        ReportTotal = 0;
        IsEmpty = true;
        HasSearched = false;
        ExportExcelCommand.RaiseCanExecuteChanged();
        ExportCsvCommand.RaiseCanExecuteChanged();
    }
    #endregion

    #region Sales Report
    private async Task GenerateSalesReportAsync(DataTable table)
    {
        AddColumn(table, "التاريخ");
        AddColumn(table, "رقم الفاتورة");
        AddColumn(table, "العميل");
        AddColumn(table, "المجموع الفرعي");
        AddColumn(table, "الخصم");
        AddColumn(table, "الضريبة");
        AddColumn(table, "الإجمالي");
        AddColumn(table, "المدفوع");
        AddColumn(table, "المتبقي");

        var result = await ReportApiService.GetSalesReportAsync(SelectedWarehouseId, DateFrom, DateTo);

        if (result.IsSuccess && result.Value != null)
        {
            decimal total = 0;
            foreach (var reportDto in result.Value.OrderByDescending(i => i.InvoiceDate))
            {
                var row = table.NewRow();
                row["التاريخ"] = reportDto.InvoiceDate.ToString("yyyy/MM/dd");
                row["رقم الفاتورة"] = reportDto.Id.ToString();
                row["العميل"] = reportDto.CustomerName;
                row["المجموع الفرعي"] = reportDto.SubTotal;
                row["الخصم"] = reportDto.DiscountAmount;
                row["الضريبة"] = reportDto.TaxAmount;
                row["الإجمالي"] = reportDto.TotalAmount;
                row["المدفوع"] = reportDto.PaidAmount;
                row["المتبقي"] = reportDto.DueAmount;
                table.Rows.Add(row);
                total += reportDto.TotalAmount;
            }
            ReportTotal = total;
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المبيعات", "ReportsViewModel.GenerateSalesReportAsync", "[ReportsViewModel.GenerateSalesReportAsync] API returned failure.");
        }
    }
    #endregion

    #region Purchases Report
    private async Task GeneratePurchasesReportAsync(DataTable table)
    {
        AddColumn(table, "التاريخ");
        AddColumn(table, "رقم الفاتورة");
        AddColumn(table, "المورد");
        AddColumn(table, "المجموع الفرعي");
        AddColumn(table, "الخصم");
        AddColumn(table, "الضريبة");
        AddColumn(table, "الإجمالي");
        AddColumn(table, "المدفوع");
        AddColumn(table, "المتبقي");

        var result = await ReportApiService.GetPurchasesReportAsync(SelectedWarehouseId, DateFrom, DateTo);

        if (result.IsSuccess && result.Value != null)
        {
            decimal total = 0;
            foreach (var reportDto in result.Value.OrderByDescending(i => i.InvoiceDate))
            {
                var row = table.NewRow();
                row["التاريخ"] = reportDto.InvoiceDate.ToString("yyyy/MM/dd");
                row["رقم الفاتورة"] = reportDto.Id.ToString();
                row["المورد"] = reportDto.SupplierName;
                row["المجموع الفرعي"] = reportDto.SubTotal;
                row["الخصم"] = reportDto.DiscountAmount;
                row["الضريبة"] = reportDto.TaxAmount;
                row["الإجمالي"] = reportDto.TotalAmount;
                row["المدفوع"] = reportDto.PaidAmount;
                row["المتبقي"] = reportDto.DueAmount;
                table.Rows.Add(row);
                total += reportDto.TotalAmount;
            }
            ReportTotal = total;
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المشتريات", "ReportsViewModel.GeneratePurchasesReportAsync", "[ReportsViewModel.GeneratePurchasesReportAsync] API returned failure.");
        }
    }
    #endregion

    #region Inventory Report
    private async Task GenerateInventoryReportAsync(DataTable table)
    {
        AddColumn(table, "رمز المنتج");
        AddColumn(table, "اسم المنتج");
        AddColumn(table, "الفئة");
        AddColumn(table, "الوحدة");
        AddColumn(table, "المخزن");
        AddColumn(table, "المخزون الحالي");
        AddColumn(table, "سعر الشراء");
        AddColumn(table, "القيمة الإجمالية");

        var result = await ReportApiService.GetStockReportAsync(SelectedWarehouseId);

        if (result.IsSuccess && result.Value != null)
        {
            decimal total = 0;
            foreach (var stock in result.Value)
            {
                var row = table.NewRow();
                row["اسم المنتج"] = stock.ProductName;
                row["الفئة"] = stock.CategoryName;
                row["الوحدة"] = stock.UnitName;
                row["المخزن"] = stock.WarehouseName;
                row["المخزون الحالي"] = stock.CurrentStock;
                row["سعر الشراء"] = stock.PurchasePrice;
                row["القيمة الإجمالية"] = stock.TotalValue;
                table.Rows.Add(row);
                total += stock.TotalValue;
            }
            ReportTotal = total;
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير المخزون", "ReportsViewModel.GenerateInventoryReportAsync", "[ReportsViewModel.GenerateInventoryReportAsync] API returned failure.");
        }
    }
    #endregion

    #region Customers Report
    private async Task GenerateCustomersReportAsync(DataTable table)
    {
        AddColumn(table, "رمز العميل");
        AddColumn(table, "اسم العميل");
        AddColumn(table, "الرصيد الافتتاحي");
        AddColumn(table, "إجمالي المبيعات");
        AddColumn(table, "إجمالي المرتجعات");
        AddColumn(table, "إجمالي المدفوعات");
        AddColumn(table, "الرصيد الحالي");

        var result = await ReportApiService.GetCustomerBalancesReportAsync();

        if (result.IsSuccess && result.Value != null)
        {
            foreach (var cb in result.Value)
            {
                var row = table.NewRow();
                row["اسم العميل"] = cb.CustomerName;
                row["الرصيد الافتتاحي"] = cb.OpeningBalance;
                row["إجمالي المبيعات"] = cb.TotalSales;
                row["إجمالي المرتجعات"] = cb.TotalReturns;
                row["إجمالي المدفوعات"] = cb.TotalPayments;
                row["الرصيد الحالي"] = cb.CurrentBalance;
                table.Rows.Add(row);
            }
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير أرصدة العملاء", "ReportsViewModel.GenerateCustomersReportAsync", "[ReportsViewModel.GenerateCustomersReportAsync] API returned failure.");
        }
    }
    #endregion

    #region Suppliers Report
    private async Task GenerateSuppliersReportAsync(DataTable table)
    {
        AddColumn(table, "رمز المورد");
        AddColumn(table, "اسم المورد");
        AddColumn(table, "الرصيد الافتتاحي");
        AddColumn(table, "إجمالي المشتريات");
        AddColumn(table, "إجمالي المرتجعات");
        AddColumn(table, "إجمالي المدفوعات");
        AddColumn(table, "الرصيد الحالي");

        var result = await ReportApiService.GetSupplierBalancesReportAsync();

        if (result.IsSuccess && result.Value != null)
        {
            foreach (var sb in result.Value)
            {
                var row = table.NewRow();
                row["اسم المورد"] = sb.SupplierName;
                row["الرصيد الافتتاحي"] = sb.OpeningBalance;
                row["إجمالي المشتريات"] = sb.TotalPurchases;
                row["إجمالي المرتجعات"] = sb.TotalReturns;
                row["إجمالي المدفوعات"] = sb.TotalPayments;
                row["الرصيد الحالي"] = sb.CurrentBalance;
                table.Rows.Add(row);
            }
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير أرصدة الموردين", "ReportsViewModel.GenerateSuppliersReportAsync", "[ReportsViewModel.GenerateSuppliersReportAsync] API returned failure.");
        }
    }
    #endregion

    #region Profit/Loss Report
    private async Task GenerateProfitLossReportAsync(DataTable table)
    {
        AddColumn(table, "الفئة");
        AddColumn(table, "البيان");
        AddColumn(table, "المبلغ");

        var salesResult = await ReportApiService.GetSalesReportAsync(SelectedWarehouseId, DateFrom, DateTo);
        var purchasesResult = await ReportApiService.GetPurchasesReportAsync(SelectedWarehouseId, DateFrom, DateTo);

        if (!salesResult.IsSuccess)
        {
            ErrorMessage = HandleFailure(salesResult.Error ?? "فشل في تحميل بيانات المبيعات", "ReportsViewModel.GenerateProfitLossReportAsync", "[ReportsViewModel.GenerateProfitLossReportAsync] API returned failure for sales.");
            return;
        }

        if (!purchasesResult.IsSuccess)
        {
            ErrorMessage = HandleFailure(purchasesResult.Error ?? "فشل في تحميل بيانات المشتريات", "ReportsViewModel.GenerateProfitLossReportAsync", "[ReportsViewModel.GenerateProfitLossReportAsync] API returned failure for purchases.");
            return;
        }

        decimal totalSales = salesResult.Value?.Sum(x => x.TotalAmount) ?? 0;
        decimal totalPurchases = purchasesResult.Value?.Sum(x => x.TotalAmount) ?? 0;
        decimal netProfit = totalSales - totalPurchases;

        var salesRow = table.NewRow();
        salesRow["الفئة"] = "الإيرادات";
        salesRow["البيان"] = "إجمالي المبيعات";
        salesRow["المبلغ"] = totalSales;
        table.Rows.Add(salesRow);

        var purchasesRow = table.NewRow();
        purchasesRow["الفئة"] = "المصروفات";
        purchasesRow["البيان"] = "إجمالي المشتريات";
        purchasesRow["المبلغ"] = -totalPurchases;
        table.Rows.Add(purchasesRow);

        var profitRow = table.NewRow();
        profitRow["الفئة"] = "صافي الربح/الخسارة";
        profitRow["البيان"] = netProfit >= 0 ? "صافي الربح" : "صافي الخسارة";
        profitRow["المبلغ"] = netProfit;
        table.Rows.Add(profitRow);

        ReportTotal = netProfit;
        ErrorMessage = null;
    }

    private async Task GenerateLowStockReportAsync(DataTable table)
    {
        AddColumn(table, "رمز المنتج");
        AddColumn(table, "اسم المنتج");
        AddColumn(table, "المخزن");
        AddColumn(table, "المخزون الحالي");
        AddColumn(table, "حد الطلب");
        AddColumn(table, "النقص (تجزئة)");
        AddColumn(table, "النقص (كرتون)");
        AddColumn(table, "باقي التجزئة");

        var result = await ReportApiService.GetLowStockReportAsync(SelectedWarehouseId);

        if (result.IsSuccess && result.Value != null)
        {
            foreach (var low in result.Value)
            {
                var row = table.NewRow();
                row["اسم المنتج"] = low.ProductName;
                row["المخزن"] = low.WarehouseName;
                row["المخزون الحالي"] = low.CurrentRetailQty;
                row["حد الطلب"] = low.ReorderLevelRetailQty;
                row["النقص (تجزئة)"] = low.DeficitRetailQty;
                row["النقص (كرتون)"] = $"{low.SuggestedWholesaleBoxes} {low.WholesaleUnitName}";
                row["باقي التجزئة"] = $"{low.SuggestedRetailRemainder} {low.RetailUnitName}";
                table.Rows.Add(row);
            }
            ReportTotal = result.Value.Count();
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تحميل تقرير النواقص", "ReportsViewModel.GenerateLowStockReportAsync", "[ReportsViewModel.GenerateLowStockReportAsync] API returned failure.");
        }
    }
    #endregion

    #region Helper Methods
    private void AddColumn(DataTable table, string columnName)
    {
        table.Columns.Add(columnName);
        InvokeOnUIThread(() => ReportColumns.Add(table.Columns[columnName]!));
    }
    #endregion

    #region Export Methods
    private async Task ExportToExcelAsync()
    {
        if (ReportData.Rows.Count == 0) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"{SelectedReportType}_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.xlsx",
            Title = "تصدير إلى Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add(ReportTitle);
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                    headerRow.Style.Font.FontColor = XLColor.White;
                    headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    for (int col = 0; col < ReportData.Columns.Count; col++)
                    {
                        worksheet.Cell(1, col + 1).Value = ReportData.Columns[col].ColumnName;
                    }

                    for (int row = 0; row < ReportData.Rows.Count; row++)
                    {
                        for (int col = 0; col < ReportData.Columns.Count; col++)
                        {
                            var cell = worksheet.Cell(row + 2, col + 1);
                            var value = ReportData.Rows[row][col];

                            if (value is decimal decimalValue)
                            {
                                cell.Value = decimalValue;
                                cell.Style.NumberFormat.Format = "#,##0.00";
                            }
                            else if (value is DateTime dateValue)
                            {
                                cell.Value = dateValue;
                                cell.Style.DateFormat.Format = "yyyy/MM/dd";
                            }
                            else
                            {
                                cell.Value = value?.ToString() ?? "";
                            }
                        }
                    }

                    worksheet.Columns().AdjustToContents();
                    int lastRow = ReportData.Rows.Count + 3;
                    worksheet.Cell(lastRow, 1).Value = "الإجمالي";
                    worksheet.Cell(lastRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(lastRow, ReportData.Columns.Count).Value = ReportTotal;
                    worksheet.Cell(lastRow, ReportData.Columns.Count).Style.NumberFormat.Format = "#,##0.00";
                    workbook.SaveAs(dialog.FileName);
                });

                Log.Information("Report exported to Excel: {FileName}", dialog.FileName);
                await DialogService.ShowSuccessAsync("نجاح", "تم تصدير التقرير إلى Excel بنجاح");
            }
            catch (Exception ex)
            {
                HandleException(ex, "ReportsViewModel.ExportToExcelAsync", "[ReportsViewModel.ExportToExcelAsync] Error exporting to Excel.");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task ExportToCsvAsync()
    {
        if (ReportData.Rows.Count == 0) return;

        var dialog = new SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"{SelectedReportType}_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.csv",
            Title = "تصدير إلى CSV"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                await Task.Run(() =>
                {
                    using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
                    writer.Write('\uFEFF');
                    var headers = ReportData.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                    writer.WriteLine(string.Join(",", headers));

                    foreach (DataRow row in ReportData.Rows)
                    {
                        var values = row.ItemArray.Select(v =>
                        {
                            var str = v?.ToString() ?? "";
                            if (str.Contains(',') || str.Contains('"'))
                            {
                                str = "\"" + str.Replace("\"", "\"\"") + "\"";
                            }
                            return str;
                        });
                        writer.WriteLine(string.Join(",", values));
                    }

                    writer.WriteLine();
                    writer.WriteLine($"\"الإجمالي\",{ReportTotal:N2}");
                });

                Log.Information("Report exported to CSV: {FileName}", dialog.FileName);
                await DialogService.ShowSuccessAsync("نجاح", "تم تصدير التقرير إلى CSV بنجاح");
            }
            catch (Exception ex)
            {
                HandleException(ex, "ReportsViewModel.ExportToCsvAsync", "[ReportsViewModel.ExportToCsvAsync] Error exporting to CSV.");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    #endregion
}
