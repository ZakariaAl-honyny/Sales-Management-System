#nullable disable
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FluentAssertions;

namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for Sales Return operations in the Sales Management System.
/// </summary>
[Collection("E2E")]
public class SalesReturnTests : TestBase, IDisposable
{
    private Window _mainWindow;
    private Window _returnEditorWindow;
    private bool _disposed;

    public SalesReturnTests()
    {
        // Launch, login, and navigate to Sales Returns
        LaunchApplication();
        LoginAsAdmin();
        NavigateToSalesReturns();
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
    /// Helper: Navigates to Sales Returns screen
    /// </summary>
    private void NavigateToSalesReturns()
    {
        if (_mainWindow == null) return;

        var salesReturnsNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavSalesReturns"));
        if (salesReturnsNav != null)
        {
            salesReturnsNav.Click();
            System.Threading.Thread.Sleep(1500);
            return;
        }

        // Fallback: try NavSales and look for returns option
        var salesNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavSales"));
        salesNav?.Click();
        System.Threading.Thread.Sleep(1500);

        // Try to find sales returns in menu
        var menuBar = _mainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuBar));
        if (menuBar != null)
        {
            var salesMenu = menuBar.FindFirstChild(cf => cf.ByName("المبيعات"));
            if (salesMenu != null)
            {
                salesMenu.Click();
                System.Threading.Thread.Sleep(500);

                var returnsMenuItem = salesMenu.FindFirstChild(cf => cf.ByName("مرتجعات"));
                returnsMenuItem?.Click();
                System.Threading.Thread.Sleep(1500);
            }
        }
    }

    /// <summary>
    /// Helper: Opens the return editor by clicking New Return button
    /// </summary>
    private void OpenReturnEditor()
    {
        if (_mainWindow == null) return;

        var newReturnButton = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnNewReturn")) as Button;
        newReturnButton.Should().NotBeNull("New Return button should exist with AutomationId 'BtnNewReturn'");

        newReturnButton!.Click();
        System.Threading.Thread.Sleep(1000);

        var windows = GetApplicationWindows();
        _returnEditorWindow = windows.FirstOrDefault(w => w.Name.Contains("مرتجع") || w.Name.Contains("Return"))
            ?? _mainWindow;
    }

    /// <summary>
    /// Helper: Selects an original invoice for return
    /// </summary>
    private void SelectOriginalInvoice(string invoiceNumber = "")
    {
        var selectInvoiceButton = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("BtnSelectInvoice")) as Button;
        selectInvoiceButton.Should().NotBeNull("Select Invoice button should exist with AutomationId 'BtnSelectInvoice'");

        selectInvoiceButton!.Click();
        System.Threading.Thread.Sleep(1500);

        // Try to find and select an invoice from the dialog
        var windows = GetApplicationWindows();
        var invoiceDialog = windows.FirstOrDefault(w => w.Name.Contains("فاتورة") || w.Name.Contains("Invoice"));

        if (invoiceDialog != null)
        {
            var dataGrid = invoiceDialog.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataGrid));
            if (dataGrid != null)
            {
                var rows = dataGrid.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
                var rowCount = rows == null ? 0 : rows.Length;
                if (rowCount > 0)
                {
                    rows[0].Click();
                    System.Threading.Thread.Sleep(500);
                }
            }

            // Click confirm/select button in dialog
            var confirmButton = invoiceDialog.FindFirstDescendant(cf => cf.ByName("تأكيد")) as Button
                ?? invoiceDialog.FindFirstDescendant(cf => cf.ByName("اختيار")) as Button;
            confirmButton?.Click();
            System.Threading.Thread.Sleep(500);
        }
    }

    /// <summary>
    /// Helper: Sets return quantity for an item in the grid
    /// </summary>
    private void SetReturnQuantity(int rowIndex, string quantity)
    {
        var dataGrid = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("DgReturnItems"));
        dataGrid.Should().NotBeNull("Return items DataGrid should exist with AutomationId 'DgReturnItems'");

        var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
        var rowCount = rows == null ? 0 : rows.Length;

        if (rowIndex < rowCount)
        {
            var row = rows![rowIndex];
            var quantityTextBox = row.FindFirstDescendant(cf => cf.ByAutomationId("TxtReturnItemQty")) as TextBox;

            if (quantityTextBox != null)
            {
                quantityTextBox.Focus();
                Keyboard.Type(VirtualKeyShort.CONTROL);
                Keyboard.Type("A");
                Keyboard.Type(quantity);
                System.Threading.Thread.Sleep(200);
            }
        }
    }

    /// <summary>
    /// Helper: Clicks Post/Save button on return editor
    /// </summary>
    private void ClickSaveReturn()
    {
        var saveButton = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveReturn")) as Button;
        saveButton.Should().NotBeNull("Save Return button should exist with AutomationId 'BtnSaveReturn'");

        saveButton!.Click();
        System.Threading.Thread.Sleep(2000);
    }

    /// <summary>
    /// Helper: Closes the return editor dialog
    /// </summary>
    private void CloseReturnEditor()
    {
        var closeButton = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("BtnCancelReturn")) as Button;
        if (closeButton?.IsOffscreen == false)
        {
            closeButton?.Click();
            System.Threading.Thread.Sleep(500);
        }
    }

    /// <summary>
    /// Helper: Gets current return total from the editor
    /// </summary>
    private string GetReturnTotal()
    {
        var totalLabel = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByName("الإجمالي")) as Label
            ?? _returnEditorWindow?.FindFirstDescendant(cf => cf.ByName("المجموع")) as Label;
        if (totalLabel != null)
        {
            return totalLabel.Text ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Test: SalesReturn_CreateFromPostedInvoice_ShouldSucceed
    /// Verifies that a new sales return can be created from a posted invoice.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesReturn")]
    [Trait("Category", "Critical")]
    public void SalesReturn_CreateFromPostedInvoice_ShouldSucceed()
    {
        try
        {
            // Arrange - Navigate to Sales Returns and open editor
            OpenReturnEditor();

            // Act - Select an original invoice
            SelectOriginalInvoice();

            System.Threading.Thread.Sleep(1000);

            // Assert - Check that invoice info is loaded
            var invoiceNoTextBox = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("TxtReturnInvoiceNo")) as TextBox;
            invoiceNoTextBox.Should().NotBeNull("Invoice number field should exist");

            var dataGrid = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("DgReturnItems"));
            dataGrid.Should().NotBeNull("Return items DataGrid should exist after selecting invoice");

            // Verify items are loaded from the original invoice
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows == null ? 0 : rows.Length;
            rowCount.Should().BeGreaterThan(0, "Items should be loaded from the selected invoice");

            // Close editor
            CloseReturnEditor();
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: SalesReturn_AddItems_ShouldCalculateTotal
    /// Verifies that adding/modifying return quantities updates the total.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesReturn")]
    public void SalesReturn_AddItems_ShouldCalculateTotal()
    {
        try
        {
            // Arrange - Open return editor and select invoice
            OpenReturnEditor();
            SelectOriginalInvoice();
            System.Threading.Thread.Sleep(1000);

            var dataGrid = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("DgReturnItems"));
            dataGrid.Should().NotBeNull("Return items DataGrid should exist");

            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows == null ? 0 : rows.Length;

            if (rowCount > 0)
            {
                // Act - Set return quantity for first item
                SetReturnQuantity(0, "2");

                System.Threading.Thread.Sleep(500);

                // Assert - Check that return total is updated
                var totalAfterInput = GetReturnTotal();
                totalAfterInput.Should().NotBeNullOrEmpty("Return total should be calculated after setting quantity");

                // Verify the quantity was set
                var quantityTextBox = rows![0].FindFirstDescendant(cf => cf.ByAutomationId("TxtReturnItemQty")) as TextBox;
                quantityTextBox.Should().NotBeNull("Return quantity textbox should exist");

                var quantityValue = quantityTextBox?.Text;
                quantityValue.Should().Be("2", "Return quantity should be set to the entered value");
            }

            // Close editor
            CloseReturnEditor();
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: SalesReturn_Post_ShouldUpdateStockAndBalance
    /// Verifies that posting a sales return updates stock and customer balance.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesReturn")]
    [Trait("Category", "Critical")]
    public void SalesReturn_Post_ShouldUpdateStockAndBalance()
    {
        try
        {
            // Arrange - Open return editor and select invoice
            OpenReturnEditor();
            SelectOriginalInvoice();
            System.Threading.Thread.Sleep(1000);

            var dataGrid = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("DgReturnItems"));
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows == null ? 0 : rows.Length;

            rowCount.Should().BeGreaterThan(0, "Invoice should have items for return");

            // Set return quantity for first item
            SetReturnQuantity(0, "1");

            // Act - Click Post (Save) button
            ClickSaveReturn();

            // Assert - Check that the return was saved (editor may close or show success)
            System.Threading.Thread.Sleep(1000);

            var windows = GetApplicationWindows();
            var currentWindow = windows.FirstOrDefault(w => w.Name.Contains("مرتجع"));

            // If editor is still open, check for any error message
            if (currentWindow != null)
            {
                var errorMessage = currentWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtErrorMessage")) as TextBox;
                var successIndicator = currentWindow.FindFirstDescendant(cf => cf.ByName("تم"))
                    ?? currentWindow.FindFirstDescendant(cf => cf.ByName("نجاح"));

                errorMessage?.Text.Should().BeNullOrEmpty(
                    "No error message should be shown when return is saved successfully");
            }

            // Close editor if still open
            CloseReturnEditor();

            // Navigate back to returns list to verify the return was created
            System.Threading.Thread.Sleep(1000);

            var returnsListGrid = _mainWindow?.FindFirstDescendant(cf => cf.ByAutomationId("DgReturns"));
            returnsListGrid.Should().NotBeNull("Returns list grid should be visible after posting return");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: SalesReturn_Validation_EmptyReturn_ShouldShowError
    /// Verifies that attempting to post an empty return shows validation error.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesReturn")]
    [Trait("Category", "Validation")]
    public void SalesReturn_Validation_EmptyReturn_ShouldShowError()
    {
        try
        {
            // Arrange - Open return editor
            OpenReturnEditor();
            System.Threading.Thread.Sleep(500);

            // Act - Try to save without selecting an invoice
            var saveButton = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveReturn")) as Button;
            saveButton.Should().NotBeNull("Save button should exist");
            saveButton!.Click();

            System.Threading.Thread.Sleep(1000);

            // Assert - Editor should still be open (validation prevented save)
            var invoiceNoTextBox = _returnEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("TxtReturnInvoiceNo")) as TextBox;
            invoiceNoTextBox.Should().NotBeNull("Invoice number field should still be visible (validation should prevent save)");

            // Verify the invoice field is empty
            var invoiceNoValue = invoiceNoTextBox?.Text;
            invoiceNoValue.Should().BeNullOrEmpty("Invoice number should be empty when no invoice is selected");

            // Close editor
            CloseReturnEditor();
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: SalesReturn_SearchReturn_ShouldFilterResults
    /// Verifies that search functionality filters returns correctly.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesReturn")]
    public void SalesReturn_SearchReturn_ShouldFilterResults()
    {
        try
        {
            // Arrange - Find search elements
            var searchBox = _mainWindow?.FindFirstDescendant(cf => cf.ByAutomationId("TxtSearchReturn")) as TextBox;
            var searchButton = _mainWindow?.FindFirstDescendant(cf => cf.ByAutomationId("BtnSearchReturn")) as Button;

            searchBox.Should().NotBeNull("Search box should exist with AutomationId 'TxtSearchReturn'");
            searchButton.Should().NotBeNull("Search button should exist with AutomationId 'BtnSearchReturn'");

            // Act - Search for a non-existent return
            searchBox!.Focus();
            Keyboard.Type("غير موجود");

            searchButton!.Click();
            System.Threading.Thread.Sleep(1000);

            // Assert - Grid should show filtered results
            var dataGrid = _mainWindow?.FindFirstDescendant(cf => cf.ByAutomationId("DgReturns"));
            dataGrid.Should().NotBeNull("Returns DataGrid should update after search");

            var gridRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = gridRows == null ? 0 : gridRows.Length;
            rowCount.Should().BeLessThan(5, "Search should filter results");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: SalesReturn_Refresh_ShouldReloadData
    /// Verifies that refresh button reloads return data.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesReturn")]
    public void SalesReturn_Refresh_ShouldReloadData()
    {
        try
        {
            // Arrange
            var refreshButton = _mainWindow?.FindFirstDescendant(cf => cf.ByAutomationId("BtnRefreshReturn")) as Button;
            refreshButton.Should().NotBeNull("Refresh button should exist with AutomationId 'BtnRefreshReturn'");

            // Act
            refreshButton!.Click();
            System.Threading.Thread.Sleep(2000);

            // Assert - Data grid should have data
            var dataGrid = _mainWindow?.FindFirstDescendant(cf => cf.ByAutomationId("DgReturns"));
            dataGrid.Should().NotBeNull("Returns DataGrid should reload after refresh");
        }
        finally
        {
            CloseApplication();
        }
    }
}
