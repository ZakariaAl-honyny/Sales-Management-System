
$root = "SalesSystem/SalesSystem.Contracts/Requests"
$subdirs = Get-ChildItem -Path $root -Directory
foreach ($dir in $subdirs) {
    $files = Get-ChildItem -Path $dir.FullName -Filter *.cs
    foreach ($f in $files) {
        $targetPath = Join-Path $root $f.Name
        if (!(Test-Path $targetPath)) {
            Move-Item $f.FullName $targetPath
        } else {
            # If already exists in root, just delete the subfolder version if they are identical (or skip)
            Remove-Item $f.FullName
        }
    }
    Remove-Item $dir.FullName -Force
}

# Fix namespaces in all request files
$files = Get-ChildItem -Path $root -Filter *.cs -Recurse
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $content = $content -replace "namespace SalesSystem.Contracts.Requests.[^; ]+", "namespace SalesSystem.Contracts.Requests"
    Set-Content $f.FullName -Value $content -Encoding UTF8
}
