#!/usr/bin/env pwsh
# Add 'using PCL.Core.App;' to files that use SetResourceReference but don't have it
Set-Location "D:\a\j\PCL.CE-Linux"
$files = Get-ChildItem -Recurse -Include *.cs -Path "src\Plain Craft Launcher 2" | Where-Object { (Select-String -Path $_.FullName -Pattern 'SetResourceReference\(' -Quiet) -and -not (Select-String -Path $_.FullName -Pattern 'using PCL\.Core\.App;' -Quiet) }
Write-Host "$($files.Count) files need using PCL.Core.App"
foreach ($f in $files) {
    $lines = [System.IO.File]::ReadAllLines($f.FullName, [System.Text.Encoding]::UTF8)
    $insertIdx = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^using\s+') { $insertIdx = $i + 1; break }
    }
    if ($insertIdx -gt 0) {
        $newLines = New-Object System.Collections.Generic.List[string]
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $newLines.Add($lines[$i])
            if ($i -eq $insertIdx - 1) { $newLines.Add("using PCL.Core.App;") }
        }
        [System.IO.File]::WriteAllLines($f.FullName, $newLines.ToArray(), [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $($f.Name)"
    }
}
