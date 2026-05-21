
$path = "SalesSystem/SalesSystem.Desktop/Program.cs"
$content = Get-Content $path -Raw
$usings = "using SalesSystem.Desktop.Services.Api.Interfaces;`r`nusing SalesSystem.Desktop.Services.Api;"
if ($content -notmatch "using SalesSystem.Desktop.Services.Api.Interfaces;") {
    $content = $usings + "`r`n" + $content
}
Set-Content $path -Value $content -Encoding UTF8
