using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;

namespace PCL;

public class MyTextButton : Label
{
    public delegate void ClickEventHandler(object sender, EventArgs e);

    // 指向动画

    private const int animationTimeIn = 100;
    private const int animationTimeOut = 200;

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MyTextButton, string>(
        nameof(Text), string.Empty);

    static MyTextButton()
    {
        TextProperty.Changed.AddClassHandler<MyTextButton>((d, e) =>
        {
            if (Equals(e.OldValue, e.NewValue)) return;
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaOpacity(d, -d.Opacity, 50),
                    ModAnimation.AaCode(() => d.Content = e.NewValue, after: true),
                    ModAnimation.AaOpacity(d, 1d, 170)
                }, "MyTextButton Text " + d.Uuid);
        });
    }
    
    private string colorName;

    // 鼠标事件

    public bool isMouseDown;

    // 基础

    public int Uuid = ModBase.GetUuid();

    public MyTextButton()
    {
        this.SetResourceReference(ForegroundProperty, "ColorBrush1");
        Background = ThemeManager.colorSemiTransparent.ToBrush();
        // [port] PreviewPointerPressed (WPF tunnel) -> Avalonia AddHandler(Tunnel)
        AddHandler(InputElement.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>(MyTextButton_PointerPressed), RoutingStrategies.Tunnel, true);
        PointerExited += (_, _) => MyTextButton_PointerExited();
        // [port] PreviewPointerReleased (WPF tunnel) -> Avalonia AddHandler(Tunnel)
        AddHandler(InputElement.PointerReleasedEvent, new EventHandler<PointerReleasedEventArgs>(MyTextButton_PointerReleased), RoutingStrategies.Tunnel, true);
        PointerEntered += (_, _) => RefreshColor();
        PointerExited += (_, _) => RefreshColor();
        // [port] IsEnabledChanged -> Avalonia PropertyChanged 上的 IsEnabledProperty
        this.PropertyChanged += (_, e) => { if (e.Property == IsEnabledProperty) RefreshColor(); };
        PointerPressed += (_, _) => RefreshColor();
        PointerReleased += (_, _) => RefreshColor();
    }

    // 文本

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public event ClickEventHandler? Click;

    private (string ForeName, int Time) GetVisualState()
    {
        if (isMouseDown)
            return ("ColorBrush4", 30);
        if (IsPointerOver)
            return ("ColorBrush3", animationTimeIn);
        return ("ColorBrush1", animationTimeOut);
    }

    private void MyTextButton_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        isMouseDown = true;
        e.Handled = true;
    }

    private void MyTextButton_PointerExited()
    {
        isMouseDown = false;
    }

    private void MyTextButton_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!isMouseDown) return;
        isMouseDown = false;
        ModBase.Log("[Control] 按下文本按钮：" + Text);
        Click?.Invoke(this, null);
        ModMain.RaiseCustomEvent(this);
        e.Handled = true;
    }

    private void RefreshColor()
    {
        var (ForeName, Time) = GetVisualState();

        // 重复性验证
        if ((colorName ?? "") == (ForeName ?? ""))
            return;
        colorName = ForeName;
        // 触发颜色动画
        ControlVisualHelpers.AnimateColorOrSetResource(this, ForegroundProperty, ForeName, Time,
            "MyTextButton Color " + Uuid, ControlVisualHelpers.ShouldAnimate(this));
    }
}
