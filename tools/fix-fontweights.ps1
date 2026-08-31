#!/usr/bin/env pwsh
# Fix FontWeights.xxx -> FontWeight.xxx (Avalonia uses instance not static class)
# Also fix FontWeight normal/bold patterns
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern '\bFontWeights\.' -Quiet }
Write-Host "$($files.Count) files contain FontWeights."
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # FontWeights.Bold -> FontWeight.Bold
    $content = $content -replace '\bFontWeights\.Bold\b', 'FontWeight.Bold'
    # FontWeights.Normal -> FontWeight.Normal
    $content = $content -replace '\bFontWeights\.Normal\b', 'FontWeight.Normal'
    # FontWeights.Light -> FontWeight.Light
    $content = $content -replace '\bFontWeights\.Light\b', 'FontWeight.Light'
    # FontWeights.Thin -> FontWeight.Thin
    $content = $content -replace '\bFontWeights\.Thin\b', 'FontWeight.Thin'
    # Generic
    $content = $content -replace '\bFontWeights\.(\w+)', 'FontWeight.$1'
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
