# Bulk pattern fixes for PageInstance/PageSetup .cs (ASCII only)
param([Parameter(Mandatory=$true)][string]$Root)

$rules = [ordered]@{
    '\.Visibility = Visibility\.Collapsed'      = '.IsVisible = false'
    '\.Visibility = Visibility\.Hidden'         = '.IsVisible = false'
    '\.Visibility = Visibility\.Visible'        = '.IsVisible = true'
    '!= Visibility\.Visible'                    = '!= true'
    '== Visibility\.Visible'                    = '== true'
    '\(Visibility\)value != Visibility\.Visible' = 'value is false'
    'Visibility\.Collapsed'                     = 'false'
    'Visibility\.Hidden'                        = 'false'
    'Visibility\.Visible'                       = 'true'
    '\.Visibility = '                           = '.IsVisible = '
    'MouseLeftButtonDown'                       = 'PointerPressed'
    'MouseLeftButtonUp'                         = 'PointerReleased'
    'MouseButtonEventArgs'                      = 'PointerPressedEventArgs'
    'MouseEventArgs'                            = 'PointerEventArgs'
    'MouseWheelEventArgs'                       = 'PointerWheelEventArgs'
    'MouseWheel='                               = 'PointerWheelChanged='
    'MouseEnter'                                = 'PointerEntered'
    'MouseLeave'                                = 'PointerExited'
    'MouseUp'                                   = 'PointerReleased'
    'System\.Windows\.Forms\.Orientation\.Horizontal' = 'System.Windows.Controls.Orientation.Horizontal'
}

$n = 0
Get-ChildItem $Root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $orig = $t
    foreach ($k in $rules.Keys) { $t = [regex]::Replace($t, $k, $rules[$k]) }
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($_.FullName, $t); $n++ }
}
"files changed: $n"
