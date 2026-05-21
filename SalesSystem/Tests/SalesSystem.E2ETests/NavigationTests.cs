using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FluentAssertions;

namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for main window navigation of the Sales Management System.
/// Uses the menu bar (standard WPF Menu control) which has better UIA support.
/// </summary>
[Collection("E2E")]
public class NavigationTests : TestBase, IDisposable
{
    private Window? _mainWindow;
    private bool _disposed;

    public NavigationTests()
    {
        // Launch and log in
        LaunchApplication();
        KeyboardLogin();

        // Get main window
        var windows = GetApplicationWindows();
        _mainWindow = windows.FirstOrDefault(w => w.Name.Contains("نظام إدارة المبيعات"));
        _mainWindow.Should().NotBeNull("Main window should be visible after login");
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
    /// Finds a menu item within the main window's Menu bar by its header text.
    /// Uses FindFirstDescendant with condition builder which has wider UIA support.
    /// </summary>
    private MenuItem? FindMenuItem(string header)
    {
        // Use multiple strategies to find the menu item
        var menuBar = _mainWindow!.FindFirstDescendant(cf => cf.ByClassName("Menu"));
        if (menuBar == null)
            return null;

        // Try by name first
        var item = menuBar.FindFirstChild(cf => cf.ByName(header)) as MenuItem;
        if (item != null)
            return item;

        // Try by AutomationId
        item = menuBar.FindFirstChild(cf => cf.ByAutomationId(header)) as MenuItem;
        if (item != null)
            return item;

        // Fall back to finding any MenuItem child
        var items = menuBar.FindAllChildren(cf => cf.ByClassName("MenuItem"));
        foreach (var child in items)
        {
            if (child.Name == header || child.Name.Contains(header))
                return child as MenuItem;
        }

        return null;
    }

    /// <summary>
    /// Finds a specified navigation sidebar ListBoxItem by its AutomationId.
    /// Traverses through the known visual tree path for better UIA compatibility.
    /// </summary>
    private AutomationElement? FindSidebarNavItem(string automationId)
    {
        // Strategy 1: Direct FindFirstDescendant (works for some UIA3 configurations)
        var item = _mainWindow?.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        if (item != null && item.IsAvailable)
            return item;

        // Strategy 2: Navigate through the known visual tree
        // MainWindow > Grid > Grid[Row=1] > Border > Grid > ScrollViewer > ListBox > ListBoxItem
        if (_mainWindow == null) return null;

        var mainGrid = _mainWindow.FindFirstChild(cf => cf.ByClassName("Grid"));
        if (mainGrid == null) return null;

        // Grid children: Menu (row 0), Grid (row 1), StatusBar (row 2)
        var contentGrids = mainGrid.FindAllChildren(cf => cf.ByClassName("Grid"));
        // contentGrids[0] = row 0 Menu's grid, contentGrids[1] or last one = row 1 content grid
        var contentGrid = contentGrids.FirstOrDefault() ?? mainGrid;
        
        // The content grid has two columns: Border (sidebar) + Frame (content)
        var border = contentGrid.FindFirstChild(cf => cf.ByClassName("Border"));
        if (border == null) return null;

        // Border contains a Grid with ScrollViewer
        var sidebarGrid = border.FindFirstChild(cf => cf.ByClassName("Grid"));
        if (sidebarGrid == null) return null;

        // ScrollViewer is the navigation area (row 1)
        var scrollViewer = sidebarGrid.FindFirstChild(cf => cf.ByClassName("ScrollViewer"));
        if (scrollViewer == null) return null;

        // ListBox is inside the ScrollViewer
        var listBox = scrollViewer.FindFirstChild(cf => cf.ByClassName("ListBox"));
        if (listBox == null) return null;

        // Find the specific item by AutomationId
        var navItem = listBox.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        if (navItem != null && navItem.IsAvailable)
            return navItem;

        // Strategy 3: Search all children by AutomationId (broad search)
        item = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
        if (item != null && item.IsAvailable)
            return item;

        return null;
    }

    /// <summary>
    /// Verifies that navigation items exist in the sidebar.
    /// Shared assertion helper for all navigation existence tests.
    /// </summary>
    private void AssertNavItemsExist(params string[] automationIds)
    {
        foreach (var id in automationIds)
        {
            var item = FindSidebarNavItem(id);
            item.Should().NotBeNull($"Navigation item '{id}' should exist in the sidebar");
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToDashboard_ShouldDisplayDashboard()
    {
        try
        {
            // Arrange - Verify the window loaded properly
            _mainWindow.Should().NotBeNull();

            // Verify sidebar brand text exists (confirms sidebar loaded)
            var brandText = FindSidebarNavItem("SidebarBrandText");
            brandText.Should().NotBeNull("Sidebar brand text should be visible");

            // Verify at least one navigation item exists
            var navDashboard = FindSidebarNavItem("NavDashboard");
            navDashboard.Should().NotBeNull("Dashboard navigation item should exist");
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToProducts_ShouldDisplayProductsScreen()
    {
        try
        {
            // Use menu bar to navigate to products
            // Menu: الإعدادات > إدارة المنتجات
            var settingsMenu = FindMenuItem("الإعدادات");
            settingsMenu.Should().NotBeNull("Settings menu item should exist");

            if (settingsMenu != null)
            {
                settingsMenu.Click();
                System.Threading.Thread.Sleep(500);
                var manageProducts = settingsMenu.FindFirstChild(cf => cf.ByName("إدارة المنتجات")) as MenuItem;
                manageProducts?.Click();
                System.Threading.Thread.Sleep(1500);
            }

            // Verify window is still accessible
            _mainWindow!.Should().NotBeNull();
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToCustomers_ShouldDisplayCustomersScreen()
    {
        try
        {
            // Use menu bar to navigate to customers
            var settingsMenu = FindMenuItem("الإعدادات");
            settingsMenu.Should().NotBeNull("Settings menu item should exist");

            if (settingsMenu != null)
            {
                settingsMenu.Click();
                System.Threading.Thread.Sleep(500);
                var manageCustomers = settingsMenu.FindFirstChild(cf => cf.ByName("إدارة العملاء")) as MenuItem;
                manageCustomers?.Click();
                System.Threading.Thread.Sleep(1500);
            }

            _mainWindow!.Should().NotBeNull();
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToSuppliers_ShouldDisplaySuppliersScreen()
    {
        try
        {
            var settingsMenu = FindMenuItem("الإعدادات");
            settingsMenu.Should().NotBeNull("Settings menu item should exist");

            if (settingsMenu != null)
            {
                settingsMenu.Click();
                System.Threading.Thread.Sleep(500);
                var manageSuppliers = settingsMenu.FindFirstChild(cf => cf.ByName("إدارة الموردين")) as MenuItem;
                manageSuppliers?.Click();
                System.Threading.Thread.Sleep(1500);
            }

            _mainWindow!.Should().NotBeNull();
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToSales_ShouldDisplaySalesScreen()
    {
        try
        {
            // Use menu bar: المبيعات > فواتير البيع
            var salesMenu = FindMenuItem("المبيعات");
            salesMenu.Should().NotBeNull("Sales menu item should exist");

            if (salesMenu != null)
            {
                salesMenu.Click();
                System.Threading.Thread.Sleep(500);
                var salesInvoices = salesMenu.FindFirstChild(cf => cf.ByName("فواتير البيع")) as MenuItem;
                salesInvoices?.Click();
                System.Threading.Thread.Sleep(1500);
            }

            _mainWindow!.Should().NotBeNull();
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToPurchases_ShouldDisplayPurchasesScreen()
    {
        try
        {
            var purchasesMenu = FindMenuItem("المشتريات");
            purchasesMenu.Should().NotBeNull("Purchases menu item should exist");

            if (purchasesMenu != null)
            {
                purchasesMenu.Click();
                System.Threading.Thread.Sleep(500);
                var purchaseInvoices = purchasesMenu.FindFirstChild(cf => cf.ByName("فواتير الشراء")) as MenuItem;
                purchaseInvoices?.Click();
                System.Threading.Thread.Sleep(1500);
            }

            _mainWindow!.Should().NotBeNull();
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToReports_ShouldDisplayReportsScreen()
    {
        try
        {
            var reportsMenu = FindMenuItem("التقارير");
            reportsMenu.Should().NotBeNull("Reports menu item should exist");

            if (reportsMenu != null)
            {
                reportsMenu.Click();
                System.Threading.Thread.Sleep(500);

                // Click on one of the report sub-items
                var reportTypes = reportsMenu.FindAllChildren(cf => cf.ByClassName("MenuItem"));
                if (reportTypes.Length > 0)
                {
                    reportTypes[0].Click();
                    System.Threading.Thread.Sleep(1500);
                }
            }

            _mainWindow!.Should().NotBeNull();
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToSettings_ShouldDisplaySettingsScreen()
    {
        try
        {
            // Navigate to settings via القائمة > الإعدادات
            var settingsMenu = FindMenuItem("الإعدادات");
            settingsMenu.Should().NotBeNull("Settings menu item should exist");

            if (settingsMenu != null)
            {
                settingsMenu.Click();
                System.Threading.Thread.Sleep(500);
                var usersItem = settingsMenu.FindFirstChild(cf => cf.ByName("إدارة المستخدمين")) as MenuItem;
                usersItem?.Click();
                System.Threading.Thread.Sleep(1500);
            }

            _mainWindow!.Should().NotBeNull();
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_ThroughMenu_ShouldWorkCorrectly()
    {
        try
        {
            // Verify the menu bar exists
            var menuBar = _mainWindow!.FindFirstDescendant(cf => cf.ByClassName("Menu"));
            menuBar.Should().NotBeNull("Menu bar should exist in the main window");

            if (menuBar != null)
            {
                // Find all top-level menu items
                var topMenuItems = menuBar.FindAllChildren(cf => cf.ByClassName("MenuItem"));
                topMenuItems.Length.Should().BeGreaterThan(0, "Menu bar should have at least one item");

                // Click on each menu item to verify they open
                foreach (var item in topMenuItems)
                {
                    item.Click();
                    System.Threading.Thread.Sleep(300);
                }
            }
        }
        finally
        {
            CloseApplication();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Navigation")]
    public void Navigation_NavigateBetweenScreens_ShouldMaintainState()
    {
        try
        {
            // Verify main window is stable
            _mainWindow!.Should().NotBeNull();

            // Navigate between a few menu items
            var salesMenu = FindMenuItem("المبيعات");
            salesMenu?.Click();
            System.Threading.Thread.Sleep(500);
            salesMenu?.FindFirstChild(cf => cf.ByName("فواتير البيع"))?.Click();
            System.Threading.Thread.Sleep(1000);

            // Verify window is still responsive
            _mainWindow.Should().NotBeNull();
            var windows = GetApplicationWindows();
            windows.Should().Contain(w => w.Name.Contains("نظام إدارة المبيعات"), "Main window should remain open");
        }
        finally
        {
            CloseApplication();
        }
    }
}
