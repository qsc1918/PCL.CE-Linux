# G4 Page Port Mapping Guide (WPF → Avalonia 12)

移植而非重写。绝不做任何布局/颜色/尺寸/文本/交互逻辑/业务逻辑/UI 风格上的改动。
任何不能用纯映射表达的改动，必须加 `// [port] 原因` 注释说明。不得删除有意义的业务逻辑。
只编辑分配给该工作的文件，绝不动其它文件。
文件必须 UTF-8 无 BOM（用 PowerShell 读时 `[System.IO.File]::ReadAllText($path,[System.Text.Encoding]::UTF8)`；写时用 write 工具）。

## 高概率映射规则

1. **鼠标事件**
   - `MouseLeftButtonDown/MouseRightButtonDown/MouseLeftButtonUp/MouseRightButtonUp/MouseMove/MouseDown` → `PointerPressed/PointerReleased/PointerMoved`
   - 处理器签名：WPF 的 `(object sender, MouseButtonEventArgs e)` 等 → Avalonia `(object sender, PointerPressedEventArgs e)`（事件名和 `e` 类型要同步改）。
   - 若 `MouseRightButtonUp` 引用报错（如 `MyLocalCompItem.MouseRightButtonUp`），改用 `PointerReleased`。若代码 `x.MouseRightButtonUp += handler`，改为 `x.PointerReleased += handler`（handler 签名改 `PointerReleasedEventArgs`）。
   - `MouseMove` 事件 → `PointerMoved`（签名 `(object, PointerMovedEventArgs)`）。

2. **拖放**
   - `DragEventArgs.Data` → `DragEventArgs.DataTransfer`（`IAsyncDataTransfer`）。
   - `e.Data.GetData("format")` / `e.Data.GetData(DataFormats.X)` → `e.DataTransfer.TryGetData<T>(...)`；取文件用 `e.DataTransfer.GetFiles()`（返回 `IEnumerable<IStorageItem>`，用 `.Select(i => i.Path)` 或 `i.TryGetLocalPath()` 取路径）。
   - `e.Data.GetDataPresent(...)` → `e.DataTransfer.Contains(DataFormat.File)`（DataFormat 从 `Avalonia.Input` 来）。
   - `DataFormats.FileDrop` → `DataFormat.File`。
   - `DragDropEffects` → `DragDropOperations`；`e.Effects`（设置）在 Avalonia 不存在 → 用 `e.Operations`；`e.Effects = DragDropEffects.Copy` → `e.Operations = DragDropOperations.Copy`。比较用 `DragDropOperations.Copy` / `.Move` 等。
   - "DragEventArgs 未包含 Data/Effects" → 按上述映射。

3. **Visibility**
   - `control.Visibility = Visibility.Visible/Collapsed/Hidden` → `control.IsVisible = true/false`。
   - `control.Visibility == Visibility.Visible` → `control.IsVisible`。
   - `Visibility`(枚举类型) 残留 → 换 `IsVisible`(bool)。
   - 报 `"MyHint/MyListItem/MyCheckBox/MyIconButton/MyRadioButton/MyCard/MyButton/StackPanel/Path/MyComboBox/MyComboBoxItem 未包含 Visibility"` → 就是 `.Visibility` 属性要改成 `.IsVisible`。

4. **绑定**
   - `element.SetBinding(Prop, binding)` → `element.Bind(Prop, binding)`（`Bind` 是返回 void 的 `AvaloniaObject.Bind` 扩展）。WPF 的 `Binding` 对象构造方式若报错，用 `new Avalonia.Data.Binding(...)`。
   - 报 `"Path/Control 未包含 SetBinding"` → 换 `Bind(...)`。若原来是 `x.SetBackground(...)` 之类，直接映射。
   - `Binding.DoNothing` → Avalonia 无，删除该赋值或用 `Avalonia.Data.BindingOperations`。除个别签名需 `// [port]`。

5. **MyColor → IBrush**
   - `PCL.ModBase.MyColor` 到 `Avalonia.Media.IBrush` 无隐式转换（报 CS0266 "无法将类型 MyColor 隐式转换为 IBrush。存在一个显式转换"）。
   - 最小改法：在赋值处包一层 `new SolidColorBrush(x)`（MyColor 隐式转 `Color`，SolidColorBrush 有 `(Color)` 构造）；或对已是 Brush 属性的用 `(Brush)x`。**优先用 `new SolidColorBrush(x)`**，这样语义最贴近 WPF 的 Brush 值。若 x 是 `MyColor` 派生变量类型，同样处理。
   - 报 `"无法将类型 MyBitmap 隐式转换为 IBrush/IImage"`：`MyBitmap` → 图片。若是 Background 赋值，看上下文用 `ImageBrush` 或换成可接受的 IImage（通常在别处有 `ToImage()` 或直接 `new ImageBrush(new Image { Source = bit })` 之类）。先读上下文再定，不一定能纯映射；确不能就加 `// [port]` 并用最小改动。

6. **AXAML `Name=` vs `x:Name=` / 命名字段缺失**
   - Avalonia 只对 `Control`(或 StyledElement) 的 `x:Name`/`Name` 生成 code-behind 字段；对 **`ColumnDefinition`/`RowDefinition`/`ScaleTransform`/`TranslateTransform`/`RotateTransform` 等非控件** 不生成字段。
   - 报 `"名称不存在 ColumnRamUsed/ColumnRamGame/ColumnRamEmpty/RowFilenameFormat/.../ProgressLaunchingFinished/ProgressLaunchingUnfinished/TransSelect/AprilPosTrans/AprilScaleTrans"`：
     - 若 AXAML 里是 `Name="Xxx"`，把 `Name=` 改为 `x:Name=`（对 Control 也统一，但不改变非控件）。
     - 若是**列/行定义**（`<ColumnDefinition Name="...">`），Avalonia 不生成字段：在代码里改用所属 Grid 的字段来访问，例如 `((ColumnDefinition)PanGrid.ColumnDefinitions[i])`，或给该 Grid 加 `x:Name` 后用 `Grid.ColumnDefinitions`。**最稳妥：给该 Grid/元素补 `x:Name`，代码里用 grid.ColumnDefinitions[index] 访问并赋 `Width = new GridLength(v, GridUnitType.Star)`**。`GridLength`/`GridUnitType` 在 `Avalonia.Controls`。
     - 若 `Width.Value` 用错（`GridLength` 无 `.Width`，有 `.Value`），确认用 `.Value`。
   - `x:Name` 放 `RenderTransform` 里的 transform（`AprilScaleTrans`/`AprilPosTrans`）不生成字段 → 给所在 Grid 加 `x:Name`，用 `((TransformGroup)grid.RenderTransform)` 访问其 `Children`。

7. **`FindName`**
   - WPF `this.FindName("Xxx")` / `(Type)FindName("Xxx")` → Avalonia `this.FindControl<T>("Xxx")`（`Avalonia.Controls.ControlExtensions.FindControl<T>`）。返回 `T?`。例如 `((MyRadioBox)FindName("RadioLogoType"+i))` → `this.FindControl<MyRadioBox>("RadioLogoType"+i)`。

8. **`ToolTip`**
   - WPF `new MyX { ToolTip = "text" }` / `myX.ToolTip = "text"` → Avalonia 用 `ToolTip.SetTip(myX, "text")`（静态方法）。若代码在对象初始化器里写 `ToolTip = "..."`，改为先 new 再 `ToolTip.SetTip(...)`。**不要编辑 MyListItem/MyCard/MyComboBoxItem 等 Control 类文件**，一律改调用点。加 `// [port] ToolTip->SetTip`。

9. **`Clipboard`**
   - WPF `Clipboard.SetText(...)` / `Clipboard.GetText()` → Avalonia `Avalonia.Application.Current.Clipboard.SetTextAsync(...)` / `.GetTextAsync()`（async）。若方法不能 async，用 `.GetAwaiter().GetResult()`。报 `"不存在名称 Clipboard"` 时先用 `Avalonia.Application.Current.Clipboard` 或 `App.Clipboard`（读上下文）。
   - 报 `BitmapImage`/`BitmapCacheOption` 不存在 → 换 `Avalonia.Media.Imaging.Bitmap`（构造 `new Bitmap(stream)`）。`BitmapCacheOption` 无等价，去掉该选项（`// [port]`）。

10. **`MyScrollViewer`**
   - `VerticalOffset`/`ViewportHeight`/`ExtentHeight` 报缺失 → 若 `MyScrollViewer` 无这些属性，改用 `MyScrollViewer.Offset`(Vector)、`MyScrollViewer.Viewport`(Size)、`MyScrollViewer.Extent`(Size)。读上下文确认属性名。

11. **`RoutedEventHandler` / 事件订阅**
   - WPF `xxx += new RoutedEventHandler(Method)` / 方法组 `+= Method`，参数 5 报 "无法从方法组转换为 RoutedEventHandler" → 因为某事件委托类型不是 `RoutedEventHandler`（WPF 遗留）。若事件是 Avalonia 的 `EventHandler<T>`，改为 `xxx += Method`，且 `Method(object sender, T e)`。若 `RoutedEventHandler` 类型名本身不存在，删掉该类型引用，直接 `+=` 方法组，方法签名改 `(object, EventArgs)` 或对应事件类型。
   - `RoutedEventArgs` → `Avalonia.Interactivity.RoutedEventArgs`（一般已 using）。

12. **CS0411 `AvaloniaProperty.Register<TOwner,TValue>(...)` 无法推断类型参数**
   - `Register<TOwner,TValue>(nameof(X), default(SomeType))` 中 `default(SomeType)` 若写成 `null` 或 `"..."` 字符串常量无法推断 `TValue`。把第 2 参数改成强类型默认：`Register<Owner, string>(nameof(X), string.Empty)` 或 `Register<Owner, object>(nameof(X), null)`。读上下文，给 `TValue` 显式指定，第 2 参给 `default(TValue)` 类型匹配值。

13. **`await` 只能在 async 方法**
   - `await` 用在非 async 方法（CS4033）→ 给方法加 `async` 并确保返回 `Task`/`Task<T>`；若必须同步，用 `.GetAwaiter().GetResult()`。`SystemDialogs.SelectFile`/`SelectFileAsync` 已异步化，调用点 `await`。

14. **桩成员（Stubs.cs 已由主代理准备，勿改）**
   - `ModVideoBack`/`ModMusic`/`ModSkin` 在 Stubs.cs 中定义。若还缺成员，**不要编辑 Stubs.cs**，在报告里说明缺的成员，让主代理补。
   - `ModMain.frmMain.VideoBack` 若报 "FormMain 未包含 VideoBack"：视频背景功能暂缓移植（见 csproj DeferredExcludes 与 FormMain.axaml 的 `[port]` 注释）。按既定模式：把视频相关代码块删除/注释掉，加 `// [port] 视频背景暂缓移植，删除`。**不要凭空造 VideoBack 属性到 FormMain。**
   - `PageLoginProfileSkin.Skin` 报不存在：MySkin（皮肤）已排除（DeferredExcludes），皮肤加载代码在目标页里属暂缓功能。把引用 `ModMain.frmLoginProfileSkin.Skin` 的皮肤加载逻辑块删除/注释掉，加 `// [port] 皮肤功能暂缓移植，删除`。

## 通用要求
- 每改一个非纯映射处，加 `// [port] 原因`。
- 不要重命名 XAML 里的元素名、不改变布局/尺寸/颜色/文本。
- 改完保证语义不变（除暂缓功能删除处）。
- 完成后向主代理报告：改动概要、新增 [port] 标注、无法纯映射需决策项。
