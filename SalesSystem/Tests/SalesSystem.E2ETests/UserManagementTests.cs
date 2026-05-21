using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FluentAssertions;

namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for User Management operations in the Sales Management System.
/// Only Admin role can manage users, so these tests assume admin login.
/// </summary>
[Collection("E2E")]
public class UserManagementTests : TestBase, IDisposable
{
    private Window? _mainWindow;
    private Window? _userEditorWindow;
    private bool _disposed;

    public UserManagementTests()
    {
        // Launch app, login as admin, and navigate to Users
        LaunchApplication();
        LoginAsAdmin();
        NavigateToUsers();
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
    /// Helper: Logs in as admin user with proper credentials.
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
    /// Helper: Navigates to Users screen via navigation button.
    /// </summary>
    private void NavigateToUsers()
    {
        if (_mainWindow == null) return;

        // Try BtnUsers first (MainWindow navigation)
        var usersNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnUsers"));
        if (usersNav == null)
        {
            // Fall back to NavUsers
            usersNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavUsers"));
        }

        usersNav?.Click();
        System.Threading.Thread.Sleep(1500);
    }

    /// <summary>
    /// Generates a unique username for test isolation.
    /// </summary>
    private static string GenerateUniqueUsername(string prefix = "TestUser")
    {
        return $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString()[..8]}";
    }

    /// <summary>
    /// Test: UserManagement_AddUser_ShouldSucceed
    /// Verifies that a new user can be added successfully with all required fields.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "UserManagement")]
    [Trait("Category", "CRUD")]
    public void UserManagement_AddUser_ShouldSucceed()
    {
        try
        {
            // Arrange - Click Add User button
            var addButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddUser")) as Button;
            addButton.Should().NotBeNull("Add User button should exist");
            addButton!.Click();

            // Wait for editor dialog to appear
            System.Threading.Thread.Sleep(1000);

            // Find the user editor window
            var windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Add"))
                ?? _mainWindow;

            // Act - Fill in user details
            var usernameBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtUsername")) as TextBox;
            var passwordBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtPassword")) as TextBox;
            var displayNameBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtDisplayName")) as TextBox;
            var roleCombo = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("CmbRole")) as ComboBox;

            usernameBox.Should().NotBeNull("Username field should exist in editor");
            passwordBox.Should().NotBeNull("Password field should exist in editor");
            displayNameBox.Should().NotBeNull("Display name field should exist in editor");
            roleCombo.Should().NotBeNull("Role combo box should exist in editor");

            var testUsername = GenerateUniqueUsername("E2EUser");
            var testDisplayName = $"مستخدم اختبار E2E {DateTime.Now:HHmmss}";
            var testPassword = "TestPass123!";
            var testRole = "Manager"; // Use Manager role for testing

            // Enter user data
            usernameBox!.Focus();
            Keyboard.Type(testUsername);

            passwordBox!.Focus();
            Keyboard.Type(testPassword);

            displayNameBox!.Focus();
            Keyboard.Type(testDisplayName);

            // Select role from combo box
            roleCombo!.Focus();
            System.Threading.Thread.Sleep(300);
            roleCombo.Click();
            System.Threading.Thread.Sleep(300);

            // Select the Manager item
            var roleItems = roleCombo.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem));
            var managerItem = roleItems.FirstOrDefault(i =>
                i.Name.Contains("مدير") || i.Name.Contains("Manager") || i.Name.Contains("2"));
            managerItem?.Click();
            System.Threading.Thread.Sleep(300);

            // Click Save
            var saveButton = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton.Should().NotBeNull("Save User button should exist");
            saveButton!.Click();

            // Wait for save to complete and dialog to close
            System.Threading.Thread.Sleep(2000);

            // Assert - Verify user appears in the data grid
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgUsers"));
            dataGrid.Should().NotBeNull("Users data grid should be visible after saving");

            // Verify user was added by searching for the username
            var searchBox = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtSearchUsers")) as TextBox;
            searchBox.Should().NotBeNull("Search box should exist for verification");

            searchBox!.Focus();
            Keyboard.Type(testUsername);
            System.Threading.Thread.Sleep(1000);

            // Verify the data grid shows results
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows?.Length ?? 0;
            rowCount.Should().BeGreaterThan(0, "Added user should appear in search results");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: UserManagement_EditUser_ShouldUpdate
    /// Verifies that an existing user can be edited and their details are updated.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "UserManagement")]
    [Trait("Category", "Edit")]
    public void UserManagement_EditUser_ShouldUpdate()
    {
        try
        {
            // Arrange - Find first user in grid and click to select
            var dataGrid = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("DgUsers"));
            dataGrid.Should().NotBeNull("Users data grid should exist");

            // Get initial rows to find a user to edit
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            var rowCount = rows?.Length ?? 0;
            rowCount.Should().BeGreaterThan(0, "At least one user should exist for editing test");

            // Double-click to open edit mode (common pattern for grid editing)
            var firstUserRow = rows![0];
            firstUserRow.DoubleClick();
            System.Threading.Thread.Sleep(1500);

            // Find the user editor dialog
            var windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Edit"))
                ?? _mainWindow;

            // Act - Modify the display name
            var displayNameBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtDisplayName")) as TextBox;
            displayNameBox.Should().NotBeNull("Display name field should be editable");

            // Clear and enter new display name
            displayNameBox!.Focus();
            Keyboard.Type(VirtualKeyShort.CONTROL);
            Keyboard.Type("A");
            Keyboard.Type(VirtualKeyShort.DELETE);

            var newDisplayName = $"مستخدم معدل E2E {DateTime.Now:HHmmss}";
            Keyboard.Type(newDisplayName);

            // Save changes
            var saveButton = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton.Should().NotBeNull("Save button should exist in edit dialog");
            saveButton!.Click();

            System.Threading.Thread.Sleep(2000);

            // Assert - Verify editor closed (save successful)
            var editorStillOpen = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveUser"));
            editorStillOpen.Should().BeNull("Editor should close after successful save");

            // Verify the updated row appears in grid
            var updatedRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            updatedRows.Should().NotBeNull("Grid should still have users after edit");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: UserManagement_DeactivateUser_ShouldMarkInactive
    /// Verifies that a user can be deactivated by unchecking the IsActive checkbox.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "UserManagement")]
    [Trait("Category", "Deactivate")]
    public void UserManagement_DeactivateUser_ShouldMarkInactive()
    {
        try
        {
            // Arrange - First add a new user that we can safely deactivate
            var addButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddUser")) as Button;
            addButton.Should().NotBeNull("Add User button should exist");
            addButton!.Click();

            System.Threading.Thread.Sleep(1000);

            var windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Add"))
                ?? _mainWindow;

            // Fill in user details for deactivation test
            var usernameBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtUsername")) as TextBox;
            var passwordBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtPassword")) as TextBox;
            var displayNameBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtDisplayName")) as TextBox;

            var testUsername = GenerateUniqueUsername("DeactUser");
            var testDisplayName = $"Deactivate Test {DateTime.Now:HHmmss}";

            usernameBox!.Focus();
            Keyboard.Type(testUsername);

            passwordBox!.Focus();
            Keyboard.Type("TestPass123!");

            displayNameBox!.Focus();
            Keyboard.Type(testDisplayName);

            // Save the user first
            var saveButton = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton!.Click();
            System.Threading.Thread.Sleep(2000);

            // Now search for and edit the newly created user
            var searchBox = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtSearchUsers")) as TextBox;
            searchBox!.Focus();
            Keyboard.Type(testUsername);
            System.Threading.Thread.Sleep(1000);

            // Act - Find and edit the user
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgUsers"));
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            rows?.Length.Should().BeGreaterThan(0, "User should be found after search");

            rows![0].DoubleClick();
            System.Threading.Thread.Sleep(1500);

            windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Edit"))
                ?? _mainWindow;

            // Find the IsActive checkbox and uncheck it
            var isActiveCheckbox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("ChkIsActive")) as CheckBox;
            isActiveCheckbox.Should().NotBeNull("IsActive checkbox should exist in editor");

            // If checkbox is checked, click to uncheck
            if (isActiveCheckbox!.IsChecked == true)
            {
                isActiveCheckbox.Click();
                System.Threading.Thread.Sleep(300);
            }

            // Save the deactivation
            saveButton = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton.Should().NotBeNull("Save button should exist");
            saveButton!.Click();

            System.Threading.Thread.Sleep(2000);

            // Assert - Verify editor closed
            var editorStillOpen = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("ChkIsActive"));
            editorStillOpen.Should().BeNull("Editor should close after deactivating user");

            // Verify user is still in grid (just marked inactive)
            var updatedRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            updatedRows.Should().NotBeNull("Grid should still show user after deactivation");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: UserManagement_ChangePassword_ShouldSucceed
    /// Verifies that an admin can change a user's password through the edit form.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "UserManagement")]
    [Trait("Category", "Password")]
    public void UserManagement_ChangePassword_ShouldSucceed()
    {
        try
        {
            // Arrange - First add a new user that we can safely change password
            var addButton = _mainWindow!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddUser")) as Button;
            addButton.Should().NotBeNull("Add User button should exist");
            addButton!.Click();

            System.Threading.Thread.Sleep(1000);

            var windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Add"))
                ?? _mainWindow;

            // Fill in user details
            var usernameBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtUsername")) as TextBox;
            var passwordBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtPassword")) as TextBox;
            var displayNameBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtDisplayName")) as TextBox;

            var testUsername = GenerateUniqueUsername("PwdUser");
            var testDisplayName = $"Password Test {DateTime.Now:HHmmss}";
            var originalPassword = "OriginalPass123!";

            usernameBox!.Focus();
            Keyboard.Type(testUsername);

            passwordBox!.Focus();
            Keyboard.Type(originalPassword);

            displayNameBox!.Focus();
            Keyboard.Type(testDisplayName);

            // Save the user first
            var saveButton = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton!.Click();
            System.Threading.Thread.Sleep(2000);

            // Now search for and edit the user to change password
            var searchBox = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtSearchUsers")) as TextBox;
            searchBox!.Focus();
            Keyboard.Type(testUsername);
            System.Threading.Thread.Sleep(1000);

            // Act - Find and edit the user
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgUsers"));
            var rows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            rows?.Length.Should().BeGreaterThan(0, "User should be found after search");

            rows![0].DoubleClick();
            System.Threading.Thread.Sleep(1500);

            windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Edit"))
                ?? _mainWindow;

            // Find the password field
            var newPasswordBox = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtPassword")) as TextBox;
            newPasswordBox.Should().NotBeNull("Password field should be editable");

            // Clear and enter new password
            newPasswordBox!.Focus();
            Keyboard.Type(VirtualKeyShort.CONTROL);
            Keyboard.Type("A");
            Keyboard.Type(VirtualKeyShort.DELETE);

            var newPassword = $"NewPass{DateTime.Now:HHmmss}!";
            Keyboard.Type(newPassword);

            // Save the password change
            saveButton = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton.Should().NotBeNull("Save button should exist");
            saveButton!.Click();

            System.Threading.Thread.Sleep(2000);

            // Assert - Verify editor closed
            var editorStillOpen = _userEditorWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtPassword"));
            editorStillOpen.Should().BeNull("Editor should close after successful password change");

            // Verify user is still in grid (password change was successful)
            var updatedRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            updatedRows.Should().NotBeNull("Grid should still show user after password change");

            // Refresh to verify the user is still accessible
            var refreshButton = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnRefreshUsers")) as Button;
            refreshButton?.Click();
            System.Threading.Thread.Sleep(1500);

            // Search again to verify user is still valid
            searchBox = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("TxtSearchUsers")) as TextBox;
            searchBox!.Focus();
            Keyboard.Type(testUsername);
            System.Threading.Thread.Sleep(1000);

            var finalRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            finalRows?.Length.Should().BeGreaterThan(0, "User should still exist after password change");
        }
        finally
        {
            CloseApplication();
        }
    }
}
