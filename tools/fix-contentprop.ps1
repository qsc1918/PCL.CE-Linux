# Move class-level [ContentProperty("X")] to property-level [Content] on the X property. ASCII only.
param([Parameter(Mandatory=$true)][string]$Root)

$files = @('MyButton.xaml.cs','MyCheckBox.xaml.cs','MyExtraTextButton.xaml.cs','MyHint.xaml.cs','MyIconTextButton.xaml.cs','MyListItem.xaml.cs','MyRadioBox.xaml.cs','MyRadioButton.xaml.cs','CustomEventCollection.cs')
Get-ChildItem $Root -Recurse -Include $files -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $m = [regex]::Match($t, '\[ContentProperty\("(\w+)"\)\]\s*\r?\n([^\r\n]*class\s+\w+)')
    if (-not $m.Success) { "SKIP (no match): $($_.Name)"; return }
    $prop = $m.Groups[1].Value
    # remove class-level attribute line
    $t2 = [regex]::Replace($t, '\s*\[ContentProperty\("\w+"\)\](\s*\r?\n[^\r\n]*class\s+\w+)', '$1', 'Singleline')
    # insert [Content] above the property: find "public ... <prop> ..." declaration (expression or block body)
    $pat = '(\r?\n(\s*)public\s+[^\r\n]*\b' + $prop + '\b[^\r\n]*)'
    $m2 = [regex]::Match($t2, $pat)
    if (-not $m2.Success) { "SKIP (prop not found): $($_.Name) prop=$prop"; return }
    $t2 = $t2.Remove($m2.Groups[1].Index, $m2.Groups[1].Length).Insert($m2.Groups[1].Index, "`r`n    [Content] // [port] WPF 类级 [ContentProperty(`"$prop`")] → Avalonia 12 属性级 [Content]" + $m2.Groups[1].Value)
    [System.IO.File]::WriteAllText($_.FullName, $t2)
    "fixed: $($_.Name) ($prop)"
}
'done'
