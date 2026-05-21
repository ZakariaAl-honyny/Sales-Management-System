
$folders = @("SalesSystem/SalesSystem.Desktop/Forms/", "SalesSystem/SalesSystem.Desktop/Controls/", "SalesSystem/SalesSystem.Desktop/Services/Api/", "SalesSystem/SalesSystem.Desktop/Services/Api/Interfaces/")
foreach ($folder in $folders) {
    if (Test-Path $folder) {
        $files = Get-ChildItem -Path $folder -Filter *.cs -Recurse
        foreach ($f in $files) {
            $content = Get-Content $f.FullName -Raw
            $changed = $false
            if ($content -notmatch "using SalesSystem.Contracts.Responses;") {
                $content = "using SalesSystem.Contracts.Responses;`r`n" + $content
                $changed = $true
            }
            if ($content -notmatch "using SalesSystem.Contracts.Requests;") {
                $content = "using SalesSystem.Contracts.Requests;`r`n" + $content
                $changed = $true
            }
            if ($content -notmatch "using SalesSystem.Contracts.Common;") {
                $content = "using SalesSystem.Contracts.Common;`r`n" + $content
                $changed = $true
            }
            # Fix flattened request usings
            if ($content -match "using SalesSystem.Contracts.Requests\.[^; ]+;") {
                $content = $content -replace "using SalesSystem.Contracts.Requests\.[^; ]+;", "using SalesSystem.Contracts.Requests;"
                $changed = $true
            }
            # Rename Dtos to Responses
            $oldContent = $content
            $content = $content -replace "ProductDto", "ProductResponse"
            $content = $content -replace "CategoryDto", "CategoryResponse"
            $content = $content -replace "CustomerDto", "CustomerResponse"
            $content = $content -replace "SupplierDto", "SupplierResponse"
            $content = $content -replace "WarehouseDto", "WarehouseResponse"
            $content = $content -replace "UnitDto", "UnitResponse"
            $content = $content -replace "SalesInvoiceDto", "SalesInvoiceResponse"
            $content = $content -replace "PurchaseInvoiceDto", "PurchaseInvoiceResponse"
            if ($oldContent -ne $content) { $changed = $true }

            if ($changed) {
                Set-Content $f.FullName -Value $content -Encoding UTF8
            }
        }
    }
}
