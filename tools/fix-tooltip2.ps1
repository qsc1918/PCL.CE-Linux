# Migrate WPF ToolTipService -> Avalonia ToolTip. ASCII only.
param([Parameter(Mandatory=$true)][string]$Root)

$csRules = [ordered]@{
    'ToolTipService\.SetPlacement\('    = 'ToolTip.SetPlacement('
    'ToolTipService\.SetVerticalOffset\('   = 'ToolTip.SetVerticalOffset('
    'ToolTipService\.SetHorizontalOffset\(' = 'ToolTip.SetHorizontalOffset('
}
$xamlRules = [ordered]@{
    'ToolTipService\.Placement='      = 'ToolTip.Placement='
    'ToolTipService\.HorizontalOffset=' = 'ToolTip.HorizontalOffset='
    'ToolTipService\.VerticalOffset='   = 'ToolTip.VerticalOffset='
}

$cs = 0; $ax = 0
Get-ChildItem $Root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName); $o = $t
    foreach ($k in $csRules.Keys) { $t = [regex]::Replace($t, $k, $csRules[$k]) }
    # remove delay lines (no Avalonia equivalent)
    $t = [regex]::Replace($t, '(?m)^\s*.*ToolTipService\.Set(?:InitialShowDelay|BetweenShowDelay)\([^\n]*\n', '')
    if ($t -ne $o) { [System.IO.File]::WriteAllText($_.FullName, $t); $cs++ }
}
Get-ChildItem $Root -Recurse -Include *.axaml -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName); $o = $t
    foreach ($k in $xamlRules.Keys) { $t = [regex]::Replace($t, $k, $xamlRules[$k]) }
    $t = [regex]::Replace($t, '\s*ToolTipService\.(?:InitialShowDelay|BetweenShowDelay)="[^"]*"', '')
    if ($t -ne $o) { [System.IO.File]::WriteAllText($_.FullName, $t); $ax++ }
}
"cs: $cs, axaml: $ax"
