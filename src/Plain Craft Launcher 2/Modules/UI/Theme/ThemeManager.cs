using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.VisualTree;
using Path = Avalonia.Controls.Shapes.Path;
using PCL.Core.App;
using PCL.Core.UI.Theme;

namespace PCL;

public static class ThemeManager
{
    private static bool _contextMenuHandlerRegistered;

    public static bool IsDarkMode => ThemeService.IsDarkMode;

    public static ResourceDictionary AppResources => (ResourceDictionary)Avalonia.Application.Current.Resources;

    public static ModBase.MyColor colorGray1 = new(AppResources["ColorObjectGray1"]);
    public static ModBase.MyColor colorGray4 = new(AppResources["ColorObjectGray4"]);
    public static ModBase.MyColor colorGray5 = new(AppResources["ColorObjectGray5"]);
    public static ModBase.MyColor colorSemiTransparent = new(AppResources["ColorBrushSemiTransparent"]);

    public static void ThemeRefresh(int newTheme = -1)
    {
        colorGray1 = new ModBase.MyColor(AppResources["ColorObjectGray1"]);
        colorGray4 = new ModBase.MyColor(AppResources["ColorObjectGray4"]);
        colorGray5 = new ModBase.MyColor(AppResources["ColorObjectGray5"]);
        colorSemiTransparent = new ModBase.MyColor(AppResources["ColorBrushSemiTransparent"]);
        ThemeRefreshMain();
    }

    public static void ThemeRefreshMain()
    {
        ModBase.RunInUi(() =>
        {
            if (!ModMain.frmMain.IsLoaded) return;
            RefreshBackground();
            RefreshAllContextMenuThemes();
        });
    }
    
    // 主页面背景
    private static void RefreshBackground()
    {
        if (Config.Preference.Background.BackgroundColorful)
        {
            var brush = new LinearGradientBrush
            {
                // [port] WPF LinearGradientBrush 端点用 Point → Avalonia 用 RelativePoint
                EndPoint = new RelativePoint(0.1, 1, RelativeUnit.Relative),
                StartPoint = new RelativePoint(0.9, 0, RelativeUnit.Relative)
            };

            var hue = ThemeService.GetCurrentThemeArgs().Hue;
            var hue1 = hue - 15;
            var hue2 = hue + 15;
            var tone = ThemeService.CurrentTone;
            var darkLight = IsDarkMode ? 0.2d : 1d;
            brush.GradientStops.Add(new GradientStop
                { Offset = -0.1d, Color = LabColor.FromLch(0.84d * darkLight, tone.C5, hue1) });
            brush.GradientStops.Add(new GradientStop
                { Offset = 0.4d, Color = LabColor.FromLch(0.96d * darkLight, tone.C7, hue) });
            brush.GradientStops.Add(new GradientStop
                { Offset = 1.1d, Color = LabColor.FromLch(0.84d * darkLight, tone.C5, hue2) });
            ModMain.frmMain.PanForm.Background = brush;
        }
        else
        {
            ModMain.frmMain.PanForm.Background = (Brush)Avalonia.Application.Current.Resources["ColorBrushBackground"];
        }

        // [port] WPF IBrush.Freeze() → Avalonia 画刷已不可变，无需冻结
    }

    // 通用ContextMenu主题刷新
    private static void RefreshAllContextMenuThemes()
    {
        try
        {
            if (!_contextMenuHandlerRegistered)
            {
                // [port] WPF EventManager.RegisterClassHandler + RoutedEventHandler → Avalonia 静态类处理器
                ContextMenu.OpenedEvent.AddClassHandler<ContextMenu>((o, e) => OnContextMenuOpened(o, e));
                _contextMenuHandlerRegistered = true;
            }

            // [port] WPF Application.Current.Windows → Avalonia 通过桌面应用生命周期枚举窗口
            if (Avalonia.Application.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                foreach (Window window in desktop.Windows)
                    RefreshContextMenusInElement(window);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "刷新ContextMenu主题时出错");
        }
    }

    private static void OnContextMenuOpened(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is ContextMenu contextMenu)
            {
                // [port] WPF ClearValue(Control.StyleProperty)+UpdateDefaultStyle 无 Avalonia 对应；主题自动应用，无需手动刷新
                _ = contextMenu;
            }
        }
        catch
        {
            // 忽略个别错误
        }
    }

    private static void RefreshContextMenusInElement(AvaloniaObject element)
    {
        if (element is null)
            return;

        try
        {
            if (element is Control { ContextMenu: not null } fe)
            {
                // [port] WPF ClearValue(Control.StyleProperty)+UpdateDefaultStyle 无 Avalonia 对应；主题自动应用，无需手动刷新
                _ = fe;
            }

            if (element is Avalonia.Visual visual)
                foreach (var child in visual.GetVisualChildren())
                    RefreshContextMenusInElement(child);
        }
        catch
        {
            // 忽略个别元素的错误，继续处理其他元素
        }
    }
}
