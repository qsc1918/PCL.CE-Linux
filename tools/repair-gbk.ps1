# Repair GBK-corrupted files: recover original bytes from baseline commit,
# decode as GBK(936), re-apply conversion rules, write UTF-8. ASCII only!
param([Parameter(Mandatory=$true)][string]$RepoRoot)

$ErrorActionPreference = 'Stop'
$enc936 = [System.Text.Encoding]::GetEncoding(936)
$enc8 = [System.Text.UTF8Encoding]::new($false)
$baseline = '6a525ab'

$csRules = [ordered]@{
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
    '\.Visibility = Visibility\.Collapsed'        = '.IsVisible = false'
    '\.Visibility = Visibility\.Hidden'           = '.IsVisible = false'
    '\.Visibility = Visibility\.Visible'          = '.IsVisible = true'
    '!= Visibility\.Visible'                      = '!= true'
    '== Visibility\.Visible'                      = '== true'
    '\(Visibility\)value != Visibility\.Visible'  = 'value is false'
    'Visibility\.Collapsed'                       = 'false'
    'Visibility\.Hidden'                          = 'false'
    'Visibility\.Visible'                         = 'true'
    '\.Visibility = '                             = '.IsVisible = '
    'MouseLeftButtonDown'                         = 'PointerPressed'
    'MouseLeftButtonUp'                           = 'PointerReleased'
    'MouseButtonEventArgs'                        = 'PointerPressedEventArgs'
    'MouseEventArgs'                              = 'PointerEventArgs'
    'MouseWheelEventArgs'                         = 'PointerWheelEventArgs'
    'MouseEnter'                                  = 'PointerEntered'
    'MouseLeave'                                  = 'PointerExited'
    'MouseUp'                                     = 'PointerReleased'
}

$files = @(
    'Pages\PageInstance\PageInstanceCompResource.xaml.cs',
    'Pages\PageInstance\PageInstanceInstall.xaml.cs',
    'Pages\PageInstance\PageInstanceExport.xaml.cs',
    'Pages\PageInstance\PageInstanceSaves\PageInstanceSavesDatapack.xaml.cs',
    'Pages\PageInstance\PageInstanceSaves\PageInstanceSavesInfo.xaml.cs',
    'Pages\PageInstance\PageInstanceSaves\PageInstanceSavesLeft.xaml.cs',
    'Pages\PageInstance\PageInstanceSetup.xaml.cs',
    'Pages\PageInstance\PageInstanceOverall.xaml.cs',
    'Pages\PageInstance\PageInstanceSaves.xaml.cs',
    'Pages\PageInstance\PageInstanceServer.xaml.cs',
    'Pages\PageInstance\PageInstanceScreenshot.xaml.cs',
    'Pages\PageInstance\PageInstanceLeft.xaml.cs',
    'Pages\PageInstance\MyLocalModItem.xaml.cs',
    'Pages\PageInstance\ServerCard.xaml.cs',
    'Pages\PageSetup\PageSetupLeft.xaml.cs',
    'Pages\PageSetup\PageSetupUI.xaml.cs',
    'Pages\PageSetup\PageSetupLaunch.xaml.cs',
    'Pages\PageSetup\PageSetupUpdate.xaml.cs',
    'Pages\PageSetup\PageSetupGameLink.xaml.cs',
    'Pages\PageSetup\PageSetupGameManage.xaml.cs',
    'Pages\PageSetup\PageSetupLauncherLanguage.xaml.cs',
    'Pages\PageSetup\PageSetupLauncherMisc.xaml.cs',
    'Pages\PageSetup\PageSetupLog.xaml.cs',
    'Pages\PageSetup\PageSetupJava.xaml.cs',
    'Pages\PageSetup\PageSetupAbout.xaml.cs',
    'Pages\PageSetup\PageSetupFeedback.xaml.cs'
)

foreach ($rel in $files) {
    $full = Join-Path $RepoRoot $rel
    $gitPath = 'src/Plain Craft Launcher 2/' + ($rel -replace '\\','/')
    $tmp = [System.IO.Path]::GetTempFileName()
    cmd /c "git -C `"$RepoRoot`" show ${baseline}:`"$gitPath`" > `"$tmp`"" 2>$null
    if ((Get-Item $tmp).Length -eq 0) { "MISS IN GIT: $rel"; Remove-Item $tmp -Force; continue }
    $bytes = [System.IO.File]::ReadAllBytes($tmp)
    Remove-Item $tmp -Force
    # auto-detect encoding: strict UTF-8 first, fallback GBK
    $strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
    try { $text = $strictUtf8.GetString($bytes); $enc = 'utf8' } catch { $text = $enc936.GetString($bytes); $enc = 'gbk' }
    foreach ($k in $csRules.Keys) { $text = [regex]::Replace($text, $k, $csRules[$k]) }
    [System.IO.File]::WriteAllText($full, $text, $enc8)
    "repaired ($enc): $rel"
}
'done'
