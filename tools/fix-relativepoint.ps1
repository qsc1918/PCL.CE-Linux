#!/usr/bin/env pwsh
# Fix Point -> RelativePoint for RenderTransformOrigin
# Pattern: new Point(x, y) -> new RelativePoint(x, y, RelativeUnit.Relative)
# Only in RenderTransformOrigin context
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'RenderTransformOrigin\s*=' -Quiet }
Write-Host "$($files.Count) files with RenderTransformOrigin"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # RenderTransformOrigin = new Point(x, y) -> RenderTransformOrigin = new RelativePoint(new Point(x, y), RelativeUnit.Relative)
    # Or simpler: new RelativePoint(x, y, RelativeUnit.Relative)
    $content = [regex]::Replace($content,
        'RenderTransformOrigin\s*=\s*new Point\(([^)]+)\)',
        'RenderTransformOrigin = new RelativePoint(new Point($1), RelativeUnit.Relative)')
    # Also: RenderTransformOrigin = New Point(...) (no 'new' but that shouldn't happen)
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
