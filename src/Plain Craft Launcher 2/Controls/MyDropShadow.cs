using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;

namespace PCL;

public class MyDropShadow : Decorator
{
    public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<MyDropShadow, Color>(
        nameof(Color), Color.FromArgb(0x71, 0x0, 0x0, 0x0));

    public static readonly StyledProperty<double> ShadowRadiusProperty = AvaloniaProperty.Register<MyDropShadow, double>(
        nameof(ShadowRadius), 5d);

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = AvaloniaProperty.Register<MyDropShadow, CornerRadius>(
        nameof(CornerRadius), new CornerRadius());

    static MyDropShadow()
    {
        ColorProperty.Changed.AddClassHandler<MyDropShadow>((o, e) => ((MyDropShadow)o)._brushes = null);
        ShadowRadiusProperty.Changed.AddClassHandler<MyDropShadow>((o, e) => ((MyDropShadow)o)._brushes = null);
        CornerRadiusProperty.Changed.AddClassHandler<MyDropShadow>((o, e) => ((MyDropShadow)o)._brushes = null);
    }

    private static Brush[] _commonBrushes;
    private static CornerRadius _commonCornerRadius;
    private static readonly object _resourceAccess = new();
    private Brush[] _brushes;

    /// <summary>
    ///     阴影颜色。
    /// </summary>
    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    ///     阴影模糊半径。
    /// </summary>
    public double ShadowRadius
    {
        get => (double)GetValue(ShadowRadiusProperty);
        set => SetValue(ShadowRadiusProperty, value);
    }

    /// <summary>
    ///     圆角大小。
    /// </summary>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    // =======================================
    // 渲染
    // =======================================


    public override void Render(DrawingContext drawingContext)
    {
        var cornerRadius = CornerRadius;
        // [port] WPF RenderSize → Avalonia 使用 Bounds（Render 方法中无 RenderSize）
        var shadowBounds = new Rect(0d, 0d, Bounds.Width, Bounds.Height);
        var color = Color;

        if (shadowBounds.Width > 0d && shadowBounds.Height > 0d && color.A > 0)
        {
            var centerWidth = shadowBounds.Right - shadowBounds.Left - 2d * ShadowRadius;
            var centerHeight = shadowBounds.Bottom - shadowBounds.Top - 2d * ShadowRadius;
            var maxRadius = Math.Min(centerWidth * 0.5d, centerHeight * 0.5d);
            // [port] WPF CornerRadius 为可变 struct，Avalonia 为只读；解构后重建
            cornerRadius = new CornerRadius(
                Math.Min(cornerRadius.TopLeft, maxRadius),
                Math.Min(cornerRadius.TopRight, maxRadius),
                Math.Min(cornerRadius.BottomLeft, maxRadius),
                Math.Min(cornerRadius.BottomRight, maxRadius));
            var brushes = GetBrushes(color, cornerRadius);
            var centerTop = shadowBounds.Top + ShadowRadius;
            var centerLeft = shadowBounds.Left + ShadowRadius;
            var centerRight = shadowBounds.Right - ShadowRadius;
            var centerBottom = shadowBounds.Bottom - ShadowRadius;
            var guidelineSetX = new[]
            {
                centerLeft, centerLeft + cornerRadius.TopLeft, centerRight - cornerRadius.TopRight,
                centerLeft + cornerRadius.BottomLeft, centerRight - cornerRadius.BottomRight, centerRight
            };
            var guidelineSetY = new[]
            {
                centerTop, centerTop + cornerRadius.TopLeft, centerTop + cornerRadius.TopRight,
                centerBottom - cornerRadius.BottomLeft, centerBottom - cornerRadius.BottomRight, centerBottom
            };
            // [port] GuidelineSet 吸附是 WPF 特有，Avalonia 无对应，移除吸附但保留分段渲染
            // drawingContext.PushGuidelineSet(new GuidelineSet(guidelineSetX, guidelineSetY));
            cornerRadius = new CornerRadius(
                cornerRadius.TopLeft + ShadowRadius,
                cornerRadius.TopRight + ShadowRadius,
                cornerRadius.BottomLeft + ShadowRadius,
                cornerRadius.BottomRight + ShadowRadius);
            var topLeft = new Rect(shadowBounds.Left, shadowBounds.Top, cornerRadius.TopLeft, cornerRadius.TopLeft);
            drawingContext.DrawRectangle(brushes[(int)Placement.TopLeft], null, topLeft);
            var topWidth = guidelineSetX[2] - guidelineSetX[1];

            if (topWidth > 0d)
            {
                var top = new Rect(guidelineSetX[1], shadowBounds.Top, topWidth, ShadowRadius);
                drawingContext.DrawRectangle(brushes[(int)Placement.Top], null, top);
            }

            var topRight = new Rect(guidelineSetX[2], shadowBounds.Top, cornerRadius.TopRight, cornerRadius.TopRight);
            drawingContext.DrawRectangle(brushes[(int)Placement.TopRight], null, topRight);
            var leftHeight = guidelineSetY[3] - guidelineSetY[1];

            if (leftHeight > 0d)
            {
                var left = new Rect(shadowBounds.Left, guidelineSetY[1], ShadowRadius, leftHeight);
                drawingContext.DrawRectangle(brushes[(int)Placement.Left], null, left);
            }

            var rightHeight = guidelineSetY[4] - guidelineSetY[2];

            if (rightHeight > 0d)
            {
                var right = new Rect(guidelineSetX[5], guidelineSetY[2], ShadowRadius, rightHeight);
                drawingContext.DrawRectangle(brushes[(int)Placement.Right], null, right);
            }

            var bottomLeft = new Rect(shadowBounds.Left, guidelineSetY[3], cornerRadius.BottomLeft,
                cornerRadius.BottomLeft);
            drawingContext.DrawRectangle(brushes[(int)Placement.BottomLeft], null, bottomLeft);
            var bottomWidth = guidelineSetX[4] - guidelineSetX[3];

            if (bottomWidth > 0d)
            {
                var bottom = new Rect(guidelineSetX[3], guidelineSetY[5], bottomWidth, ShadowRadius);
                drawingContext.DrawRectangle(brushes[(int)Placement.Bottom], null, bottom);
            }

            var bottomRight = new Rect(guidelineSetX[4], guidelineSetY[4], cornerRadius.BottomRight,
                cornerRadius.BottomRight);
            drawingContext.DrawRectangle(brushes[(int)Placement.BottomRight], null, bottomRight);

            if (cornerRadius.TopLeft == ShadowRadius && cornerRadius.TopLeft == cornerRadius.TopRight &&
                cornerRadius.TopLeft == cornerRadius.BottomLeft && cornerRadius.TopLeft == cornerRadius.BottomRight)
            {
                var center = new Rect(guidelineSetX[0], guidelineSetY[0], centerWidth, centerHeight);
                drawingContext.DrawRectangle(brushes[(int)Placement.Center], null, center);
            }
            else
            {
                var figure = new PathFigure();

                if (cornerRadius.TopLeft > ShadowRadius)
                {
                    figure.StartPoint = new Point(guidelineSetX[1], guidelineSetY[0]);
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[1], guidelineSetY[1]), IsStroked = true });
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[0], guidelineSetY[1]), IsStroked = true });
                }
                else
                {
                    figure.StartPoint = new Point(guidelineSetX[0], guidelineSetY[0]);
                }

                if (cornerRadius.BottomLeft > ShadowRadius)
                {
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[0], guidelineSetY[3]), IsStroked = true });
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[3], guidelineSetY[3]), IsStroked = true });
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[3], guidelineSetY[5]), IsStroked = true });
                }
                else
                {
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[0], guidelineSetY[5]), IsStroked = true });
                }

                if (cornerRadius.BottomRight > ShadowRadius)
                {
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[4], guidelineSetY[5]), IsStroked = true });
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[4], guidelineSetY[4]), IsStroked = true });
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[5], guidelineSetY[4]), IsStroked = true });
                }
                else
                {
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[5], guidelineSetY[5]), IsStroked = true });
                }

                if (cornerRadius.TopRight > ShadowRadius)
                {
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[5], guidelineSetY[2]), IsStroked = true });
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[2], guidelineSetY[2]), IsStroked = true });
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[2], guidelineSetY[0]), IsStroked = true });
                }
                else
                {
                    figure.Segments.Add(new LineSegment { Point = new Point(guidelineSetX[5], guidelineSetY[0]), IsStroked = true });
                }

                figure.IsClosed = true;
                // [port] WPF PathFigure.Freeze()：Avalonia 几何不可变性由内部管理，无 Freeze API，移除
                // figure.Freeze();
                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                // [port] WPF PathGeometry.Freeze()：Avalonia 无 Freeze API，移除
                // geometry.Freeze();
                drawingContext.DrawGeometry(brushes[(int)Placement.Center], null, geometry);
            }

            // [port] WPF DrawingContext.Pop()：与 PushGuidelineSet 成对；Avalonia 无对应，移除
            // drawingContext.Pop();
        }
    }

    private System.Collections.Generic.List<GradientStop> CreateStops(Color c, double cornerRadius)
    {
        var gradientScale = 1d / (ShadowRadius + cornerRadius);
        var gsc = new System.Collections.Generic.List<GradientStop>();
        var stopColor = c;
        gsc.Add(new GradientStop(stopColor, (ShadowRadius * 0.1d + cornerRadius) * gradientScale));
        // [port] WPF Color.A 可写；Avalonia Color 为只读 struct，需重建
        stopColor = Color.FromArgb((byte)Math.Round(0.74336d * c.A), stopColor.R, stopColor.G, stopColor.B);
        gsc.Add(new GradientStop(stopColor, (ShadowRadius * 0.3d + cornerRadius) * gradientScale));
        stopColor = Color.FromArgb((byte)Math.Round(0.38053d * c.A), stopColor.R, stopColor.G, stopColor.B);
        gsc.Add(new GradientStop(stopColor, (ShadowRadius * 0.5d + cornerRadius) * gradientScale));
        stopColor = Color.FromArgb((byte)Math.Round(0.12389d * c.A), stopColor.R, stopColor.G, stopColor.B);
        gsc.Add(new GradientStop(stopColor, (ShadowRadius * 0.7d + cornerRadius) * gradientScale));
        stopColor = Color.FromArgb((byte)Math.Round(0.02654d * c.A), stopColor.R, stopColor.G, stopColor.B);
        gsc.Add(new GradientStop(stopColor, (ShadowRadius * 0.9d + cornerRadius) * gradientScale));
        stopColor = Color.FromArgb(0, stopColor.R, stopColor.G, stopColor.B);
        gsc.Add(new GradientStop(stopColor, (ShadowRadius + cornerRadius) * gradientScale));
        // [port] WPF List<GradientStop>.Freeze()：Avalonia 无 Freeze API，移除
        return gsc;
    }

    // [port] WPF LinearGradientBrush(stops, start, end) / RadialGradientBrush(stops) 构造在 Avalonia 不存在，组装 GradientStops 集合
    private static GradientStops ToGradientStops(System.Collections.Generic.IEnumerable<GradientStop> stops)
    {
        var gs = new GradientStops();
        gs.AddRange(stops);
        return gs;
    }

    private Brush[] CreateBrushes(Color c, CornerRadius cornerRadius)
    {
        var brushes = new Brush[9];
        brushes[(int)Placement.Center] = new SolidColorBrush(c);
        // [port] WPF Brush.Freeze()：Avalonia 画笔不可变，无需冻结，移除
        // brushes[(int)Placement.Center].Freeze();
        var sideStops = CreateStops(c, 0d);
        var top = new LinearGradientBrush
        {
            GradientStops = ToGradientStops(sideStops),
            StartPoint = new RelativePoint(0d, 1d, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0d, 0d, RelativeUnit.Relative)
        };
        // top.Freeze();
        brushes[(int)Placement.Top] = top;
        var left = new LinearGradientBrush
        {
            GradientStops = ToGradientStops(sideStops),
            StartPoint = new RelativePoint(1d, 0d, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0d, 0d, RelativeUnit.Relative)
        };
        // left.Freeze();
        brushes[(int)Placement.Left] = left;
        var right = new LinearGradientBrush
        {
            GradientStops = ToGradientStops(sideStops),
            StartPoint = new RelativePoint(0d, 0d, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1d, 0d, RelativeUnit.Relative)
        };
        // right.Freeze();
        brushes[(int)Placement.Right] = right;
        var bottom = new LinearGradientBrush
        {
            GradientStops = ToGradientStops(sideStops),
            StartPoint = new RelativePoint(0d, 0d, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0d, 1d, RelativeUnit.Relative)
        };
        // bottom.Freeze();
        brushes[(int)Placement.Bottom] = bottom;
        System.Collections.Generic.List<GradientStop> topLeftStops;

        if (cornerRadius.TopLeft == 0d)
            topLeftStops = sideStops;
        else
            topLeftStops = CreateStops(c, cornerRadius.TopLeft);

        var topLeft = new RadialGradientBrush
        {
            RadiusX = new RelativeScalar(1d, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1d, RelativeUnit.Relative),
            Center = new RelativePoint(1d, 1d, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(1d, 1d, RelativeUnit.Relative),
            GradientStops = ToGradientStops(topLeftStops)
        };
        // topLeft.Freeze();
        brushes[(int)Placement.TopLeft] = topLeft;
        System.Collections.Generic.List<GradientStop> topRightStops;

        if (cornerRadius.TopRight == 0d)
            topRightStops = sideStops;
        else if (cornerRadius.TopRight == cornerRadius.TopLeft)
            topRightStops = topLeftStops;
        else
            topRightStops = CreateStops(c, cornerRadius.TopRight);

        var topRight = new RadialGradientBrush
        {
            RadiusX = new RelativeScalar(1d, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1d, RelativeUnit.Relative),
            Center = new RelativePoint(0d, 1d, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0d, 1d, RelativeUnit.Relative),
            GradientStops = ToGradientStops(topRightStops)
        };
        // topRight.Freeze();
        brushes[(int)Placement.TopRight] = topRight;
        System.Collections.Generic.List<GradientStop> bottomLeftStops;

        if (cornerRadius.BottomLeft == 0d)
            bottomLeftStops = sideStops;
        else if (cornerRadius.BottomLeft == cornerRadius.TopLeft)
            bottomLeftStops = topLeftStops;
        else if (cornerRadius.BottomLeft == cornerRadius.TopRight)
            bottomLeftStops = topRightStops;
        else
            bottomLeftStops = CreateStops(c, cornerRadius.BottomLeft);

        var bottomLeft = new RadialGradientBrush
        {
            RadiusX = new RelativeScalar(1d, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1d, RelativeUnit.Relative),
            Center = new RelativePoint(1d, 0d, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(1d, 0d, RelativeUnit.Relative),
            GradientStops = ToGradientStops(bottomLeftStops)
        };
        // bottomLeft.Freeze();
        brushes[(int)Placement.BottomLeft] = bottomLeft;
        System.Collections.Generic.List<GradientStop> bottomRightStops;

        if (cornerRadius.BottomRight == 0d)
            bottomRightStops = sideStops;
        else if (cornerRadius.BottomRight == cornerRadius.TopLeft)
            bottomRightStops = topLeftStops;
        else if (cornerRadius.BottomRight == cornerRadius.TopRight)
            bottomRightStops = topRightStops;
        else if (cornerRadius.BottomRight == cornerRadius.BottomLeft)
            bottomRightStops = bottomLeftStops;
        else
            bottomRightStops = CreateStops(c, cornerRadius.BottomRight);

        var bottomRight = new RadialGradientBrush
        {
            RadiusX = new RelativeScalar(1d, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1d, RelativeUnit.Relative),
            Center = new RelativePoint(0d, 0d, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0d, 0d, RelativeUnit.Relative),
            GradientStops = ToGradientStops(bottomRightStops)
        };
        // bottomRight.Freeze();
        brushes[(int)Placement.BottomRight] = bottomRight;
        return brushes;
    }

    private Brush[] GetBrushes(Color c, CornerRadius cornerRadius)
    {
        if (_commonBrushes is null)
            lock (_resourceAccess)
            {
                if (_commonBrushes is null)
                {
                    _commonBrushes = CreateBrushes(c, cornerRadius);
                    _commonCornerRadius = cornerRadius;
                }
            }

        if (c == ((SolidColorBrush)_commonBrushes[(int)Placement.Center]).Color && cornerRadius == _commonCornerRadius)
        {
            _brushes = null;
            return _commonBrushes;
        }

        if (_brushes is null) _brushes = CreateBrushes(c, cornerRadius);

        return _brushes;
    }

    private enum Placement
    {
        TopLeft = 0,
        Top = 1,
        TopRight = 2,
        Left = 3,
        Center = 4,
        Right = 5,
        BottomLeft = 6,
        Bottom = 7,
        BottomRight = 8
    }
}

// 参考自：https://referencesource.microsoft.com/#PresentationFramework.Aero/parent/Shared/Microsoft/Windows/Themes/SystemDropShadowChrome.cs,6d9c27d92a8128c1
