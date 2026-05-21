
$forms = @("SalesInvoiceForm.cs", "PurchaseInvoiceForm.cs")
foreach ($f in $forms) {
    $path = "SalesSystem/SalesSystem.Desktop/Forms/$f"
    if (Test-Path $path) {
        $c = Get-Content $path -Raw
        $c = $c -replace '\.InvoiceNo', '.InvoiceNumber'
        $c = $c -replace '\.DiscountAmount', '.InvoiceDiscount'
        $c = $c -replace '== 1', '== InvoiceStatus.Draft'
        $c = $c -replace '== 2', '== InvoiceStatus.Posted'
        $c = $c -replace 'new CustomerResponse\(0, null, "عميل نقدي", "", "", "", 0, 0, true\)', 'new CustomerResponse(0, null, "عميل نقدي", "", "", "", 0, 0, true)'
        # Wait, I updated CustomerResponse to have 9 args. 
        # int Id, string? Code, string Name, string? Phone, string? Address, string? Email, decimal CurrentBalance, decimal CreditLimit, bool IsActive
        # 0, null, "عميل نقدي", "", "", "", 0, 0, true -> matches 9 args.
        Set-Content $path -Value $c -Encoding UTF8
    }
}

# Fix IInventoryApiService
$path = "SalesSystem/SalesSystem.Desktop/Services/Api/Interfaces/IInventoryApiService.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'IEnumerable', 'List'
Set-Content $path -Value $c -Encoding UTF8

# Fix ProductDialog
$path = "SalesSystem/SalesSystem.Desktop/Forms/ProductDialog.cs"
$c = Get-Content $path -Raw
# Change creation/update to use requests
$c = $c -replace 'var product = new ProductResponse\(', 'var createReq = new CreateProductRequest('
$c = $c -replace 'await _productApi\.CreateAsync\(product\)', 'await _productApi.CreateAsync(createReq)'
$c = $c -replace 'await _productApi\.UpdateAsync\(product\)', 'await _productApi.UpdateAsync(_existingProduct.Id, new UpdateProductRequest(createReq.Code, createReq.Barcode, createReq.Name, createReq.CategoryId, createReq.UnitId, createReq.PurchasePrice, createReq.SalePrice, createReq.MinStock, createReq.Description, createReq.IsActive))'
Set-Content $path -Value $c -Encoding UTF8
