# Insert Avalonia base class into code-behind partials that lack one. ASCII only.
param([Parameter(Mandatory=$true)][string]$Root)

$skipNames = @('MySkin','MyMsgMarkdown','PageToolsGameLink','PageToolsLeft','PageToolsTest')
$n = 0
Get-ChildItem $Root -Recurse -Filter *.axaml -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $axaml = $_
    $first = $null; $xc = $null
    foreach ($line in [System.IO.File]::ReadAllLines($axaml.FullName)) {
        if ($null -eq $first -and $line -match '^\s*<([\w:]+)') { $first = $Matches[1] }
        if ($null -eq $xc -and $line -match 'x:Class="([^"]+)"') { $xc = $Matches[1] }
        if ($first -and $xc) { break }
    }
    if (-not $first -or -not $xc) { return }
    $shortClass = $xc.Split('.')[-1]
    if ($skipNames -contains $shortClass) { return }
    # resolve root type: local:X -> X
    $base = $first -replace '^local:', ''
    # find code-behind: try <Class>.xaml.cs then <AxamlName>.xaml.cs
    $dir = $axaml.DirectoryName
    $csPath = Join-Path $dir "$shortClass.xaml.cs"
    if (-not (Test-Path $csPath)) { $csPath = Join-Path $dir "$($axaml.BaseName).xaml.cs" }
    if (-not (Test-Path $csPath)) { "NO CODEBEHIND: $shortClass ($($axaml.Name))"; return }
    $t = [System.IO.File]::ReadAllText($csPath)
    # match class declaration without base
    $m = [regex]::Match($t, '(public\s+partial\s+class\s+' + $shortClass + '\b)(\s*)(\r?\n|\{)')
    if ($m.Success -and $t -notmatch 'partial\s+class\s+' + $shortClass + '\s*:') {
        $t = $t.Remove($m.Groups[1].Index, $m.Groups[1].Length).Insert($m.Groups[1].Index, $m.Groups[1].Value + ' : ' + $base)
        [System.IO.File]::WriteAllText($csPath, $t)
        $n++
        "base added: $shortClass : $base"
    }
}
"total: $n"
