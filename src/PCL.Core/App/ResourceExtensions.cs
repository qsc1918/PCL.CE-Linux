using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace PCL.Core.App;

// [port] WPF Application.TryFindResource/FindResource/MainWindow → Avalonia TryGetResource + 桌面生命周期。
// 以扩展方法保持上游调用点零改动。

public static class ResourceExtensions
{
    public static object? TryFindResource(this Application app, object key)
        => app.TryGetResource(key, app.ActualThemeVariant, out var value) ? value : null;

    public static object? FindResource(this Application app, object key)
        => app.TryGetResource(key, app.ActualThemeVariant, out var value) ? value : null;

    public static Window? MainWindow(this Application app)
        => (app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
