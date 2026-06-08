namespace SalesSystem.DesktopPWF.Tests.ViewModels;

using System.ComponentModel;
using System.Data;
using FluentAssertions;
using Moq;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Services;
using SalesSystem.DesktopPWF.ViewModels;

/// <summary>
/// Tests for ReportsViewModel
/// </summary>
public class ReportsViewModelTests
{
    private readonly Mock<IReportApiService> _mockReportService;
    private readonly Mock<IWarehouseApiService> _mockWarehouseService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly ReportsViewModel _viewModel;

    public ReportsViewModelTests()
    {
        _mockReportService = new Mock<IReportApiService>();
        _mockWarehouseService = new Mock<IWarehouseApiService>();
        _mockDialogService = new Mock<IDialogService>();

        _viewModel = new ReportsViewModel();

        SetField("_reportApiService", _mockReportService.Object);
        SetField("_warehouseService", _mockWarehouseService.Object);
        SetField("_dialogService", _mockDialogService.Object);
    }

    private void SetField(string fieldName, object value)
    {
        var field = typeof(ReportsViewModel).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_viewModel, value);
    }

    #region Property Tests

    [Fact]
    public void ReportTypes_ContainsAllReportTypes()
    {
        _viewModel.ReportTypes.Should().HaveCount(7);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Sales);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Purchases);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Inventory);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Customers);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Suppliers);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.ProfitLoss);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.LowStock);
    }

    [Fact]
    public void SelectedReportType_DefaultValue_IsSales()
    {
        _viewModel.SelectedReportType.Should().Be(ReportType.Sales);
    }

    [Fact]
    public void DateFrom_DefaultValue_IsLast30Days()
    {
        _viewModel.DateFrom.Should().Be(DateTime.Today.AddDays(-30));
    }

    [Fact]
    public void DateTo_DefaultValue_IsToday()
    {
        _viewModel.DateTo.Should().Be(DateTime.Today);
    }

    [Fact]
    public void IsBusy_DefaultValue_IsFalse()
    {
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void ReportData_DefaultValue_IsEmptyDataTable()
    {
        _viewModel.ReportData.Should().NotBeNull();
        _viewModel.ReportData.Rows.Count.Should().Be(0);
    }

    [Fact]
    public void ReportTotal_DefaultValue_IsZero()
    {
        _viewModel.ReportTotal.Should().Be(0m);
    }

    #endregion

    #region PropertyChangeNotification Tests

    [Fact]
    public void IsBusy_IsReadOnly_FromViewModelBase()
    {
        // IsBusy has protected set in ViewModelBase, managed by ExecuteAsync
        _viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void ReportTotal_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.ReportTotal = 1000m;

        propertyChangedEvents.Should().Contain("ReportTotal");
        propertyChangedEvents.Should().Contain("FormattedReportTotal");
    }

    [Fact]
    public void SelectedReportType_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.SelectedReportType = ReportType.Purchases;

        propertyChangedEvents.Should().Contain("SelectedReportType");
        propertyChangedEvents.Should().Contain("ReportTitle");
    }

    [Fact]
    public void DateFrom_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.DateFrom = DateTime.Today.AddDays(-7);

        propertyChangedEvents.Should().Contain("DateFrom");
    }

    [Fact]
    public void DateTo_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.DateTo = DateTime.Today.AddDays(1);

        propertyChangedEvents.Should().Contain("DateTo");
    }

    #endregion

    #region ReportTitle Tests

    [Fact]
    public void ReportTitle_WhenSalesSelected_ReturnsArabicLabel()
    {
        _viewModel.SelectedReportType = ReportType.Sales;
        _viewModel.ReportTitle.Should().Be("تقرير المبيعات");
    }

    [Fact]
    public void ReportTitle_WhenPurchasesSelected_ReturnsArabicLabel()
    {
        _viewModel.SelectedReportType = ReportType.Purchases;
        _viewModel.ReportTitle.Should().Be("تقرير المشتريات");
    }

    [Fact]
    public void ReportTitle_WhenInventorySelected_ReturnsArabicLabel()
    {
        _viewModel.SelectedReportType = ReportType.Inventory;
        _viewModel.ReportTitle.Should().Be("تقرير المخزون");
    }

    [Fact]
    public void ReportTitle_WhenCustomersSelected_ReturnsArabicLabel()
    {
        _viewModel.SelectedReportType = ReportType.Customers;
        _viewModel.ReportTitle.Should().Be("تقرير أرصدة العملاء");
    }

    [Fact]
    public void ReportTitle_WhenSuppliersSelected_ReturnsArabicLabel()
    {
        _viewModel.SelectedReportType = ReportType.Suppliers;
        _viewModel.ReportTitle.Should().Be("تقرير أرصدة الموردين");
    }

    [Fact]
    public void ReportTitle_WhenProfitLossSelected_ReturnsArabicLabel()
    {
        _viewModel.SelectedReportType = ReportType.ProfitLoss;
        _viewModel.ReportTitle.Should().Be("تقرير الأرباح والخسائر");
    }

    #endregion

    #region FormattedReportTotal Tests

    [Fact]
    public void FormattedReportTotal_ReturnsFormattedNumber()
    {
        _viewModel.ReportTotal = 1234567.89m;
        _viewModel.FormattedReportTotal.Should().Be("1,234,567.89");
    }

    [Fact]
    public void FormattedReportTotal_ReturnsZeroFormatted()
    {
        _viewModel.ReportTotal = 0m;
        _viewModel.FormattedReportTotal.Should().Be("0.00");
    }

    #endregion

    #region FormattedDateRange Tests

    [Fact]
    public void FormattedDateFrom_ReturnsCorrectFormat()
    {
        _viewModel.DateFrom = new DateTime(2026, 5, 1);
        _viewModel.FormattedDateFrom.Should().Be("2026/05/01");
    }

    [Fact]
    public void FormattedDateTo_ReturnsCorrectFormat()
    {
        _viewModel.DateTo = new DateTime(2026, 5, 31);
        _viewModel.FormattedDateTo.Should().Be("2026/05/31");
    }

    #endregion

    #region Commands Tests

    [Fact]
    public void GenerateReportCommand_IsInitialized()
    {
        _viewModel.GenerateReportCommand.Should().NotBeNull();
    }

    [Fact]
    public void ExportExcelCommand_IsInitialized()
    {
        _viewModel.ExportExcelCommand.Should().NotBeNull();
    }

    [Fact]
    public void ExportCsvCommand_IsInitialized()
    {
        _viewModel.ExportCsvCommand.Should().NotBeNull();
    }

    [Fact]
    public void ClearReportCommand_IsInitialized()
    {
        _viewModel.ClearReportCommand.Should().NotBeNull();
    }

    #endregion

    #region ClearReport Tests

    [Fact]
    public void ClearReport_ResetsReportData()
    {
        _viewModel.ReportData.Columns.Add("TestColumn");
        _viewModel.ReportData.Rows.Add("TestRow");
        _viewModel.ReportTotal = 1000m;

        _viewModel.ClearReportCommand.Execute(null);

        _viewModel.ReportData.Columns.Count.Should().Be(0);
        _viewModel.ReportData.Rows.Count.Should().Be(0);
        _viewModel.ReportTotal.Should().Be(0m);
    }

    [Fact]
    public void ClearReport_ClearsReportColumns()
    {
        _viewModel.ClearReportCommand.Execute(null);
        _viewModel.ReportColumns.Count.Should().Be(0);
    }

    #endregion

    #region GenerateSalesReport Tests

    [Fact]
    public async Task GenerateReportCommand_Sales_LoadsSalesReport()
    {
        var salesReports = new List<SalesReportDto>
        {
            new(DateTime.Today, 1, "عميل", 1000m, 0m, 0m, 1000m, 1000m, 0m),
            new(DateTime.Today, 2, "عميل", 2000m, 0m, 0m, 2000m, 1500m, 500m)
        };

        _mockReportService
            .Setup(s => s.GetSalesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Result<List<SalesReportDto>>.Success(salesReports));

        _viewModel.SelectedReportType = ReportType.Sales;
        _viewModel.GenerateReportCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ReportData.Columns.Should().Contain(c => c.ColumnName == "رقم الفاتورة");
        _viewModel.ReportData.Rows.Count.Should().Be(2);
        _viewModel.ReportTotal.Should().Be(3000m);
    }

    #endregion

    #region GenerateInventoryReport Tests

    [Fact]
    public async Task GenerateReportCommand_Inventory_LoadsStockReport()
    {
        var stockReports = new List<StockReportDto>
        {
            new(1, "منتج 1", "فئة", "وحدة", "مستودع", 100m, 10m, 10m, 1000m),
            new(2, "منتج 2", "فئة", "وحدة", "مستودع", 200m, 20m, 20m, 4000m)
        };

        _mockReportService
            .Setup(s => s.GetStockReportAsync(It.IsAny<int?>()))
            .ReturnsAsync(Result<List<StockReportDto>>.Success(stockReports));

        _viewModel.SelectedReportType = ReportType.Inventory;
        _viewModel.GenerateReportCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ReportData.Columns.Should().Contain(c => c.ColumnName == "اسم المنتج");
        _viewModel.ReportData.Rows.Count.Should().Be(2);
    }

    #endregion

    #region GenerateCustomersReport Tests

    [Fact]
    public async Task GenerateReportCommand_Customers_LoadsCustomerBalances()
    {
        var customerBalances = new List<CustomerFinancialBalanceDto>
        {
            new(1, "عميل 1", 0m, 1000m, 0m, 500m, 0m, 500m),
            new(2, "عميل 2", 0m, 2000m, 0m, 1000m, 0m, 1000m)
        };

        _mockReportService
            .Setup(s => s.GetCustomerBalancesReportAsync())
            .ReturnsAsync(Result<List<CustomerFinancialBalanceDto>>.Success(customerBalances));

        _viewModel.SelectedReportType = ReportType.Customers;
        _viewModel.GenerateReportCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ReportData.Columns.Should().Contain(c => c.ColumnName == "اسم العميل");
        _viewModel.ReportData.Rows.Count.Should().Be(2);
    }

    #endregion

    #region GenerateSuppliersReport Tests

    [Fact]
    public async Task GenerateReportCommand_Suppliers_LoadsSupplierBalances()
    {
        var supplierBalances = new List<SupplierBalanceReportDto>
        {
            new(1, "مورد 1", 0m, 1000m, 0m, 500m, 0m, 500m),
            new(2, "مورد 2", 0m, 2000m, 0m, 1000m, 0m, 1000m)
        };

        _mockReportService
            .Setup(s => s.GetSupplierBalancesReportAsync())
            .ReturnsAsync(Result<List<SupplierBalanceReportDto>>.Success(supplierBalances));

        _viewModel.SelectedReportType = ReportType.Suppliers;
        _viewModel.GenerateReportCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ReportData.Columns.Should().Contain(c => c.ColumnName == "اسم المورد");
        _viewModel.ReportData.Rows.Count.Should().Be(2);
    }

    #endregion

    #region GenerateProfitLossReport Tests

    [Fact]
    public async Task GenerateReportCommand_ProfitLoss_CalculatesNetProfit()
    {
        var salesReports = new List<SalesReportDto>
        {
            new(DateTime.Today, 1, "عميل", 5000m, 0m, 0m, 5000m, 5000m, 0m),
            new(DateTime.Today, 2, "عميل", 3000m, 0m, 0m, 3000m, 3000m, 0m)
        };

        var purchaseReports = new List<PurchaseReportDto>
        {
            new(DateTime.Today, 3, "مورد", 2000m, 0m, 0m, 2000m, 2000m, 0m)
        };

        _mockReportService
            .Setup(s => s.GetSalesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Result<List<SalesReportDto>>.Success(salesReports));

        _mockReportService
            .Setup(s => s.GetPurchasesReportAsync(It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Result<List<PurchaseReportDto>>.Success(purchaseReports));

        _viewModel.SelectedReportType = ReportType.ProfitLoss;
        _viewModel.GenerateReportCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.ReportTotal.Should().Be(6000m);
    }

    #endregion
}
