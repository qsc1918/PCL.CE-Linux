using System.Diagnostics;
using Avalonia;
using PCL.Core.App;
using PCL.Core.App.Essentials;
using PCL.Core.App.IoC;
using PCL.Core.Utils.OS;

namespace PCL;

// [port] WPF Application.Run() → Avalonia AppBuilder 经典桌面生命周期：
// - Application 实例由 AppBuilder 创建，ApplicationService.Attach 在 OnFrameworkInitializationCompleted 挂接；
// - 主循环通过 Lifecycle.LifetimeRunner 注入；
// - WPF 专属的 Tablet.TabletDevices 修复移除（Windows 兼容后续用 #if WINDOWS 保留思路）。

internal static class Program
{
    /// <summary>
    /// Program startup point
    /// </summary>
    [STAThread]
    public static void Main()
    {
        if (Basics.CommandLineArguments.Contains("--console")) KernelInterop.AllocateConsole();
#if DEBUG
        if (Basics.CommandLineArguments.Contains("--debug"))
        {
            Console.WriteLine("Waiting for debugger...");
            while (!Debugger.IsAttached) Thread.Sleep(50);
        }
#endif
        Console.WriteLine("Welcome to Plain Craft Launcher 2 Community Edition!");
        // 主窗体工厂（Avalonia 下 Application 由 AppBuilder 创建，不再由工厂提供）
        MainWindowService.Loading = static () =>
        {
            var form = new FormMain();
            return form;
        };
        // [port] 注入 Avalonia 主循环运行器
        Lifecycle.LifetimeRunner = static () =>
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(Basics.CommandLineArguments);
        };
        // Start lifecycle
        Lifecycle.OnInitialize();
    }

    // [port] Avalonia 应用构建器
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<Application>();
        builder.UsePlatformDetect();
        return builder;
    }
}
