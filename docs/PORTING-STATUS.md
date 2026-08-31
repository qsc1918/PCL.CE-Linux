# 移植进度（PORTING-STATUS）— 夜班结束快照（约 01:20，电脑自动关机前）

> 上游基线：D:\a\j\PCL-CE（verbatim 副本已提交 git 根提交 6a525ab）。
> **明日恢复点：见文末"当前状态/明日恢复点"。**

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

### 当前波次：主工程 C# 546 错误（首轮 build 已取样）
高频模式（按计数）：
1. MouseButtonEventArgs(332)/MouseEventArgs(28)：XAML 事件处理器签名 → 与 XAML 移植联动改 PointerPressed/Moved/Released/WheelChanged
2. `using Avalonia.Shapes;`(24) → `Avalonia.Controls.Shapes;`（可脚本化）
3. ContentProperty(20)：My* 控件类级 [ContentProperty("X")] → 属性级 [Content]
4. AttachedPropertyBrowsableForType(12)：WPF 特性 → 删除
5. System.Drawing.Bitmap 歧义(8)：MyBitmap.cs 手工 SkiaSharp 化（B5）
6. ImageSource(6) → IImage；FlowDocument(2)（Markdown 残留）；PageHomepageNewsView 缺失(2) → 把 Pages\PageHomepage 从排除清单拿回来（启动页新闻轮播，P0 关联）
7. OnApplyTemplate/MeasureOverride/OnRender/OnActivated/OnSourceInitialized 覆写不匹配 → 逐文件（Avalonia: OnApplyTemplate 存在；Window 无 OnActivated/OnSourceInitialized → 用 Opened/PlatformSettings 事件）
8. MyVirtualizingElement<T> 约束（基类未继承 Control）

### 执行策略（下一轮起）
- 先把 Pages\PageHomepage 移回编译范围；脚本化修 using Avalonia.Shapes
- **子代理并行分片**（每个只改自己负责的文件，构建由主会话统一跑）：
  A. Controls\My* 全套（~20 文件）
  B. FormMain.xaml(.cs) + Pages\PageLaunch\PageLog\PageSelect
  C. Pages\PageDownload 全部 + Controls\MyMsg
  D. Pages\PageInstance\PageSetup + Modules\Event + Modules\Base 剩余
- 主会话职责：统一构建、分发错误、XAML 编译波（C# 全绿后出现，Avalonia XamlIl 错误一次一个文件）、最终集成
- 约束写入子代理提示：逻辑零改动、[port] 注释标注偏差、禁止重新设计、事件签名与 XAML 对齐（先改 XAML 事件名，再改 C# 签名）

### Application.xaml 已完成（作为全部页面翻译范本）
- 结构：资源在 Application.Resources，样式在 Application.Styles（Avalonia 要求）
- 选择器化：TextBlock/Label/PasswordBox 触发 → :pointerover/:focus/:disabled
- ToggleButton 箭头 Storyboard → TransformOperationsTransition + :checked
- ScrollBar/ScrollViewer 模板 → Avalonia Track/PART 结构（保留 8px 细滚动条视觉）
- 待验证：BooleanConverters 全名、Track 附加属性、PageComp :is 选择器

## 关键环境事实
- pwsh 工具实际按 GBK 读 .ps1 文件 → 脚本必须纯 ASCII 或带 BOM
- 上游源文件编码混杂（GBK/UTF-8）：改文件前先严格 UTF-8 校验，GBK 用 Encoding.GetEncoding(936) 读（tools/repair-gbk.ps1 有完整实现）
- dotnet 10 SDK 在 Windows 主机可用；Avalonia 12.0.4 已在 nuget 缓存
- **【重大发现 2026-08-31】`EnableDefaultItems=false` 会让 Avalonia 内置命名字段生成器（Avalonia.Generators，包 analyzers 目录）整体静默失效——所有 x:Name 字段/InitializeComponent 不生成，csc 报数千"名称不存在"级联错误。必须 EnableDefaultItems=true，排除用 `<Compile Remove>/<AvaloniaXaml Remove>` 实现。生成器对 axaml 文件逐个解析，无法解析（WPF 残留）的文件被静默跳过——csc 的"字段不存在"错误即该文件的 axaml 未完成 Avalonia 化的信号。**
- .xaml 必须改名为 .axaml（生成器只认 axaml）；code-behind 保持 .xaml.cs
- Avalonia 12 关键差异（实测）：无 Visibility 枚举（用 IsVisible bool）、无 ContentPropertyAttribute（→ [Content] 标注在属性上）、无 RichTextBlock（用 SelectableTextBlock + Inlines + LineBreak）、ReadLocalValue→已移除（用 IsSet）、Matrix 三行制 M31/M32、BezierSegment 等只有无参 ctor、IAsyncDataTransfer（TryGetFilesAsync 扩展）、IDataObject 移除、Clipboard 全面重写、Application 无 FrameworkInitializationCompleted 事件（用桌面生命周期 Startup）/无 MainWindow 属性（经 IClassicDesktopStyleApplicationLifetime）、FormattedText 六参构造、Dispatcher.InvokeAsync(Action) 返回 DispatcherOperation（Func<Task> 重载直接返回 Task）、ItemsControl 容器方法改名（CreateContainerForItemOverride(item,index,recycleKey) / NeedsContainerOverride(item,index,out recycleKey)）、OnApplyTemplate(TemplateAppliedEventArgs)、OnRender→Render、Control 无 SetResourceReference（用 GetResourceObservable + Bind）、Control 无 .ToolTip 实例属性（用 ToolTip.SetTip/GetTip）、ToolTipService 为内部 API（SetPlacement/SetVerticalOffset/SetHorizontalOffset → ToolTip.SetPlacement 等；InitialShowDelay/BetweenShowDelay 无对应删除）、SystemDialogs 已异步化（SelectSaveFileAsync 等）

## 当前状态（2026-08-31 上午，commit bc2247b）

### 昨夜+今晨大事记
1. PCL.Core 编译全绿（cbd9538）。
2. 主工程 csc 主阶段已通过到较深层，剩余 ~10.5k 错误，**全部是"axaml 未 Avalonia 化 → 命名字段未生成"的连锁**（约 60 个 axaml 待转换）+ 少量真实 C# 残留（ModSetup 对 FormMain.ImgBack/PanMainRight 等、ModDownloadLib 的 ToolTipService、PageSelectLeft/Right 的 SystemDialogs 异步化与 Keyboard/DragDrop、PageSetupUI 的 FindName/ModMusic/ModVideoBack 引用、Program.cs 的 UsePlatformDetect using/MainWindowService.Loading 返回类型、PageLogLeft 的 Blocks→Inlines、MySkin/MyMsgLogin 等）。
3. 昨夜 GBK 损坏事故已全部修复（26 文件，tools/repair-gbk.ps1）；今晨去重脚本误删 using( 语句的事故已修复（ModBase/ModLocalComp/ModComp/ModLaunch/ModDownloadLib，tools/repair-using*.ps1 + 手工）。

### 今日恢复点（按序）
1. **核心任务 = 把剩余 ~60 个 axaml 逐个转成合法 Avalonia XAML**（tools/convert-axaml.ps1 已跑过安全子集；剩余 WPF 残留见 tools/fix-tooltip2.ps1 后状态）。判据：csc 错误按文件分组，每组对应一个 axaml。转换要点照 Application.axaml 范本 + docs/PORTING-MAPPING.md。每转一个，csc 该文件错误消失且 Generated 下出现对应 .g.cs（EmitCompilerGeneratedFiles 已开）。
2. 注意 PageSetupUI/PageLaunchLeft 等引用 FormMain 的字段（ImgBack/PanTitleSelect/BtnTitleSelect*）——FormMain.axaml 转换成功后自动解决。
3. PageSetupUI 引用 ModMusic/ModVideoBack（已排除类型）→ 相应代码段注释 + [port]。
4. PageSelectLeft/Right：SystemDialogs.SelectFolder→SelectFolderAsync、Keyboard→KeyEventArgs 处理、DragEventArgs.Data→新 API、MenuBase.IsOpen setter 不可访问（用 Open/Close 方法或 ContextMenu.IsOpen=）。
5. Program.cs：AppBuilder.UsePlatformDetect 缺 using Avalonia（已加？验证）；MainWindowService.Loading 返回 FormMain→需 Window 类型匹配（FormMain 必须已继承 Window——由 FormMain.axaml 生成基类，先修 FormMain.axaml）。
6. PageLogLeft/Right：SelectableTextBlock 无 Blocks → Inlines 改写（已开始，继续完成 maxLog 修剪逻辑）。
7. MySkin/MyMsgLogin 等已排除或待定文件的引用清理。
8. csc 全绿后 → XAML 编译波 → WSL 冒烟 → 发布 → VM 实测 → 向用户集中提问。

### git 提交链（最新在前）
bc2247b(EnableDefaultItems 修复) → 96227df → f6eaa1e → 3d08af5 → 0f0a3b8 → 7052b07 → cbd9538(Core 绿) → 6a525ab(基线)

## 当前状态（夜班结束，2026-08-31 约 01:20，commit a5b5a12）

### 大事记
1. **PCL.Core 编译全绿**（commit cbd9538）。
2. 主工程 C#：868 → 2 → **csc 主阶段通过**，构建推进到 XAML 编译波，暴露约 11280 个错误（含大量级联）。
3. **重大事故与修复**：上游源文件编码混杂，早期脚本按 UTF-8 读写损坏 26 个 PageInstance/PageSetup 文件（中文乱码+引号破坏）。tools/repair-gbk.ps1（严格 UTF-8 校验自动判码 + git 基线恢复 + 重套规则）已全部修复。
4. 子代理通道夜间瘫痪（连续秒挂），改单人小批量推进 + 频繁提交。FormMain/下载页分片中断前已交付大部分成果。
5. FEATURES.md 功能盘点完成（57KB，164 条，P0 十章 108 条细分）。

### 明日恢复点（按序）
1. 首轮构建看错误面。预计大头：`Path` 二义性（转换脚本给含 `using System.Windows.Controls;` 的文件注入了 `using Avalonia.Controls.Shapes;`，与 `using System.IO;` 冲突）——脚本化：对相关文件加 `using Path = Avalonia.Controls.Shapes.Path;` 别名并去重 using；Controls\AnimatedBackgroundGrid.cs 等夜班小代理半成品（PropertyMetadata、Dispatcher.BeginInvoke 残留）；Application.xaml.cs（ToolTipService 不可访问、Tooltip.Enable 缺失——PCL.Core Tooltip.cs 被排除、Path 歧义）；零散 SetResourceReference（Avalonia 无此 API）。
2. C# 全绿后进入 **XAML 编译波**：91 个 XAML 中约 60 个只做过机械转换，需按 Application.xaml 范本人工选择器化。注意 PageLogRight.xaml 的 RichTextBox 已换 SelectableTextBlock。
3. 全绿后：WSL 无头冒烟（PCL_SMOKE_LAUNCH=1）→ 自包含发布 → 用户 VM 实测。
4. 向用户集中提问：B 类偏差确认 + C 类降级确认 + FEATURES.md P1/P2/P3 优先级确认 + PCL.Neo.Core/PCL.Neo.UI 是否删除。

### 已知未移植/降级清单（代码中 [port] 标注）
- 联机、音乐、视频背景、换皮肤、崩溃分析、Markdown 公告、工具页（用户指定暂缓）
- HLSL 模糊特效族（AdaptiveBlur/BlurBorder→Border/Tooltip）——Avalonia 无自定义像素着色器
- TaskbarItemInfo 已删除；SplashScreen → 空实现 shim（src\Plain Craft Launcher 2\SplashScreen.cs）
- 工具页字段 dynamic? 擦除（ModMain.frmToolsLeft 等，运行期恒 null）；PageToolsTest 的 {hint}/{cave} 占位替换为空文本
- ModBase.GetObjectFromXML 的 XAML 安全检查：上游用 System.Xaml XamlXmlReader，转换后走 Avalonia.Markup.XamlReader.Load——需运行期验证

### git 提交链
6a525ab(基线) → cbd9538(Core 绿) → 7052b07 → d0ee128 → 3ee37e1 → f894814 → 0f0a3b8 → 3d08af5 → a5b5a12(最新)
- 分段构造参数版不存在：new LineSegment { Point = x, IsStroked = true }
