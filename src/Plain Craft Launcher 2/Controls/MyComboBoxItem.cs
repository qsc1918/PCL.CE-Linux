using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;

namespace PCL;

public class MyComboBoxItem : ComboBoxItem
{
    // 指向动画

    private const int animationTimeIn = 100;
    private const int animationTimeOut = 300;
    private string backColorName;
    private double fontOpacity;

    // 基础

    public int Uuid = ModBase.GetUuid();

    public MyComboBoxItem()
    {
        // [port] WPF 框架样式/触发器块移除：视觉状态由 RefreshColor 经资源引用/IsSelected/IsEnabled 实现
        // [port] WPF 的 Selected/Unselected 事件 -> Avalonia 的 IsSelectedProperty 变更
        this.PropertyChanged += (_, e) => { if (e.Property == IsSelectedProperty) RefreshColor(); };
        PointerMoved += (_, _) => RefreshColor();
        PointerExited += (_, _) => RefreshColor();
        // [port] IsEnabledChanged -> Avalonia PropertyChanged 上的 IsEnabledProperty
        this.PropertyChanged += (_, e) => { if (e.Property == IsEnabledProperty) RefreshColor(); };
        PointerReleased += MyComboBoxItem_PointerReleased;
    }

    private void RefreshColor()
    {
        // 判断当前颜色
        string newBackColorName;
        double newFontOpacity;
        int time;
        if (IsSelected)
        {
            newBackColorName = "ColorBrush6";
            newFontOpacity = 1d;
            time = animationTimeIn;
        }
        else if (IsPointerOver)
        {
            newBackColorName = "ColorBrush8";
            newFontOpacity = 1d;
            time = animationTimeIn;
        }
        else if (IsEnabled)
        {
            newBackColorName = "ColorBrushTransparent";
            newFontOpacity = 1d;
            time = animationTimeOut;
        }
        else
        {
            newBackColorName = "ColorBrushTransparent";
            newFontOpacity = 0.4d;
            time = animationTimeOut;
        }

        if ((backColorName ?? "") == (newBackColorName ?? "") && fontOpacity == newFontOpacity)
            return;
        backColorName = newBackColorName;
        fontOpacity = newFontOpacity;
        // 触发颜色动画
        if (IsLoaded && ModAnimation.AniControlEnabled == 0) // 防止默认属性变更触发动画
        {
            // 有动画
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaColor(this, BackgroundProperty, backColorName, time),
                    ModAnimation.AaOpacity(this, fontOpacity - Opacity, time)
                }, "ComboBoxItem Color " + Uuid);
        }
        else
        {
            // 无动画
            ModAnimation.AniStop("ComboBoxItem Color " + Uuid);
            this.SetResourceReference(BackgroundProperty, backColorName);
            Opacity = fontOpacity;
        }
    }

    public override string ToString()
    {
        return Content?.ToString() ?? "";
    }

    public static implicit operator string(MyComboBoxItem value)
    {
        return value.Content?.ToString() ?? "";
    }

    private void MyComboBoxItem_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        ModBase.Log("[Control] 选择下拉列表项：" + ToString());
    }
}
