# PCL-CE UI 原样移植评估报告（WPF → Avalonia / Linux）

> 结论先行：**WPF 程序无法在 Linux 上直接运行**（微软不提供 Linux 版 WPF 运行时，这是框架限制，不是移植方法问题）。
> 因此"原样移植"的唯一可行形式是：把 PCL-CE 的 XAML/C# **逐行等价翻译**为 Avalonia（Linux 可用的开源 UI 框架，API 与 WPF 高度同源）。
> 翻译保证：布局结构、控件种类、颜色、尺寸、字体、动画、交互逻辑与原版一一对应；**不重新设计、不发明新样式、不改动任何文案与交互**。
>
> 盘点结果：约 90% 的 XAML 元素与大部分 C# 逻辑可机械对应（A 类，无需批准）；
> 12 项需要等价改写（B 类，**每项列出原因与改法，请你批准**）；
> 8 项 Windows 专属系统功能在 Linux 上没有对应 API（C 类，**需要你决定降级行为**）。

---

## 1. 移植对象规模（实测）

| 部分 | 内容 | 规模 |
|---|---|---|
| 主工程 `Plain Craft Launcher 2` | 91 个 XAML（页面/控件/资源） | ~9,600 行 |
| 主工程 | C# 代码（含页面逻辑、Mod* 业务模块） | ~78,000 行 / 204 文件 |
| `PCL.Core` UI 子系统 | 动画框架 30+ 文件、特效、控件、主题、SvgIcon | ~15,000 行 |
| `PCL.Core` 本地化 | 7 种语言 ResourceDictionary | ~12,900 行 |
| 图片/图标资源 | Images/**（约 80 个）、lucide SVG ×130、内嵌字体 Font.ttf | 原样复制，零改动 |

PCL-CE 的 UI 与业务是耦合的：每个页面 code-behind 直接调用 `Setup.*`、`ModMain`、`McVersion`、`AniStart` 等。
**因此移植面 = 主工程全部 UI + Modules + PCL.Core 中被 UI 引用的子系统**，纯翻译，不改架构。

---

## 2. A 类：可直接机械翻译（无需批准）

以下 WPF 元素在 Avalonia 中同名同义，逐行对应翻译：

- **布局容器**：Grid / StackPanel / Border / Canvas / DockPanel / WrapPanel / UniformGrid —— 属性同名（Row/Column/Margin/HorizontalAlignment…）
- **基础控件**：TextBlock / TextBox / Button / CheckBox / RadioButton / Slider / ComboBox / ListBox / ProgressBar / Image / Path / Rectangle / Ellipse / ScrollViewer / Menu —— 同名
- **资源系统**：ResourceDictionary / StaticResource / DynamicResource / x:Static —— 完全一致；主题换色依赖的 DynamicResource 运行时切换语义相同
- **画笔与变换**：LinearGradientBrush / RadialGradientBrush / SolidColorBrush / RotateTransform / TranslateTransform / ScaleTransform / TransformGroup —— 同名
- **样式与模板**：Style / Setter / ControlTemplate / Style BasedOn —— 同名（Trigger 除外，见 B1）
- **事件与数据绑定**：Click / Checked / MouseDown / Binding / MultiBinding / Converter —— 同名（IValueConverter 接口形状一致）
- **语言资源**：7 个语言 xaml 原样翻译（键值结构不变）
- **图片资源**：Images/** 原样复制；内嵌字体 Font.ttf 原样嵌入

---

## 3. B 类：需要等价改写（语义不变、实现方式改变）—— 请求批准

| # | 组件 | 用在哪 | 为什么不能直接搬 | 改法 | 行为是否一致 |
|---|---|---|---|---|---|
| B1 | WPF `Trigger` / `Storyboard`（仅 Application.xaml 内 ComboBox/ContextMenu 样式用到） | 全局样式 | Avalonia 无 Trigger 机制 | 用 Classes + 选择器 + Transitions/Animation 等价实现 | 视觉与动画一致 |
| B2 | `WindowChrome`（无边框、圆角 12、标题栏拖动） | FormMain | WPF 专属窗口镶边 API | `SystemDecorations="None"` + ExtendClientArea + 手动拖拽/缩放（成熟做法） | 视觉一致；拖动/缩放/最大化行为一致 |
| B3 | `DropShadowEffect` / `BlurEffect` ×12；`DropShadowBitmapEffect` ×1（PageLaunchLeft） | 按钮、弹窗、滑条、皮肤阴影 | 前两者 Avalonia 11.1+ 有同名实现（需开启实验特性开关）；后者是 WPF 已废弃 API，Avalonia 无 | DropShadowEffect 直接映射；BitmapEffect 一处换成 DropShadowEffect | 一致（阴影参数不变） |
| B4 | 自定义动画框架底座（PCL.Core/Animation 共 30+ 文件，全局 `AniStart/AniStop` 依赖它） | 所有控件动画 | 其中 4 个文件直接耦合 WPF：WpfAnimatable、WpfUIAccessProvider(Dispatcher)、WpfCompositionTargetRenderingClock(CompositionTarget)、WinMMClock(winmm P/Invoke) | 框架其余 ~95% 代码原样保留；仅替换这 4 个底座为 Avalonia 等价物（Dispatcher.UIThread / RequestAnimationFrame / 跨平台计时器） | 上层 API 零改动，动画时序一致 |
| B5 | GDI+ 位图层 `MyBitmap`（MySkin/PageToolsTest/IconHelper/ModBase 引用 System.Drawing） | 皮肤渲染、图标提取 | System.Drawing 自 .NET 7 起**仅支持 Windows**，Linux 直接抛异常 | `MyBitmap` 对外 API 不变，内部换 SkiaSharp（已在依赖里）；调用代码零改动 | 一致 |
| B6 | WPF 渲染 API：RenderTargetBitmap / DrawingVisual / FormattedText / BitmapDecoder（MotdRenderer、BackgroundPresenter、BlurBorder 等） | 服务器 MOTD 渲染、背景合成 | DrawingVisual 是 WPF 专属可视树 | RenderTargetBitmap/FormattedText Avalonia 有同名 API；DrawingVisual → DrawingContext 即时绘制；BitmapDecoder → Avalonia 位图解码 | 一致 |
| B7 | HLSL 像素着色器模糊：AdaptiveBlur.ps + AdaptiveBlurEffect/EnhancedBlurEffect/OptimizedBlurEffect/SamplingBlurProcessor（~55KB） | 背景自适应模糊 | **Avalonia 不支持自定义像素着色器**（框架无此机制，无等价物） | 对外 API（AdaptiveBlurEffect）不变，内部改用 Avalonia 内置 BlurEffect + 降采样叠加实现 | **B 类中唯一视觉不完全逐像素一致的项**：模糊观感近似，强度/渐变行为一致 |
| B8 | Markdig.Wpf + FlowDocument（Resources/Markdown.xaml、MyMsgMarkdown 公告弹窗） | 公告/Markdown 弹窗 | FlowDocument 是 WPF 专属文档树 | 改用 Markdown.Avalonia 渲染同一 Markdown 文本；Markdown.xaml 的颜色/字号/间距翻译为对应样式 | 内容一致；个别排版细节可能有细微差异 |
| B9 | Microsoft.Xaml.Behaviors.Wpf（EventTrigger 触发新闻翻页等） | PageHomepageNewsView 等 | WPF 专属包 | 换 Avalonia 版 Xaml.Behaviors（同 API 形状） | 一致 |
| B10 | SvgIcon 控件（~40KB，自写 SVG path → WPF Geometry 解析） | 全部图标（lucide ×130） | 依赖 WPF Geometry 类型 | 解析层换 Avalonia StreamGeometry（Path.Data 语法同源）；图标资源不变 | 一致 |
| B11 | 自定义控件基类：Tooltip(21KB) / WaterfallPanel / MyVirtualizingElement / LazyLoadBehavior / ClipboardInterceptor | 各列表页 | 基类是 WPF FrameworkElement/Panel；ClipboardInterceptor 用 WinForms Clipboard | 基类换 Avalonia 对应基类（逻辑照搬）；剪贴板换 Avalonia Clipboard API | 一致 |
| B12 | 线程/UI 调度：`RunInUi/RunInThread`（ModBase）内部用 WPF Dispatcher | 全局 | WPF Dispatcher 对象 | 内部换 `Dispatcher.UIThread`（Avalonia）；所有调用处零改动 | 一致 |

---

## 4. C 类：Windows 专属系统功能（Linux 无对应系统 API）—— 请决定降级行为

| # | 功能 | 现状 | Linux 处理 |
|---|---|---|---|
| C1 | **视频背景** MediaElement + ModVideoBack | 主窗口可播视频背景 | Avalonia 无内置视频。a) Linux 禁用视频，保留图片背景/动态网格（推荐）；b) 集成 LibVLCSharp（+~30MB 原生依赖） |
| C2 | **音乐播放** NAudio + ModMusic | 音乐彩蛋 | NAudio 是 Windows 专属。a) Linux 禁用（推荐）；b) 复用 LibVLCSharp |
| C3 | 系统通知 ToastNotification（WinRT） | 下载完成等通知 | 改用 DBus org.freedesktop.Notifications，样式由桌面环境决定 |
| C4 | 任务栏进度 TaskbarItemInfo | 任务栏缩略图绑定 | Linux 无任务栏 API，忽略（无实际视觉损失） |
| C5 | 注册表（主题检测/Java 扫描/代理/单实例） | 4 处 | Windows 分支保留；Linux 走等价实现（gsettings、which java、文件锁），扫描结果一致 |
| C6 | DPAPI 令牌加密（微软登录凭据） | ProtectedData | Linux 改 AES + 0600 文件权限，存储位置 ~/.config |
| C7 | Win32 P/Invoke 全部 12 处（窗口文本、单实例、GetShortPathName、SetDllDirectory） | ModWatcher/ModMain/ModBase 等 | 条件编译；Linux 等价实现（文件锁单实例等） |
| C8 | app.manifest / DPI 声明 | Windows 清单 | Linux 无需，忽略 |

> C 类其余非 UI 功能（遥测 Sentry、更新通道、联机 ModLink 等）本身是跨平台 .NET 代码，随主工程一起移植，不在此列。

---

## 5. 架构决定

- **现有 PCL.CE-Linux 中的自制 UI（MainWindow/ViewModel/自制深蓝主题）整体删除**，由翻译后的 PCL-CE UI 取代。
- PCL-CE 主工程命名空间 `PCL`、全部文件名、类名、方法名**原样保留**，便于与上游逐行对照。
- 之前从 PCL.Neo 复制的 Core 服务（Account/Game/Profile 等，与 PCL-CE 自带 Mod* 模块功能重复）**不再接入 UI**；是否从仓库删除请你定。
- 移植阶段：
  - **P0** 基础设施：动画框架底座、主题服务、转换器、SvgIcon、语言资源
  - **P1** My* 控件库 19 个控件 + MyMsg 弹窗族
  - **P2** ModBase/ModAnimation/ModSetup/ModLoader + Application.xaml 全局资源与 MyWindow
  - **P3** FormMain（窗口镶边/导航/背景系统）+ 启动页 PageLaunch* + 登录页 PageLogin* + MySkin
  - **P4** 下载页 / 实例页 / 设置页 / 工具页 / 日志页 / 测速页
  - **P5** Linux 适配收尾（C 类）、无头冒烟测试、你的 VM 实测
- 每阶段交付时我会附"与原版的差异清单"，逐项可核对。

## 6. 风险

- 移植面约 10 万行，P0–P3 是硬骨头（动画框架 + 控件库 + 主窗口），P4 页面多但模式重复。
- Avalonia 与 WPF 的文字测量/字距存在引擎级细微差异，个别页面可能达不到逐像素一致（排版语义一致）。
- 第三方包（Markdown.Avalonia / LibVLCSharp）有版本约束，选型时验证与 Avalonia 12 兼容。
