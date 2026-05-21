
$controls = @(
    @{ Name = 'DashboardControl'; Path = 'Controls/Dashboard' },
    @{ Name = 'ProductsListControl'; Path = 'Controls/Products' },
    @{ Name = 'CustomersListControl'; Path = 'Controls/Customers' },
    @{ Name = 'SuppliersListControl'; Path = 'Controls/Suppliers' },
    @{ Name = 'WarehousesListControl'; Path = 'Controls/Warehouses' },
    @{ Name = 'PurchasesListControl'; Path = 'Controls/Purchases' },
    @{ Name = 'SalesListControl'; Path = 'Controls/Sales' },
    @{ Name = 'SalesReturnsListControl'; Path = 'Controls/SalesReturns' },
    @{ Name = 'PurchaseReturnsListControl'; Path = 'Controls/PurchaseReturns' },
    @{ Name = 'StockTransfersListControl'; Path = 'Controls/StockTransfers' },
    @{ Name = 'CustomerPaymentsListControl'; Path = 'Controls/Payments' },
    @{ Name = 'SupplierPaymentsListControl'; Path = 'Controls/Payments' },
    @{ Name = 'ReportsControl'; Path = 'Controls/Reports' },
    @{ Name = 'SettingsControl'; Path = 'Controls/Settings' },
    @{ Name = 'UsersControl'; Path = 'Controls/Users' }
)

foreach ($c in $controls) {
    $namespace = "SalesSystem.Desktop.$($c.Path.Replace('/', '.'))"
    $fullPath = "SalesSystem/SalesSystem.Desktop/$($c.Path)/$($c.Name).cs"
    $dir = [System.IO.Path]::GetDirectoryName($fullPath)
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force }
    
    $content = @"
namespace $namespace;

public partial class $($c.Name) : UserControl
{
    public $($c.Name)()
    {
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.Name = "$($c.Name)";
        this.Size = new Size(800, 600);
        this.ResumeLayout(false);
    }
}
"@
    Set-Content -Path $fullPath -Value $content -Encoding UTF8
}
