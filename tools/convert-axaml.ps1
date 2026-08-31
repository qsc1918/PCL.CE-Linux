# Safe axaml conversion subset (TargetType->Selector etc.). ASCII only.
param([Parameter(Mandatory=$true)][string]$Root)

$n = 0
$triggerFiles = @()
Get-ChildItem $Root -Recurse -Include *.axaml -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $orig = $t
    # Style TargetType -> Selector
    $t = [regex]::Replace($t, '<Style\s+TargetType="\{x:Type ([^}]+)\}"', '<Style Selector="$1"')
    $t = [regex]::Replace($t, '<Style\s+TargetType="([^"]+)"', '<Style Selector="$1"')
    $t = [regex]::Replace($t, '<Style\s+x:Key="([^"]+)"\s+TargetType="([^"]+)"', '<Style Selector="$2" x:Key="$1"')
    # remove WPF-only attributes/setters
    $t = [regex]::Replace($t, '\s*<Setter Property="OverridesDefaultStyle" Value="(?:True|False)" />', '')
    $t = [regex]::Replace($t, '\s*<Setter Property="FocusVisualStyle" Value="[^"]*" />', '')
    $t = [regex]::Replace($t, '\s*<Setter Property="(?:Stylus|TextOptions)\.[^"]*" Value="[^"]*" />', '')
    $t = [regex]::Replace($t, '\s+(?:SnapsToDevicePixels|FocusVisualStyle|Stylus\.\w+)="[^"]*"', '')
    # ControlTemplate TargetType: keep (Avalonia supports TargetType attr? no - Avalonia ControlTemplate uses DataType or nothing) -> strip
    $t = [regex]::Replace($t, '<ControlTemplate TargetType="(?:\{x:Type )?([^}"]+)"?>', '<ControlTemplate>')
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($_.FullName, $t); $n++ }
    if ($t -match '<Trigger |<MultiTrigger|<DataTrigger|<Storyboard|<EventTrigger|Style\.Triggers|ControlTemplate\.Triggers|<Trigger\.') { $triggerFiles += $_.FullName.Replace("$Root\", '') }
}
"converted: $n files"
"files still containing WPF trigger/storyboard constructs: $($triggerFiles.Count)"
$triggerFiles
