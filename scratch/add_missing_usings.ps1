
$folders = @("SalesSystem/SalesSystem.Desktop/Services/Api/Interfaces/", "SalesSystem/SalesSystem.Desktop/Services/Api/")
foreach ($folder in $folders) {
    $files = Get-ChildItem -Path $folder -Filter *.cs
    foreach ($f in $files) {
        $content = Get-Content $f.FullName -Raw
        if ($content -notmatch "using SalesSystem.Contracts.Common;") {
            $content = "using SalesSystem.Contracts.Common;`r`n" + $content
            Set-Content $f.FullName -Value $content -Encoding UTF8
        }
    }
}
