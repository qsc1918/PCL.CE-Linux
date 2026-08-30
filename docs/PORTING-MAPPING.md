# WPF → Avalonia 移植规则表（PORTING-MAPPING）

> 本文件是整个移植的规范依据。所有源码从 PCL-CE 上游逐字复制后按本表转换；
> 规则保证：布局、颜色、尺寸、文案、交互逻辑与上游一一对应，仅框架 API 名称映射。
> 标注【手】的项目需要人工判断；标注【跳】的按用户指示暂不移植。

## 一、XAML 通用映射（【手】逐文件应用，【脚本】部分可脚本化）

| WPF | Avalonia | 说明 |
|---|---|---|
| `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` | `xmlns="https://github.com/avaloniaui"` | 【脚本】 |
| `<Page>` | `<UserControl>` | MyPage 基类改继承 UserControl |
| `<Style TargetType="{x:Type X}">` | `<Style Selector="X">` | 【手】 |
| `BasedOn="{StaticResource K}"` | `BasedOn="{StaticResource K}"` 属性移到 Style 标签 | 【手】 |
| `<Trigger Property="IsMouseOver" Value="True">` | `<Style Selector="^:pointerover">` | 【手】 |
| `<Trigger Property="IsEnabled" Value="False">` | `^:disabled`；IsFocused→`:focus`；IsChecked→`:checked` | 【手】 |
| `Style.Triggers` / `EventTrigger` / `Storyboard` | 嵌套 Style 选择器 + Avalonia Animation（个别删除并记录） | 【手】 |
| `<Window WindowStyle=... WindowChrome TaskbarItemInfo>` | `SystemDecorations` / ExtendClientArea；Chrome 与任务栏项删除（B2/C4） | 【手】 |
| `pack://application:,,,/Assembly;component/X` | `avares://Assembly/X` | 【脚本】 |
| `mc:Ignorable`/`d:Design*` 设计期属性 | 删除（运行期无影响） | 【脚本】 |
| `RenderOptions.BitmapScalingMode` | 同名（Avalonia 支持） | 保持 |
| `SnapsToDevicePixels` | 删除（Avalonia 无此概念，行为默认） | 【脚本】 |
| `UseLayoutRounding` | 同名 | 保持 |
| `x:Name` | 同名（Avalonia 编译期生成字段，code-behind 引用方式不变） | 保持 |
| `InitializeComponent()` | 同名（Avalonia 自动生成） | 保持 |
| `DropShadowEffect/BlurEffect` | 同名（若编译器要求实验开关则开启；不可用则整条效果删除并记录） | 【手】 |
| `DropShadowBitmapEffect`（已废弃 API） | 换 `DropShadowEffect`（等价参数） | 【手】 |
| `MediaElement`（视频背景 C1） | 删除（暂不移植） | 【跳】 |

## 二、C# 通用映射

### using 替换【脚本】
| WPF | Avalonia |
|---|---|
| `using System.Windows;` | `using Avalonia;` + `Avalonia.Controls;` + `Avalonia.Media;` + `Avalonia.Interactivity;` + `Avalonia.Layout;` |
| `using System.Windows.Controls;` | `using Avalonia.Controls;` + `Avalonia.Controls.Shapes;` |
| `using System.Windows.Media;` | `using Avalonia.Media;` |
| `using System.Windows.Media.Imaging;` | `using Avalonia.Media.Imaging;` |
| `using System.Windows.Media.Effects;` | `using Avalonia.Media;` |
| `using System.Windows.Media.Animation;` | `using Avalonia.Animation;` |
| `using System.Windows.Input;` | `using Avalonia.Input;` |
| `using System.Windows.Data;` | `using Avalonia.Data;` + `Avalonia.Data.Converters;` |
| `using System.Windows.Threading;` | `using Avalonia.Threading;` |
| `using System.Windows.Markup;` | `using Avalonia.Markup;` |
| `using System.Windows.Documents;` | `using Avalonia.Controls.Documents;` |
| `using Microsoft.Xaml.Behaviors;` | `using Avalonia.Xaml.Interactivity;` |
| `using System.Windows.Shell/Interop/Resources;` | 注释掉，【手】处理 |
| `using System.Drawing;` | 不动，【手】换 SkiaSharp（B5） |

### 类型重命名【脚本，全字匹配】
| WPF | Avalonia |
|---|---|
| `FrameworkElement` | `Control` |
| `UIElement` | `Control` |
| `DependencyObject` | `AvaloniaObject` |
| `DependencyProperty` | `AvaloniaProperty` |
| `DependencyPropertyChangedEventArgs` | `AvaloniaPropertyChangedEventArgs` |
| `.ActualWidth` / `.ActualHeight` | `.Bounds.Width` / `.Bounds.Height` |

### 依赖属性注册【手，每个文件】
```csharp
// WPF
public static readonly DependencyProperty ThemeProperty =
    DependencyProperty.Register("Theme", typeof(string), typeof(MyButton), new PropertyMetadata(""));

// Avalonia
public static readonly StyledProperty<string> ThemeProperty =
    AvaloniaProperty.Register<MyButton, string>("Theme", "");
```
`RegisterAttached` → `AttachedProperty<T>.RegisterAttached`。回调 `PropertyMetadata(cb)` → `param (_, __) => cb(...)`。

### 事件与输入【手，逐个签名】
| WPF | Avalonia |
|---|---|
| `MouseLeftButtonDown/Up` | `PointerPressed` / `PointerReleased` |
| `MouseMove` / `MouseWheel` | `PointerMoved` / `PointerWheelChanged` |
| `MouseEnter/Leave` | `PointerEntered/Exited` |
| `MouseButtonEventArgs` | `PointerPressedEventArgs` 等（GetPosition 需传 visual） |
| `Keyboard.Modifiers` | `KeyModifiers` |
| `DragEnter/Over/Drop` | `DragDrop.AddHandler` + `DragEventArgs`（Data→Data) |

### 其余【手】速查
| WPF | Avalonia |
|---|---|
| `Dispatcher.*` | `Dispatcher.UIThread.Invoke/Post/InvokeAsync` |
| `FindResource/TryFindResource` | `TryGetResource(key, theme, out v)`（包一层扩展保持调用点不变） |
| `SetResourceReference` | 同名（StyledElement 支持） |
| `VisualTreeHelper` | `Avalonia.VisualTree` 扩展方法 |
| `BitmapImage/BitmapFrame` | `new Bitmap(stream)` / `Image` 控件 |
| `RenderTargetBitmap` | 同名（Render(visual)） |
| `FormattedText` | 同名（ctor 签名不同） |
| `DrawingVisual` | `DrawingContext` 即时绘制 |
| `Clipboard`（WinForms/WPF） | `TopLevel.GetTopLevel(ctrl).Clipboard` |
| `OpenFileDialog 等` | `StorageProvider` API（SystemDialogs 单文件适配） |
| `Application.Current.Shutdown` | `Application.Current.Shutdown()`（存在） |
| `Environment/IO/Net/Json` | 不变（跨平台） |

## 三、动画
- 应用侧动画引擎 ModAnimation（AniStart/AniTimer/AniData）：**逻辑原样保留**，仅框架 API 映射（FrameworkElement→Control、Transforms 同名、dynamic 调用 Avalonia 同样有效）。
- PCL.Core 动画框架（AnimationService 等 ~30 文件）：保留，仅替换 4 个 WPF 底座（WpfAnimatable→AvaloniaAnimatable、WpfUIAccessProvider→AvaloniaUIAccessProvider、CompositionTarget 渲染时钟、WinMMClock→托管周期时钟）。
- XAML 内 Storyboard（Application.xaml 的 ComboBox 箭头旋转）：【手】改 Avalonia 动画或降级为无动画并记录。

## 四、跳过清单（用户指示，暂不移植）
联机（ModLink/PageToolsGameLink/PageToolsTest）、换皮肤（MySkin/ModSkin）、视频背景（ModVideoBack/MediaElement）、音乐播放器（ModMusic/NAudio）、导出崩溃报告、崩溃分析（Modules/Minecraft/CrashAnalysis/**）。这些文件从编译排除（csproj），保留在仓库以备将来。

## 五、Windows 专属（Linux 优雅降级，#if WINDOWS 保留源码）
Registry、DPAPI/CngProtectedData、WinRT Toast、System.Management/WMI、Win32 P/Invoke 12 处、app.manifest、TaskbarItemInfo。Java 注册表扫描等 Windows 分支用 `OperatingSystem.IsWindows()` 运行时守卫；编译级隔离用 `#if WINDOWS`（当前 Linux 目标不定义该常量）。
