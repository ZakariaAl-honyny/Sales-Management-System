namespace SalesSystem.DesktopPWF.Enums;

/// <summary>
/// UI Permission flags matching CONSTITUTION.md Section 4
/// </summary>
[Flags]
public enum Permission : int
{
    None = 0,

    // Basic access (All Staff - Cashier, Manager, Admin)
    SalesInvoice = 1 << 0,          // Sales Invoice CRUD
    SalesReturn = 1 << 1,          // Sales Return
    CustomerView = 1 << 2,          // View Customers (Read-only for Cashier)

    // Manager and above
    PurchaseInvoice = 1 << 3,       // Purchase Invoice CRUD
    PurchaseReturn = 1 << 5,        // Purchase Return
    ProductManagement = 1 << 6,     // Products CRUD
    SupplierManagement = 1 << 7,    // Suppliers CRUD
    WarehouseTransfer = 1 << 8,         // Warehouse Transfer
    Reports = 1 << 9,              // Reports access

    // Manager and above - extended
    CustomerManagement = 1 << 4,   // Customers CRUD (full, vs CustomerView read-only)

    // Accounting access (Manager and above)
    ChartOfAccounts = 1 << 14,     // Chart of Accounts CRUD
    JournalEntries = 1 << 15,      // Journal Entries
    CashBoxes = 1 << 16,           // Cash Boxes access
    Currencies = 1 << 17,          // Currencies (view-only for Cashier/Observer)
    FiscalYear = 1 << 18,          // Fiscal Year management

    // Admin only
    WarehouseManagement = 1 << 10,  // Warehouses CRUD
    Settings = 1 << 11,            // Settings
    UserManagement = 1 << 12,       // User Management
    Backup = 1 << 13                // Backup/Restore
}

/// <summary>
/// Extension methods for Permission checks
/// </summary>
public static class PermissionExtensions
{
    /// <summary>
    /// Get all permissions for a specific role ID
    /// </summary>
    public static Permission GetPermissionsForRole(this int roleId)
    {
        return roleId switch
        {
            1 => Permission.SalesInvoice          // Admin
                | Permission.SalesReturn
                | Permission.CustomerView
                | Permission.CustomerManagement
                | Permission.PurchaseInvoice
                | Permission.PurchaseReturn
                | Permission.ProductManagement
                | Permission.SupplierManagement
                | Permission.WarehouseTransfer
                | Permission.Reports
                | Permission.ChartOfAccounts
                | Permission.JournalEntries
                | Permission.CashBoxes
                | Permission.Currencies
                | Permission.FiscalYear
                | Permission.WarehouseManagement
                | Permission.Settings
                | Permission.UserManagement
                | Permission.Backup,

            2 => Permission.SalesInvoice          // Manager
                | Permission.SalesReturn
                | Permission.CustomerView
                | Permission.CustomerManagement
                | Permission.PurchaseInvoice
                | Permission.PurchaseReturn
                | Permission.ProductManagement
                | Permission.SupplierManagement
                | Permission.WarehouseTransfer
                | Permission.Reports
                | Permission.ChartOfAccounts
                | Permission.JournalEntries
                | Permission.CashBoxes
                | Permission.Currencies,

            3 => Permission.SalesInvoice          // Cashier
                | Permission.SalesReturn
                | Permission.CustomerView
                | Permission.CashBoxes
                | Permission.Currencies,

            4 => Permission.SalesInvoice          // Observer (view only)
                | Permission.SalesReturn
                | Permission.CustomerView
                | Permission.CashBoxes
                | Permission.Currencies,

            5 => Permission.SalesInvoice          // BranchManager
                | Permission.SalesReturn
                | Permission.CustomerView
                | Permission.CustomerManagement
                | Permission.PurchaseInvoice
                | Permission.PurchaseReturn
                | Permission.ProductManagement
                | Permission.SupplierManagement
                | Permission.Reports
                | Permission.ChartOfAccounts
                | Permission.JournalEntries
                | Permission.CashBoxes
                | Permission.Currencies,

            _ => Permission.None
        };
    }

    /// <summary>
    /// Check if a specific permission is granted
    /// </summary>
    public static bool HasPermission(this Permission permissions, Permission required)
    {
        return (permissions & required) == required;
    }
}