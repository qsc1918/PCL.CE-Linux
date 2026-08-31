#!/usr/bin/env pwsh
# Fix ColumnPaddingRight -> ColumnGap (or Padding if more appropriate)
# Avalonia Grid doesn't have ColumnPaddingRight; it's ColumnDefinitions[n].Width + Margin
# For now, stub as a property declaration in the owner class
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern '\bColumnPaddingRight\b' -Quiet }
Write-Host "$($files.Count) files with ColumnPaddingRight"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # ColumnPaddingRight -> ColumnGap (Avalonia has this)
    $content = $content -replace '\bColumnPaddingRight\b', 'ColumnGap'
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
