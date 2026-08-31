# Re-insert dedupe-damaged using( statements via baseline alignment. ASCII only.
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
    $baseLines = $baseText -split "`r?`n"
    $curPath = Join-Path $RepoRoot ($f -replace '/', '\')
    $curLines = [System.IO.File]::ReadAllLines($curPath)

    $out = [System.Collections.Generic.List[string]]::new()
    $b = 0; $c = 0; $inserted = 0
    # skip file headers (using directives / leading comments) in both
    while ($b -lt $baseLines.Count -and $baseLines[$b] -match '^(using|\s*using|//|$)') { $b++ }
    while ($c -lt $curLines.Count -and $curLines[$c] -match '^(using|\s*using|//|$)') { $out.Add($curLines[$c]); $c++ }
    while ($b -lt $baseLines.Count -and $c -lt $curLines.Count) {
        if ($baseLines[$b] -eq $curLines[$c]) { $out.Add($curLines[$c]); $b++; $c++; continue }
        # mismatch: if baseline line is a using( statement, insert it and skip baseline only
        if ($baseLines[$b] -match '^\s*using \(') {
            $out.Add($baseLines[$b]); $inserted++; $b++; continue
        }
        # otherwise try to resync: advance whichever looks like an insertion
        if ($curLines[$c] -match '^\s*using \(') { $c++; continue }
        # give up alignment for this file at this point
        "UNALIGNED in ${f}: base=[$($baseLines[$b].Trim().Substring(0,[Math]::Min(50,$baseLines[$b].Trim().Length)))] cur=[$($curLines[$c].Trim().Substring(0,[Math]::Min(50,$curLines[$c].Trim().Length)))]"
        break
    }
    if ($inserted -gt 0) {
        # append remaining current lines
        while ($c -lt $curLines.Count) { $out.Add($curLines[$c]); $c++ }
        [System.IO.File]::WriteAllLines($curPath, $out)
    }
    "${f}: inserted $inserted"
}
'done'
