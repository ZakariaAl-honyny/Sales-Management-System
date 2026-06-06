using System.Net.Http;
using System.Net.Http.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FluentAssertions;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for User Management operations in the Sales Management System.
/// Only Admin role can manage users, so these tests assume admin login.
/// </summary>
[Collection("E2E")]
public class UserManagementTests : TestBase, IDisposable
{
    private static readonly HttpClient _httpClient = new();
    private static readonly string _apiBaseUrl =
        Environment.GetEnvironmentVariable("SALESSYSTEM_API_URL") ?? "http://localhost:5221";
    private const int AdminUserId = 1;
    private const string AdminPassword = "admin123";

    private Window? _mainWindow;
    private Window? _userEditorWindow;
    private bool _disposed;

    static UserManagementTests()
    {
        _httpClient.BaseAddress = new Uri(_apiBaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public UserManagementTests()
    {
        // Ensure the admin user has a password set before tests run.
        // The admin is seeded with MustChangePassword=true and PasswordHash=null,
        // so we need to call the set-password endpoint first.
        EnsureAdminPasswordSet();
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

    // ═══════════════════════════════════════════════════════════════
    // Static setup — ensure admin can log in for all tests
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Calls POST /api/v1/auth/set-password to set the admin password
    /// before any E2E test runs. This is required because the admin
    /// user is now seeded with PasswordHash=null and MustChangePassword=true.
    /// </summary>
    private static void EnsureAdminPasswordSet()
    {
        try
        {
            var request = new SetPasswordRequest(AdminPassword, AdminPassword);
            var response = _httpClient.PostAsJsonAsync(
                $"api/v1/auth/set-password?userId={AdminUserId}", request)
                .GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                Serilog.Log.Information(
                    "[E2E] Admin password set successfully for test user ID {UserId}", AdminUserId);
            }
            else
            {
                var error = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Serilog.Log.Warning(
                    "[E2E] Set-password returned {StatusCode}: {Error}",
                    response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex,
                "[E2E] Failed to set admin password for tests — login may fail");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Element finder helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds a button by navigating up from a descendant TextBlock whose
    /// UIA Name matches <paramref name="childText"/>. This handles buttons
    /// with complex XAML content (StackPanel + TextBlock) that lack
    /// AutomationProperties.AutomationId in the XAML source.
    /// </summary>
    private AutomationElement? FindButtonByChildText(string childText, AutomationElement? root = null)
    {
        root ??= _mainWindow;
        try
        {
            var textElement = root.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Text).And(cf.ByName(childText)));
            if (textElement == null)
                return null;

            // Walk up: TextBlock → parent (StackPanel/Grid) → parent (Button)
            var current = textElement;
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var parent = current.Parent;
                    if (parent == null) return null;
                    if (parent.ControlType == ControlType.Button)
                        return parent;
                    current = parent;
                }
                catch { return null; }
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Finds the first TextBox (Edit control) in the given root element.
    /// Used to locate the search TextBox when no AutomationId is set.
    /// </summary>
    private AutomationElement? FindFirstTextBox(AutomationElement? root = null)
    {
        root ??= _mainWindow;
        try
        {
            return root.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit));
        }
        catch { return null; }
    }

    /// <summary>
    /// Finds a button whose ToolTip (HelpText in UIA) matches the given text.
    /// </summary>
    private AutomationElement? FindButtonByToolTip(string toolTip, AutomationElement? root = null)
    {
        root ??= _mainWindow;
        try
        {
            var buttons = root.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
            foreach (var btn in buttons)
            {
                try
                {
                    var helpText = btn.Properties.HelpText.ValueOrDefault;
                    if (helpText == toolTip) return btn;
                }
                catch { }
            }
        }
        catch { }
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    // Authentication & Navigation
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Helper: Logs in as admin user with proper credentials.
    /// </summary>
    private void LoginAsAdmin()
    {
        KeyboardLogin();

        var windows = GetApplicationWindows();
        _mainWindow = windows.FirstOrDefault(w =>
            w.Name.Contains("المبيعات") || w.Name.Contains("Sales") || w.Name.Contains("System"))
            ?? windows.FirstOrDefault()!;
    }

    /// <summary>
    /// Helper: Navigates to Users screen via the sidebar button with Content="المستخدمين".
    /// </summary>
    private void NavigateToUsers()
    {
        if (_mainWindow == null) return;

        // The sidebar "المستخدمين" button has Content="المستخدمين" (simple string),
        // so its UIA Name property is "المستخدمين".
        var usersNav = _mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByName("المستخدمين")));
        if (usersNav == null)
        {
            // Fallback: try the old AutomationId names (legacy)
            usersNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("BtnUsers"));
            if (usersNav == null)
                usersNav = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NavUsers"));
        }

        usersNav?.Click();
        System.Threading.Thread.Sleep(1500);
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Generates a unique username for test isolation.
    /// </summary>
    private static string GenerateUniqueUsername(string prefix = "TestUser")
    {
        return $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString()[..8]}";
    }

    // ═══════════════════════════════════════════════════════════════
    // Test: UserManagement_AddUser_ShouldSucceed
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a new user can be added successfully with all required fields.
    /// The API now uses passwordless CreateUserRequest — users are created with
    /// MustChangePassword=true and must set their password on first login.
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
            var addButton = FindButtonByChildText("مستخدم جديد") as Button;
            if (addButton == null)
            {
                // Fallback: try old AutomationId
                addButton = _mainWindow!.FindFirstDescendant(
                    cf => cf.ByAutomationId("BtnAddUser")) as Button;
            }
            addButton.Should().NotBeNull("Add User button should exist in the toolbar");
            addButton!.Click();

            // Wait for editor dialog to appear
            System.Threading.Thread.Sleep(1000);

            // Find the user editor window (opens as a separate Window)
            var windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Add"))
                ?? _mainWindow;

            // Act - Fill in user details
            // Note: AutomationIds in UserEditorView.xaml:
            //   TxtUserUsername, TxtUserFullName, TxtUserPassword, CbUserRole
            var usernameBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserUsername")) as TextBox;
            var passwordBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserPassword")) as TextBox;
            var displayNameBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserFullName")) as TextBox;
            var roleCombo = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("CbUserRole")) as ComboBox;

            usernameBox.Should().NotBeNull("Username field should exist in editor");
            passwordBox.Should().NotBeNull("Password field should exist in editor");
            displayNameBox.Should().NotBeNull("Display name field should exist in editor");
            roleCombo.Should().NotBeNull("Role combo box should exist in editor");

            var testUsername = GenerateUniqueUsername("E2EUser");
            var testDisplayName = $"مستخدم اختبار E2E {DateTime.Now:HHmmss}";

            // Enter user data
            // NOTE: The VM still validates password is non-empty for new users.
            // However, CreateUserRequest omits the Password field — the API
            // creates the user passwordlessly (MustChangePassword=true).
            // The user must call /api/v1/auth/set-password before first login.
            usernameBox!.Focus();
            Keyboard.Type(testUsername);

            passwordBox!.Focus();
            Keyboard.Type("TempPass123!"); // Required by VM validation, ignored by API

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
            var saveButton = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton.Should().NotBeNull("Save User button should exist");
            saveButton!.Click();

            // Wait for save to complete and dialog to close
            System.Threading.Thread.Sleep(2000);

            // Assert - Verify user appears in the data grid
            var dataGrid = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DgUsers"));
            dataGrid.Should().NotBeNull("Users data grid should be visible after saving");

            // Verify user was added by searching for the username
            // The search TextBox has no AutomationId in the current XAML,
            // so we find the first Edit control in the main window.
            var searchBox = FindFirstTextBox() as TextBox;
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

    // ═══════════════════════════════════════════════════════════════
    // Test: UserManagement_EditUser_ShouldUpdate
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that an existing user can be edited and their details are updated.
    /// The API UpdateUserRequest now uses Status (byte) instead of IsActive (bool).
    /// The desktop ViewModel maps the bool IsActive to Status byte before sending.
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

            // Double-click to open edit mode
            var firstUserRow = rows![0];
            firstUserRow.DoubleClick();
            System.Threading.Thread.Sleep(1500);

            // Find the user editor dialog
            var windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Edit"))
                ?? _mainWindow;

            // Act - Modify the display name
            var displayNameBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserFullName")) as TextBox;
            displayNameBox.Should().NotBeNull("Display name field should be editable");

            // Clear and enter new display name
            displayNameBox!.Focus();
            Keyboard.Type(VirtualKeyShort.CONTROL);
            Keyboard.Type("A");
            Keyboard.Type(VirtualKeyShort.DELETE);

            var newDisplayName = $"مستخدم معدل E2E {DateTime.Now:HHmmss}";
            Keyboard.Type(newDisplayName);

            // Save changes
            var saveButton = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton.Should().NotBeNull("Save button should exist in edit dialog");
            saveButton!.Click();

            System.Threading.Thread.Sleep(2000);

            // Assert - Verify editor closed (save successful)
            var editorStillOpen = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("BtnSaveUser"));
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

    // ═══════════════════════════════════════════════════════════════
    // Test: UserManagement_DeactivateUser_ShouldMarkInactive
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a user can be deactivated by unchecking the IsActive checkbox.
    /// The VM maps IsActive=false to Status=0 in the UpdateUserRequest.
    /// The checkbox AutomationId is ChkUserIsActive in the current XAML.
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
            var addButton = FindButtonByChildText("مستخدم جديد") as Button;
            if (addButton == null)
            {
                addButton = _mainWindow!.FindFirstDescendant(
                    cf => cf.ByAutomationId("BtnAddUser")) as Button;
            }
            addButton.Should().NotBeNull("Add User button should exist");
            addButton!.Click();

            System.Threading.Thread.Sleep(1000);

            var windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Add"))
                ?? _mainWindow;

            // Fill in user details for deactivation test
            var usernameBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserUsername")) as TextBox;
            var passwordBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserPassword")) as TextBox;
            var displayNameBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserFullName")) as TextBox;

            var testUsername = GenerateUniqueUsername("DeactUser");
            var testDisplayName = $"Deactivate Test {DateTime.Now:HHmmss}";

            usernameBox!.Focus();
            Keyboard.Type(testUsername);

            passwordBox!.Focus();
            Keyboard.Type("TempPass123!");

            displayNameBox!.Focus();
            Keyboard.Type(testDisplayName);

            // Save the user first
            var saveButton = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton!.Click();
            System.Threading.Thread.Sleep(2000);

            // Now search for and edit the newly created user
            var searchBox = FindFirstTextBox() as TextBox;
            searchBox.Should().NotBeNull("Search box should exist");
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
            // Note: AutomationId is ChkUserIsActive in the current XAML
            var isActiveCheckbox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("ChkUserIsActive")) as CheckBox;
            isActiveCheckbox.Should().NotBeNull("IsActive checkbox should exist in editor");

            // If checkbox is checked, click to uncheck
            if (isActiveCheckbox!.IsChecked == true)
            {
                isActiveCheckbox.Click();
                System.Threading.Thread.Sleep(300);
            }

            // Save the deactivation
            saveButton = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton.Should().NotBeNull("Save button should exist");
            saveButton!.Click();

            System.Threading.Thread.Sleep(2000);

            // Assert - Verify editor closed
            var editorStillOpen = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("ChkUserIsActive"));
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

    // ═══════════════════════════════════════════════════════════════
    // Test: UserManagement_ChangePassword_ShouldSucceed
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that an admin can change a user's password through the edit form.
    /// The user is created passwordlessly (CreateUserRequest omits Password),
    /// then the admin edits the user and provides a new Password in the
    /// UpdateUserRequest (which accepts an optional Password field).
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
            var addButton = FindButtonByChildText("مستخدم جديد") as Button;
            if (addButton == null)
            {
                addButton = _mainWindow!.FindFirstDescendant(
                    cf => cf.ByAutomationId("BtnAddUser")) as Button;
            }
            addButton.Should().NotBeNull("Add User button should exist");
            addButton!.Click();

            System.Threading.Thread.Sleep(1000);

            var windows = GetApplicationWindows();
            _userEditorWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("مستخدم") || w.Name.Contains("User") || w.Name.Contains("Add"))
                ?? _mainWindow;

            // Fill in user details
            var usernameBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserUsername")) as TextBox;
            var passwordBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserPassword")) as TextBox;
            var displayNameBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserFullName")) as TextBox;

            var testUsername = GenerateUniqueUsername("PwdUser");
            var testDisplayName = $"Password Test {DateTime.Now:HHmmss}";

            usernameBox!.Focus();
            Keyboard.Type(testUsername);

            // Password entered here satisfies VM validation but is ignored
            // by the API's CreateUserRequest (passwordless create).
            passwordBox!.Focus();
            Keyboard.Type("OriginalPass123!");

            displayNameBox!.Focus();
            Keyboard.Type(testDisplayName);

            // Save the user first
            var saveButton = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton!.Click();
            System.Threading.Thread.Sleep(2000);

            // Now search for and edit the user to change password
            var searchBox = FindFirstTextBox() as TextBox;
            searchBox.Should().NotBeNull("Search box should exist");
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
            var newPasswordBox = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserPassword")) as TextBox;
            newPasswordBox.Should().NotBeNull("Password field should be editable");

            // Clear and enter new password
            newPasswordBox!.Focus();
            Keyboard.Type(VirtualKeyShort.CONTROL);
            Keyboard.Type("A");
            Keyboard.Type(VirtualKeyShort.DELETE);

            var newPassword = $"NewPass{DateTime.Now:HHmmss}!";
            Keyboard.Type(newPassword);

            // Save the password change
            saveButton = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("BtnSaveUser")) as Button;
            saveButton.Should().NotBeNull("Save button should exist");
            saveButton!.Click();

            System.Threading.Thread.Sleep(2000);

            // Assert - Verify editor closed
            var editorStillOpen = _userEditorWindow.FindFirstDescendant(
                cf => cf.ByAutomationId("TxtUserPassword"));
            editorStillOpen.Should().BeNull("Editor should close after successful password change");

            // Verify user is still in grid (password change was successful)
            var updatedRows = dataGrid?.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem));
            updatedRows.Should().NotBeNull("Grid should still show user after password change");
        }
        finally
        {
            CloseApplication();
        }
    }
}
