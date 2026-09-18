#!/usr/bin/env pwsh
# Bulk mechanical XAML fixes for Avalonia 12 (WPF -> Avalonia attribute mapping)
# Pure ASCII script.
$ErrorActionPreference = 'Stop'
$root = "D:\a\j\PCL.CE-Linux\src\Plain Craft Launcher 2"
$files = Get-ChildItem -Recurse -Include *.axaml -Path $root
$changed = 0
foreach ($f in $files) {
    $c = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)
    $o = $c

    # 1. ToolTip="..." -> ToolTip.Tip="..." (Avalonia attached property)
    $c = $c -replace 'ToolTip="', 'ToolTip.Tip="'

    # 2. Remove RenderOptions.BitmapScalingMode="..." (WPF-only on Avalonia)
    $c = $c -replace '\s+RenderOptions\.BitmapScalingMode="[^"]*"', ''

    # 3. Remove ShadowDepth="..." (WPF DropShadowEffect-only)
    $c = $c -replace '\s+ShadowDepth="[^"]*"', ''

    # 4. Visibility enum -> IsVisible bool
    $c = $c -replace 'Visibility="Collapsed"', 'IsVisible="False"'
    $c = $c -replace 'Visibility="Hidden"', 'IsVisible="False"'
    $c = $c -replace 'Visibility="Visible"', 'IsVisible="True"'

    # 5. Remove x:Name on Transform / Effect elements (Avalonia cannot name them)
    $c = [regex]::Replace($c,
        '(<(?:DropShadowEffect|TranslateTransform|RotateTransform|ScaleTransform|SkewTransform)\b(?:(?!>).)*?)\s+x:Name="[^"]*"',
        '$1',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)

    # 6. Remove TextFormattingMode="..." (WPF TextOptions-only)
    $c = $c -replace '\s+TextFormattingMode="[^"]*"', ''

    # 7. AllowDrop="..." -> DragDrop.AllowDrop="..." (Avalonia attached property)
    $c = $c -replace '\bAllowDrop="', 'DragDrop.AllowDrop="'

    if ($c -ne $o) {
        [System.IO.File]::WriteAllText($f.FullName, $c, [System.Text.UTF8Encoding]::new($false))
        $changed++
        Write-Host "fixed: $($f.Name)"
    }
}
Write-Host "total changed: $changed"
