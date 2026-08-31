#!/usr/bin/env pwsh
# Fix LineSegment ctor: new LineSegment(point, isStroked) -> new LineSegment { Point = point, IsStroked = isStroked }
# Avalonia LineSegment has parameterless ctor + property init
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'new LineSegment\(' -Quiet }
Write-Host "$($files.Count) files with LineSegment("
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # Pattern: new LineSegment(EXPR, true/false)
    # EXPR can contain nested parens like new Point(x, y)
    # Use a balanced-paren matcher
    $content = [regex]::Replace($content,
        'new LineSegment\(([^,]+(?:\([^)]*\))?[^,]*),\s*(true|false)\)',
        'new LineSegment { Point = $1, IsStroked = $2 }')
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        $remaining = [regex]::Matches($content, 'new LineSegment\(').Count
        Write-Host "Fixed: $($f.Name) (remaining: $remaining)"
    }
}
