using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Essentials;

// [port] WPF Application 事件 → Avalonia 生命周期：
// DispatcherUnhandledException → Dispatcher.UIThread.UnhandledException；
// Application.Startup → IControlledApplicationLifetime.Startup（无桌面生命周期时回退 FrameworkInitializationCompleted）；
// Avalonia 模式下 Application 实例由 AppBuilder 创建，通过 Attach() 在 FrameworkInitializationCompleted 时挂接。

[LifecycleService(LifecycleState.BeforeLoading, Priority = int.MinValue)]
[LifecycleScope("application", "应用程序", false)]
public sealed partial class ApplicationService
{
    /// <summary>
    /// WPF 模式的应用程序工厂。Avalonia 模式下保持为空，Application 由 AppBuilder 创建。
    /// </summary>
    public static Func<Application>? Loading { private get; set; }

    [LifecycleStart]
    private static void _Start()
    {
        Context.Debug("正在初始化应用程序容器");
        var app = Loading?.Invoke();
        if (app is null)
        {
            Context.Info("Avalonia 模式：应用程序容器由 AppBuilder 创建，挂接延迟到 FrameworkInitializationCompleted");
            return;
        }
        Attach(app);
        Context.Trace("应用程序容器初始化完毕");
    }

    /// <summary>
    /// [port] Avalonia 入口：由应用侧 Application.OnFrameworkInitializationCompleted 调用。
    /// 挂接全局异常处理与 Startup → OnLoading 流程，并记录 CurrentApplication。
    /// </summary>
    public static void Attach(Application app)
    {
        Dispatcher.UIThread.UnhandledException += (_, e) => Lifecycle.OnException(e.Exception);
        var lifetime = app.ApplicationLifetime as IControlledApplicationLifetime;
        // [port] Avalonia 12 无 Application.FrameworkInitializationCompleted 事件，桌面生命周期用 Startup
        if (lifetime is not null)
            lifetime.Startup += (_, _) => Lifecycle.OnLoading();
        else
            Context.Warn("未检测到桌面生命周期，OnLoading 将不会自动触发");
        Lifecycle.CurrentApplication = app;
        Context.Trace("应用程序容器初始化完毕");
    }

    [LifecycleStop]
    private static void _Stop()
    {
        var app = Lifecycle.CurrentApplication;
        var lifetime = app?.ApplicationLifetime as IControlledApplicationLifetime;
        if (Lifecycle.IsForceShutdown)
        {
            Context.Warn("已指定强制关闭，跳过标准关闭流程");
            return;
        }
        if (lifetime is null) return;
        using var exited = new ManualResetEventSlim();
        Dispatcher.UIThread.Post(() =>
        {
            lifetime.Exit += Exited;
            Context.Debug("发起应用退出流程");
            lifetime.Shutdown();
        }, DispatcherPriority.Send);
        try
        {
            Context.Debug("正在等待应用程序容器退出");
            var result = exited.Wait(5000);
            if (result) Context.Trace("应用程序容器已退出");
            else Context.Warn("应用程序容器退出超时，停止等待");
        }
        finally
        {
            Dispatcher.UIThread.Post(() => lifetime.Exit -= Exited, DispatcherPriority.Send);
        }
        return;

        void Exited(object? sender, EventArgs e)
        {
            // ReSharper disable once AccessToDisposedClosure
            exited.Set();
        }
    }
}
