# PCL-CE → Avalonia/Linux 移植 · 交接文档（HANDOVER）

> 写于 2026-09-27 深夜，交接给下一位接手的 AI。
> 上一阶段成果：**编译 0 错误（C# 10564→0，XAML AVLN 792→0）+ Windows 运行时零异常**，程序可启动、主窗口稳定存活、本地化生效（zh-CN）。
> 最新 commit：`a07dfb8`（工作树干净）。
>
> 配套文档（都在本仓库 docs/ 下）：
> - `av12-notes.txt` — Avalonia 12 实测 API 更正（权威，与 A11 指南不同）
> - `PORTING-MAPPING.md` — 早期转换规则（**GBK 编码**，读取需 `Encoding.GetEncoding(936)`）
> - `PORTING-STATUS.md` — 历史进度快照（2026-08-31，部分内容已过时，以本文档为准）
> - `FEATURES.md` — 功能清单与 P1/P2/P3 优先级（待用户确认）

---

## 1. 项目定位与铁律（不可违反）

**目标**：把 PCL-CE（WPF Minecraft 启动器）**逐字（verbatim）移植**到 Linux 上的 Avalonia 12 / .NET 10。

**用户原话（必须时刻遵守）**：
> "然后记住目标是移植pcl ce的代码并不是重写，所以说你也要时时刻刻记着不要重写ui。要保留原有的功能逻辑和风格，这样子的话才是pcl ce启动器，而不是一个披着pcl名字重写的启动器。"

**铁律清单**：
1. **只做 API 名称/签名映射，绝不重新设计 UI**。布局/颜色/文案/交互必须与上游一致。
2. 所有非纯映射的改动**必须**带 `// [port] <原因>` 中文注释。
3. 编译结果**只信**完整构建：`dotnet build ... -v q -nologo --no-incremental`。增量构建和子代理自报的"0 错误"会**掩盖残留错误**（血泪教训）。
4. **禁止用 pwsh 批量读写 .cs 文件**（曾把 `public` 写成 `uublic` 导致语法错误）；C# 编辑一律用 read + edit 工具。
5. 频繁 commit；长文档/代码用批量 write，不要逐行 edit。
6. 所有给用户的问题**攒到最后批量问**，不要逐个打断。

---

## 2. 当前状态总览

| 项目 | 状态 |
|---|---|
| C# 编译错误 | ✅ 0（从 10564 清零） |
| XAML (AVLN) 编译错误 | ✅ 0（从 792 清零） |
| Windows 运行 | ✅ 主窗口稳定存活，`responding=True`，日志无任何 FTL/ERR/Exception |
| 本地化 | ✅ `当前 UI 语言: zh-CN` + `本地化 启动成功` |
| 生命周期 | ✅ 依赖检查→公告→更新检查→遥测→优雅关闭全走通（最新日志 573 行干净） |
| 透明窗口问题 | ⚠️ **未验证** — 用户曾在崩溃前看到透明窗口；崩溃已修复，但是否残留渲染问题需用户肉眼确认 |
| Linux 冒烟测试 | ❌ 未做（WSL headless → self-contained publish → 用户 VM） |
| 暂缓功能 | ❌ 用户指定的 8 项暂未移植（见 §8） |

**Git 提交历史（最新在前）**：
```
a07dfb8 runtime: localization working, zero runtime errors, app stays open
0c1027f runtime: fix animation per-frame errors (dynamic cannot resolve extension methods)
e84605b runtime: thread-safe resource lookup, localization resources packaged, brush cast mapping
2b72012 runtime bring-up: fix populate-time NRE and brush type mismatches
74b0d48 XAML compile wave complete: project builds with 0 errors
b01e0f4 XAML compile wave: exclude Markdown.axaml; move BlurBorder/MotdRenderer stubs to PCL.Core
102af8d MyListItem: ColumnGap via ColumnDefinitions[5]
e8cd191 MyMsgMarkdown: declare : Grid base for Avalonia name-field codegen
3c36db0 clean up temporary error-partition files
037c8e5 MyMsgMarkdown: restore axaml to codegen; degrade MarkdownViewer to SelectableTextBlock
```

---

## 3. 仓库结构与关键路径

```
D:\a\j\PCL.CE-Linux\          ← 本仓库（移植工作区）
├── src\
│   ├── Plain Craft Launcher 2\   ← 主工程（AssemblyName=PCL, RootNamespace=PCL）
│   ├── PCL.Core\                 ← 核心库（EnableDefaultItems=false, 手动 Include）
│   ├── PCL.Core.SourceGenerators\← 原样保留
│   ├── PCL.Neo.Core\             ← 旧 Neo 骨架，不在编译范围，待用户决定删除
│   └── PCL.Neo.UI\               ← 同上
├── docs\                         ← 本文档所在地
├── tools\                        ← 批量修复脚本（fix-axaml-bulk.ps1 等）
└── 上游基线：D:\a\j\PCL-CE（verbatim 副本，git 根提交 6a525ab）

构建输出：src\Plain Craft Launcher 2\bin\Debug\net10.0\PCL.exe (+ PCL.dll 5.0MB)
运行日志：同目录 PCL\Log\Launch-*.log（**GBK 编码**）
```

---

## 4. 构建与运行（权威命令）

```powershell
# 完整构建（唯一权威的错误检查方式）
dotnet build "D:\a\j\PCL.CE-Linux\src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" -v q -nologo --no-incremental

# 运行
& "D:\a\j\PCL.CE-Linux\src\Plain Craft Launcher 2\bin\Debug\net10.0\PCL.exe"

# 读运行日志（GBK；运行中要 FileShare.ReadWrite）
$p = "...\PCL\Log\Launch-最新.log"
$lines = [System.IO.File]::ReadAllLines($p, [System.Text.Encoding]::GetEncoding(936))
$lines | Select-String "FTL|未捕获|InvalidCast|RuntimeBinder|NullReference|FileNotFound|UriFormat|启动失败|ERR!"

# 检查进程是否存活/响应
Get-Process PCL | Select-Object Id, Responding, MainWindowTitle
```

**环境事实**：
- .NET 10 SDK 在 Windows 主机可用；Avalonia 12.0.4 已在 nuget 缓存。
- pwsh 工具按 GBK 读无 BOM 的 .ps1 → **脚本必须纯 ASCII 或带 BOM**。
- pwsh 的 `Get-Content` 显示中文为乱码（仅显示问题）；`[System.IO.File]` API 用进程 CWD 而非 PowerShell 位置 → **一律绝对路径**。
- 上游源文件编码混杂（GBK/UTF-8），改文件前先校验。

---

## 5. 关键文件与改动清单

### 5.1 工程文件

**`src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj`**：
- `AssemblyName=PCL`，`RootNamespace=PCL`，`StartupObject=PCL.Program`
- 包引用：Avalonia / Avalonia.Desktop / Avalonia.Themes.Fluent 12.0.4；SkiaSharp **3.119.4**（从 3.119.0 升，修 NU1605 降级冲突）
- `EnableDefaultItems=true`（**必须**：false 会让 Avalonia 命名字段生成器整体静默失效 → 数千"字段不存在"级联错误）；排除统一用 `<Compile Remove>` / `<AvaloniaXaml Remove>`
- `DeferredExcludes`（用户指定暂缓）：CrashAnalysis / ModMusic / ModVideoBack / ModLink / ModSkin / PageTools / ClipboardInterceptor / LazyLoadBehavior
- `<AvaloniaXaml Remove="Resources\Markdown.axaml" />`（Markdown.Avalonia 主题，Avalonia 12 无法解析其 StyleKey，792 个 AVLN2000 的来源）
- `<AvaloniaXaml Remove="Pages\PageTools\**" />`（.cs 已排除但 .axaml 仍会被 glob 收集报 Unable to find type）

**`src\PCL.Core\PCL.Core.csproj`**：
- `EnableDefaultItems=false` + 手动 `<Compile Include="**\*.cs" Exclude="...">`
- 语言文件三重打包（关键！见 §7 坑 6）：
  - `<AvaloniaXaml ... Exclude="...zh-CN.xaml 等 7 个">`（阻止 XamlIl 编译）
  - `<AvaloniaResource Include="App/Localization/Languages/**/*.xaml" />`
  - `<EmbeddedResource ... LogicalName="PCL.Core.App.Localization.Languages.%(Filename)%(Extension)" />`（回退用）

### 5.2 移植支撑文件（新增）

| 文件 | 内容 |
|---|---|
| `src\PCL.Core\UI\Controls\Stubs.cs` | `BlurBorder : Border` + `MotdRenderer : Control`（namespace PCL.Core.UI.Controls，放这里 XAML 的 clr-namespace 才能解析 assembly=PCL.Core） |
| `src\Plain Craft Launcher 2\Stubs.cs` | namespace PCL：ModVideoBack/ModMusic/ModSkin 桩 + `MyColorExtensions.ToBrush()/ToColor()` + `ControlTemplateExtensions.FindName()` |
| `src\Plain Craft Launcher 2\ControlResourceHelper.cs` | `SetResourceReference(this AvaloniaObject, AvaloniaProperty, string)` 扩展 — 用 `target.Bind(property, ctrl.GetResourceObservable(key))` 实现 WPF 动态资源语义（主题切换感知）。**调用点必须写 `this.` 前缀**（扩展方法需要接收者） |

### 5.3 运行时核心修复（按文件）

| 文件 | 修复 |
|---|---|
| `Modules\Base\ModBase.cs` | `MyColor(object)` 增加 ISolidColorBrush 分支（原只认 SolidColorBrush，dynamic `.A` 在 ImmutableSolidColorBrush 上炸）；新增 ctor `MyColor(double newA, IBrush brush)` |
| `Modules\Base\ModAnimation.cs` ~L149 | dynamic 调 SetResourceReference（扩展方法 dynamic 绑不了）→ 强类型：`var colorTargets=(object[])anim.obj; ... ((AvaloniaObject)colorTargets[0]).SetResourceReference((AvaloniaProperty)colorTargets[1], resourceKey)` |
| `Controls\AnimatedBackgroundGrid.cs` | BackgroundBrushProperty/AnimatableBrush/BackgroundBrush：SolidColorBrush→IBrush |
| `Controls\MyCard.cs` | `override IBrush AnimatableBrush` |
| `Controls\MyIconButton/MyLoading/MyListItem` | `Foreground` 类型改 IBrush（StyledProperty + Register 泛型） |
| 8 个控件（MyButton/MyCheckBox/MyExtraTextButton/MyHint/MyIconTextButton/MyListItem/MyRadioBox/MyRadioButton） | 移除 `[Content] public InlineCollection Inlines => LabXxx.Inlines;` 上的 [Content]（见 §7 坑 3） |
| `Controls\MyExtraButton.xaml.cs` | `RectProgress = PanProgress.Clip as RectangleGeometry;`（几何对象不在 NameScope，FindNameScope().Find() 拿 null）+ setter 空保护 |
| `PCL.Core\App\ResourceExtensions.cs` | 重写：ConcurrentDictionary 缓存 + 非 UI 线程返回 null（不 marshal）+ ActualThemeVariant 变化清缓存 |
| `PCL.Core\App\Localization\LocalizationService.cs` | 新增 `_OpenLanguageStream(code)`：先试 `AssetLoader.Open(avares://...)`，失败回退 `assembly.GetManifestResourceStream("PCL.Core.App.Localization.Languages.{code}.xaml")` |
| `PCL.Core\App\Localization\LocalizationFontService.cs` | `_ApplicationPackUri`：`pack://application:,,,/` → `avares://PCL/`（WPF pack URI 在 Avalonia 抛 UriFormatException "Invalid port specified"） |

### 5.4 Application.axaml（作为全部页面翻译范本）

- 4 个带 x:Key 的 `<Style>` 从 `Application.Styles` 移除（Avalonia：Styles 只放选择器样式）；`ComboBoxItemTemplateWithDelete` DataTemplate 移入 `Application.Resources`
- `Easing="CircleEaseOut"` → `"CircularEaseOut"`（WPF 缓动名无效）
- ScrollBar RepeatButtons → `Name="PART_PageUpButton"/"PART_PageDownButton"`（模板部件名，无 RoutedCommands）
- MyScrollViewer 滚动条绑定 → `ScrollBarMaximum.Y` / `Offset.Y (TwoWay)` / `Viewport.Height`，`RelativeSource TemplatedParent`
- MyTextBox 模板补 `<TextPresenter x:Name="PART_TextPresenter" Text="{TemplateBinding Text, Mode=TwoWay}" PasswordChar RevealPassword SelectionStart SelectionEnd CaretIndex TextAlignment TextWrapping />`（TextBox 必需部件）
- 根 `<Grid ToolTip.Tip="{TemplateBinding ToolTip}">` → `<Grid>`（模板根不能这样绑定 ToolTip）

### 5.5 批量修复脚本（tools/）

`fix-axaml-bulk.ps1`（`ToolTip="`→`ToolTip.Tip="`；删 `RenderOptions.BitmapScalingMode`/`ShadowDepth`/`TextFormattingMode`；`Visibility`→`IsVisible`；transform/effect 标签上的 `x:Name` 剥离；`AllowDrop=`→`DragDrop.AllowDrop=`）、`fix-empty-assembly.ps1`（空 `assembly=` → `assembly=PCL`）。

---

## 6. Avalonia 12 差异速查（实测权威版）

> 更全的见 `docs/av12-notes.txt`。此处为最高频条目：

| WPF | Avalonia 12 |
|---|---|
| `PasswordBox` | `TextBox` + `PasswordChar` |
| `Visibility.Collapsed` | `IsVisible="False"` |
| `Application.Current.Clipboard` | `TopLevel.GetTopLevel(c)?.Clipboard` |
| `DragEventArgs.Data` / `e.Operations` | `e.DataTransfer`（同步 IDataTransfer）+ `e.DragEffects`；**无** `e.Data`/`e.Operations`/`DragDropOperations` |
| 选择器 `local:Type` | `local\|Type`（竖线） |
| keyed Style 放 Styles | keyed 项进 `Resources`；选择器样式进 `Styles`（如 `<Grid.Styles>`） |
| `CircleEaseOut` 等缓动 | `CircularEaseIn/Out/InOut` |
| `ScrollViewer.VerticalScrollBarMaximum/Value/ViewportSize` | `Offset`(Vector) / `Extent`(Size) / `Viewport`(Size) / `ScrollBarMaximum`(Size) |
| TextBox 模板 | 必须含 `PART_TextPresenter`（TextPresenter） |
| ScrollBar 模板 | `PART_LineUpButton/PART_LineDownButton/PART_PageUpButton/PART_PageDownButton`（无 RoutedCommands） |
| 模板里绑定 Foreground/FontSize/FontFamily | 继承属性，无需绑定 |
| 资源画刷强转 `SolidColorBrush`/`Brush` | 资源是 `ImmutableSolidColorBrush` → 用 `ISolidColorBrush`/`IBrush` |
| `SetResourceReference` | 无内置 → ControlResourceHelper 扩展（见 §5.2） |
| `ColumnDefinition`/`RowDefinition`/Transform/Effect/`RectangleGeometry` 的 `x:Name` | **不生成字段** → 给所属 Grid/元素命名，按 `ColumnDefinitions[index]` / `RenderTransform[0]` 取 |
| code-behind 命名字段生成 | 要求 partial 类声明 Avalonia 基类（如 `: Grid`） |
| `Mouse.LeftButton` 等静态查询 | 无 → PointerPressed/Released 事件跟踪私有 bool |
| `pack://application:,,,/` | `avares://PCL/` |

**跨线程**：UI 对象（含 `Application.Resources`/`ActualThemeVariant`）只能 UI 线程碰 → ResourceExtensions 已做线程安全。

**C# 语言规则**：`dynamic` **不能绑定扩展方法**（ModAnimation 每帧崩溃的根因）。

---

## 7. 踩坑记录（血泪史，新代理必读）

1. **增量构建/子代理构建掩盖错误** — 只有主会话的 `--no-incremental` 完整构建可信。子代理说"0 错误"≠0 错误。
2. **编码损坏事故** — pwsh 全文件重写把 `public` 写成 `uublic`（p→u），产生 CS1519/CS1002。修复：`git checkout --` 两个文件。此后 C# 编辑只用 read+edit 工具。已验证无 `uublic|urivate` 残留。
3. **`[Content] Inlines` populate 期 NRE** — Avalonia 按**实例类型**路由 XAML 根子元素（WPF 按**根标签类型**），`LabText` 在 `!XamlIlPopulate` 阶段还是 null → 8 个控件的 `[Content] public InlineCollection Inlines => LabXxx.Inlines;` 移除 [Content]。
4. **ImmutableSolidColorBrush** — 资源字典里的画刷是 immutable，`dynamic .A` 抛 RuntimeBinderException（ThemeManager 静态 ctor 崩）、强转 Brush 抛 InvalidCastException → 全链路改 ISolidColorBrush/IBrush + MyColor 构造分支。
5. **动画每帧 RuntimeBinderException** — 日志曾 8135 行/次 → dynamic 调扩展方法（§6）→ 强类型后降到 377 → 最终 304~573 行干净。
6. **本地化三层坑** — ① 语言 xaml 没打包 → AvaloniaResource；② Avalonia targets 仍把它们收集进 XamlIl 编译（URI 带 Build:/Populate: 前缀，AssetLoader.Open 找不到）→ Exclude + EmbeddedResource（LogicalName 稳定）回退；③ 字体服务用 WPF pack URI → UriFormatException → avares://PCL/。
7. **MSBuild `**` 在 Windows 展开为反斜杠路径** — `Exclude` 写正斜杠会静默不匹配；裸 `<Item Exclude="..."/>` 元素非法（MSB4232，需 Include/Update/Remove）。
8. **Avalonia 命名字段生成器** — ① 依赖 EnableDefaultItems=true；② 要求 code-behind 声明 Avalonia 基类；③ 不为 ColumnDefinition/RowDefinition/Transform/Effect/几何对象生成字段。csc 报"字段不存在"= 对应 axaml 未完成 Avalonia 化。
9. **跨线程 VerifyAccess** — Lang.Text 在加载器线程调 TryFindResource → `Application.get_ActualThemeVariant` 抛 InvalidOperationException → ResourceExtensions 非	UI 线程直接返回 null + 缓存。
10. **Markdown.axaml 792 错** — Markdown.Avalonia 主题样式，必须同时从 AvaloniaResource **和** AvaloniaXaml 排除。
11. **`Unable to resolve type ... assembly=PCL`** — 空的 `xmlns:local="clr-namespace:PCL;assembly="`（4 个文件）；桩类要放到被引用的程序集（BlurBorder/MotdRenderer → PCL.Core）。

---

## 8. 暂缓功能清单（用户指定，文件保留在仓库）

`DeferredExcludes`（见 §5.1）：崩溃分析(CrashAnalysis)、音乐(ModMusic)、视频背景(ModVideoBack)、联机(ModLink)、换皮肤(ModSkin)、工具页(PageTools)、剪贴板行为(ClipboardInterceptor)、LazyLoadBehavior。
另：Markdown 公告（Markdown.axaml/MarkdownViewer 已降级为 SelectableTextBlock，MyMsgMarkdown 已恢复编译）。
MySkin 和 MyMsgMarkdown **已重新启用**（不在排除清单里）。

---

## 9. 下一步任务（优先级排序）

1. **验证透明窗口问题**（⚠️ 最紧）— 用户曾在启动 ~1s 后看到透明窗口然后崩溃；崩溃链已全部修复，最新日志干净。需要：
   - 用户肉眼确认现在的窗口是否正常渲染内容；
   - 若仍透明：查 `FormMain.axaml` 的背景画刷 / `ExtendClientArea*` / `TransparencyLevelHint` / 渲染后端（WinVM/Skia）、页面内容是否实例化（日志已有"实例化：清空主页 UI"、"启动按钮：Minecraft 实例"字样说明有内容）。
2. **WSL headless 冒烟** — `dotnet publish -r linux-x64 --self-contained`，在 WSL 里无头跑，确认 Linux 上能起来（X11/Wayland 或 headless 检查日志）。
3. **Linux 真机测试** — self-contained publish 交给用户 VM 验证。
4. **暂缓功能移植**（用户确认优先级后）— PageTools（工具页）大概率是下一个大块。
5. **清理** — `src\PCL.Neo.Core` / `src\PCL.Neo.UI` 不在编译范围，待用户决定是否删除；`docs/current-errors.txt`、`g4-*.txt`、`g5-*.txt` 是历史调试产物可清理。
6. **更新 PORTING-STATUS.md** 或以本文档为准将其归档。

## 10. 待用户确认的问题（攒批问）

1. B 类偏差（有 [port] 注释的语义近似）是否接受？
2. C 类 Linux 降级（如 HLSL 着色器模糊跳过、SystemDialogs 异步化）清单是否接受？
3. FEATURES.md 的 P1/P2/P3 优先级是否认可？
4. `PCL.Neo.Core`/`PCL.Neo.UI` 两个废弃骨架是否删除？
5. 透明窗口现状（需用户跑一眼 `PCL.exe` 看画面）。

## 11. 工作方法约定（延续）

- 逻辑零改动；事件签名与 XAML 对齐（先改 XAML 事件名，再改 C# 签名）。
- 共享文件（`Stubs.cs`、`Application.axaml`、`ControlResourceHelper.cs`）多代理都改 → 编辑前先 read 检查现状。
- 子代理并行分片时：每个只改自己负责的文件，构建由主会话统一跑；子代理提示里必须写明铁律。
- 新增桩成员先在 Stubs.cs 里 grep 确认不重复。
- commit 信息用英文短句，一次修复一个 commit。

---

*本文档由上一任代理在交接时生成。若与 git 历史冲突，以 git 为准。*
