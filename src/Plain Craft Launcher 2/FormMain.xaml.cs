using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;
// [port] WPF DataFormats/Clipboard 文件拖拽 → Avalonia IDataTransfer + IStorageItem（TryGetFiles/TryGetLocalPath）
using Avalonia.Platform.Storage;
// [port] Avalonia.Interop removed
using PCL.Core.App;
using PCL.Core.App.IoC;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.Minecraft;
using PCL.Core.UI;
using PCL.Core.UI.Theme;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Validate;
using PCL.Network;

namespace PCL;

public partial class FormMain : Window
{
    // 愚人节鼠标位置
    // [port] WPF PointerEventArgs → Avalonia PointerEventArgs（PointerMoved 事件参数）
    public PointerEventArgs lastMouseArg;

    // [port] WPF MouseMove → Avalonia PointerMoved；事件参数 PointerEventArgs → PointerEventArgs
    private void FormMain_MouseMove(object? sender, PointerEventArgs e)
    {
        lastMouseArg = e;
    }

    // [port] 左键按下状态跟踪：Avalonia 无 WPF 的静态 Mouse.LeftButton/MouseButtonState 查询；
    //        DragTick/DragDoing 需在无事件参数时判断左键是否按下，故用指针事件维护该字段。
    private bool mouseLeftButtonPressed;

    // [port] Avalonia 命名字段生成器不生成 RenderTransform（TransformGroup/Clip）内部的 x:Name 字段；
    //        TransformRotate/TransformPos 在 InitializeComponent 后由 RootGrid.RenderTransform 取出；先赋默认值避免空引用。
    private RotateTransform TransformRotate = new();
    private TranslateTransform TransformPos = new();

    #region 基础

    // 更新日志
    private void ShowUpdateLog()
    {
        ModBase.RunInNewThread(() =>
        {
            var changelogFile = $"{ModBase.pathTemp}CEUpdateLog.md";
            string changelog;
            if (File.Exists(changelogFile))
                changelog = ModBase.ReadFile(changelogFile);
            else
                changelog = Lang.Text("Main.UpdateLog.Empty");
            if (ModMain.MyMsgBoxMarkdown(changelog,
                    Lang.Text("Main.UpdateLog.Title", ModBase.versionBranchName, ModBase.versionBaseName), Lang.Text("Common.Action.Confirm"), Lang.Text("Main.UpdateLog.FullChangelog")) ==
                2) ModBase.OpenWebsite("https://github.com/PCL-Community/PCL2-CE/releases");
        }, "UpdateLog Output");
    }

    // 窗口加载
    private bool isWindowLoadFinished;
    // [port] Win32 管理员拖拽助手（DragHelper 为 Windows-only；PCL.Core 已在 Linux 编译目标中排除）→ 仅 #if WINDOWS 保留
#if WINDOWS
    private readonly DragHelper _helper = new();
#endif

    public FormMain()
    {
        // [port] 上游 Window 的 Background="{x:Null}" 是配合 WPF AllowsTransparency="False" 用的 ——
        //        那时窗口底由系统提供，始终不透明。Avalonia 下 Window.Background 为 null 意味着
        //        客户区真的透明，在 Linux（无合成器/无 alpha 通道）上整个窗口会变成透明的一片。
        //        非 Windows 平台补一个跟随主题的不透明背景，Windows 保持原样不受影响。
        if (!OperatingSystem.IsWindows())
            Background = this.TryFindResource("ColorBrushBackground", out var bg) && bg is IBrush brush
                ? brush
                : new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x2E));

        ModBase.applicationStartTick = TimeUtils.GetTimeTick();
        // 刷新主题
        // ThemeCheckAll(False)
        // ThemeRefreshColor()
        ThemeService.ColorModeChanged += (_, _) => ThemeManager.ThemeRefresh();
        ThemeService.ColorThemeChanged += theme => ThemeManager.ThemeRefresh((int)theme);
        // 窗体参数初始化
        ModMain.frmMain = this;
        ModMain.frmLaunchLeft = new PageLaunchLeft();
        ModMain.frmLaunchRight = new PageLaunchRight();
        // 版本号改变
        var lastVersion = States.System.LastVersion;
        if (lastVersion < ModBase.versionCode)
        {
            // 重新询问是否启用遥测数据收集
            if (lastVersion <= 511)
            {
                if (!Config.System.TelemetryConfig.IsDefault() && Config.System.Telemetry)
                {
                    Config.System.TelemetryConfig.Reset();
                    ModBase.Log("[Start] 遥测策略变更：由旧版本升级到含新版遥测的版本，已重置遥测设置");
                }
            }
            // 触发升级
            UpgradeSub(lastVersion);
        }
        else if (lastVersion > ModBase.versionCode)
            // 触发降级
            DowngradeSub(lastVersion);

        _ = Config.Preference.Theme.ThemeSelected;
        // 注册拖拽事件（不能直接加 Handles，否则没用；#6340）
        // [port] WPF AddHandler(DragDrop.DragEnterEvent, new DragEventHandler(...), true) → Avalonia 12 DragDrop.AddDragEnterHandler（无 DragEventHandler 委托；handledEventsToo 由内部注册承载）
        DragDrop.AddDragEnterHandler(this, HandleDrag);
        DragDrop.AddDragOverHandler(this, HandleDrag);
        // [port] WPF Window.Drop 事件 → Avalonia DragDrop.AddDropHandler；WPF Window.StateChanged 事件 → Avalonia PropertyChanged(WindowStateProperty)
        DragDrop.AddDropHandler(this, FrmMain_Drop);
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty) WindowStateChanged(this, EventArgs.Empty);
        };
        // 注册 MsgBox 事件
        MsgBoxWrapper.OnShow += ModMain.MsgBoxWrapper_OnShow;
        // 注册 Hint 事件
        HintWrapper.OnShow += HintService.HintWrapper_OnShow;
        // 加载 UI
        InitializeComponent();
        // [port] Avalonia 命名字段生成器不生成 RenderTransform 内的 x:Name 字段 → 从根 Grid.RenderTransform 手工取出 TransformRotate/TransformPos（Axis: Rotate 在 [0]，Translate 在 [1]）
        if (RootGrid.RenderTransform is TransformGroup rootTransform)
        {
            TransformRotate = (RotateTransform)rootTransform.Children[0];
            TransformPos = (TranslateTransform)rootTransform.Children[1];
        }
        // [port] XAML 上已移除 Activated 属性（Avalonia Window 的激活事件由 WindowBase.Activated 承载），改在此处重新接线
        Activated += FormMain_Activated;
        Opacity = 0d;
        try
        {
            Height = States.UI.WindowHeight;
            Width = States.UI.WindowWidth;
        }
        catch (Exception ex) // 修复 #2019
        {
            ModBase.Log(
                ex,
                "读取窗口默认大小失败",
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
            Height = MinHeight + 100d;
            Width = MinWidth + 100d;
        }

        // 管理员权限下文件拖拽
        // [port] Win32 管理员拖拽（SourceInitialized + WindowInteropHelper/HwndSource + DragHelper.AddHook/RemoveHook/DragDrop）→ 仅 #if WINDOWS 保留；Linux 目标暂缓
#if WINDOWS
        if (ProcessInterop.IsAdmin())
        {
            ModBase.Log("[Start] PCL 当前正以管理员权限运行");
            SourceInitialized += (_, _) =>
            {
                var windowInterop = new WindowInteropHelper(this);
                _helper.HwndSource = HwndSource.FromHwnd(windowInterop.Handle);
                _helper.AddHook();
            };
            Closing += (_, _) => _helper.RemoveHook();
            _helper.DragDrop += (_, _) => FileDrag(_helper.DropFilePaths);
        }
#endif

        // [port] WPF ContentPresenter（Avalonia 12 移至 Avalonia.Controls.Presenters.ContentPresenter）→ 全限定类型；语义不变：清空上一宿主 ContentPresenter 的内容以允许重新挂载
        if (ModMain.frmLaunchLeft.Parent is not null)
            ModMain.frmLaunchLeft.SetValue(Avalonia.Controls.Presenters.ContentPresenter.ContentProperty, null);
        if (ModMain.frmLaunchRight.Parent is not null)
            ModMain.frmLaunchRight.SetValue(Avalonia.Controls.Presenters.ContentPresenter.ContentProperty, null);
        PanMainLeft.Child = ModMain.frmLaunchLeft;
        pageLeft = ModMain.frmLaunchLeft;
        PanMainRight.Child = ModMain.frmLaunchRight;
        pageRight = ModMain.frmLaunchRight;
        ModMain.frmLaunchRight.PageState = MyPageRight.PageStates.ContentStay;
        // 调试模式提醒
        if (ModBase.modeDebug)
            HintService.Hint(Lang.Text("Main.DebugMode.Hint"));
        // 尽早执行的加载池
        ModFolder.mcFolderListLoader
            .Start(0); // 为了让下载已存在文件检测可以正常运行，必须跑一次；为了让启动按钮尽快可用，需要尽早执行；为了与 PageLaunchLeft 联动，需要为 0 而不是 GetUuid

        ModBase.Log("[Start] 第二阶段加载用时：" + (TimeUtils.GetTimeTick() - ModBase.applicationStartTick) + " ms");
        // 注册生命周期状态事件
        Lifecycle.When(LifecycleState.WindowCreated, FormMain_Loaded);
    }

    // [port][TEMP] 页面实例化自检（移植期诊断用；仅在 --pagetest 时执行，不影响正常流程）。
    // 覆盖三层，全部在 UI 线程上执行，逐项 try/catch 并写日志（[PageTest] 前缀）：
    //   1) 直接构造各页面的 Left/Right 控件 —— 捕捉 XAML 填充期异常
    //      （命名字段为 null、集合属性 MyListItem.Buttons 填充、AvaloniaProperty 字段可见性等）；
    //   2) 调 Left.PageGet(subType) —— 覆盖由 PageGet 惰性创建的右侧页面；
    //   3) 真实 PageChange(pageType) 导航 —— 复现用户点击行为（含 PageChangeActual 全链路）。
    // 用法：PCL.exe --pagetest
    internal static void RunPageInstantiationSelfTest()
    {
        var ok = 0;
        var fail = 0;

        void Check(string name, Action action)
        {
            try
            {
                action();
                ok += 1;
                ModBase.Log($"[PageTest] OK   {name}");
            }
            catch (Exception ex)
            {
                fail += 1;
                ModBase.Log(ex, $"[PageTest] FAIL {name}");
            }
        }

        // ---- 1) 直接构造 ----
        Check("new PageLaunchLeft", static () => _ = new PageLaunchLeft());
        Check("new PageLaunchRight", static () => _ = new PageLaunchRight());
        Check("new PageDownloadLeft", static () => _ = new PageDownloadLeft());
        Check("new PageSetupLeft", static () => _ = new PageSetupLeft());
        Check("new PageSelectLeft", static () => _ = new PageSelectLeft());
        Check("new PageSelectRight", static () => _ = new PageSelectRight());
        Check("new PageSpeedLeft", static () => _ = new PageSpeedLeft());
        Check("new PageSpeedRight", static () => _ = new PageSpeedRight());
        Check("new PageInstanceLeft", static () => _ = new PageInstanceLeft());
        Check("new PageInstanceSavesLeft", static () => _ = new PageInstanceSavesLeft());
        Check("new PageDownloadCompDetail", static () => _ = new PageDownloadCompDetail());
        Check("new PageLogLeft", static () => _ = new PageLogLeft());
        Check("new PageLogRight", static () => _ = new PageLogRight());

        // ---- 2) PageGet：惰性创建右侧页面 ----
        // 注意：PageSubType 各段（Download*/Setup*/Version*）数值重叠，Enum.ToString() 可能显示同值的
        // 其它段名字，故标签同时打印数值以便核对。
        var downloadLeft = new PageDownloadLeft();
        foreach (var sub in new[]
                 {
                     PageSubType.DownloadInstall, PageSubType.DownloadMod, PageSubType.DownloadPack,
                     PageSubType.DownloadDataPack, PageSubType.DownloadResourcePack, PageSubType.DownloadShader,
                     PageSubType.DownloadWorld, PageSubType.DownloadCompFavorites, PageSubType.DownloadClient,
                     PageSubType.DownloadOptiFine, PageSubType.DownloadForge, PageSubType.DownloadNeoForge,
                     PageSubType.DownloadCleanroom, PageSubType.DownloadFabric, PageSubType.DownloadLiteLoader,
                     PageSubType.DownloadLabyMod, PageSubType.DownloadLegacyFabric
                 })
        {
            var captured = sub;
            Check($"PageDownloadLeft.PageGet({(int)sub})", () => _ = downloadLeft.PageGet(captured));
        }

        var setupLeft = new PageSetupLeft();
        foreach (var sub in new[]
                 {
                     PageSubType.SetupLaunch, PageSubType.SetupUI, PageSubType.SetupGameManage,
                     PageSubType.SetupLink, PageSubType.SetupAbout, PageSubType.SetupLog,
                     PageSubType.SetupFeedback, PageSubType.SetupGameLink, PageSubType.SetupUpdate,
                     PageSubType.SetupJava, PageSubType.SetupLauncherMisc, PageSubType.SetupLauncherLanguage
                 })
        {
            var captured = sub;
            Check($"PageSetupLeft.PageGet({(int)sub})", () => _ = setupLeft.PageGet(captured));
        }

        var instanceLeft = new PageInstanceLeft();
        foreach (var sub in new[]
                 {
                     PageSubType.VersionOverall, PageSubType.VersionSetup, PageSubType.VersionExport,
                     PageSubType.VersionWorld, PageSubType.VersionScreenshot, PageSubType.VersionMod,
                     PageSubType.VersionModDisabled, PageSubType.VersionResourcePack, PageSubType.VersionShader,
                     PageSubType.VersionSchematic, PageSubType.VersionInstall, PageSubType.VersionServer
                 })
        {
            var captured = sub;
            Check($"PageInstanceLeft.PageGet({(int)sub})", () => _ = instanceLeft.PageGet(captured));
        }

        // ---- 3) 真实导航：复现用户点击的三个动作（CompDetail/VersionSaves 需要 additional 参数，故不在此列）----
        // 实例设置前必须像 PageLaunchLeft.BtnMore_Click 那样先设置 McInstance，
        // 否则 PageInstanceOverall.Reload() 会因 instance 为 null 抛 NRE（自检自身的假阳性）。
        var frm = ModMain.frmMain;
        foreach (var page in new[]
                 {
                     PageType.Launch, PageType.Download, PageType.InstanceSelect, PageType.InstanceSetup
                 })
        {
            var captured = page;
            Check($"PageChange({page})", () =>
            {
                if (captured == PageType.InstanceSetup)
                {
                    ModInstanceList.McMcInstanceSelected.Load();
                    PageInstanceLeft.McInstance = ModInstanceList.McMcInstanceSelected;
                }

                frm.PageChange(captured);
            });
        }

        // ---- 4) 本地化回归：非 UI 线程取文案 ----
        // Avalonia 的资源宿主仅限 UI 线程，加载线程取文案曾退化为 "!key!"（如启动步骤名、启动成功提示）。
        Check("Lang.Text on background thread", () =>
        {
            var results = new string[3];
            var thread = new System.Threading.Thread(() =>
            {
                results[0] = Lang.Text("Minecraft.Launch.Success", "TestInstance");
                results[1] = Lang.Text("Minecraft.Launch.Stage.WaitWindow");
                results[2] = Lang.Text("Common.Option.All");
            });
            thread.Start();
            thread.Join();
            ModBase.Log($"[PageTest] 后台线程 Lang.Text => {string.Join(" | ", results)}");
            if (results.Any(r => r.StartsWith('!')))
                throw new Exception("非 UI 线程本地化失败（取到 !key!）：" + string.Join(" | ", results));
        });

        // ---- 6) 颜色空间往返（sRGB ↔ scRGB）----
        // 上游配色链路：Color → LabColor.FromWpfColor → (Lab/OKLCH 运算) → ToWpfColor → Color。
        // 该链路必须往返无损，否则整站配色会整体偏亮/偏暗（曾因把 scRGB 线性值当 sRGB 用而整体发黑）。
        Check("LabColor sRGB round-trip", () =>
        {
            foreach (var hex in new[] { "#3184E4", "#343D4A", "#FBFBFB", "#0A1225", "#FFFFFF", "#000000", "#1370F3" })
            {
                var src = Avalonia.Media.Color.Parse(hex);
                var back = PCL.Core.UI.Theme.LabColor.FromWpfColor(src).ToWpfColor();
                var delta = Math.Max(Math.Max(Math.Abs(back.R - src.R), Math.Abs(back.G - src.G)),
                    Math.Abs(back.B - src.B));
                ModBase.Log($"[PageTest] 色彩往返 {hex} -> #{back.R:X2}{back.G:X2}{back.B:X2} (Δ={delta})");
                if (delta > 1)
                    throw new Exception($"颜色往返偏差过大：{hex} -> #{back.R:X2}{back.G:X2}{back.B:X2}");
            }
        });

        // ---- 7) 内嵌资源图片加载（avares:// 必须经 AssetLoader 取流，不能当文件路径打开）----
        Check("MyBitmap 内嵌资源加载", () =>
        {
            foreach (var rel in new[] { "Blocks/Grass.png", "Icons/NoIcon.png", "Heads/Logo-CE.png" })
            {
                var bmp = new MyBitmap(ModBase.pathImage + rel);
                if (bmp.pic is null)
                    throw new Exception($"加载失败：{rel}");
                ModBase.Log($"[PageTest] MyBitmap {rel} -> {bmp.pic.Width}x{bmp.pic.Height}");
            }
        });

        // ---- 8) 主题/配色诊断（排查主界面发黑）----
        try
        {
            var app = Avalonia.Application.Current;
            object? Lookup(string k) =>
                app is not null && app.TryGetResource(k, app.ActualThemeVariant, out var v) ? v : null;
            ModBase.Log($"[PageTest] 主题: IsDarkMode={ThemeService.IsDarkMode}, " +
                        $"ColorMode={Config.Preference.Theme.ColorMode}, " +
                        $"BackgroundColorful={Config.Preference.Background.BackgroundColorful}, " +
                        $"ThemeSelected={Config.Preference.Theme.ThemeSelected}");
            ModBase.Log($"[PageTest] Window.Opacity={ModMain.frmMain.Opacity}, Window.Background={ModMain.frmMain.Background}, " +
                        $"PanForm.Background={ModMain.frmMain.PanForm.Background}, PanBack.Background={ModMain.frmMain.PanBack.Background}");
            ModBase.Log($"[PageTest] ColorBrushBackground={Lookup("ColorBrushBackground")}, " +
                        $"ColorBrush1={Lookup("ColorBrush1")}, ColorBrush2={Lookup("ColorBrush2")}, " +
                        $"ColorBrush3={Lookup("ColorBrush3")}, ColorBrush8={Lookup("ColorBrush8")}, " +
                        $"ColorBrushSemiTransparent={Lookup("ColorBrushSemiTransparent")}, " +
                        $"ColorBrushTransparentBackground={Lookup("ColorBrushTransparentBackground")}");
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[PageTest] 主题诊断出错");
        }

        RenderMainWindowToPng();

        ModBase.Log($"[PageTest] 汇总：成功 {ok}，失败 {fail}，共 {ok + fail}");
    }

    /// <summary>
    ///     [port][TEMP] 把主窗口离屏渲染成 PNG，供目视/像素比对（沙箱无桌面时尤其有用）。
    /// </summary>
    internal static void RenderMainWindowToPng()
    {
        try
        {
            var visual = (Visual)ModMain.frmMain;
            var size = new PixelSize(
                Math.Max(1, (int)Math.Round(ModMain.frmMain.Bounds.Width)),
                Math.Max(1, (int)Math.Round(ModMain.frmMain.Bounds.Height)));
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size, new Vector(96d, 96d));
            bitmap.Render(visual);
            var dir = System.IO.Path.Combine(Basics.ExecutableDirectory, "PCL", "Log");
            Directory.CreateDirectory(dir);
            var file = System.IO.Path.Combine(dir, "ui-render.png");
            bitmap.Save(file);
            ModBase.Log($"[PageTest] 已渲染主界面截图：{file}");
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[PageTest] 渲染主界面截图失败");
        }
    }

    // [port][TEMP] 交互链路诊断（配合 --pagetest）：验证"列表项点击"是否真的能走通。
    // 直接构造并投递一次合成的 PointerPressed + PointerReleased，检查子页面是否切换，
    // 用于定位"控件可见但点击无反应"（事件路由 / 处理器顺序 / 命中测试）。
    // 注意：不要用 InputHitTest 判断可点性——实测它对 PanForm / PanTitleMain 等子树会误报"不可达"，
    //      与本应用真实指针投递结果不一致。
    internal static void RunInteractionDiagnostics()
    {
        var left = ModMain.frmInstanceLeft;
        if (left is null)
        {
            ModBase.Log("[PageTest] 交互诊断：frmInstanceLeft 为 null，跳过");
            return;
        }

        // 1) 逻辑链路：直接触发 Check（等价于一次"点击成功"）
        try
        {
            var item = left.ItemSetup;
            ModBase.Log($"[PageTest] 逻辑链路：ItemSetup Type={item.Type} Tag={item.Tag} " +
                        $"Checked={item.Checked} IsLoaded={item.IsLoaded} IsVisible={item.IsVisible} " +
                        $"Bounds={item.Bounds.Width:F0}x{item.Bounds.Height:F0}");
            var before = left.pageID;
            item.SetChecked(true, true, true);
            ModBase.Log($"[PageTest] 逻辑链路：SetChecked 后 pageID {before} -> {left.pageID}");
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[PageTest] 逻辑链路自检失败");
        }

        // 2) 输入链路：合成按下 + 松开投递给另一个列表项，验证事件路由与处理器顺序
        try
        {
            var item = left.ItemMod;
            var root = (Visual)ModMain.frmMain;
            var pos = item.TranslatePoint(new Point(item.Bounds.Width / 2d, item.Bounds.Height / 2d), root)
                      ?? new Point(0d, 0d);
            var pointer = new Pointer(9001, PointerType.Mouse, true);
            var before = left.pageID;

            item.RaiseEvent(new PointerPressedEventArgs(item, pointer, root, pos, 0UL,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None));
            item.RaiseEvent(new PointerReleasedEventArgs(item, pointer, root, pos, 0UL,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                KeyModifiers.None, MouseButton.Left));

            ModBase.Log($"[PageTest] 输入链路：合成点击后 pageID {before} -> {left.pageID}" +
                        $"（期望变为 {(int)PageSubType.VersionMod}）");
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[PageTest] 输入链路自检失败");
        }
    }


    private void FormMain_Loaded() // (sender As Object, e As RoutedEventArgs) Handles Me.Loaded
    {
        // [port][TEMP] --pagetest：延迟到界面稳定后在 UI 线程跑页面实例化自检
        if (Basics.CommandLineArguments.Contains("--pagetest"))
        {
            var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(4d) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                try
                {
                    RunPageInstantiationSelfTest();
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[PageTest] 自检本身出错");
                }
            };
            timer.Start();

            // 自检会把界面停在实例设置页；再等一会儿让布局稳定，然后做交互链路诊断
            var interactionTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(9d) };
            interactionTimer.Tick += (_, _) =>
            {
                interactionTimer.Stop();
                try
                {
                    RunInteractionDiagnostics();
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[PageTest] 交互链路诊断出错");
                }
            };
            interactionTimer.Start();

            // 视觉切换检查（SetChecked 只改 pageID，内容替换在延时动画里）
            var swapTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(11d) };
            swapTimer.Tick += (_, _) =>
            {
                swapTimer.Stop();
                try
                {
                    var frm = ModMain.frmMain;
                    ModBase.Log($"[PageTest] 视觉切换：pageRight={frm.pageRight?.GetType().Name ?? "null"}, " +
                                $"PanMainRight.Child={frm.PanMainRight.Child?.GetType().Name ?? "null"}, " +
                                $"实例左页 pageID={ModMain.frmInstanceLeft?.pageID.ToString() ?? "null"}");
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[PageTest] 视觉切换检查出错");
                }
            };
            swapTimer.Start();

            // 回到启动页并渲染成图（核对按钮文字等静态外观）
            var renderTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(15d) };
            renderTimer.Tick += (_, _) =>
            {
                renderTimer.Stop();
                try
                {
                    ModMain.frmMain.PageChange(PageType.Launch);
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[PageTest] 回到启动页失败");
                }
            };
            renderTimer.Start();

            // 等启动页动画结束后再渲染，并核对按钮文字度量
            var renderTimer2 = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(18d) };
            renderTimer2.Tick += (_, _) =>
            {
                renderTimer2.Stop();
                try
                {
                    var btn = ModMain.frmLaunchLeft?.BtnMore;
                    if (btn is not null)
                        ModBase.Log($"[PageTest] 按钮文字度量：BtnMore.Text=\"{btn.Text}\" " +
                                    $"请求内边距={btn.TextPadding} 实际内边距={btn.LabText.Padding} " +
                                    $"文字区={btn.LabText.Bounds.Width:F0}x{btn.LabText.Bounds.Height:F0} " +
                                    $"按钮={btn.Bounds.Width:F0}x{btn.Bounds.Height:F0}");

                    RenderMainWindowToPng();
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[PageTest] 渲染启动页失败");
                }
            };
            renderTimer2.Start();

            // 切页扫描：依次进入下载/设置页并停留，用于回归"切页即崩"（下载页 Init 的输入验证、
            // 设置页 MyComboBox 模板部件等）。任一页抛异常都会在日志里留下 FTL/ERR。
            var sweepTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(20d) };
            var sweepPages = new[] { PageType.Download, PageType.Setup, PageType.Launch };
            var sweepIndex = 0;
            sweepTimer.Tick += (_, _) =>
            {
                if (sweepIndex >= sweepPages.Length)
                {
                    sweepTimer.Stop();
                    ModBase.Log("[PageTest] 切页扫描完成");
                    return;
                }

                var target = sweepPages[sweepIndex++];
                try
                {
                    ModMain.frmMain.PageChange(target);
                    ModBase.Log($"[PageTest] 切页扫描：已切到 {target}");
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, $"[PageTest] 切页扫描失败：{target}");
                }
            };
            sweepTimer.Interval = TimeSpan.FromSeconds(3d);
            sweepTimer.Start();

            // 档案列表页诊断：把 PageLoginProfile 放进启动页的 PanLogin 并渲染，核对列表是否真的渲染出来
            var profileTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(32d) };
            profileTimer.Tick += (_, _) =>
            {
                profileTimer.Stop();
                try
                {
                    var page = ModMain.frmLoginProfile ??= new PageLoginProfile();
                    page.Reload();
                    ModMain.frmLaunchLeft.PanLogin.Children.Clear();
                    ModMain.frmLaunchLeft.PanLogin.Children.Add(page);

                    var itemCount = CountDescendants<MyListItem>(page);
                    ModBase.Log($"[PageTest] 档案页：ProfileCollection={page.ProfileCollection.Count}, " +
                                $"已实体化 MyListItem={itemCount}, " +
                                $"页面尺寸={page.Bounds.Width:F0}x{page.Bounds.Height:F0}, " +
                                $"PanLogin尺寸={ModMain.frmLaunchLeft.PanLogin.Bounds.Width:F0}x{ModMain.frmLaunchLeft.PanLogin.Bounds.Height:F0}");
                    foreach (var it in page.ProfileCollection)
                        ModBase.Log($"[PageTest] 档案项：{it.Username} Logo=\"{it.Logo}\" SvgIcon=\"{it.SvgIcon}\" Info=\"{it.Info}\"");
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[PageTest] 档案页诊断失败");
                }
            };
            profileTimer.Start();

            var profileRenderTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(35d) };
            profileRenderTimer.Tick += (_, _) =>
            {
                profileRenderTimer.Stop();
                try
                {
                    var page = ModMain.frmLoginProfile;
                    if (page is not null)
                    {
                        var items = CountDescendants<MyListItem>(page);
                        var scroll = CountDescendants<MyScrollViewer>(page);
                        var ics = FindDescendants<ItemsControl>(page).ToList();
                        ModBase.Log($"[PageTest] 档案页(布局后)：页面尺寸={page.Bounds.Width:F0}x{page.Bounds.Height:F0}, " +
                                    $"MyListItem={items}, MyScrollViewer={scroll}, " +
                                    $"PanLogin={ModMain.frmLaunchLeft.PanLogin.Bounds.Width:F0}x{ModMain.frmLaunchLeft.PanLogin.Bounds.Height:F0}, " +
                                    $"PanLogin子项={ModMain.frmLaunchLeft.PanLogin.Children.Count}");
                        foreach (var ic in ics)
                        {
                            var kids = string.Join(",", Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(ic)
                                .Take(12).Select(v => v.GetType().Name + (v is Control c && !string.IsNullOrEmpty(c.Name) ? "#" + c.Name : "")));
                            ModBase.Log($"[PageTest] ItemsControl 视觉子树({Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(ic).Count()}): {kids}");
                        }
                        foreach (var ic in ics)
                            ModBase.Log($"[PageTest] ItemsControl: ItemCount={ic.ItemCount}, " +
                                        $"ItemsSource={(ic.ItemsSource is null ? "null" : ic.ItemsSource.GetType().Name)}, " +
                                        $"尺寸={ic.Bounds.Width:F0}x{ic.Bounds.Height:F0}, " +
                                        $"DataContext={ic.DataContext?.GetType().Name ?? "null"}, 模板={(ic.ItemTemplate is null ? "null" : "有")}");
                        var sv = FindDescendants<MyScrollViewer>(page).FirstOrDefault();
                        if (sv is not null)
                            ModBase.Log($"[PageTest] MyScrollViewer: 尺寸={sv.Bounds.Width:F0}x{sv.Bounds.Height:F0}, " +
                                        $"Extent={sv.Extent.Width:F0}x{sv.Extent.Height:F0}, Viewport={sv.Viewport.Width:F0}x{sv.Viewport.Height:F0}, " +
                                        $"Content={sv.Content?.GetType().Name ?? "null"}");
                    }
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[PageTest] 档案页布局后测量失败");
                }

                RenderMainWindowToPng();
                ModBase.Log("[PageTest] 档案页渲染完成");
            };
            profileRenderTimer.Start();

            // 圆角绑定验证：MyExtraTextButton 的 CornerRadius 应约为自身高度的 40%
            var radiusTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(38d) };
            radiusTimer.Tick += (_, _) =>
            {
                radiusTimer.Stop();
                try
                {
                    var extraBtn = new MyExtraTextButton { Text = "测试" };
                    ModMain.frmLaunchLeft.PanLogin.Children.Clear();
                    ModMain.frmLaunchLeft.PanLogin.Children.Add(extraBtn);
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            ModBase.Log($"[PageTest] MyExtraTextButton: 尺寸={extraBtn.Bounds.Width:F0}x{extraBtn.Bounds.Height:F0}, " +
                                        $"PanClick.CornerRadius={extraBtn.PanClick.CornerRadius}（期望约 20.8）");
                        }
                        catch (Exception ex) { ModBase.Log(ex, "[PageTest] 读取圆角失败"); }
                    }, Avalonia.Threading.DispatcherPriority.Loaded);
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[PageTest] 圆角验证失败");
                }
            };
            radiusTimer.Start();
        }

        static int CountDescendants<T>(Visual root) where T : Visual
        {
            var n = 0;
            foreach (var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(root))
                if (v is T) n++;
            return n;
        }

        static System.Collections.Generic.IEnumerable<T> FindDescendants<T>(Visual root) where T : Visual
        {
            foreach (var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(root))
                if (v is T t) yield return t;
        }

        // [port][TEMP] --logintest：用当前配置的 MS_CLIENT_ID 真跑一遍正版登录代码路径，
        // 用于验证客户端 ID 是否被读到、设备码请求是否成功、以及登录弹窗路径是否会崩。
        if (Basics.CommandLineArguments.Contains("--logintest"))
        {
            var loginProbe = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(6d) };
            loginProbe.Tick += (_, _) =>
            {
                loginProbe.Stop();
                var cid = PCL.Core.App.Secrets.MSOAuthClientId;
                ModBase.Log($"[LoginTest] 读到的客户端 ID = " +
                            $"{(string.IsNullOrEmpty(cid) ? "（空！未设置环境变量 PCL_MS_CLIENT_ID）" : cid[..8] + "…")}");
                try
                {
                    ModLaunch.mcLoginMsLoader.Start(new ModLaunch.McLoginMs(), true);
                    ModBase.Log("[LoginTest] 已启动正版登录加载器");
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "[LoginTest] 启动登录失败");
                }
            };
            loginProbe.Start();

            var loginCheck = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(22d) };
            loginCheck.Tick += (_, _) =>
            {
                loginCheck.Stop();
                ModBase.Log($"[LoginTest] 结果：加载器状态={ModLaunch.mcLoginMsLoader.State}, " +
                            $"进度={ModLaunch.mcLoginMsLoader.Progress}, " +
                            $"等待弹窗={ModMain.WaitingMyMsgBox.Count}, " +
                            $"PanMsg 子项={ModMain.frmMain?.PanMsg?.Children.Count}");

                // 复刻 PageLoginMs.BtnLogin_Click 的失败分支（在后台线程按 Msgbox 级别弹窗）
                var err = ModLaunch.mcLoginMsLoader.Error;
                if (err is not null)
                {
                    ModBase.Log("[LoginTest] 正在按 UI 路径弹窗（后台线程 + LogLevel.Msgbox）");
                    ModBase.RunInNewThread(() =>
                    {
                        ModBase.Log(err, "登录失败", ModBase.LogLevel.Msgbox, userSummary: "登录失败");
                        ModBase.Log("[LoginTest] Msgbox 调用已返回");
                    }, "LoginTest MsgBox");
                }
            };
            loginCheck.Start();

            // 心跳：UI 线程若被卡住，心跳会停止；同时观察各队列长度与托管堆大小
            var beat = 0;
            var heart = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2d) };
            heart.Tick += (_, _) =>
            {
                beat++;
                if (beat > 18) { heart.Stop(); return; }
                try
                {
                    ModBase.Log($"[LoginTest] 心跳#{beat} 托管堆={GC.GetTotalMemory(false) / 1048576}MB, " +
                                $"PanMsg子项={ModMain.frmMain?.PanMsg?.Children.Count}, " +
                                $"等待弹窗={ModMain.WaitingMyMsgBox.Count}, " +
                                $"PanHint子项={ModMain.frmMain?.PanHint?.Children.Count}, " +
                                $"PanHint子项={ModMain.frmMain?.PanHint?.Children.Count}");
                }
                catch (Exception ex) { ModBase.Log(ex, "[LoginTest] 心跳失败"); }
            };
            heart.Start();
        }

        // [port][TEMP] 定位 Loaded 卡点
        Console.Error.WriteLine("[LoadedProbe] 1 进入 Loaded 主体");
        FormMain_SizeChanged();
        ModBase.applicationStartTick = TimeUtils.GetTimeTick();
        // [port] WPF WindowInteropHelper 句柄在 Avalonia 不可用（Win32），注释掉；ModBase.frmHandle 保持默认值
        // ModBase.frmHandle = new WindowInteropHelper(this).Handle;
        // 读取设置
        Console.Error.WriteLine("[LoadedProbe] 2 即将 BackgroundRefresh");
        PageSetupUI.BackgroundRefresh(false, true);
        Console.Error.WriteLine("[LoadedProbe] 3 BackgroundRefresh 完成");
        ModMusic.MusicRefreshPlay(false, true);
        Console.Error.WriteLine("[LoadedProbe] 4 MusicRefreshPlay 完成");
        // 扩展按钮
        BtnExtraUpdateRestart.showCheck = BtnExtraUpdateRestart_ShowCheck;
        BtnExtraDownload.showCheck = BtnExtraDownload_ShowCheck;
        BtnExtraBack.showCheck = BtnExtraBack_ShowCheck;
        BtnExtraApril.showCheck = BtnExtraApril_ShowCheck;
        BtnExtraShutdown.showCheck = BtnExtraShutdown_ShowCheck;
        BtnExtraLog.showCheck = BtnExtraLog_ShowCheck;
        BtnExtraApril.ShowRefresh();
        // 初始化尺寸改变
        if (!Config.Preference.LockWindowSize)
            AddResizer();
        else
            RemoveResizer();
        // PLC 彩蛋
        if (RandomUtils.NextInt(1, 1000) == 233)
            // [port] WPF GeometryConverter.ConvertFromString → Avalonia Geometry.Parse（Avalonia 12 无 GeometryConverter）
            ShapeTitleLogo.Data = Geometry.Parse(
                "M26,29 v-25 h6 a7,7 180 0 1 0,14 h-6 M83,6.5 a10,11.5 180 1 0 0,18 M48,2.5 v24.5 h13.5");
        // 加载窗口

        Console.Error.WriteLine("[LoadedProbe] 6 即将 ThemeRefresh");
        ThemeManager.ThemeRefresh();
        Console.Error.WriteLine("[LoadedProbe] 7 ThemeRefresh 完成");
        Console.Error.WriteLine("[LoadedProbe] 8 即将 ApplyAll");
        ModSetup.ApplyAll();
        Console.Error.WriteLine("[LoadedProbe] 9 ApplyAll 完成");
        Lifecycle.CurrentApplication.Resources["BlurSamplingRate"] = Config.Preference.Blur.SamplingRate * 0.01d;
        Lifecycle.CurrentApplication.Resources["BlurType"] = Config.Preference.Blur.KernelType;
        if (Config.Preference.Blur.IsEnabled)
            Lifecycle.CurrentApplication.Resources["BlurRadius"] = Config.Preference.Blur.Radius * 1.0d;
        else
            Lifecycle.CurrentApplication.Resources["BlurRadius"] = 0.0d;

        // #If DEBUG Then
        // MinHeight = 50
        // MinWidth = 50
        // #End If
        Topmost = false;
        if (ModMain.frmStart is not null)
            // [port] WPF Window.Close(TimeSpan) 在 Avalonia 不存在 → Close() 无参（原 TimeSpan 为关闭延迟，暂缓）
            ModMain.frmStart.Close();
        // 更改窗口
        // Top = (GetWPFSize(My.Computer.Screen.WorkingArea.Height) - Height) / 2
        // Left = (GetWPFSize(My.Computer.Screen.WorkingArea.Width) - Width) / 2
        isSizeSaveable = true;
        Console.Error.WriteLine("[LoadedProbe] 10 即将 ShowWindowToTop");
        ShowWindowToTop();
        Console.Error.WriteLine("[LoadedProbe] 11 ShowWindowToTop 完成");
        // [port] WPF HwndSource 钩子（PresentationSource.FromVisual/HwndSource.AddHook(WndProc)）在 Avalonia 不可用（Win32），注释掉；WndProc 亦在下方 #if WINDOWS 保留
        // var hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        // hwndSource.AddHook(WndProc);
        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaCode(() => ModAnimation.AniControlEnabled -= 1, 50),
            ModAnimation.AaOpacity(this, Config.Preference.Theme.WindowOpacity / 1000d + 0.4d, 250, 100),
            ModAnimation.AaDouble(i => TransformPos.Y += (double)i, -TransformPos.Y, 600,
                100, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
            ModAnimation.AaDouble(i => TransformRotate.Angle += (double)i,
                -TransformRotate.Angle, 500, 100, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
            ModAnimation.AaCode(() =>
            {
                // [port] WPF this.RenderTransform = null → 清除根 Grid 的 RenderTransform（实际承载该动画的元素）
                RootGrid.RenderTransform = null;
                isWindowLoadFinished = true;
                ModBase.Log(
                    $"[System] DPI：{ModBase.dpi}，系统版本：{Environment.OSVersion.VersionString}，PCL 位置：{Basics.ExecutablePath}");
            }, after: true)
        }, "Form Show");
        Console.Error.WriteLine("[LoadedProbe] 13 即将 AniStart()");
        // Timer 启动
        ModAnimation.AniStart();
        Console.Error.WriteLine("[LoadedProbe] 14 AniStart() 完成");
        ModMain.TimerMainStart();
        // 特殊版本提示
        ModBase.RunInNewThread(() =>
        {
            // 特殊版本提示
            try
            {


#if DEBUG || DEBUGCI

                if (Environment.GetEnvironmentVariable("PCL_DISABLE_DEBUG_HINT") is null)
                {

#if DEBUG
                    var hint = Lang.Text("Main.SpecialVersion.DebugHint");
#else
                    var hint = Lang.Text("Main.SpecialVersion.CiHint");
#endif

                    ModMain.MyMsgBox(
                        $"{hint}{"\r\n"}{"\r\n"}{Lang.Text("Main.SpecialVersion.HideHintNotice")}",
                        Lang.Text("Main.SpecialVersion.Title"), Lang.Text("Main.SpecialVersion.IUnderstand"), Lang.Text("Main.SpecialVersion.OpenDownloadPageAndExit"), isWarn: true, button2Action: () =>
                        {
                            ModBase.OpenWebsite("https://github.com/PCL-Community/PCL2-CE/releases/latest");
                            EndProgram(false);
                        });
                }


#endif
                // EULA 提示
                if (!States.System.LauncherEula)
                    switch (ModMain.MyMsgBox(Lang.Text("Main.Eula.Message"), Lang.Text("Main.Eula.Title"), Lang.Text("Common.Action.Agree"), Lang.Text("Common.Action.Decline"), Lang.Text("Main.Eula.View"),
                                button3Action: () => ModBase.OpenWebsite("https://shimo.im/docs/rGrd8pY8xWkt6ryW")))
                    {
                        case 1:
                            {
                                States.System.LauncherEula = true;
                                break;
                            }
                        case 2:
                            {
                                EndProgram(false);
                                break;
                            }
                    }

                // 遥测提示
                if (Config.System.TelemetryConfig.IsDefault())
                {
                    var selection = ModMain.MyMsgBox(
                                Lang.Text("Main.Telemetry.Message"),
                                Lang.Text("Main.Telemetry.Title"), Lang.Text("Common.Action.Agree"), Lang.Text("Common.Action.Decline"));
                    Config.System.TelemetryConfig.SetValue(selection == 1, forceNewValue: true);
                }
                // 启动加载器池
                try
                {
                    ModDownload.dlClientListMojangLoader.Start(1); // PCL 会同时根据这里的加载结果决定是否使用官方源进行下载
                    RunCountSub();
                    UpdateManager.serverLoader.Start(1);
                    ModBase.RunInNewThread(ModMain.TryClearTaskTemp, "TryClearTaskTemp", ThreadPriority.BelowNormal);
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        "初始化加载池运行失败",
                        ModBase.LogLevel.Feedback,
                        userSummary: Lang.Text("Main.Error.OperationFailed"));
                }

                HardwareInfo.GetHardwareInfo();
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "初始弹窗提示运行失败",
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Main.Error.OperationFailed"));
            }
        }, "Start Loader", ThreadPriority.BelowNormal);

        ModBase.Log($"[Start] 第三阶段加载用时：{TimeUtils.GetTimeTick() - ModBase.applicationStartTick} ms");
    }

    // 根据打开次数触发的事件
    private void RunCountSub()
    {
        States.System.StartupCount += 1;
    }

    // 升级与降级事件
    private void UpgradeSub(int lastVersionCode)
    {
        ModBase.Log("[Start] 版本号从 " + lastVersionCode + " 升高到 " + ModBase.versionCode);
        States.System.LastVersion = ModBase.versionCode;
        // 检查有记录的最高版本号
        int lowerVersionCode;
#if BETA
        lowerVersionCode = States.System.LastBetaVersion;
        if (lowerVersionCode < ModBase.versionCode)
        {
            States.System.LastBetaVersion = ModBase.versionCode;
            ModBase.Log($"[Start] 最高版本号从 {lowerVersionCode} 升高到 {ModBase.versionCode}");
        }
#else
        lowerVersionCode = States.System.LastAlphaVersion;
        if (lowerVersionCode < ModBase.versionCode)
        {
            States.System.LastAlphaVersion = ModBase.versionCode;
            ModBase.Log($"[Start] 最高版本号从 {lowerVersionCode} 升高到 {ModBase.versionCode}");
        }
#endif
        // 被移除的窗口设置选项 (Commit 3161488 2026/1/23)
        if ((int)Config.Launch.GameWindowMode == 5)
            Config.Launch.GameWindowMode = GameWindowSizeMode.Default;

        // 更新后展示社区版提示
        UpdateManager.ShowCEAnnounce();
        // 输出更新日志
        if (lastVersionCode <= 0)
            return;
        if (lowerVersionCode >= ModBase.versionCode)
            return;
        ShowUpdateLog();
        
        // 重置自定义主页配置
        if (lastVersionCode < 521 && Config.Preference.Homepage.SelectedPreset >= 3)
        {
            Config.Preference.Homepage.SelectedPreset = 0;
            ModMain.MyMsgBox(Lang.Text("Main.HomepageReset.Content"), Lang.Text("Main.HomepageReset.Title"));
        }
    }

    private void DowngradeSub(int lastVersionCode)
    {
        ModBase.Log("[Start] 版本号从 " + lastVersionCode + " 降低到 " + ModBase.versionCode);
        States.System.LastVersion = ModBase.versionCode;
    }

    #endregion

    #region 自定义窗口

    private bool canResize = true;

    // 重写窗口边缘判定以使 DWM 自带的 resizer 行为看起来比较正常
    // [port] WPF/Win32 WM_NCHITTEST 边缘判定 → Avalonia 使用系统窗口装饰与系统 resizer；该方法仅在 #if WINDOWS 下保留（DWM/WindowInterop/VisualTreeHelper.GetDpi 均为 Win32）
#if WINDOWS
    private nint _SizeWndProc(nint hWnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        // 窗口活动常量
        const int WM_NCHITTEST = 0x84;
        const int HTCLIENT = 1;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;

        // WPF 尺寸的 offset
        const int offsetWpf = 6;
        const int hitWidthWpf = 5;

        // 过滤非 WM_NCHITTEST 事件
        if (msg != WM_NCHITTEST)
            return nint.Zero;

        // 提取鼠标坐标
        // 没妈的 VB 强转还得检查一下幻想的妈是不是还活着
        var mouseBytes = BitConverter.GetBytes(lParam.ToInt64());
        var xMouse = BitConverter.ToInt16(mouseBytes, 0);
        var yMouse = BitConverter.ToInt16(mouseBytes, 2);

        // 获取窗口参数
        var windowRect = WindowInterop.GetWindowRectangle(hWnd);
        var windowBounds = windowRect.ToWindowBounds();

        // 判断鼠标是否在窗口范围内
        var isInWindow = xMouse >= windowRect.Left && xMouse <= windowRect.Right && yMouse >= windowRect.Top &&
                         yMouse <= windowRect.Bottom;

        // 过滤不在窗口内的请求
        if (!isInWindow)
            return nint.Zero;

        // 如果 CanResize 为 False，直接返回 HTCLIENT
        if (!canResize)
            return new nint(HTCLIENT);

        // 真实像素尺寸的 offset
        var dpi = VisualTreeHelper.GetDpi(this);
        var offsetPxX = offsetWpf * dpi.DpiScaleX;
        var offsetPxY = offsetWpf * dpi.DpiScaleY;
        var hitWidthPxX = hitWidthWpf * dpi.DpiScaleX;
        var hitWidthPxY = hitWidthWpf * dpi.DpiScaleY;

        // 计算鼠标相对于窗口左上角的物理像素位置
        var relX = xMouse - windowRect.Left;
        var relY = yMouse - windowRect.Top;
        var w = windowBounds.Width;
        var h = windowBounds.Height;

        // 判定是否命中偏移后的热区
        var inLeft = relX >= offsetPxX && relX <= offsetPxX + hitWidthPxX;
        var inRight = relX <= w - offsetPxX && relX >= w - offsetPxX - hitWidthPxX;
        var inTop = relY >= offsetPxY && relY <= offsetPxY + hitWidthPxY;
        var inBottom = relY <= h - offsetPxY && relY >= h - offsetPxY - hitWidthPxY;

        handled = true; // 接管该区域的消息

        // 返回结果
        if (inTop && inLeft)
            return new nint(HTTOPLEFT);
        if (inTop && inRight)
            return new nint(HTTOPRIGHT);
        if (inBottom && inLeft)
            return new nint(HTBOTTOMLEFT);
        if (inBottom && inRight)
            return new nint(HTBOTTOMRIGHT);
        if (inLeft)
            return new nint(HTLEFT);
        if (inRight)
            return new nint(HTRIGHT);
        if (inTop)
            return new nint(HTTOP);
        if (inBottom)
            return new nint(HTBOTTOM);

        // 如果在 0-offset 范围内，返回 HTCLIENT 杀掉默认缩放
        return new nint(HTCLIENT);
    }
#endif

    // [port] OnSourceInitialized 覆写在 Avalonia Window 上不存在（无 PresentationSource/HwndSource/WindowInteropHelper/CompositionTarget/RenderMode）。
    //        本覆写里只剩 Win32 专属行为：硬件加速关闭（CompositionTarget.RenderMode）、DWM 扩展窗口框架（ExtendFrameIntoClientArea）、
    //        WM_NCHITTEST 边缘钩子（AddHook(_SizeWndProc)——已移入上面 _SizeWndProc 的 #if WINDOWS 块）。
    //        这些在 Linux 目标均为 Win32，暂缓移植（如需在 Avh 侧处理可在 Opened 事件中做无操作占位），故整体移除并在下方作废记录。
    // protected override void OnSourceInitialized(EventArgs e) { ... WPF/HwndSource/Win32 ... }

    // 关闭
    // [port] WPF CancelEventArgs → Avalonia WindowClosingEventArgs（Window.Closing 事件参数；该类型继承 CancelEventArgs，仍含 Cancel 属性，e.Cancel = true 有效）
    private void FormMain_Closing(object? sender, WindowClosingEventArgs e)
    {
        EndProgram(true);
        e.Cancel = true;
    }

    /// <summary>
    ///     正常关闭程序。程序将在执行此方法后约 0.3s 退出。
    /// </summary>
    /// <param name="sendWarning">是否在还有下载任务未完成时发出警告。</param>
    /// <param name="isUpdating">是否正在更新重启</param>
    public void EndProgram(bool sendWarning, bool isUpdating = false)
    {
        // 发出警告
        if (sendWarning && ModNet.HasDownloadingTask())
        {
            if (ModMain.MyMsgBox(Lang.Text("Main.Exit.HasDownloadingTask"), Lang.Text("Common.Dialog.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) == 1)
                // 强行结束下载任务
                ModBase.RunInNewThread(() =>
                {
                    ModBase.Log("[System] 正在强行停止任务");
                    foreach (var Task in ModLoader.loaderTaskbar.ToList())
                        Task.Abort();
                }, "强行停止下载任务");
            else
                return;
        }

        // 关闭联机大厅
        // Await LobbyController.CloseAsync().ConfigureAwait(False)
        // 存储上次使用的档案编号
        ModProfile.SaveProfile();
        // 关闭
        ModBase.RunInUiWait(() =>
        {
            // [port] 清理视频背景（VideoBack.Stop/Source/Close，MediaElement）——视频背景暂缓移植，删除
            IsHitTestVisible = false;
            if (RenderTransform is null)
            {
                var transformPos = new TranslateTransform(0d, 0d);
                var transformRotate = new RotateTransform(0d);
                var transformScale = new ScaleTransform(1d, 1d);
                // [port] WPF ScaleTransform.CenterX/CenterY → Avalonia ScaleTransform 无中心点；用 RenderTransformOrigin=(0.5,0.5) 等价于“以窗口中心缩放”
                RenderTransformOrigin = new RelativePoint(new Point(0.5d, 0.5d), RelativeUnit.Relative);
                // [port] WPF TransformGroup { Children = new TransformCollection(...) } → Avalonia 无 TransformCollection，改由 Children.Add 逐个加入
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(transformRotate);
                transformGroup.Children.Add(transformPos);
                transformGroup.Children.Add(transformScale);
                RenderTransform = transformGroup;
                ModAnimation.AniStart(new[]
                {
                    ModAnimation.AaOpacity(this, -Opacity, 140, 40,
                        new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                    ModAnimation.AaDouble(i =>
                    {
                        transformScale.ScaleX += (double)i;
                        transformScale.ScaleY += (double)i;
                    }, 0.88d - transformScale.ScaleX, 180),
                    ModAnimation.AaDouble(i => transformPos.Y += (double)i,
                        20d - transformPos.Y, 180, 0,
                        new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                    ModAnimation.AaDouble(i => transformRotate.Angle += (double)i,
                        0.6d - transformRotate.Angle, 180, 0,
                        new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak)),
                    ModAnimation.AaCode(() =>
                    {
                        IsHitTestVisible = false;
                        // [port] WPF Visibility.Collapsed → Avalonia IsVisible = false（无 Visibility 枚举）
                        IsVisible = false;
                        // [port] Avalonia Window 存在 ShowInTaskbar 属性（经查证非 WPF 专属），保留；Linux 平台效果可能有限
                        ShowInTaskbar = false;
                    }, 210),
                    ModAnimation.AaCode(() => EndProgramForce(force: false, isUpdating: isUpdating), 230)
                }, "Form Close");
            }
            else
            {
                EndProgramForce(force: false, isUpdating: isUpdating);
            }

            ModBase.Log("[System] 收到关闭指令");
        });
    }

    private static bool isLogShown;

    public static void EndProgramForce(ModBase.ProcessReturnValues returnCode = ModBase.ProcessReturnValues.Success,
        bool force = true, bool isUpdating = false)
    {
        // On Error Resume Next
        // 关闭联机大厅
        // Await LobbyController.CloseAsync().ConfigureAwait(False)
        ModBase.isProgramEnded = true;
        ModAnimation.AniControlEnabled += 1;
        if (UpdateManager.isUpdateWaitingRestart && !isUpdating)
            UpdateManager.UpdateRestart(false, false);
        if (returnCode == ModBase.ProcessReturnValues.Exception)
        {
            if (!isLogShown)
            {
                ModBase.FeedbackInfo();
                ModBase.Log("请在 https://github.com/PCL-Community/PCL2-CE/issues 提交错误报告，以便于社区解决此问题！（这也有可能是原版 PCL 的问题）");
                isLogShown = true;
                ModBase.ShellOnly(LogWrapper.CurrentLogger.CurrentLogFiles.Last());
            }

            Thread.Sleep(500); // 防止 PCL 在记事本打开前就被掐掉
        }

        ModBase.Log("[System] 程序已退出，返回值：" + ModBase.GetStringFromEnum(returnCode));
        // If ReturnCode <> ProcessReturnValues.Success Then Environment.Exit(ReturnCode)
        // Process.GetCurrentProcess.Kill()
        Lifecycle.Shutdown((int)returnCode, force);
    }

    private void BtnTitleClose_Click(object sender, EventArgs e)
    {
        EndProgram(true);
    }

    // 移动
    // [port] WPF PointerPressedEventArgs → Avalonia PointerPressedEventArgs；IsMouseDirectlyOver → IsPointerOver；DragMove() → Window.BeginMoveDrag(e)
    private void FormDragMove(object? sender, PointerPressedEventArgs e)
    {
        // On Error Resume Next
        // [port] 上游用 WPF 的 IsMouseDirectlyOver（仅当鼠标位于该元素"自身"、不含子元素时成立）。
        //        Avalonia 没有 IsMouseDirectlyOver，只能取 IsPointerOver —— 而它"包含子元素"，
        //        于是点击标题栏里的选项卡/最小化/关闭/返回按钮时也会命中拖动分支，
        //        BeginMoveDrag 会捕获指针，按钮再也收不到 PointerReleased → 点击全部失效
        //        （只有按下动画，无任何响应）。故改为判断事件源就是标题栏自身，还原"直接在标题栏上"的语义。
        if (ReferenceEquals(e.Source, sender) && ((InputElement)sender).IsPointerOver)
            BeginMoveDrag(e);
    }

    // 改变大小
    /// <summary>
    ///     是否可以向注册表储存尺寸改变信息。以此避免初始化时误储存。
    /// </summary>
    public bool isSizeSaveable;

    private void FormMain_SizeChanged(object? sender = null, SizeChangedEventArgs? e = null)
    {
        if (isSizeSaveable)
        {
            States.UI.WindowHeight = Height;
            States.UI.WindowWidth = Width;
        }

        if (PanBack is not null)
        {
            // [port] WPF x:Name="RectForm"（Border.Clip 内的 RectangleGeometry）在 Avalonia 命名字段生成器中不生成字段；由 PanBack.Clip 取用
            if (PanBack.Clip is RectangleGeometry rectForm)
                rectForm.Rect = new Rect(0d, 0d, PanBack.Bounds.Width, PanBack.Bounds.Height);

            var formWidth = PanBack.Bounds.Width + 0.001d;
            var formHeight = PanBack.Bounds.Height + 0.001d;

            PanForm.Width = formWidth;
            PanForm.Height = formHeight;
            PanMain.Width = formWidth;

            if (PanTitle is not null)
                PanMain.Height = Math.Max(0d, formHeight - PanTitle.Bounds.Height);
            else
                PanMain.Height = formHeight;

            // [port] 视频背景（VideoBack.Width/Height）——视频背景暂缓移植，删除
        }

        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal; // 修复 #1938
    }

    // 标题栏改变大小
    private void PanTitle_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // [port] WPF ColumnDefinition.Bounds.Width → Avalonia ColumnDefinition.ActualWidth（无 Bounds）
        if (PanTitleMain.ColumnDefinitions[0].ActualWidth - 30 <= 0)
            PanTitleLeft.ColumnDefinitions[0].MaxWidth = 0;
        else
            PanTitleLeft.ColumnDefinitions[0].MaxWidth = PanTitleMain.ColumnDefinitions[0].ActualWidth - 30;
    }

    // 最小化
    private void BtnTitleMin_Click(object sender, EventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnTitleHelp_Click(object sender, EventArgs e)
    {
        ModBase.OpenWebsite("https://www.bilibili.com/video/BV1uT4y1P7CX");
    }

    #endregion

    #region 窗体事件

    public void AddResizer()
    {
        canResize = true;
    }

    public void RemoveResizer()
    {
        canResize = false;
    }

    // 按键事件
    private void FormMain_KeyDown(object sender, KeyEventArgs e)
    {
        // [port] Avalonia KeyEventArgs 无 IsRepeat；重复按键事件由平台自行发送，暂缓做重复过滤
        // if (e.IsRepeat)
        //     return;
        // 调用弹窗：回车选择第一个，Esc 选择最后一个
        if (PanMsg.Children.Count > 0)
        {
            if (e.Key == Key.Enter)
            {
                var msg = PanMsg.Children[0];
                Action? enterAction = msg switch
                {
                    MyMsgInput input => () => input.Btn1_Click(sender, null),
                    MyMsgSelect select => () => select.Btn1_Click(sender, null),
                    MyMsgText text => () => text.Btn1_Click(sender, null),
                    // [port] MyMsgMarkdown 为暂缓功能（且非 Control，模式无法匹配）；Markdown 弹窗暂不支持 Enter 触发
                    MyMsgLogin login => () => login.Btn1_Click(sender, null),
                    _ => null
                };
                enterAction?.Invoke();
                return;
            }

            if (e.Key == Key.Escape)
            {
                var msg = PanMsg.Children[0];
                Action? escapeAction = msg switch
                {
                    MyMsgInput input => input.Btn2.IsVisible
                        ? () => input.Btn2_Click(sender, null)
                        : () => input.Btn1_Click(sender, null),
                    MyMsgSelect select => select.Btn2.IsVisible
                        ? () => select.Btn2_Click(sender, null)
                        : () => select.Btn1_Click(sender, null),
                    MyMsgText text => text.Btn3.IsVisible
                        ? () => text.Btn3_Click(sender, null)
                        : text.Btn2.IsVisible
                            ? () => text.Btn2_Click(sender, null)
                            : () => text.Btn1_Click(sender, null),
                    // [port] MyMsgMarkdown 为暂缓功能（且非 Control，模式无法匹配）；Markdown 弹窗暂不支持 Esc 触发
                    MyMsgLogin login => login.Btn3.IsVisible
                        ? () => login.Btn3_Click(sender, null)
                        : () => login.Btn1_Click(sender, null),
                    _ => null
                };
                escapeAction?.Invoke();
                return;
            }
        }

        // 按 ESC 返回上一级
        if (e.Key == Key.Escape)
            TriggerPageBack();
        // 更改隐藏实例可见性
        if (e.Key == Key.F11 && pageCurrent == PageType.InstanceSelect)
        {
            ModMain.frmSelectRight.showHidden = !ModMain.frmSelectRight.showHidden;
            ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                ModLoader.LoaderFolderRunType.ForceRun, 1, @"versions\");
            return;
        }

        // 更改功能隐藏可见性
        if (e.Key == Key.F12)
        {
            PageSetupUI.HiddenForceShow = !PageSetupUI.HiddenForceShow;
            if (PageSetupUI.HiddenForceShow)
                HintService.Hint(Lang.Text("Main.HiddenFeature.Disabled"), HintType.Success);
            else
                HintService.Hint(Lang.Text("Main.HiddenFeature.Enabled"), HintType.Success);
            PageSetupUI.HiddenRefresh();
            return;
        }

        // 按 F5 刷新页面
        if (e.Key == Key.F5)
        {
            if (pageLeft is IRefreshable)
                ((IRefreshable)pageLeft).Refresh();
            if (pageRight is IRefreshable)
                ((IRefreshable)pageRight).Refresh();
            return;
        }

        // 调用启动游戏
        if (e.Key == Key.Enter && pageCurrent == PageType.Launch)
        {
            if (ModMain.isAprilEnabled && !ModMain.isAprilGiveup)
                HintService.Hint(Lang.Text("Main.April.Nope"));
            else
                ModMain.frmLaunchLeft.LaunchButtonClick();
        }

        // 修复按下 Alt 后误认为弹出系统菜单导致的冻结
        // [port] WPF KeyEventArgs.SystemKey → Avalonia KeyEventArgs.Key（Avalonia 无 SystemKey）
        if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            e.Handled = true;
    }

    // [port] WPF PointerPressedEventArgs/MouseButton.XButton1/2 → Avalonia PointerPressedEventArgs + PointerPointProperties.IsXButton1/2Pressed（侧键判定）
    private void FormMain_MouseDown(object? sender, PointerPressedEventArgs e)
    {
        // [port] 跟踪左键按下状态（Avalonia 无 WPF 静态 Mouse.LeftButton，供 DragTick/DragDoing 查询）
        mouseLeftButtonPressed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        // 鼠标侧键返回上一级
        if (ModMain.frmMain!.PanMsg.Children.Count > 0 || ModMain.WaitingMyMsgBox.Any())
            return; // 弹窗中（#5513）
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsXButton1Pressed || props.IsXButton2Pressed)
            TriggerPageBack();
    }

    private void FormMain_MouseUp(object? sender, PointerReleasedEventArgs e)
    {
        // [port] 释放时清除左键状态（对应 Mouse.LeftButton == Released）
        mouseLeftButtonPressed = false;
    }

    private void TriggerPageBack()
    {
        if (pageCurrent == PageType.Download && PageCurrentSub == PageSubType.DownloadInstall &&
            ModMain.frmDownloadInstall.isInSelectPage)
            ModMain.frmDownloadInstall.ExitSelectPage();
        else if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.VersionInstall &&
                 ModMain.frmInstanceInstall.isInSelectPage)
            ModMain.frmInstanceInstall.ExitSelectPage();
        else
            PageBack();
    }

    // 切回窗口
    // [port] XAML 上已移除 Activated 属性，改由构造函数中 `Activated += FormMain_Activated;` 重新接线（Avalonia WindowBase.Activated，EventHandler 签名 object?/EventArgs 兼容）
    private void FormMain_Activated(object? sender, EventArgs e)
    {
        try
        {
            // [port] 原 OnActivated 覆写（Avalonia Window 无该覆写）的逻辑：激活时若处于隐藏态则取消隐藏
            if (Hidden)
                Hidden = false;
            if (Config.Download.Comp.ReadClipboard)
                ModComp.CompClipboard.GetClipboardResource();
            if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.VersionMod)
            {
                // Mod 管理自动刷新
                ModMain.frmInstanceMod.ReloadCompFileList();
            }
            else if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.VersionResourcePack)
            {
                // 资源包管理自动刷新
                if (ModMain.frmInstanceResourcePack is not null)
                    ModMain.frmInstanceResourcePack.ReloadCompFileList();
            }
            else if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.VersionShader)
            {
                // 光影包管理自动刷新
                if (ModMain.frmInstanceShader is not null)
                    ModMain.frmInstanceShader.ReloadCompFileList();
            }
            else if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.VersionSchematic)
            {
                // 投影原理图管理自动刷新
                if (ModMain.frmInstanceSchematic is not null)
                    ModMain.frmInstanceSchematic.ReloadCompFileList();
            }
            else if (pageCurrent == PageType.InstanceSelect)
            {
                // 实例选择自动刷新
                ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                    ModLoader.LoaderFolderRunType.RunOnUpdated, 1, @"versions\");
            }
            else if (ModMain.frmMain.pageRight is PageInstanceSavesDatapack &&
                     ModMain.frmInstanceSavesDatapack is not null)
            {
                // 数据包管理自动刷新
                ModMain.frmInstanceSavesDatapack.ReloadDatapackFileList();
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "切回窗口时出错",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
        }
    }

    private object? _HandleDrag_PrevData; // [port] WPF IDataObject → Avalonia 12 以 object 缓存 IDataTransfer 引用
    private DragDropEffects _HandleDrag_PrevEffects;

    // 文件拖放
    private void HandleDrag(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Handled && e.DragEffects != DragDropEffects.None)
                return;
            // 缓存
            e.Handled = true;
            if (ReferenceEquals(e.DataTransfer, _HandleDrag_PrevData))
            {
                e.DragEffects = _HandleDrag_PrevEffects;
                return;
            }

            // 确定拖放效果
            // [port] WPF IDataObject.GetData/GetDataPresent + e.Effects → Avalonia IDataTransfer（Contains/TryGetText/TryGetFiles）+ e.DragEffects
            e.DragEffects = DragDropEffects.None;
            if (e.DataTransfer.Contains(DataFormat.Text))
            {
                var str = e.DataTransfer.TryGetText();
                if (str is not null && str.StartsWithF("authlib-injector:yggdrasil-server:"))
                    e.DragEffects = DragDropEffects.Copy;
                else if (str is not null && str.StartsWithF("file:///")) e.DragEffects = DragDropEffects.Copy;
            }
            else if (e.DataTransfer.Contains(DataFormat.File))
            {
                var files = e.DataTransfer.TryGetFiles();
                if (files is not null && files.Length > 0) e.DragEffects = DragDropEffects.Link;
            }

            _HandleDrag_PrevData = e.DataTransfer;
            _HandleDrag_PrevEffects = e.DragEffects;
            ModBase.Log("[System] 设置拖放类型：" + ModBase.GetStringFromEnum(e.DragEffects));
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "处理拖放时出错",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
        }
    }

    private void FrmMain_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.DataTransfer.Contains(DataFormat.Text))
            {
                // 获取文本
                // [port] WPF e.Data.GetDataPresent + (string)e.Data.GetData → Avalonia 12 e.DataTransfer.Contains + TryGetText()
                try
                {
                    var str = e.DataTransfer.TryGetText();
                    if (str is null) return;
                    ModBase.Log("[System] 接受文本拖拽：" + str);
                    if (str.StartsWithF("authlib-injector:yggdrasil-server:"))
                    {
                        // Authlib 拖拽
                        e.Handled = true;
                        e.DragEffects = DragDropEffects.Copy;
                        var authlibServer =
                            WebUtility.UrlDecode(str.Substring("authlib-injector:yggdrasil-server:".Length));
                        ModBase.Log("[System] Authlib 拖拽：" + authlibServer);
                        if (!new HttpValidator().Validate(authlibServer).IsValid)
                        {
                            HintService.Hint(Lang.Text("Main.FileDrag.AuthlibInvalid", authlibServer), HintType.Error);
                            return;
                        }

                        if (ModMain.MyMsgBox(Lang.Text("Main.FileDrag.CreateAuthlibProfile", authlibServer), Lang.Text("Main.FileDrag.CreateAuthlibProfileTitle"),
                                Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) == 2)
                            return;
                        ModProfile.selectedProfile = null;
                        ModBase.RunInUi(() =>
                        {
                            PageLoginAuth.draggedAuthServer = authlibServer;
                            ModMain.frmLaunchLeft.RefreshPage(true, ModLaunch.McLoginType.Auth);
                        });
                        if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.VersionSetup)
                            // 正在服务器选项页，需要刷新设置项显示
                            ModMain.frmInstanceSetup.Reload();
                    }
                    else if (str.StartsWithF("file:///"))
                    {
                        // 文件拖拽（例如从浏览器下载窗口拖入）
                        var filePath = WebUtility.UrlDecode(str).Substring("file:///".Length).Replace("/", @"\");
                        e.Handled = true;
                        e.DragEffects = DragDropEffects.Copy;
                        FileDrag(new List<string> { filePath });
                    }
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "无法接取文本拖拽事件", ModBase.LogLevel.Developer);
                }
            }
            else if (e.DataTransfer.Contains(DataFormat.File))
            {
                // 获取文件并检查
                // [port] WPF e.DataTransfer.Contains + e.Data.GetData(string[]) → Avalonia 12 e.DataTransfer.Contains + TryGetFiles()（IStorageItem[]），再映射为本地路径
                var filePathRaw = e.DataTransfer.TryGetFiles();
                if (filePathRaw is null) // #2690
                {
                    HintService.Hint(Lang.Text("Main.FileDrag.ExtractFirst"), HintType.Error);
                    return;
                }

                e.Handled = true;
                e.DragEffects = DragDropEffects.Link;
                FileDrag(filePathRaw.Select(f => f.TryGetLocalPath()).Where(p => p is not null).Select(p => p!));
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "接取拖拽事件失败",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
        }
    }

    private void FileDrag(IEnumerable<string> filePathList)
    {
        ModBase.RunInNewThread(() =>
        {
            var filePath = filePathList.First();
            ModBase.Log("[System] 接受文件拖拽：" + filePath + (filePathList.Any() ? $" 等 {filePathList.Count()} 个文件" : ""),
                ModBase.LogLevel.Developer);
            // 基础检查
            if (Directory.Exists(filePathList.First()) && !File.Exists(filePathList.First()))
            {
                HintService.Hint(Lang.Text("Main.FileDrag.FileOnly"), HintType.Error);
                return;
            }

            if (!File.Exists(filePathList.First()))
            {
                HintService.Hint(Lang.Text("Main.FileDrag.FileNotFound", filePathList.First()), HintType.Error);
                return;
            }

            // 多文件拖拽
            if (filePathList.Count() > 1)
            {
                // 检查是否为同类型文件
                var firstExtension = filePathList.First().AfterLast(".").ToLower();
                var allSameType = filePathList.All(f => (f.AfterLast(".").ToLower() ?? "") == (firstExtension ?? ""));

                if (allSameType &&
                    new[] { "jar", "litemod", "disabled", "old", "litematic", "nbt", "schematic", "schem" }.Contains(
                        firstExtension))
                {
                }
                // 允许同类型的 Mod 文件或投影文件批量拖拽
                else
                {
                    HintService.Hint(Lang.Text("Main.FileDrag.SameTypeOnly"), HintType.Error);
                    return;
                }
            }

            // 主页
            var extension = filePath.AfterLast(".").ToLower();
            if (extension == "xaml")
            {
                ModBase.Log("[System] 文件后缀为 XAML，作为主页加载");
                if (File.Exists(ModBase.exePath + @"PCL\Custom.xaml"))
                    if (ModMain.MyMsgBox(Lang.Text("Main.FileDrag.HomepageExists"), Lang.Text("Main.FileDrag.OverwriteTitle"), Lang.Text("Common.Action.Overwrite"), Lang.Text("Common.Action.Cancel")) == 2)
                        return;

                ModBase.CopyFile(filePath, ModBase.exePath + @"PCL\Custom.xaml");
                ModBase.RunInUi(() =>
                {
                    Config.Preference.Homepage.Type = 1;
                    ModMain.frmLaunchRight.ForceRefresh();
                    HintService.Hint(Lang.Text("Main.FileDrag.HomepageLoaded"), HintType.Success);
                });
                return;
            }

            // 安装 Mod
            if (PageInstanceCompResource.InstallMods(filePathList))
                return;
            // 安装投影文件
            if (new[] { "litematic", "nbt", "schematic", "schem" }.Contains(extension))
            {
                ModBase.Log($"[System] 文件为 {extension} 格式，尝试作为原理图安装");
                // 获取当前文件夹路径（如果在资源管理页面）
                string targetFolderPath = null;
                if (pageCurrent == PageType.InstanceSetup && PageCurrentSub == PageSubType.VersionSchematic &&
                    ModMain.frmInstanceSchematic is not null &&
                    ModMain.frmInstanceSchematic is PageInstanceCompResource)
                    targetFolderPath = ModMain.frmInstanceSchematic.CurrentFolderPath;
                PageInstanceCompResource.InstallCompFiles(filePathList, ModComp.CompType.Schematic, targetFolderPath);
                return;
            }

            // 处理资源安装
            if (pageCurrent == PageType.InstanceSetup && new[] { "zip" }.Any(i => (i ?? "") == (extension ?? "")))
                switch (PageCurrentSub)
                {
                    case PageSubType.VersionWorld:
                    {
                        var destFolder = PageInstanceLeft.McInstance.PathIndie + @"saves\" +
                                         ModBase.GetFileNameWithoutExtentionFromPath(filePath);
                        var destLevelDat = System.IO.Path.Combine(destFolder, "level.dat");
                        if (Directory.Exists(destFolder))
                        {
                            HintService.Hint(Lang.Text("Main.FileDrag.SameFolderExists", destFolder), HintType.Error);
                            return;
                        }

                        var extractFolder = System.IO.Path.Combine(ModBase.pathTemp, "Cache", "WorldImport", ModBase.GetUuid().ToString());
                        try
                        {
                            ModBase.ExtractFile(filePath, extractFolder);
                            var saveRoot = SaveImportHelper.GetSaveRootDirectory(extractFolder);
                            if (saveRoot is null)
                            {
                                HintService.Hint(Lang.Text("Main.FileDrag.SaveNotFound"), HintType.Error);
                                return;
                            }

                            ModBase.CopyDirectory(saveRoot, destFolder);
                            if (!File.Exists(destLevelDat))
                            {
                                if (Directory.Exists(destFolder))
                                    ModBase.DeleteDirectory(destFolder, true);
                                HintService.Hint(Lang.Text("Main.FileDrag.SaveInvalid"), HintType.Error);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (Directory.Exists(destFolder))
                                ModBase.DeleteDirectory(destFolder, true);
                            ModBase.Log(
                                ex,
                                Lang.Text("Main.FileDrag.SaveImportFailed"),
                                ModBase.LogLevel.Hint,
                                userSummary: Lang.Text("Main.FileDrag.SaveImportFailed"));
                            return;
                        }
                        finally
                        {
                            if (Directory.Exists(extractFolder))
                                ModBase.DeleteDirectory(extractFolder, true);
                        }

                        HintService.Hint(Lang.Text("Main.FileDrag.Imported", ModBase.GetFileNameWithoutExtentionFromPath(filePath)),
                            HintType.Success);
                        if (ModMain.frmInstanceSaves is not null)
                            ModBase.RunInUi(() => ModMain.frmInstanceSaves.Reload());
                        return;
                    }
                    case PageSubType.VersionResourcePack:
                    {
                        var destFile = PageInstanceLeft.McInstance.PathIndie + @"resourcepacks\" +
                                       ModBase.GetFileNameFromPath(filePath);
                        if (File.Exists(destFile))
                        {
                            HintService.Hint(Lang.Text("Main.FileDrag.SameFileExists", destFile), HintType.Error);
                            return;
                        }

                        ModBase.CopyFile(filePath, destFile);
                        HintService.Hint(Lang.Text("Main.FileDrag.Imported", ModBase.GetFileNameFromPath(filePath)), HintType.Success);
                        if (ModMain.frmInstanceResourcePack is not null)
                            ModBase.RunInUi(() => ModMain.frmInstanceResourcePack.ReloadCompFileList());
                        return;
                    }
                    case PageSubType.VersionShader:
                    {
                        var destFile = PageInstanceLeft.McInstance.PathIndie + @"shaderpacks\" +
                                       ModBase.GetFileNameFromPath(filePath);
                        if (File.Exists(destFile))
                        {
                            HintService.Hint(Lang.Text("Main.FileDrag.SameFileExists", destFile), HintType.Error);
                            return;
                        }

                        ModBase.CopyFile(filePath, destFile);
                        HintService.Hint(Lang.Text("Main.FileDrag.Imported", ModBase.GetFileNameFromPath(filePath)), HintType.Success);
                        if (ModMain.frmInstanceShader is not null)
                            ModBase.RunInUi(() => ModMain.frmInstanceShader.ReloadCompFileList());
                        return;
                    }
                }

            // 处理投影文件
            if (pageCurrent == PageType.InstanceSetup &&
                new[] { "litematic", "nbt", "schematic", "schem" }.Contains(extension) &&
                PageCurrentSub == PageSubType.VersionSchematic)
            {
                var destFile = PageInstanceLeft.McInstance.PathIndie + @"schematics\" +
                               ModBase.GetFileNameFromPath(filePath);
                if (File.Exists(destFile))
                {
                    HintService.Hint(Lang.Text("Main.FileDrag.SameFileExists", destFile), HintType.Error);
                    return;
                }

                Directory.CreateDirectory(PageInstanceLeft.McInstance.PathIndie + @"schematics\");
                ModBase.CopyFile(filePath, destFile);
                HintService.Hint(Lang.Text("Main.FileDrag.Imported", ModBase.GetFileNameFromPath(filePath)), HintType.Success);
                if (ModMain.frmInstanceSchematic is not null)
                    ModBase.RunInUi(() => ModMain.frmInstanceSchematic.ReloadCompFileList());
                return;
            }

            // 安装整合包
            if (new[] { "zip", "rar", "mrpack" }.Any(t =>
                    (t ?? "") == (extension ?? ""))) // 部分压缩包是 zip 格式但后缀为 rar，总之试一试
            {
                ModBase.Log("[System] 文件为压缩包，尝试作为整合包安装");
                try
                {
                    ModModpack.ModpackInstall(filePath);
                    return;
                }
                catch (ModBase.CancelledException ex)
                {
                    return; // 用户主动取消
                }
                catch (Exception ex)
                {
                    // 安装失败，继续往后尝试
                }
            }

            // [port] 错误报告分析（CrashAnalyzer）暂缓移植：CrashAnalysis 模块已从编译目标排除（CS0246），
            //        保留逻辑但整体跳过，落入下方“未知操作”提示。
            /*
            // 错误报告分析
            do
            {
                try
                {
                    ModBase.Log("[System] 尝试进行错误报告分析");
                    var analyzer = new CrashAnalyzer(ModBase.GetUuid());
                    analyzer.Import(filePath);
                    if (!analyzer.Prepare())
                        break;
                    analyzer.Analyze();
                    analyzer.Output(true, new List<string>());
                    return;
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        "自主错误报告分析失败",
                        ModBase.LogLevel.Feedback,
                        userSummary: Lang.Text("Main.Error.OperationFailed"));
                }
            } while (false);
            */

            // 未知操作
            HintService.Hint(Lang.Text("Main.FileDrag.UnknownOperation"));
        }, "文件拖拽");
    }

    // 接受到 Windows 窗体事件
    public bool isSystemTimeChanged;

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == 30)
        {
            var nowDate = DateTime.Now;
            if (nowDate.Date == ModBase.applicationOpenTime.Date)
            {
                ModBase.Log("[System] 系统时间微调为：" + nowDate.ToLongDateString() + " " + nowDate.ToLongTimeString());
                isSystemTimeChanged = false;
            }
            else
            {
                ModBase.Log("[System] 系统时间修改为：" + nowDate.ToLongDateString() + " " + nowDate.ToLongTimeString());
                isSystemTimeChanged = true;
            }
        }
        else if (msg == 400 * 16 + 2)
        {
            ModBase.Log("[System] 收到置顶信息：" + hwnd.ToInt64());
            if (!isWindowLoadFinished)
            {
                ModBase.Log("[System] 窗口尚未加载完成，忽略置顶请求");
                return nint.Zero;
            }

            ShowWindowToTop();
            handled = true;
        }
        else if (msg == 26) // WM_SETTINGCHANGE
        {
            if (Marshal.PtrToStringAuto(lParam) == "ImmersiveColorSet")
            {
                ModBase.Log($"[System] 系统主题更改，深色模式：{SystemTheme.IsSystemInDarkMode()}");
                if (Config.Preference.Theme.ColorMode == ColorMode.System &&
                    (ThemeManager.IsDarkMode != SystemTheme.IsSystemInDarkMode())) ThemeService.RefreshColorMode();
            }
        }

        return nint.Zero;
    }

    // 窗口隐藏与置顶
    public bool Hidden
    {
        get => field;
        set
        {
            if (field == value)
                return;
            field = value;
            if (value)
            {
                // 隐藏
                // [port] WPF Window.Left -= n → Avalonia Window.Position（PixelPoint）整体改写；Visibility.Hidden → IsVisible=false
                Position = new PixelPoint(Position.X - 10000, Position.Y);
                ShowInTaskbar = false;
                IsVisible = false;
                ModBase.Log("[System] 窗口已隐藏，位置：(" + Position.X + "," + Position.Y + ")");
            }
            else
            {
                // 取消隐藏
                if (Position.X < -2000)
                    Position = new PixelPoint(Position.X + 10000, Position.Y);
                ShowWindowToTop();
            }
        }
    }
    /// <summary>
    ///     把当前窗口拖到最前面。
    /// </summary>
    public void ShowWindowToTop()
    {
        ModBase.RunInUi(() =>
        {
            // 这一坨乱七八糟的，别改，改了指不定就炸了，自己电脑还复现不出来
            // [port] WPF Visibility.Visible → Avalonia IsVisible=true
            IsVisible = true;
            ShowInTaskbar = true;
            WindowState = WindowState.Normal;
            Hidden = false;
            Topmost = true; // 偶尔 SetForegroundWindow 失效
            Topmost = false;
            ModMain.SetForegroundWindow(ModBase.frmHandle);
            Focus();
            ModBase.Log($"[System] 窗口已置顶，位置：({Position.X}, {Position.Y}), {Width} x {Height}");
        });
    }

    // 背景视频循环播放
    // [port] 背景视频（VideoBack 原 MediaElement）暂缓移植：AXAML 已移除该元素，此处理器为遗留死代码；保留签名，实现注释并留待将来接入。
    private void VideoEnded(object sender, RoutedEventArgs e)
    {
        // VideoBack.Position = TimeSpan.Zero;
        // VideoBack.Play();
    }

    // 最小化时暂停背景视频
    private void WindowStateChanged(object sender, EventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Minimized:
            {
                ModVideoBack.isMinimized = true;
                ModVideoBack.VideoPause();
                break;
            }
            case WindowState.Normal:
            {
                ModVideoBack.isMinimized = false;
                ModVideoBack.VideoPlay();
                break;
            }
        }
    }

    #endregion

    #region 切换页面

    // 页面种类与属性
    // 注意，这一枚举在 “切换页面” EventType 中调用，应视作公开 API 的一部分
    /// <summary>
    ///     页面种类。
    /// </summary>
    public enum PageType
    {
        /// <summary>
        ///     启动。
        /// </summary>
        Launch = 0,

        /// <summary>
        ///     下载。
        /// </summary>
        Download = 1,

        /// <summary>
        ///     联机。
        /// </summary>
        Tools = 3,

        /// <summary>
        ///     设置。
        /// </summary>
        Setup = 2,

        /// <summary>
        ///     实例选择。这是一个副页面。
        /// </summary>
        InstanceSelect = 5,

        /// <summary>
        ///     任务管理。这是一个副页面。
        /// </summary>
        TaskManager = 6,

        /// <summary>
        ///     实例设置。这是一个副页面。
        /// </summary>
        InstanceSetup = 7,

        /// <summary>
        ///     资源工程详情。这是一个副页面。
        /// </summary>
        CompDetail = 8,

        /// <summary>
        ///     游戏实时日志。这是一个副页面。
        /// </summary>
        GameLog = 10,

        /// <summary>
        ///     存档详细管理，这是一个副页面。
        /// </summary>
        VersionSaves = 12,
    }

    /// <summary>
    ///     次要页面种类。其数值必须与 StackPanel 中的下标一致。
    /// </summary>
    public enum PageSubType
    {
        Default = 0,
        DownloadInstall = 1,
        DownloadMod = 2,
        DownloadPack = 3,
        DownloadDataPack = 4,
        DownloadResourcePack = 5,
        DownloadShader = 6,
        DownloadWorld = 7,
        DownloadCompFavorites = 8,
        DownloadClient = 9,
        DownloadOptiFine = 10,
        DownloadForge = 11,
        DownloadNeoForge = 12,
        DownloadCleanroom = 13,
        DownloadFabric = 14,
        DownloadLiteLoader = 16,
        DownloadLabyMod = 17,
        DownloadLegacyFabric = 18,

        SetupLaunch = 0,
        SetupUI = 1,
        SetupGameManage = 2,
        SetupLink = 3,
        SetupAbout = 4,
        SetupLog = 5,
        SetupFeedback = 6,
        SetupGameLink = 7,
        SetupUpdate = 8,
        SetupJava = 9,
        SetupLauncherMisc = 10,
        SetupLauncherLanguage = 11,

        ToolsGameLink = 1,
        ToolsTest = 3,

        VersionOverall = 0,
        VersionSetup = 1,
        VersionExport = 2,
        VersionWorld = 3,
        VersionScreenshot = 4,
        VersionMod = 5,
        VersionModDisabled = 6,
        VersionResourcePack = 7,
        VersionShader = 8,
        VersionSchematic = 9,
        VersionInstall = 10,
        VersionServer = 11,
        VersionSavesInfo = 0,
        VersionSavesDatapack = 1
    }

    /// <summary>
    ///     获取次级页面的名称。若并非次级页面则返回空字符串，故可以以此判断是否为次级页面。
    /// </summary>
    private string PageNameGet(PageStackData stack)
    {
        switch (stack.page)
        {
            case PageType.InstanceSelect:
            {
                return Lang.Text("Main.Title.InstanceSelect");
            }
            case PageType.TaskManager:
            {
                return Lang.Text("Main.Title.TaskManager");
            }
            case PageType.GameLog:
            {
                return Lang.Text("Main.Title.GameLog");
            }
            case PageType.InstanceSetup:
            {
                return Lang.Text("Main.Title.InstanceSetup", PageInstanceLeft.McInstance is null ? Lang.Text("Common.State.Unknown") : PageInstanceLeft.McInstance.Name);
            }
            case PageType.CompDetail:
            {
                return Lang.Text("Main.Title.ResourceDownload", stack.additional.Value.CompProject.TranslatedName);
            }
            case PageType.VersionSaves:
            {
                return Lang.Text("Main.Title.SaveManagement", ModBase.GetFolderNameFromPath(stack.additional.Value.SavePath));
            }

            default:
            {
                return "";
            }
        }
    }

    /// <summary>
    ///     刷新次级页面的名称。
    /// </summary>
    public void PageNameRefresh(PageStackData type)
    {
        LabTitleInner.Text = PageNameGet(type);
    }

    /// <summary>
    ///     刷新次级页面的名称。
    /// </summary>
    public void PageNameRefresh()
    {
        PageNameRefresh(pageCurrent);
    }

    // 页面状态存储
    /// <summary>
    ///     当前的主页面。
    /// </summary>
    public PageStackData pageCurrent = PageType.Launch;

    /// <summary>
    ///     上一个主页面。
    /// </summary>
    public PageStackData pageLast = PageType.Launch;

    /// <summary>
    ///     当前的子页面。
    /// </summary>
    public PageSubType PageCurrentSub
    {
        get
        {
            switch (pageCurrent.page)
            {
                case PageType.Download:
                {
                    if (ModMain.frmDownloadLeft is null)
                        ModMain.frmDownloadLeft = new PageDownloadLeft();
                    return ModMain.frmDownloadLeft.pageID;
                }

                case PageType.Setup:
                {
                    if (ModMain.frmSetupLeft is null)
                        ModMain.frmSetupLeft = new PageSetupLeft();
                    return ModMain.frmSetupLeft.pageID;
                }

                case PageType.InstanceSetup:
                {
                    if (ModMain.frmInstanceLeft is null)
                        ModMain.frmInstanceLeft = new PageInstanceLeft();
                    return ModMain.frmInstanceLeft.pageID;
                }

                default:
                {
                    return 0; // 没有子页面
                }
            }
        }
    }

    /// <summary>
    ///     上层页面的编号堆栈，用于返回。
    /// </summary>
    public List<PageStackData> pageStack = new();

    public class PageStackData
    {
        /// <summary>
        /// <list type="bullet">
        ///   <item><description>CompDetail: (CompProject, ExpandedTitles, TargetVersion, TargetLoader, ResourceType)</description></item>
        ///   <item><description>VersionSaves: SavePath</description></item>
        /// </list>
        /// </summary>
        public (
            ModComp.CompProject CompProject,
            List<string> ExpandedTitles,
            string TargetVersion,
            ModComp.CompLoaderType TargetLoader,
            ModComp.CompType ResourceType,
            string SavePath
        )? additional;

        public PageType page;

        public override bool Equals(object other)
        {
            if (other is null)
                return false;
            if (other is PageStackData)
            {
                var pageOther = (PageStackData)other;
                if (page != pageOther.page)
                    return false;
                if (additional is null) return pageOther.additional is null;

                return pageOther.additional is not null && additional.Equals(pageOther.additional);
            }

            if (other is int o)
            {
                if ((int)page == o)
                    return false;
                return additional is null;
            }

            return false;
        }

        public static bool operator ==(PageStackData left, PageStackData right)
        {
            return EqualityComparer<PageStackData>.Default.Equals(left, right);
        }

        public static bool operator !=(PageStackData left, PageStackData right)
        {
            return !(left == right);
        }

        public static implicit operator PageStackData(PageType value)
        {
            return new PageStackData { page = value };
        }

        public static implicit operator PageType(PageStackData value)
        {
            return value.page;
        }
    }

    public MyPageLeft pageLeft;
    public MyPageRight pageRight;

    // 引发实际页面切换的入口
    private bool isChangingPage;

    /// <summary>
    ///     切换页面，并引起对应选择 UI 的改变。
    /// </summary>
    public void PageChange(PageStackData stack, PageSubType subType = PageSubType.Default)
    {
        if (string.IsNullOrEmpty(PageNameGet(stack)))
        {
            // 切换到主页面
            PageChangeExit();
            isChangingPage = true; // 防止下面的勾选直接触发了 PageChangeActual
            ((MyRadioButton)PanTitleSelect.Children[(int)stack.page]).SetChecked(true, true,
                string.IsNullOrEmpty(PageNameGet(pageCurrent)));
            isChangingPage = false;
            switch (stack.page)
            {
                case PageType.Download:
                {
                    if (ModMain.frmDownloadLeft is null)
                        ModMain.frmDownloadLeft = new PageDownloadLeft();
                    foreach (var item in ModMain.frmDownloadLeft.PanItem.Children)
                        if (item is MyListItem listItem &&
                            ModBase.Val(listItem.Tag) == (double)subType)
                        {
                            listItem.SetChecked(true, true, stack == pageCurrent);
                            break;
                        }

                    break;
                }
                case PageType.Setup:
                {
                    if (ModMain.frmSetupLeft is null)
                        ModMain.frmSetupLeft = new PageSetupLeft();
                    foreach (var item in ModMain.frmSetupLeft.PanItem.Children)
                        if (item is MyListItem listItem &&
                            ModBase.Val(listItem.Tag) == (double)subType)
                        {
                            listItem.SetChecked(true, true, stack == pageCurrent);
                            break;
                        }

                    break;
                }
            }

            PageChangeActual(stack, subType);
        }
        else
        {
            // 切换到次页面
            switch (stack.page)
            {
                case PageType.InstanceSetup:
                {
                    if (ModMain.frmInstanceLeft is null)
                        ModMain.frmInstanceLeft = new PageInstanceLeft();
                    foreach (var item in ModMain.frmInstanceLeft.PanItem.Children)
                        if (item is MyListItem listItem &&
                            ModBase.Val(listItem.Tag) == (double)subType)
                        {
                            listItem.SetChecked(true, true, stack == pageCurrent);
                            break;
                        }

                    break;
                }
                case PageType.VersionSaves:
                {
                    if (ModMain.frmInstanceSavesLeft is null)
                        ModMain.frmInstanceSavesLeft = new PageInstanceSavesLeft();
                    foreach (var item in ModMain.frmInstanceSavesLeft.PanItem.Children)
                        if (item is MyListItem listItem &&
                            ModBase.Val(listItem.Tag) == (double)subType)
                        {
                            listItem.SetChecked(true, true, stack == pageCurrent);
                            break;
                        }

                    break;
                }
            }

            PageChangeActual(stack, subType);
        }
    }

    /// <summary>
    ///     通过点击导航栏改变页面。
    /// </summary>
    private void BtnTitleSelect_Click(MyRadioButton sender, bool raiseByMouse)
    {
        if (isChangingPage)
            return;
        var pageType = (PageType)int.Parse(sender.Tag.ToString());
        PageChangeActual(pageType, PageSubType.Default);
        }

    private void BtnTitleInner_Click(object sender, EventArgs e)
    {
        PageBack();
    }

    /// <summary>
    ///     通过点击返回按钮或手动触发返回来改变页面。
    /// </summary>
    public void PageBack()
    {
        if (pageStack.Any())
            PageChangeActual(pageStack[0], PageSubType.Default);
        else
            PageChange(PageType.Launch);
    }

    // 实际处理页面切换
    /// <summary>
    ///     切换现有页面的实际方法。
    /// </summary>
    private void PageChangeActual(PageStackData stack, PageSubType subType)
    {
        if (pageCurrent == stack && (PageCurrentSub == subType || (int)subType == -1))
            return;
        ModAnimation.AniControlEnabled += 1;
        try
        {
            #region 子页面处理

            var pageName = PageNameGet(stack);
            if (string.IsNullOrEmpty(pageName))
            {
                // 即将切换到一个顶级页面
                PageChangeExit();
            }
            // 即将切换到一个子页面
            else if (pageStack.Any())
            {
                // 子页面 → 另一个子页面，更新
                ModAnimation.AniStart(
                    new[]
                    {
                    ModAnimation.AaOpacity(LabTitleInner, -LabTitleInner.Opacity, 130),
                    ModAnimation.AaCode(() => LabTitleInner.Text = pageName, after: true),
                    ModAnimation.AaOpacity(LabTitleInner, 1d, 150, 30)
                    }, "FrmMain Titlebar SubLayer");
                if (pageStack.Contains(stack))
                    // 返回到更上层的子页面
                    while (pageStack.Contains(stack))
                        pageStack.RemoveAt(0);
                else
                    // 进入更深层的子页面
                    pageStack.Insert(0, pageCurrent);
            }
            else
            {
                // 主页面 → 子页面，进入
                // [port] WPF Visibility.Visible/Collapsed → Avalonia IsVisible true/false（无 Visibility 枚举）
                PanTitleInner.IsVisible = true;
                PanTitleMain.IsHitTestVisible = false;
                PanTitleInner.IsHitTestVisible = true;
                PageNameRefresh(stack);
                ModAnimation.AniStart(
                    new[]
                    {
                    ModAnimation.AaOpacity(PanTitleMain, -PanTitleMain.Opacity, 150),
                    ModAnimation.AaX(PanTitleMain, 12d - PanTitleMain.Margin.Left, 150,
                        ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
                    ModAnimation.AaOpacity(PanTitleInner, 1d - PanTitleInner.Opacity, 150, 200),
                    ModAnimation.AaX(PanTitleInner, -PanTitleInner.Margin.Left, 350, 200,
                        new ModAnimation.AniEaseOutBack()),
                    ModAnimation.AaCode(() => PanTitleMain.IsVisible = false, after: true)
                    }, "FrmMain Titlebar FirstLayer");
                pageStack.Insert(0, pageCurrent);
            }

            #endregion

            #region 实际更改页面框架 UI

            pageLast = pageCurrent;
            pageCurrent = stack;
            switch (stack.page)
            {
                case PageType.Launch: // 启动
                    {
                        PageChangeAnim(ModMain.frmLaunchLeft, ModMain.frmLaunchRight);
                        break;
                    }
                case PageType.Download: // 下载
                    {
                        ModMain.frmDownloadLeft ??= new PageDownloadLeft();
                        if (subType != PageSubType.Default)
                            ModMain.frmDownloadLeft.pageID = subType;
                        else
                            subType = ModMain.frmDownloadLeft.pageID;
                        // PageGet 方法会在未设置 SubType 时指定默认值，并建立相关页面的实例
                        PageChangeAnim(ModMain.frmDownloadLeft, (Control)ModMain.frmDownloadLeft.PageGet(subType));
                        break;
                    }
                case PageType.Tools: // 联机
                    {
                        // [port] 工具页（联机/测试）暂缓移植：该标签暂不展示内容页
                        break;
                    }
                case PageType.Setup: // 设置
                    {
                        ModMain.frmSetupLeft ??= new PageSetupLeft();
                        subType = ModMain.frmSetupLeft.pageID;
                        PageChangeAnim(ModMain.frmSetupLeft, (Control)ModMain.frmSetupLeft.PageGet(subType));
                        break;
                    }
                case PageType.GameLog: // 实时日志
                    {
                        if (ModMain.frmLogLeft is null)
                            ModMain.frmLogLeft = new PageLogLeft();
                        if (ModMain.frmLogLeft is null)
                            ModMain.frmLogRight = new PageLogRight();
                        PageChangeAnim(ModMain.frmLogLeft, ModMain.frmLogRight);
                        break;
                    }
                case PageType.InstanceSelect: // 实例选择
                    {
                        if (ModMain.frmSelectLeft is null)
                            ModMain.frmSelectLeft = new PageSelectLeft();
                        if (ModMain.frmSelectRight is null)
                            ModMain.frmSelectRight = new PageSelectRight();
                        PageChangeAnim(ModMain.frmSelectLeft, ModMain.frmSelectRight);
                        break;
                    }
                case PageType.TaskManager: // 任务管理
                    {
                        if (ModMain.frmSpeedLeft is null)
                            ModMain.frmSpeedLeft = new PageSpeedLeft();
                        if (ModMain.frmSpeedRight is null)
                            ModMain.frmSpeedRight = new PageSpeedRight();
                        PageChangeAnim(ModMain.frmSpeedLeft, ModMain.frmSpeedRight);
                        break;
                    }
                case PageType.InstanceSetup: // 实例设置
                    {
                        ModMain.frmInstanceLeft ??= new PageInstanceLeft();
                        subType = ModMain.frmInstanceLeft.pageID;
                        PageChangeAnim(ModMain.frmInstanceLeft, (Control)ModMain.frmInstanceLeft.PageGet(subType));
                        break;
                    }
                case PageType.CompDetail: // Mod 信息
                    {
                        if (ModMain.frmDownloadCompDetail is null)
                            ModMain.frmDownloadCompDetail = new PageDownloadCompDetail();
                        PageChangeAnim(new MyPageLeft(), ModMain.frmDownloadCompDetail);
                        break;
                    }
                case PageType.VersionSaves: // 存档管理
                    {
                        if (ModMain.frmInstanceSavesLeft is null)
                            ModMain.frmInstanceSavesLeft = new PageInstanceSavesLeft();
                        PageInstanceSavesLeft.currentSave = stack.additional.Value.SavePath;
                        subType = ModMain.frmInstanceSavesLeft.pageID;
                        PageChangeAnim(ModMain.frmInstanceSavesLeft,
                            (Control)ModMain.frmInstanceSavesLeft.PageGet(subType));
                        break;
                    }
            }

            #endregion

            #region 设置为最新状态

            BtnExtraDownload.ShowRefresh();
            BtnExtraApril.ShowRefresh();

            #endregion

            ModBase.Log("[Control] 切换主要页面：" + ModBase.GetStringFromEnum(stack) + ", " + (int)subType);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "切换主要页面失败（ID " + (int)pageCurrent.page + "）",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
        }
        finally
        {
            ModAnimation.AniControlEnabled -= 1;
        }
    }

    private void PageChangeAnim(Control targetLeft, Control targetRight)
    {
        ModAnimation.AniStop("FrmMain LeftChange");
        ModAnimation.AniStop("PageLeft PageChange"); // 停止左边栏变更导致的右页面切换动画，防止它与本动画一起触发多次 PageOnEnter
        ModAnimation.AniControlEnabled += 1;
        // 清除新页面关联性
        // [port] WPF ContentPresenter → Avalonia.Controls.Presenters.ContentPresenter（Avalonia 12 命名空间移动）
        if (targetLeft.Parent is not null)
            targetLeft.SetValue(Avalonia.Controls.Presenters.ContentPresenter.ContentProperty, null);
        if (targetRight is not null && targetRight.Parent is not null)
            targetRight.SetValue(Avalonia.Controls.Presenters.ContentPresenter.ContentProperty, null);
        pageLeft = (MyPageLeft)targetLeft;
        pageRight = (MyPageRight)targetRight;
        // 触发页面通用动画
        ((MyPageLeft)PanMainLeft.Child).TriggerHideAnimation();
        ((MyPageRight)PanMainRight.Child).PageOnExit();
        ModAnimation.AniControlEnabled -= 1;
        // 执行动画
        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaCode(() =>
            {
                ModAnimation.AniControlEnabled += 1;
                // 把新页面添加进容器
                PanMainLeft.Child = pageLeft;
                pageLeft.Opacity = 0d;
                PanMainLeft.Background = null;
                ModAnimation.AniControlEnabled -= 1;
                ModBase.RunInUi(() => PanMainLeft_Resize(PanMainLeft.Bounds.Width), true);
            }, 110),
            ModAnimation.AaCode(() =>
            {
                // 延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                pageLeft.Opacity = 1d;
                pageLeft.TriggerShowAnimation();
            }, 30, true)
        }, "FrmMain PageChangeLeft");
        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaCode(() =>
            {
                ModAnimation.AniControlEnabled += 1;
                ((MyPageRight)PanMainRight.Child).PageOnForceExit();
                // 把新页面添加进容器
                PanMainRight.Child = pageRight;
                pageRight.Opacity = 0d;
                PanMainRight.Background = null;
                ModAnimation.AniControlEnabled -= 1;
                ModBase.RunInUi(() => BtnExtraBack.ShowRefresh(), true);
            }, 110),
            ModAnimation.AaCode(() =>
            {
                // 延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                pageRight.Opacity = 1d;
                pageRight.PageOnEnter();
            }, 30, true)
        }, "FrmMain PageChangeRight");
    }

    /// <summary>
    ///     退出子界面。
    /// </summary>
    private void PageChangeExit()
    {
        if (pageStack.Any())
        {
            // 子页面 → 主页面，退出
            PanTitleMain.IsVisible = true;
            PanTitleMain.IsHitTestVisible = true;
            PanTitleInner.IsHitTestVisible = false;
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaOpacity(PanTitleInner, -PanTitleInner.Opacity, 150),
                    ModAnimation.AaX(PanTitleInner, -18 - PanTitleInner.Margin.Left, 150,
                        ease: new ModAnimation.AniEaseInFluent()),
                    ModAnimation.AaOpacity(PanTitleMain, 1d - PanTitleMain.Opacity, 150, 200),
                    ModAnimation.AaX(PanTitleMain, -PanTitleMain.Margin.Left, 350, 200,
                        new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
                    ModAnimation.AaCode(() => PanTitleInner.IsVisible = false, after: true)
                }, "FrmMain Titlebar FirstLayer");
            pageStack.Clear();
        }
        // 主页面 → 主页面，无事发生
    }

    // 左边栏改变
    private void PanMainLeft_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged)
            return;
        PanMainLeft_Resize(e.NewSize.Width);
    }

    private void PanMainLeft_Resize(double newWidth)
    {
        var delta = newWidth - RectLeftBackground.Width;
        if (Math.Abs(delta) > 0.1d && ModAnimation.AniControlEnabled == 0)
        {
            if (PanMain.Opacity < 0.1d)
                PanMainLeft.IsHitTestVisible = false; // 避免左边栏指向背景未能完美覆盖左边栏
            if (newWidth > 0d)
                // 宽度足够，显示
                ModAnimation.AniStart(
                    new[]
                    {
                        ModAnimation.AaWidth(RectLeftBackground, newWidth - RectLeftBackground.Width, 180,
                            ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)),
                        ModAnimation.AaOpacity(RectLeftShadow, 1d - RectLeftShadow.Opacity, 180),
                        ModAnimation.AaCode(() => PanMainLeft.IsHitTestVisible = true, 150)
                    }, "FrmMain LeftChange", true);
            else
                // 宽度不足，隐藏
                ModAnimation.AniStart(
                    new[]
                    {
                        ModAnimation.AaWidth(RectLeftBackground, -RectLeftBackground.Width, 180,
                            ease: new ModAnimation.AniEaseOutFluent()),
                        ModAnimation.AaOpacity(RectLeftShadow, -RectLeftShadow.Opacity, 180),
                        ModAnimation.AaCode(() => PanMainLeft.IsHitTestVisible = true, 150)
                    }, "FrmMain LeftChange", true);
        }
        else
        {
            RectLeftBackground.Width = newWidth;
            PanMainLeft.IsHitTestVisible = true;
            ModAnimation.AniStop("FrmMain LeftChange");
        }
    }

    #endregion

    #region 控件拖动

    // 在时钟中调用，使得即使鼠标在窗口外松开，也可以释放控件
    public void DragTick()
    {
        if (ModMain.dragControl is null)
            return;
        // [port] WPF Mouse.LeftButton == MouseButtonState.Pressed → Avalonia 无静态鼠标状态查询，改用指针事件维护的 mouseLeftButtonPressed
        if (!mouseLeftButtonPressed) DragStop();
    }

    // 在鼠标移动时调用，以改变 Slider 位置
    public void DragDoing()
    {
        if (ModMain.dragControl is null)
            return;
        if (mouseLeftButtonPressed) 
        {
            ModMain.dragControl.DragDoing();
        }
        else
            DragStop();
    }

    private void PanBack_MouseMove(object sender, EventArgs e)
    {
        DragDoing();
    }

    public void DragStop()
    {
        // 存在其他线程调用的可能性，因此需要确保在 UI 线程运行
        ModBase.RunInUi(() =>
        {
            if (ModMain.dragControl is null)
                return;
            var control = ModMain.dragControl;
            ModMain.dragControl = null;
            control.DragStop(); // 控件会在该事件中判断 DragControl，所以得放在后面
        });
    }

    #endregion

    #region 附加按钮

    // 更新重启
    private void BtnExtraUpdateRestart_Click(object sender, PointerReleasedEventArgs e)
    {
        UpdateManager.UpdateRestart(true);
    }

    private bool BtnExtraUpdateRestart_ShowCheck()
    {
        return UpdateManager.isUpdateWaitingRestart;
    }

    // 音乐
    private void BtnExtraMusic_Click(object sender, PointerReleasedEventArgs e)
    {
        ModMusic.MusicControlPause();
    }

    private void BtnExtraMusic_RightClick(object sender, PointerReleasedEventArgs e)
    {
        ModMusic.MusicControlNext();
    }

    // 任务管理
    private void BtnExtraDownload_Click(object sender, PointerReleasedEventArgs e)
    {
        PageChange(PageType.TaskManager);
    }

    private bool BtnExtraDownload_ShowCheck()
    {
        return ModNet.HasDownloadingTask() && !(pageCurrent == PageType.TaskManager);
    }

    // 投降
    public void AprilGiveup()
    {
        if (ModMain.isAprilEnabled && !ModMain.isAprilGiveup)
        {
            HintService.Hint("=D", HintType.Success);
            ModMain.isAprilGiveup = true;
            // [port] AprilScaleTrans（PageLaunchLeft 内 RenderTransform 的 x:Name）未被 Avalonia 命名字段生成器生成，
            //        且 PageLaunchLeft 不在本次可改动范围内，无法在 FormMain 侧复位缩放；此行为暂缓（愚人节“投降”仅不再复位缩放）。
            // ModMain.frmLaunchLeft.AprilScaleTrans.ScaleX = 1d;
            // ModMain.frmLaunchLeft.AprilScaleTrans.ScaleY = 1d;
            BtnExtraApril.ShowRefresh();
        }
    }

    private void BtnExtraApril_Click(object sender, PointerReleasedEventArgs e)
    {
        AprilGiveup();
    }

    public bool BtnExtraApril_ShowCheck()
    {
        return ModMain.isAprilEnabled && !ModMain.isAprilGiveup && pageCurrent == PageType.Launch;
    }

    // 关闭 Minecraft
    private void BtnExtraShutdown_Click(object sender, PointerReleasedEventArgs e)
    {
        try
        {
            if (ModLaunch.mcLaunchLoaderReal is not null)
                ModLaunch.mcLaunchLoaderReal.Abort();
            foreach (var Watcher in ModWatcher.mcWatcherList)
                Watcher.Kill();
            HintService.Hint(Lang.Text("Main.ShutdownMinecraft.Success"), HintType.Success);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "强制关闭所有 Minecraft 失败",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Main.Error.OperationFailed"));
        }
    }

    public bool BtnExtraShutdown_ShowCheck()
    {
        return ModWatcher.hasRunningMinecraft;
    }

    // 游戏日志
    private void BtnExtraLog_Click(object sender, PointerReleasedEventArgs e)
    {
        PageChange(PageType.GameLog);
    }

    public bool BtnExtraLog_ShowCheck()
    {
        if (ModMain.frmLogLeft is null || ModMain.frmLogRight is null || pageCurrent == PageType.GameLog)
            return false;
        return ModMain.frmLogLeft.shownLogs.Count > 0;
    }

    /// <summary>
    ///     返回顶部。
    /// </summary>
    public void BackToTop()
    {
        var realScroll = BtnExtraBack_GetRealChild();
        if (realScroll is not null)
            // [port] WPF ScrollViewer.VerticalOffset → Avalonia ScrollViewer.Offset.Y（Vector）
            realScroll.PerformVerticalOffsetDelta(-realScroll.Offset.Y);
        else
            ModBase.Log(
                "[UI] 无法返回顶部，未找到合适的 RealScroll",
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Main.Error.ScrollToTopFailed"));
    }

    private void BtnExtraBack_Click(object sender, PointerReleasedEventArgs e)
    {
        BackToTop();
    }

    private bool BtnExtraBack_ShowCheck()
    {
        var realScroll = BtnExtraBack_GetRealChild();
        // [port] WPF ScrollViewer.VerticalOffset → Avalonia ScrollViewer.Offset.Y（Vector）
        return realScroll is not null && realScroll.IsVisible &&
               realScroll.Offset.Y > Height + (BtnExtraBack.Show ? 0 : 700);
    }

    private MyScrollViewer? BtnExtraBack_GetRealChild()
    {
        if (PanMainRight.Child is null || !(PanMainRight.Child is MyPageRight))
            return null;
        return ((MyPageRight)PanMainRight.Child).PanScroll;
    }

    #endregion
}
