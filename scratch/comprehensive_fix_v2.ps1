
$forms = @("SalesInvoiceForm.cs", "PurchaseInvoiceForm.cs")
foreach ($f in $forms) {
    $path = "SalesSystem/SalesSystem.Desktop/Forms/$f"
    if (Test-Path $path) {
        $c = Get-Content $path -Raw
        $c = $c -replace '\.InvoiceNo', '.InvoiceNumber'
        $c = $c -replace '\.DiscountAmount', '.InvoiceDiscount'
        $c = $c -replace '== 1', '== InvoiceStatus.Draft'
        $c = $c -replace '== 2', '== InvoiceStatus.Posted'
        $c = $c -replace '\(SalesSystem\.Domain\.Enums\.PaymentType\)', '(PaymentType)'
        Set-Content $path -Value $c -Encoding UTF8
    }
}

# Fix CustomerResponse constructor in SalesInvoiceForm.cs
$path = "SalesSystem/SalesSystem.Desktop/Forms/SalesInvoiceForm.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'new CustomerResponse\(0, null, "عميل نقدي", "", "", "", 0, 0, true\)', 'new CustomerResponse(0, null, "عميل نقدي", "", "", "", 0, 0, true)'
Set-Content $path -Value $c -Encoding UTF8

# Fix IInventoryApiService
$path = "SalesSystem/SalesSystem.Desktop/Services/Api/Interfaces/IInventoryApiService.cs"
$c = Get-Content $path -Raw
$c = $c -replace 'IEnumerable', 'List'
Set-Content $path -Value $c -Encoding UTF8

# Fix ProductDialog
$path = "SalesSystem/SalesSystem.Desktop/Forms/ProductDialog.cs"
$c = Get-Content $path -Raw
# Replace ProductResponse creation with CreateProductRequest
$c = $c -replace 'var product = new ProductResponse\(', 'var createReq = new CreateProductRequest('
# The record constructor for ProductResponse has 13 args, CreateProductRequest has 9.
# We need to remove the nulls and the last arg (IsActive) for Create.
$c = $c -replace '(?s)var createReq = new CreateProductRequest\(\s+_existingProduct\?\.Id \?\? 0,\s+txtCode\.Text,\s+txtBarcode\.Text,\s+txtName\.Text,\s+\(int\?\)cmbCategory\.SelectedValue,\s+null,\s+\(int\?\)cmbUnit\.SelectedValue,\s+null,\s+txtPurchasePrice\.DecimalValue,\s+txtSalePrice\.DecimalValue,\s+decimal\.TryParse\(txtMinStock\.Text, out var ms\) \? ms : 0m,\s+txtDescription\.Text,\s+chkIsActive\.Checked\s+\)', 'var createReq = new CreateProductRequest(txtCode.Text, txtBarcode.Text, txtName.Text, (int)cmbCategory.SelectedValue, (int)cmbUnit.SelectedValue, txtPurchasePrice.DecimalValue, txtSalePrice.DecimalValue, decimal.TryParse(txtMinStock.Text, out var ms) ? ms : 0m, txtDescription.Text)'

# Fix the calls
$c = $c -replace 'await _productApi\.CreateAsync\(product\)', 'await _productApi.CreateAsync(createReq)'
$c = $c -replace 'await _productApi\.UpdateAsync\(product\)', 'await _productApi.UpdateAsync(_existingProduct.Id, new UpdateProductRequest(createReq.Code, createReq.Barcode, createReq.Name, createReq.CategoryId, createReq.UnitId, createReq.PurchasePrice, createReq.SalePrice, createReq.MinStock, createReq.Description, chkIsActive.Checked))'
Set-Content $path -Value $c -Encoding UTF8
