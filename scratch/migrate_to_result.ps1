
$interfaces = Get-ChildItem -Path SalesSystem/SalesSystem.Desktop/Services/Api/Interfaces/ -Filter *.cs
foreach ($i in $interfaces) {
    $content = Get-Content $i.FullName -Raw
    # Task<List<T>?> -> Task<Result<List<T>>>
    $content = [regex]::Replace($content, "Task<List<([^>]+)>\?>", "Task<Result<List<`$1>>>")
    # Task<T?> -> Task<Result<T>>
    $content = [regex]::Replace($content, "Task<([^<]+)>\?", "Task<Result<`$1>>")
    Set-Content $i.FullName -Value $content -Encoding UTF8
}

$apis = Get-ChildItem -Path SalesSystem/SalesSystem.Desktop/Services/Api/ -Filter *.cs
foreach ($a in $apis) {
    $content = Get-Content $a.FullName -Raw
    # Task<List<T>?> -> Task<Result<List<T>>>
    $content = [regex]::Replace($content, "Task<List<([^>]+)>\?>", "Task<Result<List<`$1>>>")
    # Task<T?> -> Task<Result<T>>
    $content = [regex]::Replace($content, "Task<([^<]+)>\?", "Task<Result<`$1>>")
    Set-Content $a.FullName -Value $content -Encoding UTF8
}
