# Repair v2: for each baseline using( line, locate by following-context line, insert if missing. ASCII only.
param([Parameter(Mandatory=$true)][string]$RepoRoot)

$enc936 = [System.Text.Encoding]::GetEncoding(936)
$strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$baseline = '6a525ab'

$files = @(
    'Modules/Base/ModBase.cs',
    'Modules/Minecraft/ModLocalComp.cs',
    'Modules/Minecraft/ModComp.cs'
)

foreach ($f in $files) {
    $gitPath = 'src/Plain Craft Launcher 2/' + $f
    $tmp = [System.IO.Path]::GetTempFileName()
    cmd /c "git -C `"$RepoRoot`" show ${baseline}:`"$gitPath`" > `"$tmp`"" 2>$null
    $bytes = [System.IO.File]::ReadAllBytes($tmp)
    Remove-Item $tmp -Force
    try { $baseText = $strictUtf8.GetString($bytes) } catch { $baseText = $enc936.GetString($bytes) }
    $baseLines = [System.Collections.Generic.List[string]]($baseText -split "`r?`n")
    if ($baseLines.Count -gt 0) { $baseLines[0] = $baseLines[0].TrimStart([char]0xFEFF) }
    $curPath = Join-Path $RepoRoot ($f -replace '/', '\')
    $curLines = [System.Collections.Generic.List[string]]([System.IO.File]::ReadAllLines($curPath))

    $inserted = 0; $offset = 0
    for ($i = 0; $i -lt $baseLines.Count; $i++) {
        if ($baseLines[$i] -notmatch '^\s*using \(') { continue }
        $stmt = $baseLines[$i].Trim()
        $after = if ($i + 1 -lt $baseLines.Count) { $baseLines[$i + 1].Trim() } else { $null }
        if ([string]::IsNullOrWhiteSpace($after)) { $after = if ($i + 2 -lt $baseLines.Count) { $baseLines[$i + 2].Trim() } else { $null } }
        if ($null -eq $after) { continue }

        # count occurrences in baseline vs current of the exact statement text
        $baseCount = 0
        for ($j = 0; $j -lt $baseLines.Count; $j++) { if ($baseLines[$j].Trim() -eq $stmt) { $baseCount++ } }
        $curCount = 0
        for ($j = 0; $j -lt $curLines.Count; $j++) { if ($curLines[$j].Trim() -eq $stmt) { $curCount++ } }
        if ($curCount -ge $baseCount) { continue }

        # find context line in current after the previously handled offset
        for ($j = $offset; $j -lt $curLines.Count; $j++) {
            if ($curLines[$j].Trim() -eq $after) {
                $indent = ''
                if ($baseLines[$i] -match '^(\s*)') { $indent = $Matches[1] }
                $curLines.Insert($j, $indent + $stmt)
                $inserted++; $offset = $j + 1
                "${f}: inserted before L$($j+1): $stmt"
                break
            }
        }
    }
    if ($inserted -gt 0) { [System.IO.File]::WriteAllLines($curPath, $curLines) }
    "${f}: total inserted $inserted"
}
'done'
