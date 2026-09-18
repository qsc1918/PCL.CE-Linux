using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Input;

namespace PCL;

public class MyScrollBar : ScrollBar
{
    // 基础

    public int Uuid = ModBase.GetUuid();

    private bool _isMouseCaptured;

    public MyScrollBar()
    {
        // [port] WPF IsEnabledChanged / IsVisibleChanged / GotMouseCapture / LostMouseCapture 在 Avalonia 无同名事件；
        // 用 PropertyChanged（监听 IsEnabled/IsVisible）+ 指针按下状态近似实现相同刷新时机
        PropertyChanged += (_, e) =>
        {
            if (e.Property == IsEnabledProperty || e.Property == IsVisibleProperty)
                RefreshColor();
        };
        PointerEntered += (_, _) => RefreshColor();
        PointerExited += (_, _) => RefreshColor();
        PointerPressed += (_, _) => { _isMouseCaptured = true; RefreshColor(); };
        PointerReleased += (_, _) => { _isMouseCaptured = false; RefreshColor(); };
        PointerCaptureLost += (_, _) => { _isMouseCaptured = false; RefreshColor(); };
    }

    // 指向动画

    private void RefreshColor()
    {
        try
        {
            // 判断当前颜色
            double newOpacity;
            string newColor;
            int time;
            if (!IsVisible)
            {
                newOpacity = 0d;
                time = 20; // 防止错误的尺寸判断导致闪烁
                newColor = "ColorBrush4";
            }
            // [port] WPF IsMouseCaptureWithin → Avalonia 无直接属性，用指针按下状态近似
            else if (_isMouseCaptured)
            {
                newOpacity = 1d;
                newColor = "ColorBrush4";
                time = 50;
            }
            else if (IsPointerOver)
            {
                newOpacity = 0.9d;
                newColor = "ColorBrush3";
                time = 130;
            }
            else
            {
                newOpacity = 0.5d;
                newColor = "ColorBrush4";
                time = 180;
            }

            // 触发颜色动画
            if (IsLoaded && ModAnimation.AniControlEnabled == 0) // 防止默认属性变更触发动画
            {
                // 有动画
                ModAnimation.AniStart(
                    new[]
                    {
                        ModAnimation.AaColor(this, ForegroundProperty, newColor, time),
                        ModAnimation.AaOpacity(this, newOpacity - Opacity, time)
                    }, "MyScrollBar Color " + Uuid);
            }
            else
            {
                // 无动画
                ModAnimation.AniStop("MyScrollBar Color " + Uuid);
                this.SetResourceReference(ForegroundProperty, newColor);
                Opacity = newOpacity;
            }
        }

        catch (Exception ex)
        {
            ModBase.Log(ex, "滚动条颜色改变出错");
        }
    }
}
