
$controls = Get-ChildItem "SalesSystem/SalesSystem.Desktop/Controls/Placeholders/*.cs"
foreach ($f in $controls) {
    $c = Get-Content $f.FullName -Raw
    
    # Add missing usings
    if ($c -notmatch "using SalesSystem.Contracts.Enums;") {
        $c = $c -replace "namespace", "using SalesSystem.Contracts.Enums;`r`nnamespace"
    }

    # Replace Dtos with Responses in BindingList
    $c = $c -replace 'BindingList<ProductDto>', 'BindingList<ProductResponse>'
    $c = $c -replace 'BindingList<CustomerDto>', 'BindingList<CustomerResponse>'
    $c = $c -replace 'BindingList<SupplierDto>', 'BindingList<SupplierResponse>'
    $c = $c -replace 'BindingList<WarehouseDto>', 'BindingList<WarehouseResponse>'
    $c = $c -replace 'BindingList<SalesInvoiceDto>', 'BindingList<SalesInvoiceResponse>'
    $c = $c -replace 'BindingList<PurchaseInvoiceDto>', 'BindingList<PurchaseInvoiceResponse>'
    $c = $c -replace 'BindingList<SalesReturnDto>', 'BindingList<SalesReturnResponse>'
    $c = $c -replace 'BindingList<PurchaseReturnDto>', 'BindingList<PurchaseReturnResponse>'
    $c = $c -replace 'BindingList<InventoryMovementDto>', 'BindingList<InventoryMovementResponse>'
    $c = $c -replace 'BindingList<UserDto>', 'BindingList<UserResponse>'

    # Fix GetAllAsync calls (adding nulls for dates if missing)
    # Most placeholders use _apiService.GetAllAsync(_searchBar.SearchText)
    # If the interface expects (search, from, to), this is correct.
    
    # Fix specific issues
    $c = $c -replace 'new BindingList<ProductDto>', 'new BindingList<ProductResponse>'
    $c = $c -replace 'new BindingList<CustomerDto>', 'new BindingList<CustomerResponse>'
    $c = $c -replace 'new BindingList<SupplierDto>', 'new BindingList<SupplierResponse>'
    $c = $c -replace 'new BindingList<WarehouseDto>', 'new BindingList<WarehouseResponse>'
    $c = $c -replace 'new BindingList<SalesInvoiceDto>', 'new BindingList<SalesInvoiceResponse>'
    $c = $c -replace 'new BindingList<PurchaseInvoiceDto>', 'new BindingList<PurchaseInvoiceResponse>'
    $c = $c -replace 'new BindingList<SalesReturnDto>', 'new BindingList<SalesReturnResponse>'
    $c = $c -replace 'new BindingList<PurchaseReturnDto>', 'new BindingList<PurchaseReturnResponse>'
    $c = $c -replace 'new BindingList<InventoryMovementDto>', 'new BindingList<InventoryMovementResponse>'
    $c = $c -replace 'new BindingList<UserDto>', 'new BindingList<UserResponse>'

    Set-Content $f.FullName -Value $c -Encoding UTF8
}

# Fix Forms missing usings
$forms = Get-ChildItem "SalesSystem/SalesSystem.Desktop/Forms/*.cs"
foreach ($f in $forms) {
    $c = Get-Content $f.FullName -Raw
    if ($c -notmatch "using SalesSystem.Contracts.Enums;") {
        $c = $c -replace "namespace", "using SalesSystem.Contracts.Enums;`r`nnamespace"
    }
    Set-Content $f.FullName -Value $c -Encoding UTF8
}
