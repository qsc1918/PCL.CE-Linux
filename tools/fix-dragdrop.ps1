#!/usr/bin/env pwsh
# Fix DragEventArgs.Data -> DragEventArgs.DataTransfer
# And DragDropEffects -> DragDropEffects (same in Avalonia)
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'e\.Data\b|\.DragEventArgs' -Quiet }
Write-Host "$($files.Count) files with DragEventArgs patterns"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # e.Data.TryGetData<string>(DataFormats.FileDrop) -> e.DataTransfer.TryGetData<string>(DataFormat.File)
    $content = $content -replace 'e\.Data\.TryGetData', 'e.DataTransfer.TryGetData'
    # e.Data.GetDataPresent(DataFormats.FileDrop) -> e.DataTransfer.Contains(DataFormat.File)
    $content = $content -replace 'e\.Data\.GetDataPresent\(DataFormats\.FileDrop\)', 'e.DataTransfer.Contains(DataFormat.File)'
    # DataFormats.FileDrop -> DataFormat.File
    $content = $content -replace '\bDataFormats\.FileDrop\b', 'DataFormat.File'
    $content = $content -replace '\bDataFormats\.Text\b', 'DataFormat.Text'
    $content = $content -replace '\bDataFormats\.UnicodeText\b', 'DataFormat.Text'
    # DragDropEffects is still valid in Avalonia
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
