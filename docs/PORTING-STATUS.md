# 移植进度（PORTING-STATUS）

> 随时更新。上游基线：D:\a\j\PCL-CE（verbatim 副本已提交 git 根提交 6a525ab）。

## 已完成

### 仓库结构
- `src/PCL.Core` ← 上游 PCL.Core 逐字副本（转换后 net10.0 + Avalonia 12.0.4）
- `src/PCL.Core.SourceGenerators` ← 原样
- `src/Plain Craft Launcher 2` ← 上游主工程逐字副本（转换中，AssemblyName=PCL）
- 旧 Neo 骨架已删（src/PCL.Neo、Desktop、tests）；`src/PCL.Neo.Core`、`src/PCL.Neo.UI` 保留但不在编译范围
- `docs/PORTING-MAPPING.md`：全部转换规则（规范性文件）
- `tools/convert-cs.ps1`：确定性转换脚本（ASCII-only！本机 PowerShell 按 GBK 读无 BOM 的 UTF-8 脚本，中文注释会破坏解析）

### PCL.Core：编译全绿（commit cbd9538）
关键移植点（详见 git diff）：
- 动画框架底座：AvaloniaAnimatable（原 WpfAnimatable，Width/Height 读 Bounds）、AvaloniaUIAccessProvider（FrameTick 用 DispatcherTimer）、ManagedClock（原 WinMMClock，PeriodicTimer）、AvaloniaRenderLoopClock
- RunAnimation.cs 排除（依赖 Avalonia.Xaml.Behaviors——Avalonia 12 无对应包，当前最新 11.3.0.6）
- ThemeService/LabColor/CatColorResource：ScRgbCompat.FromScRgb 替代 WPF ScRGB 接口
- SvgIcon 全套：StyledProperty + AffectsMeasure/AffectsRender、Render(DrawingContext)、PushTransform(Matrix)、Geometry 子集适配（分段构造为属性初始化、FillRule.NonZero、CounterClockwise）
- ImageLoaderHelper/SvgIconLoader：pack:// → avares:// + AssetLoader
- LocalizationService：语言字典 XML 解析构建（Avalonia 无 ResourceDictionary.Source）
- SystemDialogs：异步 StorageProvider 版（SelectSaveFileAsync/SelectFileAsync/SelectFilesAsync/SelectFolderAsync）
- ClipboardUtils.SetClipboardFilesAsync：DataTransferItem.SetFile
- Files.cs 剪贴板粘贴：TryGetDataAsync + DataFormat.File + TryGetFilesAsync
- IconManager：运行时 XAML 解析（Avalonia 12 无公开 API）→ 专用子集解析器（Viewbox/Canvas/Rectangle/Path，ServerCard 信号图标用）
- CrashMessageBox：日志致命弹窗替代 WPF MessageBox
- ApplicationService.Attach：Avalonia 下由 App.OnFrameworkInitializationCompleted 调用（不再走 Loading 工厂）
- MainWindowService.MainWindowSetter：桌面生命周期 MainWindow 赋值
- Application 扩展：TryFindResource/FindResource/MainWindow（PCL.Core.App.ResourceExtensions）
- AssemblyInfo：Avalonia.Metadata.XmlnsDefinition + XmlnsPrefix（Avalonia 12 都有）
- 仍排除（待移植）：UI\Effects\**（HLSL 着色器模糊，Avalonia 无自定义像素着色器——用户已同意跳过特效）、BlurBorder、MotdRenderer、Tooltip、BackgroundPresenter、ImageConverter、ToastNotification、Utils\OS\DragHelper、LanguageSpecificStringDictionaryExtensions、UI\Animation\Core\RunAnimation.cs

### 主工程（进行中）
- convert-cs.ps1 已跑完（115 .cs + 91 .xaml 机械转换）
- Program.cs：已改为 AppBuilder + LifetimeRunner 模式
- Application.xaml.cs：OnFrameworkInitializationCompleted → ApplicationService.Attach；Initialize() 覆盖；WPF 残留清理完
- csproj：暂缓功能排除清单已加（DeferredExcludes）

## 下一步（按序）
1. **Application.xaml（668 行）手工移植** —— 全局样式核心，作为其余 90 个 XAML 的翻译范本：
   - 头部：Avalonia xmlns；删 ShutdownMode/Markdown.xaml merge/KernelType；BooleanToVisibilityConverter → BoolConverters 映射
   - `<Style TargetType="{x:Type X}">` → `<Style Selector="X">`；Trigger → 选择器（:pointerover/:disabled/:focus/:checked）
   - MyWindow 模板 → Avalonia Window 模板（B2）；ScrollBar/ScrollViewer 模板 → Avalonia 模板（PART 名兼容：Avalonia ScrollViewer 模板钩子不同，参考 Avalonia Fluent 主题改写）
   - MyTextBox/PasswordBox 等逐样式翻译；Storyboard（ComboBox 箭头）→ 删或 Avalonia 动画
2. FormMain.xaml + FormMain.xaml.cs（88KB，窗口镶边/导航/背景）
3. My* 控件库逐个（MyButton/MyCard/MyListItem/MyRadioButton…，依赖属性注册→StyledProperty）
4. 页面（PageLaunch/PageLogin/PageLog/PageSelect → PageDownload → PageInstance → PageSetup）
5. ModBase/ModMain/ModAnimation 等 Modules 编译修复（ModAnimation 独立动画引擎保留，API 映射即可）
6. 全绿后：WSL 无头冒烟（PCL_SMOKE_LAUNCH）→ 用户 VM 实测

## 关键环境事实
- pwsh 工具实际按 GBK 读 .ps1 文件 → 脚本必须纯 ASCII 或带 BOM
- dotnet 10 SDK 在 Windows 主机可用；Avalonia 12.0.4 已在 nuget 缓存
- Avalonia 12 关键差异（实测）：无 Visibility 枚举（用 IsVisible bool）、无 ContentPropertyAttribute（→ [Content] 标注在属性上）、ReadLocalValue→已移除（用 IsSet）、Matrix 三行制 M31/M32、BezierSegment 等只有无参 ctor、IAsyncDataTransfer（TryGetFilesAsync 扩展）、Clipboard 全面重写、Application 无 FrameworkInitializationCompleted/MainWindow 事件属性、FormattedText 六参构造、Dispatcher.InvokeAsync(Action) 返回 DispatcherOperation（Func<Task> 重载直接返回 Task）
- 分段构造参数版不存在：new LineSegment { Point = x, IsStroked = true }
