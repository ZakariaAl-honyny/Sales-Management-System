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
    private readonly Mock<ISalesInvoiceApiService> _mockSalesService;
    private readonly Mock<IProductApiService> _mockProductService;
    private readonly Mock<ICustomerApiService> _mockCustomerService;
    private readonly Mock<ISupplierApiService> _mockSupplierService;
    private readonly ReportsViewModel _viewModel;

    public ReportsViewModelTests()
    {
        _mockSalesService = new Mock<ISalesInvoiceApiService>();
        _mockProductService = new Mock<IProductApiService>();
        _mockCustomerService = new Mock<ICustomerApiService>();
        _mockSupplierService = new Mock<ISupplierApiService>();

        _viewModel = new ReportsViewModel();
        
        // Inject mocks via reflection since default constructor uses App.GetService
        SetField("_salesInvoiceService", _mockSalesService.Object);
        SetField("_productService", _mockProductService.Object);
        SetField("_customerService", _mockCustomerService.Object);
        SetField("_supplierService", _mockSupplierService.Object);
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
        _viewModel.ReportTypes.Should().HaveCount(6);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Sales);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Purchases);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Inventory);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Customers);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.Suppliers);
        _viewModel.ReportTypes.Should().Contain(r => r.Type == ReportType.ProfitLoss);
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
    public void IsLoading_DefaultValue_IsFalse()
    {
        _viewModel.IsLoading.Should().BeFalse();
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
    public void IsLoading_Set_NotifiesPropertyChanged()
    {
        var propertyChangedEvents = new List<string>();
        _viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName ?? string.Empty);

        _viewModel.IsLoading = true;

        propertyChangedEvents.Should().Contain("IsLoading");
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

        _viewModel.DateTo = DateTime.Today;

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
        // Add some data first
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
        var columns = new System.Collections.ObjectModel.ObservableCollection<DataColumn>();
        columns.Add(new DataColumn("Test"));

        SetField("_reportColumns", columns);

        _viewModel.ClearReportCommand.Execute(null);

        columns.Count.Should().Be(0);
    }

    #endregion

    #region GenerateSalesReport Tests

    [Fact]
    public async Task GenerateReportCommand_Sales_LoadsPostedInvoices()
    {
        var invoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(1, "INV-001", 1000m, 1000m, 0m, 0m),
            CreateSalesInvoiceDto(2, "INV-002", 2000m, 1500m, 500m, 0m)
        };

        _mockSalesService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        _viewModel.SelectedReportType = ReportType.Sales;
        
        // Execute the GenerateReportCommand
        await _viewModel.GenerateReportCommand.ExecuteAsync(null);
        await Task.Delay(100);

        _viewModel.ReportData.Columns.Should().Contain(c => c.ColumnName == "رقم الفاتورة");
        _viewModel.ReportData.Rows.Count.Should().Be(2);
        _viewModel.ReportTotal.Should().Be(3000m);
    }

    [Fact]
    public async Task GenerateReportCommand_Sales_CalculatesCorrectTotal()
    {
        var invoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(1, "INV-001", 500m, 500m, 0m, 0m),
            CreateSalesInvoiceDto(2, "INV-002", 300m, 300m, 0m, 0m),
            CreateSalesInvoiceDto(3, "INV-003", 200m, 200m, 0m, 0m)
        };

        _mockSalesService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        _viewModel.SelectedReportType = ReportType.Sales;
        await _viewModel.GenerateReportCommand.ExecuteAsync(null);
        await Task.Delay(100);

        _viewModel.ReportTotal.Should().Be(1000m);
    }

    #endregion

    #region GenerateInventoryReport Tests

    [Fact]
    public async Task GenerateReportCommand_Inventory_LoadsProducts()
    {
        var products = new List<ProductDto>
        {
            new(1, "P001", null, "منتج 1", 1, "فئة", 1, "وحدة", 100m, 150m, 10m, null, true),
            new(2, "P002", null, "منتج 2", 1, "فئة", 1, "وحدة", 200m, 250m, 5m, null, true)
        };

        _mockProductService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<ProductDto>>.Success(products));

        _viewModel.SelectedReportType = ReportType.Inventory;
        await _viewModel.GenerateReportCommand.ExecuteAsync(null);
        await Task.Delay(100);

        _viewModel.ReportData.Columns.Should().Contain(c => c.ColumnName == "اسم المنتج");
        _viewModel.ReportData.Rows.Count.Should().Be(2);
    }

    #endregion

    #region GenerateCustomersReport Tests

    [Fact]
    public async Task GenerateReportCommand_Customers_LoadsActiveCustomers()
    {
        var customers = new List<CustomerDto>
        {
            new(1, "C001", "عميل 1", null, null, null, 100m, 50m, 0m, true),
            new(2, "C002", "عميل 2", null, null, null, 200m, 100m, 0m, false)
        };

        _mockCustomerService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<CustomerDto>>.Success(customers));

        _viewModel.SelectedReportType = ReportType.Customers;
        await _viewModel.GenerateReportCommand.ExecuteAsync(null);
        await Task.Delay(100);

        _viewModel.ReportData.Rows.Count.Should().Be(1);
    }

    #endregion

    #region GenerateSuppliersReport Tests

    [Fact]
    public async Task GenerateReportCommand_Suppliers_LoadsActiveSuppliers()
    {
        var suppliers = new List<SupplierDto>
        {
            new(1, "S001", "مورد 1", null, null, null, 100m, 50m, true),
            new(2, "S002", "مورد 2", null, null, null, 200m, 100m, false)
        };

        _mockSupplierService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(Result<List<SupplierDto>>.Success(suppliers));

        _viewModel.SelectedReportType = ReportType.Suppliers;
        await _viewModel.GenerateReportCommand.ExecuteAsync(null);
        await Task.Delay(100);

        _viewModel.ReportData.Rows.Count.Should().Be(1);
    }

    #endregion

    #region GenerateProfitLossReport Tests

    [Fact]
    public async Task GenerateReportCommand_ProfitLoss_CalculatesNetProfit()
    {
        var invoices = new List<SalesInvoiceDto>
        {
            CreateSalesInvoiceDto(1, "INV-001", 5000m, 5000m, 0m, 0m),
            CreateSalesInvoiceDto(2, "INV-002", 3000m, 3000m, 0m, 0m)
        };

        _mockSalesService
            .Setup(s => s.GetAllAsync(
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<byte?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<SalesInvoiceDto>>.Success(invoices));

        _viewModel.SelectedReportType = ReportType.ProfitLoss;
        await _viewModel.GenerateReportCommand.ExecuteAsync(null);
        await Task.Delay(100);

        _viewModel.ReportTotal.Should().Be(8000m);
    }

    #endregion

    #region Helper Methods

    private static SalesInvoiceDto CreateSalesInvoiceDto(
        int id,
        string invoiceNo,
        decimal totalAmount,
        decimal paidAmount,
        decimal dueAmount,
        decimal taxAmount)
    {
        return new SalesInvoiceDto(
            Id: id,
            InvoiceNo: invoiceNo,
            CustomerId: 1,
            CustomerName: "Test Customer",
            WarehouseId: 1,
            WarehouseName: "Main Warehouse",
            InvoiceDate: DateTime.Today,
            DueDate: null,
            PaymentType: 1,
            SubTotal: totalAmount,
            DiscountAmount: 0,
            TaxAmount: taxAmount,
            TotalAmount: totalAmount,
            PaidAmount: paidAmount,
            DueAmount: dueAmount,
            Notes: null,
            Status: 2,
            Items: new List<SalesInvoiceItemDto>());
    }

    #endregion
}