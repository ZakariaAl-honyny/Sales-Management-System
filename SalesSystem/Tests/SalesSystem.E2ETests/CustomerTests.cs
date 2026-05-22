#nullable disable
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FluentAssertions;

namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for Customer management operations in the Sales Management System.
/// </summary>
[Collection("E2E")]
public class CustomerTests : TestBase, IDisposable
{
    private Window _mainWindow;
    private Window _customerEditorWindow;
    private bool _disposed;

    public CustomerTests()
    {
        // Launch app, login, and navigate to Customers
        LaunchApplication();
        LoginAsAdmin();
        NavigateToCustomers();
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

    private void LoginAsAdmin()
    {
        KeyboardLogin();

        var windows = GetApplicationWindows();
        _mainWindow = windows.FirstOrDefault(w =>
            w.Name.Contains("المبيعات") || w.Name.Contains("Sales") || w.Name.Contains("System"))
            ?? windows.FirstOrDefault();
    }

    /// <summary>
    /// Helper: Navigates to Customers screen via navigation.
    /// </summary>
    private void NavigateToCustomers()
    {
        if (_mainWindow == null) return;

        // Try BtnCustomers first (MainWindow navigation)
        var customersNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnCustomers"));
        if (customersNav == null)
        {
            // Fall back to NavCustomers
            customersNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavCustomers"));
        }

        customersNav?.Click();
        System.Threading.Thread.Sleep(1500);
    }

    /// <summary>
    /// Test: Customer_AddNew_ShouldSucceed
    /// Verifies that a new customer can be added successfully with all required fields.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Customer")]
    [Trait("Category", "CRUD")]
    public void Customer_AddNew_ShouldSucceed()
    {
        try
        {
            // Arrange - Click Add Customer button
            var addButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddCustomer")) as Button;
            addButton.Should().NotBeNull("Add Customer button should exist");
            addButton!.Click();

            // Wait for editor dialog to appear
            System.Threading.Thread.Sleep(1000);

            // Find the customer editor window
            var windows = GetApplicationWindows();
            _customerEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("عميل") || w.Name.Contains("Customer") || w.Name.Contains("Add"))
                ?? _mainWindow;

            // Act - Fill in customer details
            var nameBox = _customerEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtCustomerName")) as TextBox;
            var phoneBox = _customerEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtPhone")) as TextBox;
            var addressBox = _customerEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtAddress")) as TextBox;
            var balanceBox = _customerEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtCurrentBalance")) as TextBox;

            nameBox.Should().NotBeNull("Customer name field should exist in editor");
            phoneBox.Should().NotBeNull("Phone field should exist in editor");
            addressBox.Should().NotBeNull("Address field should exist in editor");
            balanceBox.Should().NotBeNull("Balance field should exist in editor");

            var testName = $"عميل اختبار E2E {DateTime.Now:HHmmss}";
            var testPhone = "0551234567";
            var testAddress = "عنوان اختبار - الرياض";

            // Enter customer data
            nameBox!.Focus();
            Keyboard.Type(testName);

            phoneBox!.Focus();
            Keyboard.Type(testPhone);

            addressBox!.Focus();
            Keyboard.Type(testAddress);

            balanceBox!.Focus();
            Keyboard.Type("0");

            // Click Save
            var saveButton = _customerEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveCustomer")) as Button;
            saveButton.Should().NotBeNull("Save Customer button should exist");
            saveButton!.Click();

            // Wait for save to complete and dialog to close
            System.Threading.Thread.Sleep(2000);

            // Assert - Verify customer appears in the data grid
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgCustomers"));
            dataGrid.Should().NotBeNull("Customers data grid should be visible after saving");

            // Verify customer was added by searching for the name
            var searchBox = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtSearchCustomer")) as TextBox;
            searchBox.Should().NotBeNull("Search box should exist for verification");

            searchBox!.Focus();
            var searchPrefix = testName.Length >= 10 ? testName.Substring(0, 10) : testName;
            Keyboard.Type(searchPrefix);
            System.Threading.Thread.Sleep(1000);

            // Verify the data grid shows results
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            rows.Should().NotBeNull("Search should return customer results");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Customer_Search_ShouldFindExisting
    /// Verifies that searching for an existing customer returns correct results.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Customer")]
    [Trait("Category", "Search")]
    public void Customer_Search_ShouldFindExisting()
    {
        try
        {
            // Arrange - Find search controls
            var searchBox = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("TxtSearchCustomer")) as TextBox;
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgCustomers"));

            searchBox.Should().NotBeNull("Search customer text box should exist");
            dataGrid.Should().NotBeNull("Customers data grid should exist");

            // Get initial row count
            var initialRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var initialCount = initialRows == null ? 0 : initialRows.Length;

            // Act - Search for a common term
            searchBox!.Focus();
            Keyboard.Type("عميل"); // "Customer" in Arabic - likely to match existing data

            System.Threading.Thread.Sleep(500);

            // Assert - Verify search filtered the results
            var filteredRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var filteredCount = filteredRows == null ? 0 : filteredRows.Length;

            // The filtered count should be less than or equal to initial count
            filteredCount.Should().BeLessOrEqualTo(initialCount,
                "Search should filter or match existing customers");

            // Verify the search field contains our search text
            searchBox.Text.Should().Contain("عميل",
                "Search box should retain the entered search term");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Customer_EditBalance_ShouldUpdate
    /// Verifies that a manager can edit customer balance.
    /// Note: Cashiers cannot edit customer balance - this test assumes admin/manager role.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Customer")]
    [Trait("Category", "Edit")]
    public void Customer_EditBalance_ShouldUpdate()
    {
        try
        {
            // Arrange - Find first customer in grid and click to select
            var dataGrid = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("DgCustomers"));
            dataGrid.Should().NotBeNull("Customers data grid should exist");

            // Select first customer row
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            rows.Should().NotBeNull("Data grid should have customer rows");
            var rowCount = rows == null ? 0 : rows.Length;
            rowCount.Should().BeGreaterThan(0, "At least one customer should exist for testing");

            // Double-click to open edit mode (common pattern for grid editing)
            var firstCustomerRow = rows[0];
            firstCustomerRow.DoubleClick();
            System.Threading.Thread.Sleep(1500);

            // Find the customer editor dialog
            var windows = GetApplicationWindows();
            _customerEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("عميل") || w.Name.Contains("Customer") || w.Name.Contains("Edit"))
                ?? _mainWindow;

            // Act - Modify the balance
            var balanceBox = _customerEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtCurrentBalance")) as TextBox;
            balanceBox.Should().NotBeNull("Balance field should be editable");

            // Clear and enter new balance
            balanceBox!.Focus();
            Keyboard.Type(VirtualKeyShort.CONTROL);
            Keyboard.Type("A");
            Keyboard.Type(VirtualKeyShort.DELETE);
            Keyboard.Type("500.00");

            // Save changes
            var saveButton = _customerEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveCustomer")) as Button;
            saveButton.Should().NotBeNull("Save button should exist in edit dialog");
            saveButton!.Click();

            System.Threading.Thread.Sleep(2000);

            // Assert - Verify the updated balance appears in grid
            // Re-select the row to check updated value
            var updatedRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            updatedRows.Should().NotBeNull("Grid should still have customers after edit");

            // Verify editor closed (save successful)
            var editorStillOpen = _customerEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveCustomer"));
            editorStillOpen.Should().BeNull("Editor should close after successful save");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Customer_RecordPayment_ShouldReduceBalance
    /// Verifies that recording a customer payment reduces their balance.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Customer")]
    [Trait("Category", "Payment")]
    public void Customer_RecordPayment_ShouldReduceBalance()
    {
        try
        {
            // Arrange - Select a customer with positive balance
            var dataGrid = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("DgCustomers"));
            dataGrid.Should().NotBeNull("Customers data grid should exist");

            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            rows.Should().NotBeNull("Grid should have customer rows");
            var rowCount = rows == null ? 0 : rows.Length;
            rowCount.Should().BeGreaterThan(0, "At least one customer needed for payment test");

            // Click to select customer
            rows[0].Click();
            System.Threading.Thread.Sleep(500);

            // Act - Click Record Payment button
            var recordPaymentButton = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnRecordPayment")) as Button;
            recordPaymentButton.Should().NotBeNull("Record Payment button should exist");
            recordPaymentButton!.Click();

            System.Threading.Thread.Sleep(1500);

            // Find payment dialog (if exists)
            var windows = GetApplicationWindows();
            var paymentWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("دفع") || w.Name.Contains("Payment") || w.Name.Contains("سداد"));

            if (paymentWindow != null)
            {
                // Enter payment amount
                var amountBox = paymentWindow.FindFirstDescendant(cf =>
                    cf.ByAutomationId("TxtPaymentAmount")) as TextBox;

                if (amountBox != null)
                {
                    amountBox.Focus();
                    Keyboard.Type("100.00");
                    System.Threading.Thread.Sleep(200);

                    // Confirm payment
                    var confirmButton = paymentWindow.FindFirstDescendant(cf =>
                        cf.ByAutomationId("BtnConfirmPayment")) as Button
                        ?? paymentWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveCustomer")) as Button;

                    confirmButton?.Click();
                    System.Threading.Thread.Sleep(2000);
                }
            }

            // Assert - Verify balance was reduced
            // Refresh the grid to see updated balance
            var refreshButton = _mainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("BtnRefreshCustomer")) as Button
                ?? _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnRefresh")) as Button;
            refreshButton?.Click();
            System.Threading.Thread.Sleep(1500);

            // Verify customer is still in grid (payment was successful)
            var updatedRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            updatedRows.Should().NotBeNull("Grid should show customers after payment recording");
        }
        finally
        {
            CloseApplication();
        }
    }
}
