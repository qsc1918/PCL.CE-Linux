using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace PCL.Core.App;

// [port] WPF Application.TryFindResource/FindResource/MainWindow → Avalonia TryGetResource + 桌面生命周期。
// 以扩展方法保持上游调用点零改动。

public static class ResourceExtensions
{
    // [port] 线程安全资源缓存。
    // Avalonia 的 Application.Resources / ActualThemeVariant 是 AvaloniaObject 上的属性，
    // 只能在 UI 线程访问；从加载线程调用会抛 InvalidOperationException(VerifyAccess)。
    // 上游 WPF 的资源查找无此限制，故此处用缓存还原"任意线程可读"的行为：
    //  - UI 线程：照常探测真实资源，并写入缓存；
    //  - 非 UI 线程：只读取缓存（不跨线程封送，避免死锁）；未命中时返回 null，
    //    由 Lang.Text 的 _LifecycleSafeFindResource 兜底。
    // 主题切换（明暗）会使资源值变化，故订阅 ActualThemeVariant 变化并清空缓存。
    private static readonly ConcurrentDictionary<object, object?> _resourceCache = new();

    public static object? TryFindResource(this Application app, object key)
    {
        if (app is null || key is null) return null;
        if (_resourceCache.TryGetValue(key, out var cached)) return cached;

        if (!Dispatcher.UIThread.CheckAccess()) return null;

        _EnsureThemeHook(app);
        var value = app.TryGetResource(key, app.ActualThemeVariant, out var v) ? v : null;
        _resourceCache[key] = value;
        return value;
    }

    public static object? FindResource(this Application app, object key)
        => app.TryFindResource(key);

    public static Window? MainWindow(this Application app)
        => (app.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    private static bool _themeHooked;

    private static void _EnsureThemeHook(Application app)
    {
        if (_themeHooked) return;
        _themeHooked = true;
        app.PropertyChanged += (_, e) =>
        {
            if (e.Property == Application.ActualThemeVariantProperty) _resourceCache.Clear();
        };
    }
}
