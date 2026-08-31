# Map axaml root element -> x:Class for base-class insertion. ASCII only.
param([Parameter(Mandatory=$true)][string]$Root)

Get-ChildItem $Root -Recurse -Filter *.axaml -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $file = $_
    $first = $null; $xc = $null
    foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
        if ($null -eq $first -and $line -match '^\s*<([\w:]+)') { $first = $Matches[1] }
        if ($null -eq $xc -and $line -match 'x:Class="([^"]+)"') { $xc = $Matches[1] }
        if ($first -and $xc) { break }
    }
    if ($first -and $xc) { '{0} => {1} : {2}' -f $file.Name, $xc, $first }
}
