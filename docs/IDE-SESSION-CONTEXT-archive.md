# IDE 自动生成的会话上下文（存档）

> ⚠️ 这不是人工维护的文档，是开发时 IDE 自动生成并注入给 AI 代理的「项目上下文」快照，
> 内容停留在某次会话的状态，**可能已过时**。

> 保留它只是因为它按时间线记录了**需求演变和当时的技术判断**，对理解"为什么会走到今天这一步"有帮助。
> **要看权威的当前状态，请读 `AGENTS.md` 和 `docs/HANDOVER.md`。**

---

Primary Request and Intent
Original (earlier in session): Port PCL-CE (WPF Minecraft launcher) UI verbatim to Linux on Avalonia/.NET 10 — "我要求你把 PCLCE 的 UI 完整地、原样移植过来，不要自己重新写任何组件" (port, never redesign; layout/colors/text/interaction must match upstream exactly).
User correction (verbatim, must be honored at all times): "然后记住目标是移植pcl ce的代码并不是重写，所以说你也要时时刻刻记着不要重写ui。要保留原有的功能逻辑和风格，这样子的话才是pcl ce启动器，而不是一个披着pcl名字重写的启动器。" (Port, don't rewrite UI; keep original function logic and style.)
Current user goal: clear all compile errors, produce a build, and show the result — "最终目标是将所有问题都修复，编译出来，你可以按照自己的想法和节奏逐步处理1000个错误，分步来，我理解这是个大工程".
Latest user input: session was interrupted (user + subagents); user asked for a status report ("目前情况，还有干什么，然后继续"). Then user supplied Launch-2026-9-18-220810210.log and reported: on Windows the app starts, creates a transparent window, then crashes ~1 second later — "你接着做吧".
Key Technical Concepts
Avalonia 12.0.4 + net10.0; source D:\a\j\PCL.CE-Linux, upstream baseline D:\a\j\PCL-CE.
dotnet build "src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" -v q -nologo [--no-incremental] — authoritative full build; incremental builds and subagent dotnet build runs MASK residual errors (key lesson).
Avalonia name-field generator: requires code-behind partial class to declare an Avalonia base class; does NOT generate fields for ColumnDefinition/RowDefinition/Transform/Effect/geometry (RectangleGeometry) elements.
Avalonia 12 differences verified: no PasswordBox (use TextBox + PasswordChar); no Visibility (use IsVisible); Clipboard via TopLevel.GetTopLevel(c)?.Clipboard; DragEventArgs.DataTransfer (sync IDataTransfer) + e.DragEffects (no e.Data/e.Operations/DragDropOperations); selectors use local|Type (not local:Type); Application.Styles cannot hold keyed Style/DataTemplate (keyed items go to Resources; selector styles go to Styles, e.g. <Grid.Styles>); easings are CircularEaseIn/Out/InOut (WPF CircleEaseOut invalid); ScrollViewer exposes Offset(Vector)/Extent(Size)/Viewport(Size)/ScrollBarMaximum(Size) — no VerticalScrollBarMaximum/Value/ViewportSize; TextBox requires template part PART_TextPresenter (a TextPresenter); ScrollBar template parts are PART_LineUpButton/PART_LineDownButton/PART_PageUpButton/PART_PageDownButton (no RoutedCommands); Foreground/FontSize/FontFamily are inherited properties (no binding needed in templates); resource brushes are ImmutableSolidColorBrush/ISolidColorBrush (cannot cast to SolidColorBrush/Brush).
Avalonia VerifyAccess: UI objects (incl. Application.Resources/ActualThemeVariant) may only be touched on the UI thread.
C# rule: dynamic cannot bind extension methods.
MSBuild on Windows expands ** to backslash item paths — Exclude with forward slashes silently fails; a bare <Item Exclude="..."/> element is illegal (MSB4232, needs Include/Update/Remove).
pwsh tool pitfall: Get-Content renders Chinese as mojibake (display only); logs written by PCL are GBK → read with [System.Text.Encoding]::GetEncoding(936). [System.IO.File] APIs use the process CWD, not PowerShell's location — always use absolute paths.
Repo rule: EnableDefaultItems must stay true in the main project (else the Avalonia generator silently dies) and false in PCL.Core; exclusions via <Compile Remove> / <AvaloniaXaml Remove>.
Files and Code
src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj: AssemblyName=PCL, RootNamespace=PCL. Added Avalonia.Desktop 12.0.4, bumped SkiaSharp 3.119.0→3.119.4 (NU1605). Added <AvaloniaXaml Remove="Resources\Markdown.axaml" /> and <AvaloniaXaml Remove="Pages\PageTools\**" />. DeferredExcludes = CrashAnalysis/ModMusic/ModVideoBack/ModLink/ModSkin/PageTools/ClipboardInterceptor/LazyLoadBehavior (MySkin and MyMsgMarkdown were re-enabled; MySkin.axaml(.cs) removed from excludes by a subagent).
src\PCL.Core\PCL.Core.csproj: AssemblyName=PCL.Core, EnableDefaultItems=false, <Compile Include="**\*.cs" .../>, <AvaloniaXaml Include="**/*.xaml" Exclude="...;App\Localization\Languages\...7 files" />, <AvaloniaResource Include="UI/Assets/**/*" />, <AvaloniaResource Include="App/Localization/Languages/**/*.xaml" />, <EmbeddedResource Include="App\Localization\Languages\**\*.xaml" LogicalName="PCL.Core.App.Localization.Languages.%(Filename)%(Extension)" />.
src\PCL.Core\UI\Controls\Stubs.cs (new): BlurBorder : Border + MotdRenderer : Control in namespace PCL.Core.UI.Controls (moved here so clr-namespace:PCL.Core.UI.Controls;assembly=PCL.Core resolves).
src\Plain Craft Launcher 2\Stubs.cs: namespace PCL only — ModVideoBack/ModMusic/ModSkin stubs + MyColorExtensions.ToBrush()/ToColor() + ControlTemplateExtensions.FindName().
src\Plain Craft Launcher 2\ControlResourceHelper.cs (new): public static void SetResourceReference(this AvaloniaObject target, AvaloniaProperty property, string resourceKey) using target.Bind(property, ctrl.GetResourceObservable(resourceKey)) (dynamic-resource semantics, theme-aware). Call sites need this. prefix (extension methods need a receiver).
src\PCL.Core\App\ResourceExtensions.cs: rewritten — thread-safe cache ConcurrentDictionary<object,object?> _resourceCache; TryFindResource returns cached value; if !Dispatcher.UIThread.CheckAccess() returns null (never marshals); clears cache on Application.ActualThemeVariantProperty change.
src\PCL.Core\App\Localization\LocalizationService.cs: added _OpenLanguageStream(languageCode) — tries AssetLoader.Open(avares://PCL.Core/App/Localization/Languages/{code}.xaml); on failure falls back to assembly.GetManifestResourceStream($"PCL.Core.App.Localization.Languages.{code}.xaml"); else throws FileNotFoundException. _LoadLanguageDictionary now uses it.
src\PCL.Core\App\Localization\LocalizationFontService.cs: _ApplicationPackUri new("pack://application:,,,/") → new("avares://PCL/").
src\Plain Craft Launcher 2\Modules\Base\ModBase.cs: MyColor(object) now handles ISolidColorBrush (was only SolidColorBrush, dynamic .A failed on ImmutableSolidColorBrush); added ctor overload MyColor(double newA, IBrush brush) (uses (brush as ISolidColorBrush)?.Color ?? Colors.Black).
src\Plain Craft Launcher 2\Controls\AnimatedBackgroundGrid.cs: BackgroundBrushProperty/AnimatableBrush/BackgroundBrush changed SolidColorBrush→IBrush; Register<AnimatedBackgroundGrid, IBrush>; line 75 new ModBase.MyColor(brush) - new ModBase.MyColor(grid.AnimatableBrush).
src\Plain Craft Launcher 2\Controls\MyCard.cs: override protected override IBrush AnimatableBrush.
8 controls (MyButton, MyCheckBox, MyExtraTextButton, MyHint, MyIconTextButton, MyListItem, MyRadioBox, MyRadioButton): removed [Content] on public InlineCollection Inlines => LabXxx.Inlines; with a [port] comment (see Errors).
MyIconButton/MyLoading/MyListItem: Foreground typed IBrush (StyledProperty<IBrush> ForegroundProperty, Register<..., IBrush>).
src\Plain Craft Launcher 2\Modules\Base\ModAnimation.cs (~line 149): replaced dynamic ((dynamic)anim.obj)[0].SetResourceReference(...) with var colorTargets = (object[])anim.obj; var resourceKey = colorTargets[2] as string; if (!string.IsNullOrEmpty(resourceKey)) ((AvaloniaObject)colorTargets[0]).SetResourceReference((AvaloniaProperty)colorTargets[1], resourceKey);
src\Plain Craft Launcher 2\Controls\MyExtraButton.xaml.cs: RectProgress = PanProgress.Clip as RectangleGeometry; (was FindNameScope()?.Find("RectProgress")); setter guarded with if (RectProgress is not null).
Application.axaml: removed 4 keyed <Style ... x:Key=...> blocks from <Application.Styles>; moved <DataTemplate x:Key="ComboBoxItemTemplateWithDelete"> into <Application.Resources>; Easing="CircleEaseOut"→"CircularEaseOut"; ScrollBar RepeatButtons → Name="PART_PageUpButton"/"PART_PageDownButton"; MyScrollViewer ScrollBar bindings → ScrollBarMaximum.Y/Offset.Y (TwoWay)/Viewport.Height with RelativeSource TemplatedParent; MyTextBox template gained <TextPresenter x:Name="PART_TextPresenter" Text="{TemplateBinding Text, Mode=TwoWay}" PasswordChar RevealPassword SelectionStart SelectionEnd CaretIndex TextAlignment TextWrapping />; root <Grid ToolTip.Tip="{TemplateBinding ToolTip}"> → <Grid>.
tools\fix-axaml-bulk.ps1, tools\fix-empty-assembly.ps1, tools\fix-axaml-bulk.ps1 (bulk: ToolTip="→ToolTip.Tip=", remove RenderOptions.BitmapScalingMode/ShadowDepth/TextFormattingMode, Visibility→IsVisible, strip x:Name on transform/effect tags, AllowDrop=→DragDrop.AllowDrop=).
docs\av12-notes.txt (new, authoritative Avalonia-12 API notes), docs\PORTING-STATUS.md, docs\PORTING-MAPPING.md (GBK — read with GetEncoding(936)).
Build output: src\Plain Craft Launcher 2\bin\Debug\net10.0\PCL.dll (5.0 MB), PCL.exe (~158 KB), PCL.runtimeconfig.json.
Errors and Fixes
C# errors 10564 → 0; XAML (AVLN) 792 → 0 (final: 0 个错误).
UsePlatformDetect CS1061 → added Avalonia.Desktop; NU1605 SkiaSharp downgrade → bumped to 3.119.4.
Markdown.axaml 792 AVLN2000 → excluded from AvaloniaXaml too (not just AvaloniaResource). PageTools Unable to find type PCL.PageTools* → <AvaloniaXaml Remove="Pages\PageTools\**" />.
BlurBorder/MotdRenderer "Unable to resolve type ... assembly=PCL" → stubs moved into PCL.Core.
Empty xmlns:local="clr-namespace:PCL;assembly=" → assembly=PCL (4 files).
[Content] public InlineCollection Inlines => LabText.Inlines; → NullReferenceException during !XamlIlPopulate (Avalonia routes root XAML children by instance type, WPF by root tag type; LabText null at that point). Fixed by removing [Content] on all 8 controls.
ThemeManager TypeInitializationException: Microsoft.CSharp.RuntimeBinder.RuntimeBinderException: 'ImmutableSolidColorBrush' does not contain a definition for 'A' at ModBase.MyColor..ctor line 225 → added ISolidColorBrush branch.
InvalidCastException: Unable to cast ImmutableSolidColorBrush to SolidColorBrush/Brush (DynamicResource publish / resource casts) → IBrush/ISolidColorBrush mapping.
Easing "CircleEaseOut" was not found (FormatException at Application.axaml:378) → CircularEaseOut.
Every animation tick: RuntimeBinderException: 'Border' does not contain a definition for 'SetResourceReference' at ModAnimation.cs:151 → strongly-typed call (dynamic can't bind extension methods). Log dropped 8135 → 377 lines.
Cross-thread InvalidOperationException: The calling thread cannot access this object via Application.get_ActualThemeVariant (Lang.Text → TryFindResource from loader threads) → cached/thread-safe ResourceExtensions.
FileNotFoundException: avares://PCL.Core/App/Localization/Languages/zh-CN.xaml → languages embedded as AvaloniaResource + EmbeddedResource fallback; then LocalizationFontService UriFormatException: Invalid port specified from pack://application:,,,/ → avares://PCL/. Final log: 当前 UI 语言: zh-CN + 本地化 启动成功.
MyExtraButton.set_Progress NRE (RectProgress null) → take from PanProgress.Clip + null guard.
Encoding corruption incident: pwsh full-file rewrites turned p→u in MyIconButton.xaml.cs and MyListItem.xaml.cs (public→uublic), causing CS1519/CS1002 syntax errors. Fixed via git checkout -- on both files; afterwards all edits done with the read+edit tools (encoding-safe). Verified no uublic|urivate remained.
User feedback to honor: don't rewrite UI; every non-pure mapping annotated // [port] <reason>; batch-write long docs/code; commit frequently; all questions batched for the end.
Pending Jobs
Confirm the transparent window issue the user reported (window is created and stable, but user saw a transparent window ~1s before crash) — determine whether content renders (background/brush/rendering) or whether it was purely the now-fixed crash.
WSL headless smoke test → self-contained publish → user VM test on Linux.
Final batched questions to the user (B-class deviations, C-class Linux degradations, FEATURES.md P1/P2/P3 priority, whether to delete unused PCL.Neo.Core/PCL.Neo.UI).
Update docs/PORTING-STATUS.md with the final state (0 errors, app runs).
Current Work
Just achieved: build 0 errors and runtime clean: PCL.exe runs, main window 'Plain Craft Launcher Community Edition ' stays open with responding=True, log = 304 lines with no FTL|未捕获|InvalidCast|RuntimeBinder|NullReference|FileNotFound|UriFormat|启动失败|ERR!, and localization reports 当前 UI 语言: zh-CN, 展示格式: zh-CN + 本地化 (Loaded/localization) 启动成功.
Last three fixes committed as a07dfb8 ("runtime: localization working, zero runtime errors, app stays open") covering: EmbeddedResource fallback + LocalizationService._OpenLanguageStream, LocalizationFontService avares URI, MyExtraButton.RectProgress via PanProgress.Clip + null guard.
A running PCL.exe process (last pid 48520) may still be alive in the background.
Next Step
Verify the transparent-window symptom the user reported: relaunch PCL.exe, keep it running, and confirm the window actually renders content (check FormMain.axaml background/ExtendClientArea/TransparencyLevelHint and that page content is instantiated — log already shows 实例化：清空主页 UI and 启动按钮：Minecraft 实例：...), i.e. determine whether "transparent" was just the pre-crash flash or a real rendering bug; then report the result to the user.
Critical Context
Iron rule: port, never rewrite — only API-name/signature mapping; every non-pure mapping carries a // [port] Chinese comment; no UI redesign.
Compile errors are authoritative only from dotnet build ... [-v q -nologo] --no-incremental; never trust incremental or subagent-reported "0 errors".
Do not bulk read/write .cs files with pwsh (caused p→u corruption); use the read+edit tools for C# edits.
Shared files (Stubs.cs, Application.axaml, ControlResourceHelper.cs) are touched by many agents — check before editing.
Runtime logs are GBK: read with [System.IO.File]::ReadAllLines($p, [System.Text.Encoding]::GetEncoding(936)); while the app runs, open with FileShare.ReadWrite.
Git: latest commits 0c1027f (animation fix) and a07dfb8 (localization + zero runtime errors); working tree otherwise clean.