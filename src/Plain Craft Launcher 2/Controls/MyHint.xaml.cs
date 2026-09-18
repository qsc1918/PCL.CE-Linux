using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Markup;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.UI.Theme;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;

using Avalonia.Metadata;

namespace PCL;
public partial class MyHint : Border
{
    // 配色
    public enum Themes
    {
        Blue = 0,
        Red = 1,
        Yellow = 2
    }

    public static readonly StyledProperty<bool> IsWarnProperty = AvaloniaProperty.Register<MyHint, bool>(
        nameof(IsWarn), true);

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MyHint, string>(
        nameof(Text), string.Empty);

    static MyHint()
    {
        IsWarnProperty.Changed.AddClassHandler<MyHint>((d, e) =>
        {
            d.Theme = e.NewValue is not null ? Themes.Red : Themes.Blue;
        });
        TextProperty.Changed.AddClassHandler<MyHint>((d, e) => d.LabText.Text = (string)e.NewValue);
    }

    // 触发点击事件
    private bool isMouseDown;
    public int Uuid = ModBase.GetUuid();

    public MyHint()
    {
        InitializeComponent();
        UpdateUI();
        Loaded += (_, _) => UpdateUI();
        Loaded += MyHint_Loaded;
        PointerReleased += MyHint_PointerReleased;
        PointerPressed += MyHint_MouseDown;
        PointerExited += (_, _) => MyHint_PointerExited();
        Unloaded += (_, _) => Dispose();
    }

    // 边框
    public bool HasBorder
    {
        get => BorderThickness.Top > 0d;
        set
        {
            if (value)
                BorderThickness = new Thickness(3d, ModBase.GetWPFSize(1d), ModBase.GetWPFSize(1d), ModBase.GetWPFSize(1d));
            else
                BorderThickness = new Thickness(3d, 0d, 0d, 0d);
        }
    }

    public Themes Theme
    {
        get => field;
        set
        {
            field = value;
            UpdateUI();
        }
    } = Themes.Red;

    [Obsolete("IsWarn 已过时。请换用 Theme 属性。")]
    public bool IsWarn
    {
        get => Theme == Themes.Red;
        set => Theme = value ? Themes.Red : Themes.Blue;
    }

    // 文本
    // [port] WPF 类级 [ContentProperty("Inlines")] 在 Avalonia 会按实例类型路由根 XAML 子元素到 Inlines，
    // 而此时 LabText/LabTitle 等命名字段尚未初始化 → 填充期 NullReferenceException。
    // WPF 中根标签 <Border>/<Grid> 按基类内容属性(Child/Children)路由，故此处移除 [Content] 以还原该语义。
    public InlineCollection Inlines => LabText.Inlines;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // 关闭按钮
    public bool CanClose
    {
        get => BtnClose.IsVisible == true;
        set => BtnClose.IsVisible = value ? true : false;
    }

    public string RelativeSetup { get; set; } = "";

    private void UpdateUI()
    {
        var hue = default(double);
        switch (Theme)
        {
            case Themes.Blue:
            {
                hue = 210d;
                break;
            }
            case Themes.Red:
            {
                hue = 355d;
                break;
            }
            case Themes.Yellow:
            {
                hue = 40d;
                break;
            }
        }

        var s = ThemeService.CurrentTone;
        // [port] WPF MyColor(隐式转 Brush) → Avalonia 需显式 .ToBrush()
        Background = new ModBase.MyColor().FromHSL2(hue, 90, s.L7 * 100).ToBrush();
        BorderBrush = new ModBase.MyColor().FromHSL2(hue, 90, s.L2 * 100).ToBrush();
        LabText.Foreground = new ModBase.MyColor().FromHSL2(hue, 90, s.L2 * 100).ToBrush();
        // [port] MyIconButton.Foreground 类型为 SolidColorBrush，ToBrush() 返回 Brush，需显式转换
        BtnClose.Foreground = (SolidColorBrush)new ModBase.MyColor().FromHSL2(hue, 90, s.L2 * 100).ToBrush();

        // 根据提示气泡对齐方向刷新边框
        // 此处依赖 HasBorder 的副作用进行范围检查
        HasBorder = HasBorder;
    }

    private void MyHint_Loaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ColorModeChanged += (v, theme) => _ThemeChanged(v, theme);
        if (CanClose && ConfigService.TryGetConfigItemNoType(RelativeSetup, out var item) && item.GetValueNoType() is not null)
            IsVisible = false; // [port] WPF Visibility = false → Avalonia IsVisible = false
    }

    private void BtnClose_Click(object sender, EventArgs e)
    {
        if (ConfigService.TryGetConfigItemNoType(RelativeSetup, out var item))
            item.SetValueNoType(true);
        ModAnimation.AniDispose(this, false);
    }

    private void MyHint_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!isMouseDown)
            return;
        isMouseDown = false;
        ModBase.Log("[Control] 按下提示条" + (string.IsNullOrEmpty(Name) ? "" : "：" + Name));
        e.Handled = true;
        ModMain.RaiseCustomEvent(this);
    }

    private void MyHint_MouseDown(object sender, PointerPressedEventArgs e)
    {
        isMouseDown = true;
    }

    private void MyHint_PointerExited()
    {
        isMouseDown = false;
    }

    private void _ThemeChanged(bool isDarkMode, ColorTheme theme)
    {
        UpdateUI();
    }

    private void Dispose()
    {
        ThemeService.ColorModeChanged -= _ThemeChanged;
    }
}

public static partial class ModAnimation
{
    public static void AniDispose(MyHint control, bool removeFromChildren, ParameterizedThreadStart callBack = null)
    {
        if (!control.IsHitTestVisible)
            return;
        control.IsHitTestVisible = false;
        AniStart(new[]
        {
            AaScaleTransform(control, -0.08d, 200, ease: new AniEaseInFluent()),
            AaOpacity(control, -1, 200, ease: new AniEaseOutFluent()),
            AaHeight(control, -control.Bounds.Height, 150, 100, new AniEaseOutFluent()),
            AaCode(() =>
            {
                if (removeFromChildren)
                    ((Panel)control.Parent).Children.Remove(control);
                else
                    control.IsVisible = false;
                if (callBack is not null)
                    callBack(control);
            }, after: true)
        }, "MyCard Dispose " + control.Uuid);
    }
}
