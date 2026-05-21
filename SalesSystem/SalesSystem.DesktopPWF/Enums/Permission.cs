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
    CustomerPayment = 1 << 3,       // Customer Payments

    // Manager and above
    PurchaseInvoice = 1 << 4,       // Purchase Invoice CRUD
    PurchaseReturn = 1 << 5,        // Purchase Return
    ProductManagement = 1 << 6,     // Products CRUD
    SupplierManagement = 1 << 7,    // Suppliers CRUD
    StockTransfer = 1 << 8,         // Stock Transfer
    Reports = 1 << 9,              // Reports access

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
    /// Get all permissions for a specific role
    /// </summary>
    public static Permission GetPermissionsForRole(this Contracts.Enums.UserRole role)
    {
        return role switch
        {
            Contracts.Enums.UserRole.Admin => Permission.SalesInvoice
                | Permission.SalesReturn
                | Permission.CustomerView
                | Permission.CustomerPayment
                | Permission.PurchaseInvoice
                | Permission.PurchaseReturn
                | Permission.ProductManagement
                | Permission.SupplierManagement
                | Permission.StockTransfer
                | Permission.Reports
                | Permission.WarehouseManagement
                | Permission.Settings
                | Permission.UserManagement
                | Permission.Backup,

            Contracts.Enums.UserRole.Manager => Permission.SalesInvoice
                | Permission.SalesReturn
                | Permission.CustomerView
                | Permission.CustomerPayment
                | Permission.PurchaseInvoice
                | Permission.PurchaseReturn
                | Permission.ProductManagement
                | Permission.SupplierManagement
                | Permission.StockTransfer
                | Permission.Reports,

            Contracts.Enums.UserRole.Cashier => Permission.SalesInvoice
                | Permission.SalesReturn
                | Permission.CustomerView
                | Permission.CustomerPayment,

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