using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;
using PCL.Core.UI.Controls;

namespace PCL;

public class MyScrollViewer : ScrollViewer
{
    private double realOffset;

    public MyScrollBar scrollBar;

    public MyScrollViewer()
    {
        // [port] WPF PreviewMouseWheel（隧道）→ Avalonia 用 AddHandler + RoutingStrategies.Tunnel 保留隧道语义
        AddHandler(PointerWheelChangedEvent, (EventHandler<PointerWheelEventArgs>)MyScrollViewer_PreviewMouseWheel, RoutingStrategies.Tunnel);
        ScrollChanged += MyScrollViewer_ScrollChanged;
        // [port] WPF IsVisibleChanged → Avalonia 用 PropertyChanged 监听 IsVisibleProperty
        PropertyChanged += (_, e) => { if (e.Property == IsVisibleProperty) MyScrollViewer_IsVisibleChanged(this, e); };
        Loaded += (_, _) => Load();
        // [port] WPF GetTemplateChild("PART_VerticalScrollBar") → Avalonia 在 TemplateApplied 时通过 NameScope.Find 提取模板部件
        TemplateApplied += (_, e) => { scrollBar = e.NameScope.Find("PART_VerticalScrollBar") as MyScrollBar; };
        // [port] WPF PreviewGotKeyboardFocus 事件在 Avalonia 无直接对应，滚轮跟随焦点逻辑暂缓
    }

    public double DeltaMult { get; set; } = 1d;

    // [port] 滚轮量纲换算。
    // 上游 WPF 的 MouseWheelEventArgs.Delta 以"一格 = ±120"计（WPF 传统单位）；
    // Avalonia 的 PointerWheelEventArgs.Delta 把一格归一化为 ±1。
    // 若直接照用，PerformVerticalOffsetDelta 收到的位移只有上游的 1/120，表现为"滚轮几乎滚不动"。
    // 这里换算回 WPF 量纲，使滚动距离与上游一致（DeltaMult 的语义也保持不变）。
    private const double WpfWheelDeltaPerNotch = 120d;

    private void MyScrollViewer_PreviewMouseWheel(object sender, PointerWheelEventArgs e)
    {
        // [port] WPF e.Delta（int）→ Avalonia Vector，取 Y 分量比较；ScrollableHeight → Extent.Height - Viewport.Height
        if (e.Delta.Y == 0 || (Extent.Height - Viewport.Height) <= 0d)
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
        PerformVerticalOffsetDelta(-e.Delta.Y * WpfWheelDeltaPerNotch);

        // [port] WPF Tooltip.Dismiss() 在 Avalonia 12 无对应静态方法（无全局 Dismiss API），
        // 且 Avalonia 在滚动/交互时会自动收起工具提示，此处保留为无副作用空操作。
        // code kept for readability of the original intent.
    }

    public void PerformVerticalOffsetDelta(double delta)
    {
        ModAnimation.AniStart(ModAnimation.AaDouble(animDelta =>
        {
            // [port] WPF ScrollToVerticalOffset/ExtentHeight/ActualHeight → Avalonia Offset/Extent/Bounds
            realOffset = ModBase.MathClamp(realOffset + (double)animDelta, 0d, Extent.Height - Bounds.Height);
            Offset = new Vector(Offset.X, realOffset);
        }, delta * DeltaMult, 300, 0, new ModAnimation.AniEaseOutFluent((ModAnimation.AniEasePower)6), false));
    }

    private void MyScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // [port] WPF VerticalOffset → Avalonia Offset.Y；VerticalChange/ViewportHeightChange → OffsetDelta.Y/ViewportDelta.Y
        realOffset = Offset.Y;
        if (ModMain.frmMain is not null &&
            (e.OffsetDelta.Y != 0 || e.ViewportDelta.Y != 0))
            ModMain.frmMain.BtnExtraBack.ShowRefresh();
    }

    private void MyScrollViewer_IsVisibleChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        ModMain.frmMain.BtnExtraBack.ShowRefresh();
    }

    private void Load()
    {
        // [port] WPF GetTemplateChild("PART_VerticalScrollBar") 在 Avalonia 无对应；
        // 模板部件在构造函数的 TemplateApplied 事件里通过 NameScope.Find 提取。
    }

    // [port] WPF PreviewGotKeyboardFocus（阻止获得焦点时自动滚动 #3854）在 Avalonia 无直接对应，暂缓
    private void MyScrollViewer_PreviewGotKeyboardFocus(object sender, Avalonia.Input.KeyEventArgs e)
    {
        // 暂缓
    }
}
