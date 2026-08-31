#!/usr/bin/env pwsh
# Fix DataFormats obsolete warnings
# DataFormats.FileDrop -> DataFormat.File etc.
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern '\bDataFormats\.' -Quiet }
Write-Host "$($files.Count) files with DataFormats."
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    $content = $content -replace '\bDataFormats\.FileDrop\b', 'DataFormat.File'
    $content = $content -replace '\bDataFormats\.Text\b', 'DataFormat.Text'
    $content = $content -replace '\bDataFormats\.UnicodeText\b', 'DataFormat.Text'
    $content = $content -replace '\bDataFormats\.OemText\b', 'DataFormat.Text'
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
