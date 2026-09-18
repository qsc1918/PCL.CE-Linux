using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Markup;

using Avalonia.Metadata;

namespace PCL;
public partial class MyButton : Border
{
    public delegate void ClickEventHandler(object sender, PointerPressedEventArgs e); // 自定义事件

    public enum ColorState
    {
        Normal = 0,
        Highlight = 1,
        Red = 2
    }

    // 自定义事件
    private const int animationColorIn = 100;
    private const int animationColorOut = 200;

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MyButton, string>(
        nameof(Text), string.Empty);

    // 属性穿透
    public new static readonly StyledProperty<Thickness> PaddingProperty = AvaloniaProperty.Register<MyButton, Thickness>(
        nameof(Padding), default(Thickness));

    static MyButton()
    {
        TextProperty.Changed.AddClassHandler<MyButton>((d, e) => d.LabText.Text = (string)e.NewValue);
        PaddingProperty.Changed.AddClassHandler<MyButton>((d, e) => d.PanFore.Padding = (Thickness)e.NewValue);
    }
    
    private ColorState _ColorType = ColorState.Normal; // 配色方案

    // 鼠标点击判定（务必放在点击事件之后，以使得 Button_PointerReleased 先于 Button_PointerExited 执行）
    

    // 自定义属性
    public int Uuid = ModBase.GetUuid();

    public MyButton()
    {
        InitializeComponent();

        PointerEntered += RefreshColor;
        PointerExited += RefreshColor;
        Loaded += RefreshColor;
        // [port] IsEnabledChanged -> Avalonia PropertyChanged 上的 IsEnabledProperty
        this.PropertyChanged += (_, e) => { if (e.Property == IsEnabledProperty) RefreshColor(); };
        PointerReleased += Button_PointerReleased;
        PointerPressed += Button_MouseDown;
        PointerEntered += (_, _) => Button_PointerEntered();
        PointerReleased += (_, _) => Button_PointerReleased();
        PointerExited += (_, _) => Button_PointerExited();
    }
    // [port] WPF 类级 [ContentProperty("Inlines")] 在 Avalonia 会按实例类型路由根 XAML 子元素到 Inlines，
    // 而此时 LabText/LabTitle 等命名字段尚未初始化 → 填充期 NullReferenceException。
    // WPF 中根标签 <Border>/<Grid> 按基类内容属性(Child/Children)路由，故此处移除 [Content] 以还原该语义。

    public InlineCollection Inlines => LabText.Inlines;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    } // 显示文本

    public Thickness TextPadding
    {
        get => LabText.Padding;
        set => LabText.Padding = value;
    }

    public ColorState ColorType
    {
        get => _ColorType;
        set
        {
            _ColorType = value;
            RefreshColor();
        }
    }

    public new Thickness Padding
    {
        get => PanFore.Padding;
        set => PanFore.Padding = value;
    }

    public Transform RealRenderTransform
    {
        get => (Transform)PanFore.RenderTransform;
        set => PanFore.RenderTransform = value;
    }

    // 声明
    public event ClickEventHandler? Click;

    private string GetBorderBrushResourceKey()
    {
        return ColorType switch
        {
            ColorState.Normal => IsPointerOver ? "ColorBrush3" : "ColorBrush1",
            ColorState.Highlight => IsPointerOver ? "ColorBrush3" : "ColorBrush2",
            ColorState.Red => IsPointerOver ? "ColorBrushRedLight" : "ColorBrushRedDark",
            _ => "ColorBrush1"
        };
    }

    private void StartBorderBrushAnimation(string resourceKey, int duration)
    {
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaColor(PanFore, BorderBrushProperty, resourceKey, duration)
            }, "MyButton Color " + Uuid);
    }

    private void RefreshColor(object obj = null, object e = null)
    {
        try
        {
            if (ControlVisualHelpers.ShouldAnimate(this)) // 防止默认属性变更触发动画
            {
                if (IsEnabled)
                    StartBorderBrushAnimation(GetBorderBrushResourceKey(), IsPointerOver ? animationColorIn : animationColorOut);
                else
                    // 不可用（Gray 4）
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaColor(PanFore, BorderBrushProperty,
                                ThemeManager.colorGray4 - new ModBase.MyColor(PanFore.BorderBrush), animationColorOut)
                        }, "MyButton Color " + Uuid);
            }
            else
            {
                ModAnimation.AniStop("MyButton Color " + Uuid);
                if (IsEnabled)
                    PanFore.SetResourceReference(BorderBrushProperty, GetBorderBrushResourceKey());
                else
                    PanFore.BorderBrush = ThemeManager.colorGray4.ToBrush();
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "刷新按钮颜色出错");
        }
    }

    // 实现自定义事件
    private bool isMouseDown = false;
    private void Button_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!isMouseDown)
            return;
        ModBase.Log("[Control] 按下按钮：" + Text);
        // [port] 原 Click 事件携带 PointerPressedEventArgs；由 PointerReleased 触发时传入 null（与 MyTextButton 保持一致）
        Click?.Invoke(sender, null);
        ModMain.RaiseCustomEvent(this);
    }

    private void Button_MouseDown(object sender, PointerPressedEventArgs e)
    {
        isMouseDown = true;
        Focus();
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScaleTransform(PanFore, 0.955d - ((ScaleTransform)PanFore.RenderTransform).ScaleX, 80,
                    ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.ExtraStrong)),
                ModAnimation.AaScaleTransform(PanFore, -0.01d, 700, ease: new ModAnimation.AniEaseOutFluent())
            }, "MyButton Scale " + Uuid);
    }

    private void Button_PointerEntered()
    {
        ModAnimation.AniStart(
            ModAnimation.AaColor(PanFore, BackgroundProperty,
                _ColorType == ColorState.Red ? "ColorBrushRedBack" : "ColorBrush7", animationColorIn),
            "MyButton Background " + Uuid);
    }

    private void Button_PointerReleased()
    {
        if (!isMouseDown)
            return;
        isMouseDown = false;
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScaleTransform(PanFore, 1d - ((ScaleTransform)PanFore.RenderTransform).ScaleX, 300, 10,
                    new ModAnimation.AniEaseOutFluent())
            }, "MyButton Scale " + Uuid);
    }

    private void Button_PointerExited()
    {
        ModAnimation.AniStart(
            ModAnimation.AaColor(PanFore, BackgroundProperty, "ColorBrushHalfWhite", animationColorOut),
            "MyButton Background " + Uuid);
        if (!isMouseDown)
            return;
        isMouseDown = false;
        ModAnimation.AniStart(
            ModAnimation.AaScaleTransform(PanFore, 1d - ((ScaleTransform)PanFore.RenderTransform).ScaleX, 800,
                ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)), "MyButton Scale " + Uuid);
    }
}
