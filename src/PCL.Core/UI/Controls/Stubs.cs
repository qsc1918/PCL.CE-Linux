using Avalonia;
using Avalonia.Controls;

namespace PCL.Core.UI.Controls;

// [port] 桩：BlurBorder / MotdRenderer 的轻量占位。
// 原 WPF 实现（BlurBorder.cs / MotdRenderer.xaml.cs）因迁移到 Avalonia 后无法直接移植而被排除，
// 这里以最小 API 面保证引用它们的 XAML/C# 可编译。功能为"暂缓"，行为与原稿不符需在运行时验证。
// 注意：此文件必须在 PCL.Core 程序集（AssemblyName=PCL.Core）且命名空间为 PCL.Core.UI.Controls，
// 以匹配 XAML 中的 clr-namespace:PCL.Core.UI.Controls;assembly=PCL.Core 引用。

public class BlurBorder : Border
{
    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<BlurBorder, double>(nameof(BlurRadius), 10d);
    public double BlurRadius { get => GetValue(BlurRadiusProperty); set => SetValue(BlurRadiusProperty, value); }
}

public class MotdRenderer : Control
{
    public string MotdText { get; set; } = "";

    // [port] WPF MotdRenderer 成员在桩中缺席，以下为与原签名对应的空实现（渲染逻辑暂缓移植）。
    public void RenderMotd(string motd, bool isDark, int scale) { }
    public void RenderMotd(string motd, bool isDark, int scale, int width) { }
    public void RenderCanvas() { }
    public void ClearCanvas() { }
}
