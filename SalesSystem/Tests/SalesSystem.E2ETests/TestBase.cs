using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA2;
using FluentAssertions;

namespace SalesSystem.E2ETests;

/// <summary>
/// Base class for all E2E tests providing WPF application launch and automation setup.
/// </summary>
public abstract class TestBase : IDisposable
{
    protected const int DefaultTimeoutMs = 5000;
    protected const int RetryDelayMs = 500;
    protected const int MaxRetries = 10;

    private Application? _application;
    private UIA2Automation? _automation;
    private bool _disposed;

    /// <summary>
    /// Gets the UIA3 automation instance.
    /// </summary>
    protected UIA2Automation Automation => _automation!;

    /// <summary>
    /// Gets the path to the WPF application executable.
    /// Can be overridden via SALESSYSTEM_EXE_PATH environment variable.
    /// </summary>
    protected virtual string ApplicationExePath
    {
        get
        {
            var envPath = Environment.GetEnvironmentVariable("SALESSYSTEM_EXE_PATH");
            if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            {
                return envPath;
            }

            var defaultPath = Path.Combine(
                GetSolutionRoot(),
                "SalesSystem.DesktopPWF",
                "bin",
                "Debug",
                "net10.0-windows",
                "SalesSystem.DesktopPWF.exe");

            return defaultPath;
        }
    }

    /// <summary>
    /// Gets the application timeout for operations.
    /// </summary>
    protected virtual TimeSpan ApplicationTimeout => TimeSpan.FromMilliseconds(DefaultTimeoutMs);

    /// <summary>
    /// Launches the WPF application with UIA3 automation.
    /// </summary>
    protected void LaunchApplication()
    {
        var exePath = ApplicationExePath;

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException(
                $"Application executable not found at: {exePath}. " +
                "Please build the SalesSystem.DesktopPWF project first " +
                "or set the SALESSYSTEM_EXE_PATH environment variable.");
        }

        _application = Application.Launch(exePath);
        _automation = new UIA2Automation();

        // Wait for application to be ready - give it more time
        WaitForApplicationReady();
    }

    /// <summary>
    /// Waits for the application window to be ready.
    /// Polls for application windows to appear instead of just sleeping.
    /// Handles WindowStyle=None windows that GetAllTopLevelWindows cannot find.
    /// </summary>
    protected virtual void WaitForApplicationReady()
    {
        // Poll for application windows to appear (handles WindowStyle=None)
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var windows = GetApplicationWindows();
                if (windows.Length > 0)
                    return;
            }
            catch { /* Continue polling */ }
            System.Threading.Thread.Sleep(300);
        }
        // Fallback: just wait a bit longer
        System.Threading.Thread.Sleep(3000);
    }

    /// <summary>
    /// Finds a child element by condition.
    /// </summary>
    protected AutomationElement? FindFirstChild(AutomationElement parent, Func<ConditionFactory, ConditionBase> condition)
    {
        return parent.FindFirstChild(condition);
    }

    /// <summary>
    /// Finds a descendant element by condition.
    /// </summary>
    protected AutomationElement? FindFirstDescendant(AutomationElement parent, Func<ConditionFactory, ConditionBase> condition)
    {
        return parent.FindFirstDescendant(condition);
    }

    /// <summary>
    /// Finds an element by its AutomationId with retry logic.
    /// </summary>
    protected AutomationElement? FindElementById(string automationId, AutomationElement? parent = null)
    {
        var searchRoot = parent;
        if (searchRoot == null)
        {
            // Try to get any window as starting point
            var windows = GetApplicationWindows();
            if (windows.Length > 0)
            {
                searchRoot = windows[0];
            }
        }

        if (searchRoot == null)
        {
            throw new InvalidOperationException("Could not find a starting element for search. Call LaunchApplication first.");
        }

        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                var element = searchRoot.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                if (element != null)
                {
                    return element;
                }
            }
            catch
            {
                // Continue trying
            }

            if (i < MaxRetries - 1)
            {
                System.Threading.Thread.Sleep(RetryDelayMs);
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a button by its AutomationId.
    /// </summary>
    protected Button? FindButtonById(string automationId, AutomationElement? parent = null)
        => FindElementById(automationId, parent) as Button;

    /// <summary>
    /// Finds a text box by its AutomationId.
    /// </summary>
    protected TextBox? FindTextBoxById(string automationId, AutomationElement? parent = null)
        => FindElementById(automationId, parent) as TextBox;

    /// <summary>
    /// Finds a list box by its AutomationId.
    /// </summary>
    protected ListBox? FindListBoxById(string automationId, AutomationElement? parent = null)
        => FindElementById(automationId, parent) as ListBox;

    /// <summary>
    /// Finds a list box item by its AutomationId.
    /// </summary>
    protected ListBoxItem? FindListBoxItemById(string automationId, AutomationElement? parent = null)
        => FindElementById(automationId, parent) as ListBoxItem;

    /// <summary>
    /// Finds a window element.
    /// </summary>
    protected Window? FindWindowById(string automationId, AutomationElement? parent = null)
        => FindElementById(automationId, parent) as Window;

    /// <summary>
    /// Clicks on an element using mouse click.
    /// </summary>
    protected void ClickElement(AutomationElement element)
    {
        var clickablePoint = element.GetClickablePoint();
        Mouse.Click(clickablePoint);
    }

    /// <summary>
    /// Types text into a text box and optionally presses Tab.
    /// </summary>
    protected void TypeText(TextBox textBox, string text, bool pressTabAfter = true)
    {
        textBox.Focus();
        Keyboard.Type(text);
        if (pressTabAfter)
        {
            Keyboard.Type(VirtualKeyShort.TAB);
        }
    }

    /// <summary>
    /// Gets the solution root directory.
    /// </summary>
    private static string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Walk up looking for the solution root (has .sln or .slnx file)
        while (currentDir != null)
        {
            if (Directory.GetFiles(currentDir, "*.sln").Length > 0 ||
                Directory.GetFiles(currentDir, "*.slnx").Length > 0)
            {
                return currentDir;
            }

            currentDir = Directory.GetParent(currentDir)?.FullName;
        }

        // Fallback to current directory
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Gets all top-level windows from the application by process ID.
    /// Uses _automation.GetDesktop() which handles WindowStyle=None windows.
    /// </summary>
    protected Window[] GetApplicationWindows()
    {
        if (_application == null || _automation == null)
        {
            return Array.Empty<Window>();
        }

        try
        {
            var appProcessId = _application.ProcessId;
            
            // Find all windows by process ID using desktop root (handles WindowStyle=None)
            var desktop = _automation.GetDesktop();
            var allWindows = desktop.FindAllChildren(
                cf => cf.ByControlType(ControlType.Window));
            
            return allWindows
                .Where(w => {
                    try { return w.Properties.ProcessId.ValueOrDefault == appProcessId; }
                    catch { return false; }
                })
                .Select(w => w.AsWindow())
                .Where(w => w != null)
                .Select(w => w!)
                .ToArray();
        }
        catch
        {
            return Array.Empty<Window>();
        }
    }

    /// <summary>
    /// Dumps the automation tree for debugging element finding issues.
    /// </summary>
    protected void DumpAutomationTree(AutomationElement? element, int depth = 0, int maxDepth = 5)
    {
        if (element == null || depth > maxDepth)
            return;

        var indent = new string(' ', depth * 4);
        string automationId;
        string name;
        string className;

        try
        {
            automationId = element.Properties.AutomationId.ValueOrDefault ?? "(no automation id)";
            name = element.Properties.Name.ValueOrDefault ?? "(no name)";
            className = element.Properties.ClassName.ValueOrDefault ?? "(no class)";
        }
        catch
        {
            automationId = "(error reading)";
            name = "(error reading)";
            className = "(error reading)";
        }

        System.Diagnostics.Debug.WriteLine($"{indent}[{element.Properties.ControlType}] ID={automationId}, Name={name}, Class={className}");

        // Try to find children
        try
        {
            var children = element.FindAllChildren();
            foreach (var child in children)
            {
                DumpAutomationTree(child, depth + 1, maxDepth);
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Dumps all windows and their direct children for debugging.
    /// </summary>
    protected void DumpWindowDiagnostics(string label)
    {
        System.Diagnostics.Debug.WriteLine($"=== {label} ===");
        var windows = GetApplicationWindows();
        System.Diagnostics.Debug.WriteLine($"Found {windows.Length} top-level window(s)");

        foreach (var window in windows)
        {
            System.Diagnostics.Debug.WriteLine($"\nWindow: Name={window.Name}, ClassName={window.ClassName}");
            System.Diagnostics.Debug.WriteLine("Direct children:");
            DumpAutomationTree(window, 1, 3);
        }
    }

    /// <summary>
    /// Closes the application gracefully.
    /// </summary>
    protected void CloseApplication()
    {
        try
        {
            _application?.Close();
        }
        catch
        {
            // Force close if graceful close fails
            _application?.Kill();
        }
    }

    /// <summary>
    /// Keyboard-based login for the login window.
    /// Uses Tab navigation and typing instead of element finding,
    /// since AllowTransparency windows may hide elements from UIA.
    /// </summary>
    protected void KeyboardLogin(string username = "admin", string password = "admin123")
    {
        // Wait for login window to appear and have focus
        System.Threading.Thread.Sleep(1000);
        
        // Type username (the login window's first textbox should have focus on launch)
        Keyboard.Type(username);
        System.Threading.Thread.Sleep(200);
        
        // Tab to password field
        Keyboard.Type(VirtualKeyShort.TAB);
        System.Threading.Thread.Sleep(100);
        
        // Type password  
        Keyboard.Type(password);
        System.Threading.Thread.Sleep(200);
        
        // Press Enter to click the default login button (IsDefault=True)
        Keyboard.Press(VirtualKeyShort.RETURN);
        System.Threading.Thread.Sleep(200);
        Keyboard.Release(VirtualKeyShort.RETURN);
        
        // Wait for main window
        System.Threading.Thread.Sleep(2000);
    }

    /// <summary>
    /// Disposes of the test resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _automation?.Dispose();
            _application?.Dispose();
        }

        _disposed = true;
    }
}
