# Repair broken ToolTip regex replacements. ASCII only.
param([Parameter(Mandatory=$true)][string]$Root)

$BT = [char]96  # backtick
$pA = 'Avalonia.Controls.ToolTip.GetTip(' + $BT + 'Avalonia.Controls).SetTip('
$pB = 'Lang.Text("' + $BT + 'Avalonia.Controls.ToolTip.GetTip(' + $BT
$pC = '"' + $BT + 'Avalonia.Controls.ToolTip.GetTip(' + $BT

$n = 0
Get-ChildItem $Root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $o = $t
    $t = $t.Replace($pA, 'Avalonia.Controls.ToolTip.SetTip(')
    # Lang.Text("`...KEY)") -> Lang.Text("KEY")
    $t = [regex]::Replace($t, [regex]::Escape($pB) + '([^"]*)\)"', 'Lang.Text("$1")')
    # resource key strings "`...KEY" -> "KEY"  (ModMusic etc.)
    $t = [regex]::Replace($t, [regex]::Escape($pC) + '([^"]*)"', '"$1"')
    if ($t -ne $o) { [System.IO.File]::WriteAllText($_.FullName, $t); $n++ }
}
"repaired: $n files"
