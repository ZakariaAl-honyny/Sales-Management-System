
$file = 'Sales Management System PRD MVP and Code.md'
$map = @{
    'scratch/extracted_modules/ProductModule.cs' = @(32840, 33000)
    'scratch/extracted_modules/SalesReturnsModule.cs' = @(33021, 33300)
    'scratch/extracted_modules/PurchaseReturnsModule.cs' = @(35999, 36080)
    'scratch/extracted_modules/TransfersModule.cs' = @(36086, 36190)
    'scratch/extracted_modules/ReportsModule.cs' = @(33879, 34500)
    'scratch/extracted_modules/CustomersModule.cs' = @(27170, 28000)
    'scratch/extracted_modules/SuppliersModule.cs' = @(28930, 29800)
    'scratch/extracted_modules/SalesModule.cs' = @(30400, 31300)
    'scratch/extracted_modules/PurchasesModule.cs' = @(29830, 30300)
    'scratch/extracted_modules/CustomerPaymentsModule.cs' = @(31400, 31900)
    'scratch/extracted_modules/SupplierPaymentsModule.cs' = @(24280, 25000)
    'scratch/extracted_modules/WarehousesModule.cs' = @(15260, 16500)
}
foreach ($key in $map.Keys) {
    $start = $map[$key][0]
    $end = $map[$key][1]
    $count = $end - $start + 1
    Get-Content $file -TotalCount $end | Select-Object -Skip ($start - 1) | Set-Content $key -Encoding UTF8
    Write-Host "Extracted $key"
}
