
$formsPath = "SalesSystem/SalesSystem.Desktop/Forms/"
$files = Get-ChildItem -Path $formsPath -Filter *.cs
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $content = $content -replace "ProductDto", "ProductResponse"
    $content = $content -replace "CategoryDto", "CategoryResponse"
    $content = $content -replace "CustomerDto", "CustomerResponse"
    $content = $content -replace "SupplierDto", "SupplierResponse"
    $content = $content -replace "WarehouseDto", "WarehouseResponse"
    $content = $content -replace "UnitDto", "UnitResponse"
    $content = $content -replace "SalesInvoiceDto", "SalesInvoiceResponse"
    $content = $content -replace "PurchaseInvoiceDto", "PurchaseInvoiceResponse"
    $content = $content -replace "SalesReturnDto", "SalesReturnResponse"
    $content = $content -replace "PurchaseReturnDto", "PurchaseReturnResponse"
    $content = $content -replace "StockTransferDto", "StockTransferResponse"
    $content = $content -replace "UserDto", "UserResponse"
    Set-Content $f.FullName -Value $content -Encoding UTF8
}

# Also check Controls folder
$controlsPath = "SalesSystem/SalesSystem.Desktop/Controls/"
$files = Get-ChildItem -Path $controlsPath -Filter *.cs -Recurse
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $content = $content -replace "ProductDto", "ProductResponse"
    $content = $content -replace "CategoryDto", "CategoryResponse"
    $content = $content -replace "CustomerDto", "CustomerResponse"
    $content = $content -replace "SupplierDto", "SupplierResponse"
    $content = $content -replace "WarehouseDto", "WarehouseResponse"
    $content = $content -replace "UnitDto", "UnitResponse"
    Set-Content $f.FullName -Value $content -Encoding UTF8
}
