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

    public static readonly AvaloniaProperty TextProperty = AvaloniaProperty.Register("Text", typeof(string),
        typeof(MyTextButton), new PropertyMetadata("", (sender, e) =>
        {
            if (Equals(e.OldValue, e.NewValue)) return;
            var button = (MyTextButton)sender;
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaOpacity(button, -button.Opacity, 50),
                    ModAnimation.AaCode(() => button.Content = e.NewValue, after: true),
                    ModAnimation.AaOpacity(button, 1d, 170)
                }, "MyTextButton Text " + button.Uuid);
        }));
    
    private string colorName;

    // 鼠标事件

    public bool isMouseDown;

    // 基础

    public int Uuid = ModBase.GetUuid();

    public MyTextButton()
    {
        SetResourceReference(ForegroundProperty, "ColorBrush1");
        Background = ThemeManager.colorSemiTransparent;
        PreviewPointerPressed += MyTextButton_PointerPressed;
        PointerExited += (_, _) => MyTextButton_PointerExited();
        PreviewPointerReleased += MyTextButton_PointerReleased;
        PointerEntered += (_, _) => RefreshColor();
        PointerExited += (_, _) => RefreshColor();
        IsEnabledChanged += (_, _) => RefreshColor();
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
        if (IsMouseOver)
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

    private void MyTextButton_PointerReleased(object sender, PointerPressedEventArgs e)
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
