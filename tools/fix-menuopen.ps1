#!/usr/bin/env pwsh
# Fix MenuBase.IsOpen set -> Toggle() or IsOpen = false with suppression
# Avalonia MenuBase.IsOpen has no public setter
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern '\.IsOpen\s*=' -Quiet }
Write-Host "$($files.Count) files with .IsOpen ="
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # .IsOpen = false -> .Close() (for menus)
    $content = $content -replace '(\w+)\.IsOpen\s*=\s*false', '$1.Close()'
    # .IsOpen = true -> .Open() (for menus)
    $content = $content -replace '(\w+)\.IsOpen\s*=\s*true', '$1.Open()'
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
