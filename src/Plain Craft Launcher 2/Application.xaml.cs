using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using PCL.Core.App;
using PCL.Core.App.Essentials;
using PCL.Core.App.IoC;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI.Controls;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;

// [port] 将 PCL 命名空间注册到默认 Avalonia XML 命名空间 URI，使 Avalonia 12 的样式选择器（Selector="MyTextButton" 等）能解析本程序集的控件类型
[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/avaloniaui", "PCL")]

namespace PCL;

public partial class Application : Avalonia.Application
{
    public Application()
    {
        // 注册生命周期事件
        Lifecycle.When(LifecycleState.Loaded, _ApplicationStartup);
        Lifecycle.When(LifecycleState.WindowCreated, _ShowEnvironmentWarning);
        // [port] WPF SessionEnding 事件在 Avalonia 中无对应物，系统会话结束由进程信号处理
    }

    /// <summary>
    /// [port] Avalonia 应用初始化（加载 Application.xaml 资源字典，替代 WPF InitializeComponent）
    /// </summary>
    public override void Initialize()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        // [port] Avalonia 12 无 WPF BooleanToVisibilityConverter；Avalonia 用 IsVisible(bool)，故注册 bool→bool 恒等转换器
        Resources["BooleanToVisibilityConverter"] =
            new Avalonia.Data.Converters.FuncValueConverter<bool, bool>(b => b);
    }

    /// <summary>
    /// [port] WPF Application.Startup → Avalonia 框架初始化完成回调：
    /// 挂接 PCL.Core 生命周期（全局异常、OnLoading）并接好桌面主窗体。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        // [port] 原生命周期由 ApplicationService.Loading 工厂创建应用，Avalonia 下改为 Attach 已有实例
        ApplicationService.Attach(this);
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindowService.MainWindowSetter = window => desktop.MainWindow = window;
            // 原版为 OnExplicitShutdown，退出统一走 PCL 生命周期流程
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        base.OnFrameworkInitializationCompleted();
    }

    // 开始
    private static void _ApplicationStartup()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // [port] WPF PresentationTraceSources 绑定错误监听器为 WPF 专属，Avalonia 绑定错误走日志/诊断
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            StartupValidation.EnsureWpfFont();

            // 检查参数调用
            var args = Basics.CommandLineArguments;
            if (args.Length > 0)
                if (args[0] == "--gpu")
                    // 调整显卡设置（Windows 专属逻辑，Linux 上该分支不会被触发）
                    try
                    {
                        ModMain.SetGPUPreference(args[1].Trim('"'));
                        Environment.Exit((int)ModBase.ProcessReturnValues.TaskDone);
                    }
                    catch (Exception)
                    {
                        Environment.Exit((int)ModBase.ProcessReturnValues.Fail);
                    }

            // 初始化文件结构
            // [port] 用 Paths.Data 而非 exePath + "PCL"：Linux 上可执行文件就叫 PCL，
            //        <exe目录>/PCL/Pictures 会撞上它本身而抛 IOException（实测 WSL 启动失败）。
            Directory.CreateDirectory(System.IO.Path.Combine(PCL.Core.App.Paths.Data, "Pictures"));
            Directory.CreateDirectory(System.IO.Path.Combine(PCL.Core.App.Paths.Data, "Musics"));
            Directory.CreateDirectory(System.IO.Path.Combine(ModBase.pathTemp, "Cache"));
            Directory.CreateDirectory(System.IO.Path.Combine(ModBase.pathTemp, "Download"));
            Directory.CreateDirectory(ModBase.pathAppdata);

            // 设置 ToolTipService 默认值
            // [port] Avalonia 12 ToolTipService 为内部 API、Tooltip 自定义引擎暂缓（PCL.Core Tooltip.cs 排除），默认延迟逻辑暂缺
            // ToolTipService.InitialShowDelayProperty.OverrideDefaultValue<AvaloniaObject>(100);
            // Tooltip.Enable();

            // 设置初始窗口
            if (Config.Preference.ShowStartupLogo)
            {
                ModMain.frmStart = new SplashScreen(@"Images\icon.ico");
                ModMain.frmStart.Show(false, true);
            }

            // 设置初始化
            _ = Config.Debug.Enabled;
            _ = Config.Debug.AnimationSpeed;
            _ = Config.Network.HttpProxy.CustomAddress;
            _ = Config.Network.HttpProxy.CustomUsername;
            _ = Config.Network.HttpProxy.Type;
            _ = Config.Download.ThreadLimit;
            _ = Config.Download.SpeedLimit;
            _ = Config.Preference.Font;
            var updateBranchCfg = Config.Update.UpdateChannelConfig;
            if (updateBranchCfg.IsDefault())
                updateBranchCfg.SetValue(ModBase.versionBaseName.Contains("beta")
                    ? Core.App.UpdateChannel.Beta
                    : Core.App.UpdateChannel.Release);

            // 删除旧日志
            for (var i = 1; i <= 5; i++)
            {
                var oldLogFile = $@"{ModBase.exePath}PCL\Log-CE{i}.log";
                if (File.Exists(oldLogFile))
                    File.Delete(oldLogFile);
            }

            // 计时
            ModBase.Log("[Start] 第一阶段加载用时：" + (TimeUtils.GetTimeTick() - ModBase.applicationStartTick) + " ms");
            ModBase.applicationStartTick = TimeUtils.GetTimeTick();
            ModAnimation.AniControlEnabled += 1;
        }
        catch (Exception ex)
        {
            var filePath = Basics.ExecutablePath;
            var summary = Lang.Text("Application.InitializationError.Path",
                string.IsNullOrEmpty(filePath)
                    ? Lang.Text("Application.InitializationError.PathUnavailable")
                    : filePath);
            // [port] WPF MessageBox → PCL.Core CrashMessageBox（Avalonia 轻量弹窗）
            PCL.Core.UI.CrashMessageBox.Show(
                ExceptionDetails.Compose(summary, ex),
                Lang.Text("SystemDialog.Startup.InitializationTitle"));
            FormMain.EndProgramForce(ModBase.ProcessReturnValues.Exception);
        }
    }

    // 检测异常环境
    private static void _ShowEnvironmentWarning()
    {
        var problemList = new List<string>();
        // [port] NtInterop.GetCurrentOsVersion 为 Windows 专属；Linux 上跳过系统版本/位数检查
        if (OperatingSystem.IsWindows())
        {
            var currentOsVersion = NtInterop.GetCurrentOsVersion();
            if (currentOsVersion.Build < 17763)
                problemList.Add(Lang.Text("Application.EnvironmentWarning.WindowsVersion"));
            if (SystemInfo.Is32BitSystem)
                problemList.Add(Lang.Text("Application.EnvironmentWarning.System32Bit"));
        }
        if (ModBase.exePath.Contains(System.IO.Path.GetTempPath()) || ModBase.exePath.Contains(@"AppData\Local\Temp\"))
            problemList.Add(Lang.Text("Application.EnvironmentWarning.TempFolder"));
        if (ModBase.exePath.ContainsF("wechat_files", true) || ModBase.exePath.ContainsF("WeChat Files", true) ||
            ModBase.exePath.ContainsF("Tencent Files", true))
            problemList.Add(Lang.Text("Application.EnvironmentWarning.SocialSoftwareFolder"));
        if (problemList.Count == 0) return;

        ModMain.MyMsgBox(
            Lang.Text("Application.EnvironmentWarning.Message", problemList.Join("\r\n")),
            Lang.Text("Application.EnvironmentWarning.Title"),
            Lang.Text("Application.EnvironmentWarning.IKnow"),
            isWarn: true);
    }

    // [port] WPF SessionEnding 处理已移除（Avalonia 无对应事件；进程退出统一走 PCL 生命周期）

    /**
     * Error handling for unhandled exceptions
     * [port] 该流程已并入 PCL.Core ApplicationService.Attach 的 Dispatcher.UIThread.UnhandledException，
     * 此方法保留备用（当前无挂接点）。
     */
    private void Application_DispatcherUnhandledException(object sender, Avalonia.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            e.Handled = true;
            if (ModBase.isProgramEnded) return;

            ModBase.FeedbackInfo();

            var detail = e.Exception.ToString();

            // Automatic error analysis for environment issues
            if (detail.Contains("Avalonia.Threading.Dispatcher.Invoke") ||
                detail.Contains("MS.Internal.AppModel.ITaskbarList.HrInit") ||
                detail.Contains("未能加载文件或程序集"))
            {
                ModBase.OpenWebsite("https://get.dot.net/10");
                LogWrapper.Error(
                    e.Exception,
                    Lang.Text("SystemDialog.Startup.DotNetRuntimeOutdated.Message"));
            }
            else
            {
                LogWrapper.Error(e.Exception, Lang.Text("SystemDialog.Error.Unexpected.Message"));
            }
        }
        catch
        {
            // Equivalent to On Error Resume Next for safety in the global handler
        }
    }

    // [port] Win32 SetDllDirectory 仅 Windows 需要，Linux 无 DLL 搜索路径概念
#if WINDOWS
    [DllImport("kernel32", EntryPoint = "SetDllDirectoryA", CharSet = CharSet.Ansi)]
    private static extern bool _SetDllDirectory(string lpPathName);
#endif
    // 切换窗口

    // 控件模板事件
    private void _MyIconButtonClick(object sender, EventArgs e)
    {
    }
}
