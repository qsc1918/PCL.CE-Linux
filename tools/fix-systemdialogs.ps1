#!/usr/bin/env pwsh
# Fix SystemDialogs sync -> async
# SelectSaveFile -> SelectSaveFileAsync, SelectFile -> SelectFileAsync, SelectFolder -> SelectFolderAsync
# Also: SystemDialogs.SelectSaveFile(... -> SystemDialogs.SelectSaveFileAsync(...)
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'SystemDialogs\.Select(SaveFile|File|Folder|Files)\(' -Quiet }
Write-Host "$($files.Count) files with SystemDialogs sync calls"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    $content = $content -replace 'SystemDialogs\.SelectSaveFile\(', 'SystemDialogs.SelectSaveFileAsync('
    $content = $content -replace 'SystemDialogs\.SelectFile\(', 'SystemDialogs.SelectFileAsync('
    $content = $content -replace 'SystemDialogs\.SelectFiles\(', 'SystemDialogs.SelectFilesAsync('
    $content = $content -replace 'SystemDialogs\.SelectFolder\(', 'SystemDialogs.SelectFolderAsync('
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
