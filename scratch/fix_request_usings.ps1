
$folders = @("SalesSystem/SalesSystem.Desktop/")
foreach ($folder in $folders) {
    $files = Get-ChildItem -Path $folder -Filter *.cs -Recurse
    foreach ($f in $files) {
        $content = Get-Content $f.FullName -Raw
        if ($content -match "using SalesSystem.Contracts.Requests\.") {
            $content = $content -replace "using SalesSystem.Contracts.Requests\.[^; ]+;", "using SalesSystem.Contracts.Requests;"
            # Remove duplicate using if any
            $lines = $content -split "`r?`n"
            $uniqueLines = $lines | Select-Object -Unique
            $content = $uniqueLines -join "`r`n"
            Set-Content $f.FullName -Value $content -Encoding UTF8
        }
    }
}
