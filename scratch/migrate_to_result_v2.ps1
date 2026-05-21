
$folders = @("SalesSystem/SalesSystem.Desktop/Services/Api/Interfaces/", "SalesSystem/SalesSystem.Desktop/Services/Api/")
foreach ($folder in $folders) {
    $files = Get-ChildItem -Path $folder -Filter *.cs
    foreach ($f in $files) {
        $content = Get-Content $f.FullName -Raw
        
        # Task<List<T>?> -> Task<Result<List<T>>>
        $content = $content -replace 'Task<List<([^>]+)>\?>', 'Task<Result<List<$1>>>'
        
        # Task<T?> -> Task<Result<T>> where T is a class
        $content = $content -replace 'Task<([^>]+)\?>', 'Task<Result<$1>>'
        
        # Also handle cases where it was Task<T> but should be Task<Result<T>> (like DashboardSummaryResponse)
        # This is trickier, I'll just look for specific common ones if they fail.
        
        Set-Content $f.FullName -Value $content -Encoding UTF8
    }
}
