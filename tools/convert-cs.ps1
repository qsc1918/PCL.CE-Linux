# WPF -> Avalonia deterministic converter (ASCII only; see docs/PORTING-MAPPING.md)
# Usage: & tools/convert-cs.ps1 -Root <dir>
param([Parameter(Mandatory=$true)][string]$Root)

$usings = [ordered]@{
    'using System\.Windows\.Media\.Imaging;'      = "using Avalonia.Media.Imaging;"
    'using System\.Windows\.Media\.Effects;'      = "using Avalonia.Media;"
    'using System\.Windows\.Media\.Animation;'    = "using Avalonia.Animation;"
    'using System\.Windows\.Media;'               = "using Avalonia.Media;"
    'using System\.Windows\.Controls;'            = "using Avalonia.Controls;`r`nusing Avalonia.Controls.Shapes;`r`nusing Avalonia.Interactivity;"
    'using System\.Windows\.Input;'               = "using Avalonia.Input;"
    'using System\.Windows\.Data;'                = "using Avalonia.Data;`r`nusing Avalonia.Data.Converters;"
    'using System\.Windows\.Threading;'           = "using Avalonia.Threading;"
    'using System\.Windows\.Markup;'              = "using Avalonia.Markup;"
    'using System\.Windows\.Documents;'           = "using Avalonia.Controls.Documents;"
    'using System\.Windows\.Shell;'               = "// [port] System.Windows.Shell removed (WindowChrome/Taskbar)"
    'using System\.Windows\.Interop;'             = "// [port] System.Windows.Interop removed"
    'using System\.Windows\.Resources;'           = "// [port] System.Windows.Resources removed"
    'using System\.Windows;'                      = "using Avalonia;`r`nusing Avalonia.Controls;`r`nusing Avalonia.Media;`r`nusing Avalonia.Interactivity;`r`nusing Avalonia.Layout;"
    'using Microsoft\.Xaml\.Behaviors;'           = "using Avalonia.Xaml.Interactivity;"
    'using System\.Windows\.'                     = "using Avalonia."
}

$renames = [ordered]@{
    '\bFrameworkElement\b'                        = 'Control'
    '\bUIElement\b'                               = 'Control'
    '\bDependencyObject\b'                        = 'AvaloniaObject'
    '\bDependencyProperty\b'                      = 'AvaloniaProperty'
    '\bDependencyPropertyChangedEventArgs\b'      = 'AvaloniaPropertyChangedEventArgs'
    '\bDependencyPropertyChangedCallback\b'       = 'AvaloniaPropertyChangedCallback'
    '\.ActualWidth\b'                             = '.Bounds.Width'
    '\.ActualHeight\b'                            = '.Bounds.Height'
    '\bSystem\.Windows\.Media\.'                  = 'Avalonia.Media.'
    '\bSystem\.Windows\.Controls\.'               = 'Avalonia.Controls.'
    '\bSystem\.Windows\.'                         = 'Avalonia.'
}

# XAML purely mechanical part
$xamlRules = [ordered]@{
    'http://schemas\.microsoft\.com/winfx/2006/xaml/presentation' = 'https://github.com/avaloniaui'
    '\s+SnapsToDevicePixels="(True|False)"'                       = ''
    'pack://application:,,,/([^;]+);component/'                   = 'avares://$1/'
}

$csCount = 0; $xamlCount = 0
Get-ChildItem $Root -Recurse -Include *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $orig = $t
    foreach ($k in $usings.Keys) { $t = [regex]::Replace($t, $k, $usings[$k]) }
    foreach ($k in $renames.Keys) { $t = [regex]::Replace($t, $k, $renames[$k]) }
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($_.FullName, $t); $csCount++ }
}
Get-ChildItem $Root -Recurse -Include *.xaml -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $orig = $t
    foreach ($k in $xamlRules.Keys) { $t = [regex]::Replace($t, $k, $xamlRules[$k]) }
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($_.FullName, $t); $xamlCount++ }
}
"Converted .cs: $csCount, .xaml: $xamlCount (under $Root)"
