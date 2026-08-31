#!/usr/bin/env pwsh
# Fix ImageBrush.ImageSource -> ImageBrush.Source (Avalonia)
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'ImageSource\s*=' -Quiet }
Write-Host "$($files.Count) files with ImageSource="
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # .ImageSource = xxx -> .Source = xxx (for ImageBrush)
    $content = $content -replace '\.ImageSource\s*=', '.Source ='
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
