#!/usr/bin/env pwsh
# Fix LineSegment ctor: new LineSegment(point, isStroked) -> new LineSegment { Point = point, IsStroked = isStroked }
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'new LineSegment\(' -Quiet }
Write-Host "$($files.Count) files with LineSegment("
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # new LineSegment(point, isStroked) -> new LineSegment { Point = point, IsStroked = isStroked }
    $content = [regex]::Replace($content,
        'new LineSegment\(([^,]+),\s*(true|false)\)',
        'new LineSegment { Point = $1, IsStroked = $2 }')
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
