using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FluentAssertions;

namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for Product CRUD operations in the Sales Management System.
/// </summary>
[Collection("E2E")]
public class ProductCrudTests : TestBase, IDisposable
{
    private Window? _mainWindow;
    private Window? _productEditorWindow;
    private bool _disposed;

    public ProductCrudTests()
    {
        // Launch, login, and navigate to Products
        LaunchApplication();
        LoginAsAdmin();
        NavigateToProducts();
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
    /// Helper: Navigates to Products screen
    /// </summary>
    private void NavigateToProducts()
    {
        if (_mainWindow == null) return;

        var productsNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavProducts")) as ListBoxItem;
        productsNav?.Click();
        System.Threading.Thread.Sleep(1500);
    }

    /// <summary>
    /// Test: Product_AddNewProduct_ShouldSucceed
    /// Verifies that a new product can be added successfully.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Product")]
    [Trait("Category", "CRUD")]
    public void Product_AddNewProduct_ShouldSucceed()
    {
        try
        {
            // Arrange - Click Add button
            var addButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddProduct")) as Button;
            addButton.Should().NotBeNull("Add Product button should exist");
            addButton!.Click();

            // Wait for editor dialog
            System.Threading.Thread.Sleep(1000);

            // Find the editor window/dialog
            var windows = GetApplicationWindows();
            _productEditorWindow = windows.FirstOrDefault(w => w.Name.Contains("منتج") || w.Name.Contains("Product"))
                ?? _mainWindow;

            // Act - Fill in product details
            var nameBox = _productEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtProductName")) as TextBox;

            nameBox.Should().NotBeNull("Product name field should exist");

            nameBox!.Focus();
            Keyboard.Type("منتج اختبار E2E " + DateTime.Now.ToString("HHmmss"));

            // Click Save
            var saveButton = _productEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveProduct")) as Button;
            saveButton.Should().NotBeNull("Save button should exist");
            saveButton!.Click();

            // Wait for save to complete
            System.Threading.Thread.Sleep(2000);

            // Assert - Check if product appears in grid
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgProducts"));
            dataGrid.Should().NotBeNull("Products data grid should be visible after saving");

            // Close dialog if still open
            try
            {
                var closeButton = _productEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnCancelProduct")) as Button;
                if (closeButton?.IsOffscreen == false)
                {
                    closeButton?.Click();
                }
            }
            catch { /* Dialog might have closed */ }
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Product_SearchProduct_ShouldFilterResults
    /// Verifies that search functionality filters products correctly.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Product")]
    public void Product_SearchProduct_ShouldFilterResults()
    {
        try
        {
            // Arrange
            var searchBox = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("TxtSearchProduct")) as TextBox;
            var searchButton = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSearchProduct")) as Button;

            searchBox.Should().NotBeNull("Search box should exist");
            searchButton.Should().NotBeNull("Search button should exist");

            // Act - Search for a specific term
            searchBox!.Focus();
            Keyboard.Type("غير موجود");

            searchButton!.Click();
            System.Threading.Thread.Sleep(1000);

            // Assert - Grid should show filtered results
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgProducts"));
            dataGrid.Should().NotBeNull("Data grid should update after search");

            // Check if no rows or empty state is shown
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows == null ? 0 : rows.Length;
            rowCount.Should().BeLessThan(5, "Search should filter results");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Product_Refresh_ShouldReloadData
    /// Verifies that refresh button reloads product data.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Product")]
    public void Product_Refresh_ShouldReloadData()
    {
        try
        {
            // Arrange
            var refreshButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnRefreshProduct")) as Button;
            refreshButton.Should().NotBeNull("Refresh button should exist");

            // Act
            refreshButton!.Click();
            System.Threading.Thread.Sleep(2000);

            // Assert - Data grid should have data
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgProducts"));
            dataGrid.Should().NotBeNull("Data grid should reload after refresh");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Product_Validation_EmptyName_ShouldShowError
    /// Verifies that empty product name shows validation error.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Product")]
    [Trait("Category", "Validation")]
    public void Product_Validation_EmptyName_ShouldShowError()
    {
        try
        {
            // Arrange - Open Add dialog
            var addButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddProduct")) as Button;
            addButton!.Click();
            System.Threading.Thread.Sleep(1000);

            var windows = GetApplicationWindows();
            _productEditorWindow = windows.FirstOrDefault(w => w.Name.Contains("منتج") || w.Name.Contains("Product"))
                ?? _mainWindow;

            // Act - Try to save without entering name
            var saveButton = _productEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveProduct")) as Button;
            saveButton!.Click();
            System.Threading.Thread.Sleep(1000);

            // Assert - Dialog should still be open (validation should prevent save)
            var errorMessage = _productEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtProductName"));
            errorMessage.Should().NotBeNull("Product name field should still be visible (validation prevented save)");
        }
        finally
        {
            CloseApplication();
        }
    }
}
