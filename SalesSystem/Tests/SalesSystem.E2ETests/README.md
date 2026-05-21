# SalesSystem.E2ETests

End-to-end (E2E) tests for the Sales Management System WPF application using FlaUI and xUnit.

## Overview

This project contains automated UI tests that verify the functionality of the Sales Management System by interacting with the WPF application through the UI automation framework.

## Prerequisites

1. **.NET 10 SDK** - Required to build and run tests
2. **SalesSystem.DesktopPWF** - The WPF application must be built before running tests
3. **Windows** - E2E tests only run on Windows due to UIA automation requirements

## Building the Application

Before running E2E tests, ensure the WPF application is built:

```bash
cd SalesSystem\SalesSystem.DesktopPWF
dotnet build
```

## Running Tests

### Run all E2E tests:
```bash
cd SalesSystem\Tests\SalesSystem.E2ETests
dotnet test
```

### Run specific test categories:
```bash
# Run only Login tests
dotnet test --filter "Category=Login"

# Run only Navigation tests
dotnet test --filter "Category=Navigation"

# Run only Smoke tests
dotnet test --filter "Category=Smoke"
```

### Run specific test:
```bash
dotnet test --filter "FullyQualifiedName~LoginFlowTests.Login_WithValidCredentials"
```

## Configuration

### Application Path

The tests look for the WPF application at:
```
SalesSystem\SalesSystem.DesktopPWF\bin\Debug\net10.0-windows\SalesSystem.DesktopPWF.exe
```

To use a different path, set the environment variable:
```powershell
$env:SALESSYSTEM_EXE_PATH = "C:\path\to\your\SalesSystem.DesktopPWF.exe"
```

## Test Structure

```
SalesSystem.E2ETests/
├── TestBase.cs              # Base class for all E2E tests
├── LoginFlowTests.cs        # Login flow tests
├── NavigationTests.cs       # Navigation tests
├── TestCategories.cs        # Test category definitions
├── GlobalUsings.cs          # Global using directives
└── SalesSystem.E2ETests.csproj
```

## Test Categories

| Category | Description |
|----------|-------------|
| `E2E` | All end-to-end tests |
| `Login` | Login flow tests |
| `Navigation` | Main window navigation tests |
| `Smoke` | Quick sanity checks |
| `Critical` | Must-pass tests for release |

## UI Element AutomationIds

### Login Window
- `txtUsername` - Username text box
- `txtPassword` - Password box
- `btnLogin` - Login button
- `txtErrorMessage` - Error message display

### Main Window Navigation
- `MainNavigationList` - Main navigation ListBox
- `NavDashboard` - Dashboard navigation item
- `NavSales` - Sales navigation item
- `NavPurchases` - Purchases navigation item
- `NavProducts` - Products navigation item
- `NavCustomers` - Customers navigation item
- `NavSuppliers` - Suppliers navigation item
- `NavWarehouses` - Warehouses navigation item
- `NavReports` - Reports navigation item
- `NavSettings` - Settings navigation item
- `SidebarBrandText` - App brand text in sidebar

## Troubleshooting

### Application not found
If you get an error about the application executable not being found:
1. Verify the SalesSystem.DesktopPWF project builds successfully
2. Check that `SalesSystem.DesktopPWF.exe` exists in the bin folder
3. Set the `SALESSYSTEM_EXE_PATH` environment variable if using a custom location

### Tests timing out
Increase the timeout values in `TestBase.cs`:
- `DefaultTimeoutMs` - Default timeout for element searches
- `MaxRetries` - Number of retry attempts

### Elements not found
Ensure AutomationIds are correctly set in the XAML files:
```xml
AutomationProperties.AutomationId="yourElementId"
```

## CI/CD Integration

Example GitHub Actions workflow step:
```yaml
- name: Run E2E Tests
  run: |
    cd SalesSystem\Tests\SalesSystem.E2ETests
    dotnet test --logger "trx;LogFileName=results.trx"
```

## Notes

- E2E tests are slower than unit tests due to UI automation overhead
- Tests run sequentially by default (not parallel) to avoid UI conflicts
- Each test includes proper setup and teardown to ensure clean state
- Failed tests attempt to capture screenshots for debugging (when supported)
