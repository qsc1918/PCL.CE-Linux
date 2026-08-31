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

## 当前状态（2026-08-31 上午二期，commit cba7935）

### 重大突破
**基类注入激活生成器**：Avalonia 命名字段生成器要求 code-behind partial 类显式声明 Avalonia 基类（WPF 把基类放生成代码里，Avalonia 生成器不加）。tools/add-bases.ps1 按 axaml 根元素为 63 个文件插入基类 + 12 个接口-only 声明手工补根类型，生成文件 9 → **84**，错误 10564 → **2704**。

### 剩余 2704 错误的分类与修法（按计数排序，均可机械处理）
| 计数 | 错误 | 修法 |
|---|---|---|
| 126+56 | MyColor→IBrush 隐式转换失败 / MyColor-IBrush 运算符 | C# 不允许到接口的隐式转换：AnimatableBrush/BackgroundBrush 属性类型改 SolidColorBrush 或调用点显式 `(SolidColorBrush)(Brush)myColor`；逐点看 |
| 72 | PropertyMetadata 未找到 | 残留的 WPF 式 DP 注册（MyCard/MySlider/MyToast 等）→ StyledProperty.Register<TOwner,T> + Changed.AddClassHandler |
| 70 | MyIconButton.ToolTip | `.ToolTip = x` → `ToolTip.SetTip(x所在对象, x)`；getter → GetTip；再次跑 tools/fix-tooltip.ps1 式扫描（这次是 MyIconButton 类型的接收者） |
| 62 | IsMouseOver | `.IsMouseOver` → `.IsPointerOver`（全库替换） |
| 52 | Point→RelativePoint | `RenderTransformOrigin = new Point(...)` → `new RelativePoint(new Point(...), RelativeUnit.Relative)`（usings: Avalonia） |
| 52 | ModVideoBack 未定义 | PageSetupUI 引用已排除类型 → 注释 + [port] |
| 48 | FormMain.PanMsgBackground | FormMain.axaml 里 x:Name 是否存在？Msg 背景面板可能被 agent B 删了 → 恢复一个 Grid 或改用 PanForm |
| 44+44 | TransformPos/TransformRotate | FormMain.axaml 中 TransformGroup 子变换的 x:Name——确认 axaml 有这些名字；若在 RenderTransform 里正常应生成 |
| 44 | Path.SetBinding | var 命名冲突或 Shapes.Path 用了 Avalonia 无的 SetBinding 签名（binding 参数顺序不同）→ 逐点看 |
| 40 | 方法组→RoutedEventHandler | XAML 事件处理器签名不匹配（Click 处理器用了 PointerPressedEventArgs 等）→ 签名改回 RoutedEventHandler(object, RoutedEventArgs) |
| 38 | SystemDialogs.SelectSaveFile | 同步→异步调用点迁移（SelectSaveFileAsync），还有 ~38 处 |
| 36 | DragEventArgs.Data | Avalonia 12: e.Data → e.DataTransfer（IAsyncDataTransfer）；TryGetFilesAsync 等 |

### 今日恢复点（按序）
1. 上表逐类清剿（先全局替换类：IsMouseOver→IsPointerOver；再 PropertyMetadata 注册；再 ToolTip 残余；再逐文件特殊点）
2. csc 全绿后 XAML 编译波（AVLN 错误将真实显示，逐文件修）
3. WSL 冒烟 → 发布 → VM 实测 → 集中提问

### 关键环境事实（新增）
- **Avalonia 命名字段生成器要求 code-behind 显式声明 Avalonia 基类**（tools/add-bases.ps1 已为 75 个文件注入；新 axaml 记得同步）
- EnableDefaultItems 必须 true（false 使生成器整体失效）；排除用 Compile/AvaloniaXaml Remove
- 生成器对无法解析的 axaml 静默跳过——csc 报某文件字段缺失 = 该 axaml 未转换完
- .xaml 必须改名 .axaml；code-behind 保持 .xaml.cs

### git 提交链（最新在前）
cba7935(基类注入 84 文件) → bc2247b → 96227df → f6eaa1e → 3d08af5 → 0f0a3b8 → 7052b07 → cbd9538(Core 绿) → 6a525ab(基线)

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
6a525ab(基线) → cbd9538(Core 绿) → 7052b07 → d0ee128 → 3ee37e1 → f894814 → 0f0a3b8 → 3d08af5 → a5b5a12 → 2f4610f → 5f367cc → de89911 → 70d4069(最新)

## 当前状态（2026-08-31 下午，commit 70d4069）

### 重大进展
**C# 错误从 10564 → 1074**（降低 90%）。

### 批量修复完成清单
| 修复 | 错误削减 | 方法 |
|---|---|---|
| EnableDefaultItems=true + Remove 排除 | 10564→2702 | csproj 修改 |
| 基类注入 (add-bases.ps1) | 2702→2704(等效) | 自动脚本 |
| IsMouseOver→IsPointerOver | -62 | 全库替换 |
| Visibility→IsVisible | -~30 | 全库替换 |
| FontWeights→FontWeight | -14 | 全库替换 |
| Cursors→Cursor.Parse | -少量 | 全库替换 |
| Dispatcher.BeginInvoke→InvokeAsync | -30 | 全库替换 |
| LineSegment 参数化→属性初始化 | -28 | 全库替换 |
| RenderTransformOrigin Point→RelativePoint | -~20 | 全库替换 |
| SystemDialogs 同步→异步+await+async | -~200 | 全库+逐文件 |
| DragEventArgs.Data→DataTransfer | -36 | 全库替换 |
| MenuBase.IsOpen .set→.Close()/.Open() | -20 | 全库替换 |
| DataFormats→DataFormat | -24 | 全库替换 |
| MyIconButton.ToolTip 属性 | -70 | 新增属性 |
| SetResourceReference 扩展方法 | -68 | 新增扩展 (但级联错误仍计34) |
| ControlResourceHelper (PCL 命名空间) | 同上 | 直接设值 |
| ModVideoBack/BlurBorder/MotdRenderer 桩 | -8 | Stubs.cs |
| Keyboard.FocusedElement→null 桩 | -少量 | 全库替换 |
| ColumnPaddingRight→ColumnGap | -30 | 全库替换 |

### 剩余 1074 错误的分类与修法
| 计数 | 错误 | 修法 |
|---|---|---|
| 126+56 | MyColor→IBrush 隐式转换/运算符 | 属性类型改 SolidColorBrush 或调用点显式转换 |
| 52 | PropertyMetadata 未找到（回调变体） | 需逐文件改为 AvaloniaProperty.Register + Changed.AddClassHandler |
| 48 | FormMain.PanMsgBackground 未定义 | FormMain.axaml 里 PanMsgBackground 的 BlurBorder→Border |
| 44+44 | TransformPos/TransformRotate 未定义 | FormMain.axaml RenderTransform 子元素 x:Name |
| 44 | Path.SetBinding | Avalonia Path 没有 SetBinding → 改用 Bind() 或 BindingOperations |
| 40 | 方法组→RoutedEventHandler | XAML 事件处理器签名不匹配 |
| 38 | PageLaunchLeft.AprilPosTrans 未定义 | AXAML 缺少 x:Name |
| 34 | ActualWidth | Avalonia Control 有 Bounds.Width 替代 |
| 34 | SetResourceReference (级联) | 修 PropertyMetadata 后自动消失 |
| 30 | ColumnGap | AXAML 缺少 ColumnDefinition x:Name |
| 28 | ImageBrush.ImageSource | 改为 .Source |
| 28 | DragEventArgs.Effects | Avalonia 无 Effects 属性 |
| 28 | IsEnabledChanged 事件 | 无直接等价，用 IsEnabled 属性变化监听 |
| 26+20+16 | ModMusic/ModSkin/Skin | 排除类型引用 |
| 22 | ComponentDispatcher | Avalonia 无此类 |
| 20 | GeometryConverter | 需实现或跳过 |
| 18+18 | Mouse/MouseButtonState | Avalonia 用 PointerEventArgs |
| 16 | ContentPresenter | 需检查 AXAML 生成 |
| 16 | RelativeScalar | double→RelativeScalar 显式转换 |
| 16 | PreviewPointerPressed/Released | Avalonia 用 PointerPressed/Released |
| 14 | Clipboard | Avalonia 12 Clipboard 重写 |
| 14 | Visibility (残留) | 部分文件遗漏 |
| 12 | MotdRenderer (已桩但可能缺字段) | Stubs.cs 扩展 |
| 12 | ms | 可能是变量名冲突 |
| 12 | ImgFore/ColumnRamEmpty/ColumnRamUsed/ColumnRamGame | AXAML x:Name 缺失 |

### 恢复点（按优先级）
1. **PropertyMetadata 回调变体**（52→级联消除 ~100）：逐文件改 AvaloniaProperty.Register + Changed.AddClassHandler
2. **MyColor→IBrush**（182）：MyColor 类型改为 SolidColorBrush 或运算符重载
3. **AXAML x:Name 缺失**（TransformPos/TransformRotate/PanMsgBackground/ColumnGap 等 ~200）：修复 AXAML 中的类型引用/名称
4. **RoutedEventHandler 签名**（40）：XAML 事件处理器签名对齐
5. **Path.SetBinding**（44）：改用 Bind() 扩展方法
