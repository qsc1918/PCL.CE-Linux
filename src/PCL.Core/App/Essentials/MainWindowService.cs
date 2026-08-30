using System;
using Avalonia;
using Avalonia.Controls;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Essentials;

// [port] WPF Application.MainWindow → Avalonia 桌面生命周期 IClassicDesktopStyleApplicationLifetime.MainWindow。

[LifecycleService(LifecycleState.WindowCreating, Priority = int.MaxValue)]
public sealed class MainWindowService : GeneralService
{
    public static Func<Window>? Loading { private get; set; }

    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private MainWindowService() : base("window", "主窗体", false) { _context = ServiceContext; }

    public override void Start()
    {
        Context.Debug("正在初始化主窗体");
        var window = Loading!.Invoke();
        window.Loaded += (_, _) => Lifecycle.OnWindowCreated();
        if (MainWindowSetter is { } setter) setter(window);
        Context.Trace("窗体创建完毕");
    }

    /// <summary>
    /// 由应用启动流程注入的 MainWindow 赋值器（Avalonia 的 MainWindow 归桌面生命周期所有）。
    /// </summary>
    public static Action<Window>? MainWindowSetter { get; set; }
}
