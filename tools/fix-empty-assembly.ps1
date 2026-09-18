#!/usr/bin/env pwsh
# Fix empty assembly in clr-namespace declarations for PCL local types
# Pattern: clr-namespace:PCL;assembly=""  ->  clr-namespace:PCL;assembly=PCL
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.axaml -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'clr-namespace:[^";]+;assembly=""' -Quiet }
Write-Host "$($files.Count) axaml files with empty assembly"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # clr-namespace:PCL;assembly=""  ->  clr-namespace:PCL;assembly=PCL
    $content = $content -replace 'clr-namespace:([^";]+);assembly=""', 'clr-namespace:$1;assembly=PCL'
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
