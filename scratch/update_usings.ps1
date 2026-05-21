
$files = Get-ChildItem -Path SalesSystem/SalesSystem.Desktop/ -Filter *.cs -Recurse
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    if ($content -match "using SalesSystem.Desktop.Services.Interfaces;") {
        $content = $content.Replace("using SalesSystem.Desktop.Services.Interfaces;", "using SalesSystem.Desktop.Services.Interfaces;`nusing SalesSystem.Desktop.Services.Api.Interfaces;")
        Set-Content $f.FullName -Value $content -Encoding UTF8
    }
}
