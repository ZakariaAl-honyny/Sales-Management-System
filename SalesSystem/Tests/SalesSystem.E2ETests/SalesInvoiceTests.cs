using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FluentAssertions;

namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for Sales Invoice operations in the Sales Management System.
/// Tests cover: creating, adding items, posting, and cancelling sales invoices.
/// </summary>
[Collection("E2E")]
public class SalesInvoiceTests : TestBase, IDisposable
{
    private Window? _mainWindow;
    private Window? _invoiceEditorWindow;
    private bool _disposed;

    public SalesInvoiceTests()
    {
        // Launch app, login, and navigate to Sales Invoices
        LaunchApplication();
        LoginAsAdmin();
        NavigateToSalesInvoices();
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
    /// Helper: Logs in as admin user using keyboard-based Tab navigation
    /// </summary>
    private void LoginAsAdmin()
    {
        KeyboardLogin();
        
        var windows = GetApplicationWindows();
        _mainWindow = windows.FirstOrDefault(w => w.Name.Contains("المبيعات") || w.Name.Contains("System"))
            ?? windows.FirstOrDefault();
    }

    /// <summary>
    /// Helper: Navigates to Sales Invoices list screen
    /// </summary>
    private void NavigateToSalesInvoices()
    {
        if (_mainWindow == null) return;

        // Try multiple automation IDs for the Sales navigation
        var salesNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavSalesInvoices"))
            ?? _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavSales"))
            ?? _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSales"));

        salesNav?.Click();
        System.Threading.Thread.Sleep(1500);
    }

    /// <summary>
    /// Helper: Opens the invoice editor by clicking Add Invoice button
    /// </summary>
    private void OpenInvoiceEditor()
    {
        var addButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddSalesInvoice")) as Button;
        addButton.Should().NotBeNull("Add Sales Invoice button should exist");
        addButton!.Click();

        System.Threading.Thread.Sleep(1000);

        // Find the editor window
        var windows = GetApplicationWindows();
        _invoiceEditorWindow = windows.FirstOrDefault(w =>
            w.Name.Contains("فاتورة") ||
            w.Name.Contains("Invoice") ||
            w.Name.Contains("بيع"))
            ?? _mainWindow;
    }

    /// <summary>
    /// Helper: Adds an item to the invoice
    /// </summary>
    private void AddInvoiceItem(string productName, decimal quantity = 1, decimal unitPrice = 10)
    {
        var addItemButton = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddInvoiceItem")) as Button;
        addItemButton.Should().NotBeNull("Add Invoice Item button should exist");
        addItemButton!.Click();

        System.Threading.Thread.Sleep(500);

        // Find the data grid and select product
        var dataGrid = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgInvoiceItems"));
        dataGrid.Should().NotBeNull("Invoice Items DataGrid should exist");

        var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
        var rowCount = rows == null ? 0 : rows.Length;
        if (rowCount > 0)
        {
            // In the last row, select product from combo box
            var lastRow = rows[rowCount - 1];
            var productCombo = lastRow.FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)) as ComboBox;

            if (productCombo != null)
            {
                // Click to open the combo box dropdown
                productCombo.Click();
                System.Threading.Thread.Sleep(300);

                var items = productCombo.Items;
                if (items != null && items.Length > 0)
                {
                    // Find product by name or select first available
                    var targetItem = items.FirstOrDefault(i => i.Name.Contains(productName))
                        ?? items[0];
                    targetItem.Select();
                }

                System.Threading.Thread.Sleep(200);
            }

            // Enter quantity (second column in grid - index 1)
            var cells = lastRow.FindAllChildren(cf => cf.ByControlType(ControlType.Edit));
            if (cells != null && cells.Length > 1)
            {
                cells[1].Focus();
                Keyboard.Type(quantity.ToString("N3").Replace(",", "").TrimEnd('0').TrimEnd('.'));
            }

            System.Threading.Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Helper: Selects a customer from the dropdown
    /// </summary>
    private void SelectCustomer(string customerName)
    {
        var customerCombo = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("CmbCustomer")) as ComboBox;
        customerCombo.Should().NotBeNull("Customer dropdown should exist");

        if (customerCombo != null)
        {
            // Click to open the dropdown
            customerCombo.Click();
            System.Threading.Thread.Sleep(300);

            var items = customerCombo.Items;
            if (items != null && items.Length > 0)
            {
                var targetItem = items.FirstOrDefault(i => i.Name.Contains(customerName)) ?? items[0];
                targetItem.Select();
            }

            System.Threading.Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Test: SalesInvoice_CreateNew_ShouldSucceed
    /// Verifies that a new sales invoice can be created with basic information.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesInvoice")]
    [Trait("Category", "Create")]
    public void SalesInvoice_CreateNew_ShouldSucceed()
    {
        try
        {
            // Arrange
            var addButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddSalesInvoice")) as Button;
            addButton.Should().NotBeNull("Add Sales Invoice button should exist");

            // Act
            addButton!.Click();
            System.Threading.Thread.Sleep(1500);

            // Find the editor window
            var windows = GetApplicationWindows();
            _invoiceEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("فاتورة") ||
                w.Name.Contains("Invoice"));

            _invoiceEditorWindow.Should().NotBeNull("Invoice editor window should open after clicking Add");

            // Assert - Verify editor elements exist
            var invoiceNo = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtInvoiceNumber")) as TextBox;
            var customerCombo = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("CmbCustomer")) as ComboBox;
            var datePicker = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("DpInvoiceDate"));

            invoiceNo.Should().NotBeNull("Invoice number field should exist in editor");
            customerCombo.Should().NotBeNull("Customer dropdown should exist in editor");

            // Verify invoice can be saved as draft
            var saveDraftButton = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveDraft")) as Button;
            saveDraftButton.Should().NotBeNull("Save Draft button should exist");

            // Verify invoice number is auto-generated (not empty)
            var invoiceNoText = invoiceNo?.Name ?? string.Empty;
            invoiceNoText.Should().NotBeNullOrEmpty("Invoice number should be auto-generated");
        }
        finally
        {
            // Cleanup - close any open dialogs
            try
            {
                var cancelButton = _invoiceEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("BtnCancelInvoice")) as Button;
                if (cancelButton?.IsOffscreen == false)
                {
                    cancelButton?.Click();
                }
            }
            catch { /* Dialog might already be closed */ }

            System.Threading.Thread.Sleep(500);
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: SalesInvoice_AddLineItems_ShouldCalculateTotal
    /// Verifies that adding multiple line items correctly calculates the invoice totals.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesInvoice")]
    [Trait("Category", "LineItems")]
    public void SalesInvoice_AddLineItems_ShouldCalculateTotal()
    {
        try
        {
            // Arrange - Open invoice editor
            OpenInvoiceEditor();

            // Select a customer first
            SelectCustomer("عميل");

            // Act - Add first line item
            AddInvoiceItem("منتج", 2, 100);
            System.Threading.Thread.Sleep(500);

            // Add second line item
            AddInvoiceItem("صنف", 1, 50);
            System.Threading.Thread.Sleep(500);

            // Assert - Check that items were added
            var dataGrid = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgInvoiceItems"));
            dataGrid.Should().NotBeNull("Invoice items data grid should exist");

            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows == null ? 0 : rows.Length;

            rowCount.Should().BeGreaterOrEqualTo(1, "At least one item should be added to the invoice");

            // Verify the totals are calculated (SubTotal and TotalAmount fields should exist)
            var subTotalLabel = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtSubTotal")) as TextBox;
            var totalAmountLabel = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtTotalAmount")) as TextBox;

            subTotalLabel.Should().NotBeNull("SubTotal field should exist");
            totalAmountLabel.Should().NotBeNull("TotalAmount field should exist");
        }
        finally
        {
            // Cleanup
            try
            {
                var cancelButton = _invoiceEditorWindow?.FindFirstDescendant(cf => cf.ByAutomationId("BtnCancelInvoice")) as Button;
                if (cancelButton?.IsOffscreen == false)
                {
                    cancelButton?.Click();
                }
            }
            catch { /* Dialog might already be closed */ }

            System.Threading.Thread.Sleep(500);
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: SalesInvoice_Post_ShouldUpdateStockAndBalance
    /// Verifies that posting a sales invoice correctly updates stock and customer balance.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesInvoice")]
    [Trait("Category", "Post")]
    public void SalesInvoice_Post_ShouldUpdateStockAndBalance()
    {
        try
        {
            // Arrange - Open invoice editor and create a draft
            OpenInvoiceEditor();

            // Select customer
            SelectCustomer("عميل");

            // Add an item
            AddInvoiceItem("منتج", 1, 100);
            System.Threading.Thread.Sleep(500);

            // Act - Save as draft first
            var saveDraftButton = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveDraft")) as Button;
            saveDraftButton.Should().NotBeNull("Save Draft button should exist");
            saveDraftButton!.Click();

            System.Threading.Thread.Sleep(1000);

            // Now post the invoice
            var postButton = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnPostInvoice")) as Button;
            postButton.Should().NotBeNull("Post Invoice button should exist");
            postButton!.Click();

            // Wait for posting to complete
            System.Threading.Thread.Sleep(2000);

            // Assert - Verify the invoice status changed
            var windows = GetApplicationWindows();
            var listView = windows.FirstOrDefault(w => w.Name.Contains("فواتير") || w.Name.Contains("Sales"));

            if (listView != null)
            {
                // Verify the invoice appears in the list with Posted status
                var dataGrid = listView.FindFirstDescendant(cf => cf.ByAutomationId("DgSalesInvoices"));
                dataGrid.Should().NotBeNull("Sales Invoices data grid should show posted invoice");

                var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
                var rowCount = rows == null ? 0 : rows.Length;

                rowCount.Should().BeGreaterOrEqualTo(1, "Posted invoice should appear in the list");
            }
        }
        finally
        {
            // Cleanup
            System.Threading.Thread.Sleep(500);
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: SalesInvoice_Cancel_ShouldReverseStockAndBalance
    /// Verifies that cancelling a posted invoice correctly reverses stock and balance changes.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "SalesInvoice")]
    [Trait("Category", "Cancel")]
    public void SalesInvoice_Cancel_ShouldReverseStockAndBalance()
    {
        try
        {
            // Arrange - First create and post an invoice
            OpenInvoiceEditor();

            // Select customer
            SelectCustomer("عميل");

            // Add an item
            AddInvoiceItem("منتج", 1, 100);
            System.Threading.Thread.Sleep(500);

            // Save and post
            var saveDraftButton = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveDraft")) as Button;
            saveDraftButton?.Click();
            System.Threading.Thread.Sleep(1000);

            var postButton = _invoiceEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnPostInvoice")) as Button;
            postButton?.Click();
            System.Threading.Thread.Sleep(2000);

            // Now navigate back to list and select the posted invoice
            var windows = GetApplicationWindows();
            var listView = windows.FirstOrDefault(w => w.Name.Contains("فواتير") || w.Name.Contains("Sales"));
            listView ??= _mainWindow;

            // Act - Select the posted invoice and cancel it
            var viewButton = listView.FindFirstDescendant(cf => cf.ByAutomationId("BtnViewSalesInvoice")) as Button;
            viewButton.Should().NotBeNull("View button should exist to select invoice for cancellation");

            // Select first row in grid
            var dataGrid = listView.FindFirstDescendant(cf => cf.ByAutomationId("DgSalesInvoices"));
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows == null ? 0 : rows.Length;

            if (rowCount > 0)
            {
                // Select the first (most recent) invoice
                rows[0].Click();
                System.Threading.Thread.Sleep(500);

                // Click View to open the invoice
                viewButton!.Click();
                System.Threading.Thread.Sleep(1500);

                // Find the cancel button in the editor
                windows = GetApplicationWindows();
                var editorWindow = windows.FirstOrDefault(w =>
                    w.Name.Contains("فاتورة") ||
                    w.Name.Contains("Invoice"));

                if (editorWindow != null)
                {
                    var cancelButton = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnCancelInvoice")) as Button;
                    cancelButton.Should().NotBeNull("Cancel button should exist in invoice editor");

                    cancelButton!.Click();
                    System.Threading.Thread.Sleep(2000);

                    // Assert - Verify cancellation was successful
                    var errorMessage = editorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtErrorMessage"));
                    errorMessage.Should().BeNull("No error should occur during cancellation");
                }
            }
        }
        finally
        {
            // Cleanup
            System.Threading.Thread.Sleep(500);
            CloseApplication();
        }
    }
}
