
$root = "SalesSystem/SalesSystem.Contracts/Requests"
$files = Get-ChildItem -Path $root -Filter *.cs
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    # Fix the mangled namespace/record line
    if ($content -match "namespace SalesSystem\.Contracts\.Requests (record|public record|public class|class)") {
        $content = $content -replace "namespace SalesSystem\.Contracts\.Requests ", "namespace SalesSystem.Contracts.Requests;`r`npublic "
    }
    # Also handle some that might have missing semicolon
    if ($content -match "^namespace SalesSystem\.Contracts\.Requests\r?\n") {
        $content = $content -replace "namespace SalesSystem\.Contracts\.Requests", "namespace SalesSystem.Contracts.Requests;"
    }
    Set-Content $f.FullName -Value $content -Encoding UTF8
}
