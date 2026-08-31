#!/usr/bin/env pwsh
# Fix Cursors.xxx -> Cursor.Default / Cursor.Parse
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern '\bCursors\.' -Quiet }
Write-Host "$($files.Count) files contain Cursors."
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # Cursors.Arrow -> Cursor.Default
    $content = $content -replace '\bCursors\.Arrow\b', 'Cursor.Default'
    # Cursors.Hand -> Cursor.Parse("hand")
    $content = $content -replace '\bCursors\.Hand\b', 'Cursor.Parse("hand")'
    # Cursors.IBeam -> Cursor.Parse("text")
    $content = $content -replace '\bCursors\.IBeam\b', 'Cursor.Parse("text")'
    # Cursors.Wait -> Cursor.Parse("wait")
    $content = $content -replace '\bCursors\.Wait\b', 'Cursor.Parse("wait")'
    # Generic fallback
    $content = $content -replace '\bCursors\.(\w+)', 'Cursor.Parse("$1")'
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
