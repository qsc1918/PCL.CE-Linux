using Avalonia.Media;

namespace PCL.Core.UI.Controls.SvgIcon;

// [port] Brush → IBrush（Avalonia 画刷多态基于 IBrush 接口）。

internal readonly record struct SvgIconPaintOptions(
    IBrush? IconBrush,
    double StrokeThickness,
    bool UseOriginalColor);
