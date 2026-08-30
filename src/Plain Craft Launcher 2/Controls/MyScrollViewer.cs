using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Input;
using PCL.Core.UI.Controls;

namespace PCL;

public class MyScrollViewer : ScrollViewer
{
    private double realOffset;

    public MyScrollBar scrollBar;

    public MyScrollViewer()
    {
        PreviewMouseWheel += MyScrollViewer_PreviewMouseWheel;
        ScrollChanged += MyScrollViewer_ScrollChanged;
        IsVisibleChanged += MyScrollViewer_IsVisibleChanged;
        Loaded += (_, _) => Load();
        // [port] WPF PreviewGotKeyboardFocus 事件在 Avalonia 无直接对应，滚轮跟随焦点逻辑暂缓
    }

    public double DeltaMult { get; set; } = 1d;

    private void MyScrollViewer_PreviewMouseWheel(object sender, PointerWheelEventArgs e)
    {
        if (e.Delta == 0 || ScrollableHeight <= 0d)
            return;

        var src = e.Source;
        if (Content is Control element && element.TemplatedParent is null)
        {
            switch (src)
            {
                case ComboBox { IsDropDownOpen: true }:
                case TextBox { AcceptsReturn: true }:
                case ComboBoxItem:
                case CheckBox:
                    return;
            }
        }

        e.Handled = true;
        PerformVerticalOffsetDelta(-e.Delta);

        Tooltip.Dismiss();
    }

    public void PerformVerticalOffsetDelta(double delta)
    {
        ModAnimation.AniStart(ModAnimation.AaDouble(animDelta =>
        {
            realOffset = ModBase.MathClamp(realOffset + (double)animDelta, 0d, ExtentHeight - ActualHeight);
            ScrollToVerticalOffset(realOffset);
        }, delta * DeltaMult, 300, 0, new ModAnimation.AniEaseOutFluent((ModAnimation.AniEasePower)6), false));
    }

    private void MyScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        realOffset = VerticalOffset;
        if (ModMain.frmMain is not null &&
            (e.VerticalChange != 0 || e.ViewportHeightChange != 0))
            ModMain.frmMain.BtnExtraBack.ShowRefresh();
    }

    private void MyScrollViewer_IsVisibleChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        ModMain.frmMain.BtnExtraBack.ShowRefresh();
    }

    private void Load()
    {
        scrollBar = (MyScrollBar)GetTemplateChild("PART_VerticalScrollBar");
    }

    // [port] WPF PreviewGotKeyboardFocus（阻止获得焦点时自动滚动 #3854）在 Avalonia 无直接对应，暂缓
    private void MyScrollViewer_PreviewGotKeyboardFocus(object sender, Avalonia.Input.KeyEventArgs e)
    {
        // 暂缓
    }
}