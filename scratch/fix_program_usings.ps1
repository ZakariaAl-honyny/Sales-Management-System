
$path = "SalesSystem/SalesSystem.Desktop/Program.cs"
$content = Get-Content $path -Raw
$content = $content -replace 'using SalesSystem.Desktop.Controls.Dashboard;', "using SalesSystem.Desktop.Controls.Dashboard;`r`nusing SalesSystem.Desktop.Controls.Settings;`r`nusing SalesSystem.Desktop.Controls.Users;"
$content = $content -replace "using SalesSystem.Desktop.Services.Api.Interfaces;`r`nusing SalesSystem.Desktop.Services.Api;`r`nusing SalesSystem.Desktop.Services.Api.Interfaces;", "using SalesSystem.Desktop.Services.Api.Interfaces;`r`nusing SalesSystem.Desktop.Services.Api;"
Set-Content $path -Value $content -Encoding UTF8
