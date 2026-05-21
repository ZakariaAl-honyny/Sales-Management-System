
$messages = @(
    'CategoryChangedMessage', 'UnitChangedMessage', 'CustomerChangedMessage', 'SupplierChangedMessage', 
    'WarehouseChangedMessage', 'SaleInvoiceChangedMessage', 'PurchaseInvoiceChangedMessage', 
    'SalesReturnChangedMessage', 'PurchaseReturnChangedMessage', 'StockTransferChangedMessage', 
    'CustomerPaymentChangedMessage', 'SupplierPaymentChangedMessage'
)
foreach ($msg in $messages) {
    $content = "namespace SalesSystem.Desktop.Messaging.Messages;`n`npublic record $msg();"
    $path = "SalesSystem/SalesSystem.Desktop/Messaging/Messages/$msg.cs"
    Set-Content -Path $path -Value $content -Encoding UTF8
}
$stockContent = "namespace SalesSystem.Desktop.Messaging.Messages;`n`npublic record StockChangedMessage(int ProductId, int WarehouseId);"
Set-Content -Path "SalesSystem/SalesSystem.Desktop/Messaging/Messages/StockChangedMessage.cs" -Value $stockContent -Encoding UTF8
