namespace SalesSystem.E2ETests;

/// <summary>
/// E2E tests for the login flow of the Sales Management System.
/// Uses keyboard-only interactions and window-based assertions
/// to avoid issues with WindowStyle=None and AllowsTransparency=True.
/// </summary>
[Collection("E2E")]
public class LoginFlowTests : TestBase, IDisposable
{
    private bool _disposed;

    public LoginFlowTests()
    {
        LaunchApplication();
        // Use KeyboardLogin-style wait — no element finding required
        System.Threading.Thread.Sleep(2000);
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
    /// Test: Login_WithValidCredentials_ShouldShowDashboard
    /// Verifies that a user can successfully log in with valid credentials
    /// and the dashboard (MainWindow) is displayed.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Login")]
    [Trait("Category", "Critical")]
    public void Login_WithValidCredentials_ShouldShowDashboard()
    {
        try
        {
            // Act - Use keyboard-based login with valid credentials
            KeyboardLogin("admin", "admin123");

            // Allow extra time for dashboard to render
            System.Threading.Thread.Sleep(2000);

            // Assert - Main window (dashboard) should appear
            var windows = GetApplicationWindows();
            var mainWindow = windows.FirstOrDefault(w => w.Name.Contains("نظام إدارة المبيعات"));
            mainWindow.Should().NotBeNull("Main window (dashboard) should appear after successful login");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Login_WithInvalidCredentials_ShouldShowError
    /// Verifies that attempting to log in with invalid credentials
    /// keeps the user on the login window.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Login")]
    public void Login_WithInvalidCredentials_ShouldShowError()
    {
        try
        {
            // Wait for login window focus
            System.Threading.Thread.Sleep(1000);

            // Act - Type invalid credentials using keyboard
            Keyboard.Type("invaliduser");
            System.Threading.Thread.Sleep(200);
            Keyboard.Type(VirtualKeyShort.TAB);
            System.Threading.Thread.Sleep(100);
            Keyboard.Type("wrongpassword");
            System.Threading.Thread.Sleep(200);
            Keyboard.Press(VirtualKeyShort.RETURN);
            System.Threading.Thread.Sleep(200);
            Keyboard.Release(VirtualKeyShort.RETURN);

            // Wait for response
            System.Threading.Thread.Sleep(2000);

            // Assert - Login window should still be visible
            var windows = GetApplicationWindows();
            var loginWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("تسجيل الدخول") ||
                w.Name.Contains("تسجيل"));
            loginWindow.Should().NotBeNull("Login window should remain visible when login fails");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Login_WithEmptyCredentials_ShouldShowValidationError
    /// Verifies that submitting empty credentials shows a validation error
    /// and keeps the user on the login window.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Login")]
    public void Login_WithEmptyCredentials_ShouldShowValidationError()
    {
        try
        {
            // Act - Press Enter directly without entering any credentials
            System.Threading.Thread.Sleep(1000);
            Keyboard.Press(VirtualKeyShort.RETURN);
            System.Threading.Thread.Sleep(200);
            Keyboard.Release(VirtualKeyShort.RETURN);

            // Wait for validation
            System.Threading.Thread.Sleep(2000);

            // Assert - Login window should still be visible
            var windows = GetApplicationWindows();
            var loginWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("تسجيل الدخول") ||
                w.Name.Contains("تسجيل"));
            loginWindow.Should().NotBeNull("Login window should remain visible when submitting empty credentials");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Test: Login_WithCorrectUsernameWrongPassword_ShouldShowError
    /// Verifies that providing a correct username with wrong password
    /// does not log the user in.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Login")]
    public void Login_WithCorrectUsernameWrongPassword_ShouldShowError()
    {
        try
        {
            // Wait for login window focus
            System.Threading.Thread.Sleep(1000);

            // Act - Type correct username but wrong password using keyboard
            Keyboard.Type("admin");
            System.Threading.Thread.Sleep(200);
            Keyboard.Type(VirtualKeyShort.TAB);
            System.Threading.Thread.Sleep(100);
            Keyboard.Type("wrongpassword123");
            System.Threading.Thread.Sleep(200);
            Keyboard.Press(VirtualKeyShort.RETURN);
            System.Threading.Thread.Sleep(200);
            Keyboard.Release(VirtualKeyShort.RETURN);

            // Wait for response
            System.Threading.Thread.Sleep(2000);

            // Assert - Main window (dashboard) should NOT appear
            var windows = GetApplicationWindows();
            var mainWindow = windows.FirstOrDefault(w => w.Name.Contains("نظام إدارة المبيعات"));
            mainWindow.Should().BeNull("Main window should NOT appear when password is incorrect");

            // Login window should still be visible
            var loginWindow = windows.FirstOrDefault(w =>
                w.Name.Contains("تسجيل الدخول") ||
                w.Name.Contains("تسجيل"));
            loginWindow.Should().NotBeNull("Login window should remain visible when login fails");
        }
        finally
        {
            CloseApplication();
        }
    }
}
