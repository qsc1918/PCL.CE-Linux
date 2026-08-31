#!/usr/bin/env pwsh
# Add 'await' before SystemDialogs async calls that are not already awaited
# Pattern: var x = SystemDialogs.Select...Async(...) -> var x = await SystemDialogs.Select...Async(...)
# Pattern: SystemDialogs.Select...Async(...) (statement) -> await SystemDialogs.Select...Async(...)
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { Select-String -Path $_.FullName -Pattern 'SystemDialogs\.Select\w+Async\(' -Quiet }
Write-Host "$($files.Count) files with SystemDialogs async calls"
foreach ($f in $files) {
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $orig = $content
    # = SystemDialogs.Select...Async -> = await SystemDialogs.Select...Async
    $content = $content -replace '=\s*SystemDialogs\.Select(\w+)Async\(', '= await SystemDialogs.Select$1Async('
    # (statement) SystemDialogs.Select...Async -> await SystemDialogs.Select...Async
    # Only at start of line (possibly with whitespace)
    $content = [regex]::Replace($content, '(?m)^\s+SystemDialogs\.Select(\w+)Async\(', { param($m) $m.Value -replace 'SystemDialogs\.Select', 'await SystemDialogs.Select' })
    if ($content -ne $orig) {
        [System.IO.File]::WriteAllText($f.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
