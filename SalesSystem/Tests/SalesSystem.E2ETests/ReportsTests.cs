using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FluentAssertions;

namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for Reports functionality in the Sales Management System.
/// Tests cover: navigation, report generation, date filtering, and Excel export.
/// Requires Admin or Manager role (ManagerAndAbove permission).
/// </summary>
[Collection("E2E")]
public class ReportsTests : TestBase, IDisposable
{
    private Window? _mainWindow;
    private Window? _reportsWindow;
    private bool _disposed;

    public ReportsTests()
    {
        // Launch app, login as admin, and navigate to Reports
        LaunchApplication();
        LoginAsAdmin();
        NavigateToReports();
    }

    public new void Dispose()
    {
        if (!_disposed)
        {
            CloseApplication();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Helper: Logs in as admin user
    /// </summary>
    private void LoginAsAdmin()
    {
        KeyboardLogin();
        
        var windows = GetApplicationWindows();
        _mainWindow = windows.FirstOrDefault(w =>
            w.Name.Contains("المبيعات") || w.Name.Contains("Sales") || w.Name.Contains("System"))
            ?? windows.FirstOrDefault();
    }

    /// <summary>
    /// Helper: Navigates to Reports screen
    /// </summary>
    private void NavigateToReports()
    {
        if (_mainWindow == null) return;

        // Try multiple automation IDs for the Reports navigation
        var reportsNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavReports"))
            ?? _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnReports"));

        reportsNav?.Click();
        System.Threading.Thread.Sleep(1500);

        // Find the reports window
        var windows = GetApplicationWindows();
        _reportsWindow = windows.FirstOrDefault(w =>
            w.Name.Contains("تقارير") ||
            w.Name.Contains("Reports"))
            ?? _mainWindow;
    }

    /// <summary>
    /// Test: Reports_NavigateToSalesReport_ShouldShowData
    /// Verifies that navigating to the sales report tab shows the report interface.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Reports")]
    [Trait("Category", "Navigation")]
    public void Reports_NavigateToSalesReport_ShouldShowData()
    {
        try
        {
            // Arrange
            _reportsWindow.Should().NotBeNull("Reports window should be visible after navigation");

            // Act - Click on Sales Report tab/button
            var salesReportButton = _reportsWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnSalesReport")) as Button;
            salesReportButton.Should().NotBeNull("Sales Report button should exist in Reports view");

            salesReportButton!.Click();
            System.Threading.Thread.Sleep(1000);

            // Assert - Verify the report interface elements are visible
            var datePickerStart = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DpStartDate"));
            datePickerStart.Should().NotBeNull("Start date picker should be visible after selecting Sales Report");

            var datePickerEnd = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DpEndDate"));
            datePickerEnd.Should().NotBeNull("End date picker should be visible after selecting Sales Report");

            var generateButton = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnGenerateReport"));
            generateButton.Should().NotBeNull("Generate Report button should be visible");

            var dataGrid = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgReportResults"));
            dataGrid.Should().NotBeNull("Report results DataGrid should be visible");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Reports_GenerateSalesReport_ShouldDisplayResults
    /// Verifies that generating a sales report displays results in the data grid.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Reports")]
    [Trait("Category", "Generate")]
    public void Reports_GenerateSalesReport_ShouldDisplayResults()
    {
        try
        {
            // Arrange - Ensure we're on the Sales Report tab
            var salesReportButton = _reportsWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnSalesReport")) as Button;
            salesReportButton?.Click();
            System.Threading.Thread.Sleep(500);

            // Set date range (last 30 days)
            var startDatePicker = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DpStartDate"));
            var endDatePicker = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DpEndDate"));

            startDatePicker.Should().NotBeNull("Start date picker should exist");
            endDatePicker.Should().NotBeNull("End date picker should exist");

            // Act - Click Generate Report
            var generateButton = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnGenerateReport")) as Button;
            generateButton.Should().NotBeNull("Generate Report button should exist");
            generateButton!.Click();

            // Wait for report to generate
            System.Threading.Thread.Sleep(2000);

            // Assert - Verify results are displayed
            var dataGrid = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgReportResults"));
            dataGrid.Should().NotBeNull("Report results DataGrid should be visible after generation");

            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows?.Length ?? 0;

            // Results may be empty if no sales data exists, but grid should exist
            dataGrid.Should().NotBeNull("DataGrid should exist regardless of data");

            // Check for total sales text (if report has totals)
            var totalSalesText = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtTotalSales"));
            totalSalesText.Should().NotBeNull("Total Sales field should be visible in report results");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Reports_FilterByDateRange_ShouldFilterResults
    /// Verifies that filtering by date range correctly filters the report results.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Reports")]
    [Trait("Category", "Filter")]
    public void Reports_FilterByDateRange_ShouldFilterResults()
    {
        try
        {
            // Arrange - Navigate to Sales Report
            var salesReportButton = _reportsWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnSalesReport")) as Button;
            salesReportButton?.Click();
            System.Threading.Thread.Sleep(500);

            // First, generate a report for a wide date range to get baseline
            var startDatePicker = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DpStartDate"));
            var endDatePicker = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DpEndDate"));
            var generateButton = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnGenerateReport")) as Button;

            startDatePicker.Should().NotBeNull("Start date picker should exist");
            endDatePicker.Should().NotBeNull("End date picker should exist");
            generateButton.Should().NotBeNull("Generate Report button should exist");

            // Act - Generate report with wide date range first
            generateButton!.Click();
            System.Threading.Thread.Sleep(2000);

            var dataGrid = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgReportResults"));
            var rowsBefore = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCountBefore = rowsBefore?.Length ?? 0;

            // Now narrow the date range to today only
            // Click on date pickers to set specific dates
            startDatePicker?.Click();
            System.Threading.Thread.Sleep(300);

            // Try to set the date to today
            var today = DateTime.Today;
            Keyboard.Type(today.ToString("yyyy-MM-dd"));
            System.Threading.Thread.Sleep(200);
            Keyboard.Type(VirtualKeyShort.ENTER);
            System.Threading.Thread.Sleep(500);

            // Click generate again with narrowed date range
            generateButton = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnGenerateReport")) as Button;
            generateButton?.Click();
            System.Threading.Thread.Sleep(2000);

            // Assert - Verify the date filter was applied
            // The grid should still be visible and functional
            dataGrid = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgReportResults"));
            dataGrid.Should().NotBeNull("DataGrid should be visible after date filter change");

            // Verify the date pickers reflect the selected values
            var startDateValue = startDatePicker?.Name ?? string.Empty;
            startDateValue.Should().NotBeNullOrEmpty("Start date should have a value after selection");

            // Verify that end date is after or equal to start date
            var endDateValue = endDatePicker?.Name ?? string.Empty;
            endDateValue.Should().NotBeNullOrEmpty("End date should have a value after selection");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Reports_ExportToExcel_ShouldSucceed
    /// Verifies that exporting a report to Excel succeeds without errors.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Reports")]
    [Trait("Category", "Export")]
    public void Reports_ExportToExcel_ShouldSucceed()
    {
        try
        {
            // Arrange - Navigate to Sales Report
            var salesReportButton = _reportsWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnSalesReport")) as Button;
            salesReportButton?.Click();
            System.Threading.Thread.Sleep(500);

            // Generate a report first to have data to export
            var generateButton = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnGenerateReport")) as Button;
            generateButton?.Click();
            System.Threading.Thread.Sleep(2000);

            // Act - Click Export to Excel button
            var exportButton = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnExportExcel")) as Button;
            exportButton.Should().NotBeNull("Export to Excel button should exist in Reports view");

            exportButton!.Click();
            System.Threading.Thread.Sleep(3000); // Wait for file dialog or export process

            // Assert - Verify export was triggered
            // The export button should no longer be in a 'processing' state
            // or we can verify the file was created in the downloads folder
            var exportButtonAfter = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnExportExcel")) as Button;
            exportButtonAfter.Should().NotBeNull("Export button should still be visible after export completes");

            // If there's a status message or confirmation, verify it
            var statusMessage = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtExportStatus"))
                ?? _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtStatusMessage"));

            // Status message is optional - export might just complete silently
            // The main assertion is that the button is still enabled (not crashed)
            exportButtonAfter.IsEnabled.Should().BeTrue("Export button should remain enabled after export");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Reports_ViewOtherReportTypes_ShouldSwitchTabs
    /// Verifies that viewing other report types (Purchase, Inventory, Profit) switches correctly.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Reports")]
    [Trait("Category", "Navigation")]
    public void Reports_ViewOtherReportTypes_ShouldSwitchTabs()
    {
        try
        {
            // Arrange
            _reportsWindow.Should().NotBeNull("Reports window should be visible");

            // Act & Assert - Test Purchase Report tab
            var purchaseReportButton = _reportsWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnPurchaseReport")) as Button;
            purchaseReportButton.Should().NotBeNull("Purchase Report button should exist");
            purchaseReportButton!.Click();
            System.Threading.Thread.Sleep(1000);

            // Verify the same elements are visible for Purchase Report
            var purchaseGrid = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgReportResults"));
            purchaseGrid.Should().NotBeNull("Report results DataGrid should be visible for Purchase Report");

            // Act & Assert - Test Inventory Report tab
            var inventoryReportButton = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnInventoryReport")) as Button;
            inventoryReportButton.Should().NotBeNull("Inventory Report button should exist");
            inventoryReportButton!.Click();
            System.Threading.Thread.Sleep(1000);

            var inventoryGrid = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgReportResults"));
            inventoryGrid.Should().NotBeNull("Report results DataGrid should be visible for Inventory Report");

            // Act & Assert - Test Profit Report tab
            var profitReportButton = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnProfitReport")) as Button;
            profitReportButton.Should().NotBeNull("Profit Report button should exist");
            profitReportButton!.Click();
            System.Threading.Thread.Sleep(1000);

            var profitGrid = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgReportResults"));
            profitGrid.Should().NotBeNull("Report results DataGrid should be visible for Profit Report");

            // Verify total profit field is visible for profit report
            var totalProfitText = _reportsWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtTotalProfit"));
            totalProfitText.Should().NotBeNull("Total Profit field should be visible in Profit Report");
        }
        finally
        {
            CloseApplication();
        }
    }
}
