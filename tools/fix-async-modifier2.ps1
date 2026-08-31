#!/usr/bin/env pwsh
# Fix methods that use 'await' but aren't marked async
# From build error CS4033, extract file:line, find the method, add async + change void->Task
Set-Location "D:\a\j\PCL.CE-Linux"

# Get unique files from CS4033 errors
$files = @()
Select-String -Path "build-errors-5.txt" -Pattern 'CS4033' | ForEach-Object {
    $m = [regex]::Match($_.Line, '^(.+?)\((\d+),')
    if ($m.Success) { $files += $m.Groups[1].Value }
}
$files = $files | Sort-Object -Unique
Write-Host "$($files.Count) files with CS4033"

foreach ($filePath in $files) {
    $content = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
    $lines = $content -split "`r?`n"
    
    # Find all lines with 'await ' that are NOT in an async method
    # Strategy: scan for method declarations, track if they're async, 
    # and if their body has await, add async
    $changed = $false
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        # Match method declarations (with optional access modifiers, static, etc.)
        if ($line -match '^(\s*)(public|private|internal|protected)(\s+static)?\s+(?!async)(\w[\w<>\[\],\?\s]*?)\s+(\w+)\s*\(') {
            $indent = $matches[1]
            $access = $matches[2]
            $static = $matches[3]
            $returnType = $matches[4].Trim()
            $methodName = $matches[5]
            
            # Check if this method body contains 'await'
            $braceCount = 0
            $foundOpenBrace = $false
            $hasAwait = $false
            $methodEnd = $i
            
            for ($j = $i; $j -lt [Math]::Min($i + 200, $lines.Count); $j++) {
                $braceCount += ([regex]::Matches($lines[$j], '\{')).Count
                $braceCount -= ([regex]::Matches($lines[$j], '\}')).Count
                if ($lines[$j] -match '\bawait\b') { $hasAwait = $true }
                if ($braceCount -gt 0) { $foundOpenBrace = $true }
                if ($foundOpenBrace -and $braceCount -le 0) { $methodEnd = $j; break }
            }
            
            if ($hasAwait) {
                # Add async modifier
                $newLine = $line -replace '^( *)(public|private|internal|protected)(\s+static)', '$1$2$3 async'
                if ($newLine -eq $line) {
                    # static might not be present
                    $newLine = $line -replace '^( *)(public|private|internal|protected)\s+', '$1$2 async '
                }
                $lines[$i] = $newLine
                
                # Change void -> Task if return type is void
                if ($returnType -eq 'void') {
                    $lines[$i] = $lines[$i] -replace '\bvoid\b\s+' + [regex]::Escape($methodName), 'Task ' + $methodName
                }
                # Change string -> Task<string>, etc. if not already Task
                elseif ($returnType -notmatch '^Task') {
                    $lines[$i] = $lines[$i] -replace [regex]::Escape($returnType) + '\s+' + [regex]::Escape($methodName), 'Task<' + $returnType + '> ' + $methodName
                }
                $changed = $true
                Write-Host "  Line $($i+1): added async to $methodName (return: $returnType)"
            }
        }
    }
    
    if ($changed) {
        $content = $lines -join "`n"
        [System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Fixed: $(Split-Path $filePath -Leaf)"
    }
}
