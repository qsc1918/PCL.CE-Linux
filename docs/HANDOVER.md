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
| 透明窗口问题 | ✅ **已确认正常** — 窗口渲染正常（用户截图 + 离屏渲染均正常），原"透明"是崩溃前的瞬时状态 |
| 页面实例化 NRE | ✅ **已修（2026-09-28）** — 根元素 `x:Name`（坑 12）、集合属性 `x:Array`（坑 13）、`[Content]` 路由（坑 14） |
| 实例选择/设置崩溃 | ✅ **已修（2026-09-28）** — `AvaloniaProperty` 字段可见性（坑 15）+ 坑 13；`--pagetest` 4 项真实导航全通过 |
| 本地化 `!key!` | ✅ **已修（2026-09-28）** — 非 UI 线程取不到资源（坑 16） |
| 配色整体偏暗 | ✅ **已修（2026-09-28）** — scRGB/sRGB 混淆（坑 17）；离屏渲染采样与上游窗口一致 |
| 内嵌图片加载 | ✅ **已修（2026-09-28）** — avares 大小写 + AssetLoader 取流（坑 18） |
| 已知未修 | ⚠️ `设置→更新`页（上游缺 `Tahoe.png`，见坑 18）；`联机`页（暂缓功能） |
| Linux 发布产物 | ✅ **已产出并静态校验**（commit 32985f3）— `dist/linux-x64/PCL`（ELF x86-64 单文件 120MB）+ `dist/linux-x64-flat/`（非单文件 125MB）；WSL 被安全策略拉黑，真机实测待用户 VM |
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
- **本机 WSL 被安全策略拉黑**（`wsl.exe` 在 Program Blacklist）→ §9.2 的 WSL headless 冒烟**无法在本机执行**，只能用静态校验 + 用户 VM 实测替代（见 §9.2）。
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
12. **根元素 `x:Name` 与 WPF 语义不同（2026-09-28 修复，崩溃级）** — WPF 中 `<Grid x:Name="PanBack">`（MyListItem 等控件根元素）生成的字段**绑定到控件自身**（`this.PanBack = target;`，target 即 PCL.MyListItem），故 `PanBack.Children` == `this.Children`。
    Avalonia 命名字段生成器**另生成一个 `Grid PanBack` 字段**，并在 `!XamlIlPopulate` 阶段（Populate 之前、名字作用域尚未建立）执行 `PanBack = __thisNameScope__?.Find<Grid>("PanBack")` → 该值**恒为 null**。
    当子元素的属性 setter 在 Populate 期间就访问它（如 `MyListItem.Buttons` 里 `PanBack.Children.Add(...)`），必抛 NullReferenceException。
    **实测崩溃点**：`PageInstanceLeft.axaml` 实例化（`FormMain.PageChange` ← `PageLaunchLeft.BtnMore_Click`）→ 点"实例设置"即崩、程序退出。
    **修法（已用）**：从 axaml 根元素**删掉 `x:Name`**，在 code-behind 加属性 `private MyListItem PanBack => this;`（还原上游语义）；同时 axaml 内凡 `ElementName=PanBack` 的绑定改为 `RelativeSource={RelativeSource AncestorType=local:MyListItem}` 并补 `xmlns:local`。
    注意：生成器会与同名手写成员冲突（CS0102），所以必须删 x:Name，不能只加手写字段。
    **排查命令**：`for cls in <控件名>; do grep -c "\bPanBack\b" Controls/$cls.xaml.cs; done` — 根元素带 `x:Name` 且 .cs 有引用的控件都可能踩雷（MyListItem 已修；MyIconButton 的 PanBack 是**嵌套** Border，安全）。

13. **Avalonia 没有 `x:Array`（崩溃级，2026-09-28 修复）** — 上游用 `<x:Array Type="{x:Type local:MyIconButton}">` 给 `IEnumerable<MyIconButton>` 属性（`MyListItem.Buttons`）赋值。移植时误判并**删掉了包裹层**，直接把子元素内联进属性元素 → **编译能过，但运行期 `!XamlIlPopulate` 对尚未初始化的集合字段做添加 → NRE**（`PageInstanceLeft.axaml:20`，点"实例设置"即崩）。
    - Avalonia 至今不支持 `x:Array`（维护者原话："Arrays aren't currently supported. For now you can use `System.Collections.ArrayList` or `System.Collections.Generic.List`"）。
    - **正确等价写法**（实测）：
      ```xml
      xmlns:sys="clr-namespace:System.Collections.Generic;assembly=netstandard"
      <local:MyListItem.Buttons>
          <sys:List x:TypeArguments="local:MyIconButton">
              <local:MyIconButton ... />
          </sys:List>
      </local:MyListItem.Buttons>
      ```
      ⚠️ `assembly` **必须是 `netstandard`**；写 `System.Runtime` 会报 `Unable to resolve type List from namespace ... assembly=System.Runtime`。`Application.axaml` 已占用 `xmlns:sys`（指向 `System`），故那里改用前缀 `sysgen`。
    - 修复脚本：`tools/fix-buttons-collection.py`（37 处，5 个文件）。
    - **诊断手法**：`--pagetest` 自检（见 §12）。反向对照（把包裹层删掉重跑）能精确复现同一 NRE 堆栈。

14. **`[Content]` 按"实例类型"路由（崩溃级，2026-09-28 修复）** — 与坑 3 同源，但这次漏了 `PageComp`。上游 `[ContentProperty("SearchTags")]`，移植版改成属性级 `[Content]`：
    - WPF 按**根标签类型**路由：`PageComp.xaml` 的根是 `<local:MyScrollViewer>`，其子元素走 `MyScrollViewer.Content`，不受影响；子类 `<local:PageComp>…</local:PageComp>` 的子元素才进 `SearchTags`。
    - Avalonia 按**实例类型**路由：`[Content]` 一挂，PageComp **自身**根子元素（StackPanel）也被塞进 `SearchTags` → 填充期访问未初始化的 `ComboSearchTag` → NRE（`PageComp.axaml:12`）。
    - **修法**：删掉 `[Content]`，并让 6 个子类 XAML 显式写 `<local:PageComp.SearchTags>…</local:PageComp.SearchTags>`。脚本：`tools/fix-pagecomp-content.py`。
    - 全仓 `[Content]` 仅 3 处，另两处（`AnimationGroup.Children`、`CustomEventCollection.Events`）getter 非 null，安全。

15. **AvaloniaProperty 字段必须对 XAML 生成代码可见（崩溃级，2026-09-28 修复）** — 上游 `private static readonly DependencyProperty` 没问题，因为 WPF 的 XAML 走 CLR 属性 setter；**Avalonia 的编译期 XAML 会直接访问 `XxxProperty` 字段**，跨类访问 private → 运行期 `FieldAccessException`（不是编译错误！）。
    - 实测受害：`MyPageRight.PanScrollProperty`（`PageSelectRight` 实例化即崩 → 点"实例选择"后程序无响应）、`MyImage.CornerRadiusProperty`（`PageHomepageNewsView`/`PageSetupAbout`）。
    - **修法**：提升为 `public`。**扫描命令**：找非 public 的 `*Property` 字段（排除 getter 非 null 的实例字段）。

16. **非 UI 线程取本地化 → `!key!`（2026-09-28 修复）** — `Lang.Text` 的两条查找路径都经 Avalonia 资源宿主（`Application.Resources`/`ActualThemeVariant`，仅 UI 线程）；加载/启动线程上 `ResourceExtensions` 直接返回 null、`_LifecycleSafeFindResource` 吞掉 VerifyAccess，于是 DEBUG 下退化成 `!key!`（表现为"当前步骤: !Minecraft.Launch.Stage.WaitWindow!"、"启动成功"提示变成 `!Minecraft.Launch.Success!`）。
    - **修法**：`LocalizationService` 额外维护一份线程安全的 `ConcurrentDictionary<string,string>` 文本快照（语言字典是纯 XML 键值对，跨线程读安全），`Lang.Text` 最后兜底查它。
    - 注意：**语言键在 zh-CN.xaml 里是存在的**，别去查语言文件——要查的是"为什么查不到"。

17. **scRGB ≠ sRGB（整站配色偏暗，2026-09-28 修复）** — 两处色彩空间误用，合起来让所有主题色比上游暗一截：
    - `ScRgbCompat.FromScRgb` 把入参当 sRGB 分数直接 `v*255`。但 WPF `Color.FromScRgb` 的入参是 **scRGB 线性值**，须先过"线性→sRGB"传递函数再量化。
    - `LabColor.FromWpfColor` 传 `(double)color.R/255`，命中的是 `FromRgb(double…)` 重载 = `ColourSpace.RgbLinear`，等于把 sRGB 数值当线性用。上游传的是 `color.ScR`（真线性）。改用语义明确的**字节重载** `FromRgb(byte…)`。
    - **实测**：标题栏 `ColorBrush3` 从 `#0637C3` → `#2A80E3`；离屏渲染主界面采样内容区 `#0A1225`、侧栏 `#27282C`，与上游窗口的 `#0A1225`/`#27282E` 一致。`Color → LabColor → Color` 往返现在 Δ=0。
    - **判据**：如果你看到某个颜色 ≈ 原版颜色的"线性化"结果（明显更暗），就是这个问题。

18. **`avares://` 大小写敏感 + 不能用 FileStream 读（2026-09-28 修复）** — Avalonia 按清单资源名**精确匹配**，Windows 文件系统不区分大小写但资源名区分：
    - 20 处 `avares://PCL/images/...` 而目录是 `Images/` → `FileNotFoundException`。脚本：`tools/check-avares-case.py`（可 `--fix`）。
    - `ModBase.pathImage` 仍是 `pack://application:,,,/Plain Craft Launcher 2;component/Images/`，且 `MyBitmap` 对它用 `new FileStream(...)` → 必然失败。已改为 `avares://PCL/Images/` + 经 `AssetLoader.Open` 取流（实例方块图标因此恢复正常）。
    - `PageSetupAbout`/`PageSetupUpdate`/`MinecraftServer` 还有 12 处 WPF pack 语法残留，已转 `avares://PCL/...`。
    - **已知上游缺口（未改）**：`PageSetupUpdate.axaml:150` 引用 `Images/Icons/Tahoe.png`，**上游仓库里也不存在**（两版 Images 均为 47 个文件）。WPF 对缺失图片静默不渲染，Avalonia 直接抛异常 → "设置 → 更新"页当前实例化失败。属上游数据缺口，改文案会偏离上游，留给用户定夺。
    - **补充（2026-09-30）**：历史配置兼容。旧版本把 `pathImage` 存成 pack URI 后写进了实例配置（`versions/<实例>/PCL/config.v1.yml` 的 `Logo`），改成 avares 后这些旧值不再匹配资源前缀 → `MyBitmap` 用 FileStream 打开 pack URI → IOException 从 `MyListItem.UpdateLogo` 的属性变更回调逃逸 → **打开实例列表即崩**（用户报的"加载实例列表失败 + 崩溃"）。修法：`MyBitmap._NormalizeImagePath` 归一化已知 WPF pack 前缀。

19. **同一事件上的多个 Tunnel 处理器是「逆序」调用（点击失效，2026-09-30 修复）** — Avalonia 对同一 `RoutedEvent` + 同一 `RoutingStrategies` 的多个处理器，**后注册的先执行**（实测：注册顺序 1→2，调用顺序 2→1）；WPF 是按注册顺序。
    - 受害点：`MyListItem` 把「点击处理」`Button_PointerReleased` 与「复位」`Button_PointerExited` 都挂在 `PointerReleasedEvent` 的 Tunnel 上（上游 WPF 依赖"点击先于复位"）。Avalonia 下复位先跑 → `isMouseDown` 被清零 → 点击处理器直接 `return` → **实例设置页左侧所有列表项全部点不动**，日志里连"按下单选列表项"都不会出现。
    - **修法**：复位改为 `Dispatcher.UIThread.Post` 异步执行，保证点击处理一定先完成。
    - **排查命令**：`grep -rn "RoutingStrategies.Tunnel" --include=*.cs` — 检查同一事件上是否有多个 Tunnel 处理器且存在顺序依赖。

20. **WPF `IsMouseDirectlyOver` 无 Avalonia 等价物（标题栏按钮全部点不动，2026-09-30 修复）** — 上游 `FormDragMove` 用 `IsMouseDirectlyOver`（**仅元素自身**，不含子元素）决定是否 `DragMove()`。移植版换成了 `IsPointerOver`（**包含子元素**），于是点击标题栏内的选项卡/最小化/关闭/返回按钮时也会命中拖动分支，`BeginMoveDrag` **捕获指针** → 按钮收不到 `PointerReleased` → 点击全部失效（只有按下/悬停动画）。
    - **修法**：改为 `ReferenceEquals(e.Source, sender)`，还原"直接在标题栏上"的语义。

21. **WPF `TextBlock.Text` 默认 `string.Empty`，Avalonia 默认 `null`（崩溃级，2026-09-30 修复）** — 上游 `ModStyle.SetColorfulTextLab` 只判 `lab is null`，不判 `text`；调用方会直接传 `LabInfo.Text`（该文本可能从未赋值）。Avalonia 下传入 null → `foreach (var c in text)` NRE → **"加载 Mod 列表 UI 失败"**。
    - **修法**：`text ??= string.Empty;`。**排查方向**：凡是把 `.Text` 当非空字符串用的地方都要留意。

22. **WPF `TextBlock.Padding` 可以超过可用尺寸，Avalonia 会把文字压没（按钮文字空白，2026-09-30 修复）** — 上游 `BtnMore` 是 `Height="35"` 配 `TextPadding="36"`（`TextPadding` 直接映射到 `LabText.Padding`）。WPF 下文字仍居中可见；Avalonia 下内容区高度被压成 0，**文字完全不绘制** → "实例设置按钮文字空白"。
    - **修法**：`MyButton.ApplyTextPadding()` 按控件实际可用尺寸收敛内边距（至少给文字留 1.5 倍字号），并在 `SizeChanged` 时重算。
    - **验证手法**：打印 `请求内边距 / 实际内边距 / 文字区尺寸` 即可确认（实测收敛为 `36,7.75,36,7.75`，文字区 124x32）。

23. **滚轮量纲不同（滚动异常缓慢，2026-09-30 修复）** — WPF `MouseWheelEventArgs.Delta` 每格 **±120**；Avalonia `PointerWheelEventArgs.Delta` 每格 **±1**。上游 `PerformVerticalOffsetDelta(-e.Delta)` 直接照用会让滚动距离只有 1/120。
    - **修法**：`MyScrollViewer` 中换算回 WPF 量纲（`* 120`），`DeltaMult` 语义不变。

24. **`Template.FindName` 垫片不可靠（崩溃级，2026-09-30 修复）** — `Stubs.cs` 里的垫片是 `control.FindNameScope()?.Find(name)`；模板刚应用时可能落到外层（页面）名称作用域，取不到模板内元素并**静默返回 null**。
    - 受害点：`MyTextBox.labHint` 恒为 null → `OnApplyTemplate` 里 `labHint.Text` NRE，且 `HintText` 提示文本永不显示。
    - **修法**：在 `OnApplyTemplate` 里用 `e.NameScope.Find<T>(...)` 取部件并缓存（与 `MyScrollViewer` 取 `PART_VerticalScrollBar` 的写法一致），惰性 getter 仅作兜底。

---

25. **应用没有引入 Avalonia 主题，模板全靠手写（漏一个控件就整块空白，2026-10-01 修复）** — `Application.axaml` 里手写了 MyScrollBar / MyScrollViewer / MyTextBox / MyComboBox / ToggleButton / ComboBoxItem / ContextMenu / MyMenuItem / Calendar 等模板，但**漏了 `ItemsControl`**。`ItemsControl` 是 `TemplatedControl`，没有模板就不会实例化 `ItemsPresenter` → 一个子项都不渲染。
    - 实测：档案列表 `ItemCount=4`，但 `ItemsControl` 的**视觉子树为 0**，界面完全空白（对应"选择档案界面一个账号都没有"）。
    - **修法**：补上等价模板 `<Style Selector="ItemsControl">` + `<ControlTemplate><ItemsPresenter Name="PART_ItemsPresenter" ItemsPanel="{TemplateBinding ItemsPanel}" /></ControlTemplate>`（`ItemsPanel` 有内置默认值 StackPanel，无需再设）。
    - **排查提示**：凡是"数据有、界面空"的列表，先确认对应控件模板是否存在于 `Application.axaml`。

26. **`Grid` 的 `*` 行在"可用高度无限"时 Avalonia 算 0，WPF 按内容撑开（2026-10-01）** — `PageLoginProfile` 的滚动列表行是 `*`，而它被放进 `PageLaunchLeft.PanLogin`（`VerticalAlignment="Center"`，按内容自适应高度）→ 无约束 → `*` 塌成 0 → `MyScrollViewer` 高 0 → 列表不渲染。已把该行改为 `Auto`（该页宿主本就按内容自适应，`*` 无实际作用）。
    - **判据**：页面高度恰好等于"固定行之和"、`*` 行内容不可见，就是这个坑。

27. **`BoxShadow` 是空格分隔，逗号只分隔"多个阴影"（崩溃级，2026-10-01 修复）** — `Application.axaml` 写成 `BoxShadow="0 2 4 0, #66000000"`，被当成两个阴影；第一个 `0 2 4 0` 只有 4 个 token，按 `offsetX offsetY blur color` 解析时把末位 `0` 当颜色 → `FormatException: Invalid color string: '0'`。
    - 该模板在**首次弹出 ContextMenu** 时实例化 → "右键菜单一出现就崩溃"。
    - 正确写法：`offsetX offsetY [blur] [spread] color`（3~6 个 token，空格分隔），如 `0 2 4 0 #66000000`。

28. **`(int)converter.Result` 在结果为 null 时抛 NRE（崩溃级，2026-10-01 修复）** — `ModMain.MyMsgBox` / `MyMsgBoxMarkdown` 直接 `return (int)converter.Result;`。`Result` 是 `object`，当弹窗没能产生结果时（关闭流程中 `PushFrame` 直接返回、后台线程投递失败）为 null，`(int)(object)null` 抛 `NullReferenceException` → FTL → 强制关闭。改为 `converter.Result is int r ? r : 0`。（`MyMsgBoxSelect` 用 `(int?)`、`MyMsgBoxInput` 用 `?.ToString()`，天然安全。）
    - **排查提示**：日志里 `普通弹框返回：null` 紧跟 FTL，就是这个。

29. **`DispatcherFrame` 构造函数会 `VerifyAccess`（跨线程弹窗必崩，2026-10-01 修复）** — `MyMsgBoxConverter.WaitFrame = new(true)` 是**字段初始化器**。WPF 的 `DispatcherFrame` 绑定"当前线程"的 Dispatcher、构造不校验；Avalonia 的会执行 `Dispatcher.VerifyAccess()` → 任何**后台线程**调用 `MyMsgBox`（MS 登录、下载报错）都在 `new MyMsgBoxConverter` 这一步就崩。
    - 另：后台线程调用时原实现也不会执行 `MyMsgBoxTick()`（弹窗永不显示），且 `Dispatcher.UIThread.PushFrame` 本身就要求 UI 线程。
    - **修法**：`WaitFrame` 改为惰性创建；新增 `PumpMsgBoxFrame(converter)`，非 UI 线程时把"创建弹窗 + 泵送模态帧"整体投递到 UI 线程（`RunInUiWait`），调用线程阻塞等待弹窗关闭。

30. **`MyComboBox` / `MyMenuItem` 的模板部件同样取不到（2026-10-01 修复，坑 24 的扩展）** — 都走 `Template.FindName` 垫片。`MyComboBox.OnApplyTemplate` 的 NRE 会以 Feedback 级日志弹出消息框 → 连带触发弹窗系统 → 表现为"点设置选项卡后整个 UI 冻结"。统一改为缓存 `e.NameScope` 并从中取部件。

31. **`MyTextBox.GetValidateResult` 给 FluentValidation 传 null 模型（崩溃级，2026-10-01 修复，坑 21 的扩展）** — `rule.Validate(Text)`，`Text` 为 null（Avalonia 的 `TextBox.Text` 默认 null）→ `Cannot pass a null model to Validate`。**点下载选项卡约 1 秒后崩溃**就是这条（`PageDownloadInstall.Init()` 设置 `ValidateRules`）。改为 `Text ?? string.Empty`。

32. **`ElementName` 指向"根元素 x:Name"的绑定取不到值（2026-10-01 修复，坑 12 的扩展）** — `MyExtraTextButton` 用 `CornerRadius="{Binding Bounds.Height, ..., ElementName=PanBack, Mode=OneWay}"`；`ElementName` 指向根元素 `x:Name` 在 Avalonia 下解析不到 → 转换器拿到非数值返回 0 → **按钮变方角**。`$parent[local:类型]` 与 `ReflectionBinding` 实测也无效。该模板高度是固定值 `Height="52"`，上游 `ActualHeight * 0.4` 恒为 `20.8`，故直接写常量（渲染结果与上游一致）。**排查提示**：凡 `ElementName=` 指向本文件根元素 `x:Name` 的绑定都要怀疑。

## 8. 暂缓功能清单（用户指定，文件保留在仓库）

`DeferredExcludes`（见 §5.1）：崩溃分析(CrashAnalysis)、音乐(ModMusic)、视频背景(ModVideoBack)、联机(ModLink)、换皮肤(ModSkin)、工具页(PageTools)、剪贴板行为(ClipboardInterceptor)、LazyLoadBehavior。
另：Markdown 公告（Markdown.axaml/MarkdownViewer 已降级为 SelectableTextBlock，MyMsgMarkdown 已恢复编译）。
MySkin 和 MyMsgMarkdown **已重新启用**（不在排除清单里）。

---

## 9. 下一步任务（优先级排序）

1. **验证透明窗口问题**（✅ 2026-09-28 已闭环）— 用户已确认窗口正常显示；离屏渲染（`--pagetest` 第 8 项）也正常。原"透明窗口"是崩溃前的瞬时状态。
2. **用户报告的一批缺陷**（✅ 2026-09-28 已修）— 实例设置崩溃、实例选择无响应、本地化 `!key!`、整站配色偏暗、内嵌图片加载。详见 §7 坑 13~18 与 §12。
   待用户复测：`PCL.exe` 点「实例选择」「实例设置」「下载→Mod」应均不崩；启动 MC 时步骤名与成功提示应为中文。
3. **WSL headless 冒烟** — `dotnet publish -r linux-x64 --self-contained`，在 WSL 里无头跑，确认 Linux 上能起来（X11/Wayland 或 headless 检查日志）。
   - ⚠️ **本机 `wsl.exe` 被安全策略拉黑**（Program Blacklist，无法授权/绕过）→ 冒烟只能交给用户 VM。本机可做的替代校验（已验证通过）：
     1. 单文件：`dotnet publish "src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" -c Release -r linux-x64 --self-contained true -o dist/linux-x64` → 产出 `dist/linux-x64/PCL`（ELF x86-64，120MB，`OutputType=WinExe` + `PublishSingleFile` 使其无 `.exe` 后缀）。
     2. 非单文件（便于 VM 上排查、可用 `DOTNET_` 环境变量）：追加 `-p:PublishSingleFile=false -o dist/linux-x64-flat`。**注意**：不要用 `-p:OutputType=Exe` 全局覆盖，会把 `PCL.Core.SourceGenerators` 也改成 Exe → `CS5001 程序不包含入口点`。
     3. 静态校验（已通过）：`PCL` 与 `libSkiaSharp.so`/`libHarfBuzzSharp.so`/`libe_sqlite3.so` 全为 ELF machine=62（x86-64）；bundle 内可见 `Avalonia.FreeDesktop`、`Avalonia.Skia`、`PCL.Core.dll`、`App/Localization/Languages/zh-CN.xaml`。
     4. 入口 `Program.Main` 用 `UsePlatformDetect()` → Linux 上自动选 FreeDesktop 后端，无需改代码。
   - `dist/` 已入 `.gitignore`（116MB，勿入库）。给用户的目录内含 `run.sh`（`LC_ALL=zh_CN.UTF-8` + `exec ./PCL`）。
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
- **遇到"页面能编译但一打开就崩"时，先跑 `PCL.exe --pagetest`**（见 §12），不要靠猜。

---

## 12. 移植期自检工具（`--pagetest`，2026-09-28 新增）

**用法**：`PCL.exe --pagetest`（正常启动不受影响；逻辑在 `FormMain.RunPageInstantiationSelfTest`）。

在 UI 线程上依次执行并把结果写入运行日志（`[PageTest]` 前缀）：

| # | 检查项 | 作用 |
|---|---|---|
| 1 | 直接构造各页面 Left/Right 控件 | 抓 XAML 填充期异常（命名字段为 null、集合属性填充、字段可见性） |
| 2 | `Left.PageGet(subType)` 全子类型 | 覆盖惰性创建的右侧页面 |
| 3 | 真实 `PageChange(pageType)` 导航 | 复现用户点击（含 `PageChangeActual` 全链路） |
| 4 | 后台线程 `Lang.Text` | 回归 `!key!`（坑 16） |
| 5 | 7 个颜色 `Color→LabColor→Color` 往返 | 回归 scRGB（坑 17），要求 Δ≤1 |
| 6 | `MyBitmap` 加载内嵌图片 | 回归 avares 取流（坑 18） |
| 7 | 打印主题状态与关键画刷 | 排查配色 |
| 8 | 渲染主界面到 `PCL/Log/ui-render.png` | **目视/像素比对**（无桌面环境时尤其有用） |
| 9 | 列表项**逻辑链路**：直接 `SetChecked` | 区分"处理器链路坏"还是"输入投递坏" |
| 10 | 列表项**输入链路**：合成 `PointerPressed`+`PointerReleased` 投递 | **回归坑 19**（Tunnel 处理器逆序）。修复前 pageID 不变，修复后切换成功 |
| 11 | 视觉切换检查（`PanMainRight.Child`） | `SetChecked` 只改 pageID，内容替换在延时动画里 |
| 12 | 按钮文字度量（`BtnMore`） | **回归坑 22**（内边距收敛 + 文字区尺寸） |

⚠️ **不要用 `InputHitTest` 判断控件可点性**：实测它对 `PanForm` / `PanTitleMain` 等子树会误报"不可达"（连能正常点击的启动页按钮也报不可达），与本应用真实指针投递结果不一致。判断可点性请用第 10 项的**合成事件**方式。

**当前基线（2026-09-28，commit 9e00ef6）**：59 项通过 / 2 项失败，两项均为已知上游缺口：
- `PageSetupLeft.PageGet(3)` = `SetupLink`（联机页，暂缓功能，未实现）
- `PageSetupLeft.PageGet(8)` = `SetupUpdate`（引用上游也不存在的 `Tahoe.png`，见坑 18）

**注意**：`PageSubType` 各段（Download*/Setup*/Version*）**数值重叠**，`Enum.ToString()` 可能显示同值的其它段名字——日志里同时打印了数值以便核对。

**配套脚本**（都在 `tools/`）：
- `fix-buttons-collection.py` — 坑 13（x:Array → sys:List）
- `fix-pagecomp-content.py` — 坑 14（PageComp.SearchTags 显式包裹）
- `check-avares-case.py` — 坑 18（avares 路径大小写，`--fix` 可写回）
- `capture-window.py` — 采样活动窗口像素（注意：沙箱进程的桌面与应用不同，普通截屏看不到应用窗口；优先用第 8 项自渲染）

---

*本文档由上一任代理在交接时生成。若与 git 历史冲突，以 git 为准。*
