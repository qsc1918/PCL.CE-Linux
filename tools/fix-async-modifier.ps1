#!/usr/bin/env pwsh
# Fix methods that now use 'await' but aren't marked async
# Find all CS4033 errors, get the file+method, add async modifier
Set-Location "D:\a\j\PCL.CE-Linux"
$log = Get-Content "build-errors-5.txt" -Raw

# Find all files with await that have CS4033 errors
$cs4033Files = @()
Select-String -Path "build-errors-5.txt" -Pattern 'CS4033' | ForEach-Object {
    $m = [regex]::Match($_.Line, '^(.+?)\(')
    if ($m.Success) { $cs4033Files += $m.Groups[1].Value }
}
$cs4033Files = $cs4033Files | Sort-Object -Unique
Write-Host "$($cs4033Files.Count) files need async modifier"

foreach ($filePath in $cs4033Files) {
    $content = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
    $orig = $content
    
    # Strategy: find methods that contain 'await' but aren't async
    # Match: accessibility static? returntype MethodName(...) { ... await ... }
    # Replace: add 'async' modifier
    
    # Simple heuristic: find 'public/private/internal/protected ... MethodName(' without 'async'
    # and if the method body contains 'await', add async
    $lines = $content -split "`r?`n"
    $inMethod = $false
    $methodStartLine = -1
    $braceCount = 0
    $needsAsync = @()
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        # Detect method start (not inside class-level field/property)
        if ($line -match '^\s+(public|private|internal|protected)\s+(static\s+)?(async\s+)?([\w<>\[\],\s\?]+)\s+(\w+)\s*\(' -and -not ($line -match 'async')) {
            $methodStartLine = $i
            $inMethod = $true
            $braceCount = 0
            $hasAwait = $false
        }
        if ($inMethod) {
            $braceCount += ([regex]::Matches($line, '\{')).Count
            $braceCount -= ([regex]::Matches($line, '\}')).Count
            if ($line -match '\bawait\b') { $hasAwait = $true }
            if ($braceCount -le 0 -and $methodStartLine -ge 0) {
                if ($hasAwait -and -not ($lines[$methodStartLine] -match '\basync\b')) {
                    $needsAsync += $methodStartLine
                }
                $inMethod = $false
                $methodStartLine = -1
            }
        }
    }
    
    # Add async to identified methods (work backwards to keep line numbers valid)
    for ($idx = $needsAsync.Count - 1; $idx -ge 0; $idx--) {
        $lineIdx = $needsAsync[$idx]
        $lines[$lineIdx] = $lines[$lineIdx] -replace '(\s+(public|private|internal|protected)\s+(static\s+)?)', '$1async '
    }
    
    if ($needsAsync.Count -gt 0) {
        $content = $lines -join "`n"
        [System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed $($needsAsync.Count) methods in: $(Split-Path $filePath -Leaf)"
    }
}
