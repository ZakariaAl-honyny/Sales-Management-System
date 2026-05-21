$files = @(
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Transfers\StockTransfersListView.xaml",
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Returns\SalesReturnsListView.xaml",
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Returns\SalesReturnEditorView.xaml",
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Returns\PurchaseReturnsListView.xaml",
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Returns\PurchaseReturnEditorView.xaml",
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Purchases\PurchaseInvoicesListView.xaml",
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Purchases\PurchaseInvoiceEditorView.xaml",
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Payments\SupplierPaymentsListView.xaml",
    "c:\Users\ALlahabi\Desktop\Sales Management System\SalesSystem\SalesSystem.DesktopPWF\Views\Payments\CustomerPaymentsListView.xaml"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $newContent = $content -replace 'ColumnHeaderStyle="\{StaticResource DataGridColumnHeaderStyle\}"', ''
        # Clean up any resulting double spaces or empty lines if needed, but usually it's fine
        Set-Content $file $newContent
        Write-Host "Updated $file"
    } else {
        Write-Host "File not found: $file"
    }
}
