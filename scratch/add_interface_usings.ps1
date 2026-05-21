
$folders = @("SalesSystem/SalesSystem.Desktop/Forms/", "SalesSystem/SalesSystem.Desktop/Controls/", "SalesSystem/SalesSystem.Desktop/Services/Api/")
foreach ($folder in $folders) {
    if (Test-Path $folder) {
        $files = Get-ChildItem -Path $folder -Filter *.cs -Recurse
        foreach ($f in $files) {
            $content = Get-Content $f.FullName -Raw
            $changed = $false
            if ($content -notmatch "using SalesSystem.Desktop.Services.Api.Interfaces;") {
                $content = "using SalesSystem.Desktop.Services.Api.Interfaces;`r`n" + $content
                $changed = $true
            }
            if ($changed) {
                Set-Content $f.FullName -Value $content -Encoding UTF8
            }
        }
    }
}
