
$folders = @("SalesSystem/SalesSystem.Desktop/Forms/", "SalesSystem/SalesSystem.Desktop/Services/Api/", "SalesSystem/SalesSystem.Desktop/Services/Api/Interfaces/")
foreach ($folder in $folders) {
    $files = Get-ChildItem -Path $folder -Filter *.cs
    foreach ($f in $files) {
        $content = Get-Content $f.FullName -Raw
        if ($content -notmatch "using SalesSystem.Contracts.Responses;") {
            $content = "using SalesSystem.Contracts.Responses;`r`n" + $content
        }
        if ($content -notmatch "using SalesSystem.Contracts.Requests;") {
            $content = "using SalesSystem.Contracts.Requests;`r`n" + $content
        }
        Set-Content $f.FullName -Value $content -Encoding UTF8
    }
}
