
$path = "SalesSystem/SalesSystem.Desktop/Program.cs"
$content = Get-Content $path -Raw
# Add missing usings if they are not there
if ($content -notmatch "using SalesSystem.Desktop.Controls.Settings;") {
    $content = $content -replace "using SalesSystem.Desktop.Controls.Dashboard;", "using SalesSystem.Desktop.Controls.Dashboard;`r`nusing SalesSystem.Desktop.Controls.Settings;`r`nusing SalesSystem.Desktop.Controls.Users;"
}
Set-Content $path -Value $content -Encoding UTF8
