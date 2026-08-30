using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using PCL.Core.UI.Animation;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Animation.Easings;

namespace PCL.Core.UI.Controls.SvgIcon;

// [port] WPF DependencyProperty/FrameworkPropertyMetadata → Avalonia StyledProperty；
// FrameworkPropertyMetadataOptions.AffectsMeasure/Render → Layoutable.AffectsMeasure / Visual.AffectsRender；
// RenderSize → Bounds.Size；DrawingContext.PushTransform(Transform) → PushTransform(Matrix)；
// Freezable 冻结逻辑 → 检测不可变画刷（IImmutableSolidColorBrush）并替换为可变实例。

public class SvgIcon : Control
{
    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<SvgIcon, string>(
            nameof(Icon),
            string.Empty);

    public static readonly StyledProperty<string> DefaultPackProperty =
        AvaloniaProperty.Register<SvgIcon, string>(
            nameof(DefaultPack),
            SvgIconLoader.DefaultIconPack);

    public static readonly StyledProperty<IBrush?> IconBrushProperty =
        AvaloniaProperty.Register<SvgIcon, IBrush?>(
            nameof(IconBrush),
            Brushes.Black);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<SvgIcon, double>(
            nameof(StrokeThickness),
            2D,
            validate: value => !double.IsNaN(value) && value >= 0D);

    public static readonly StyledProperty<bool> UseOriginalColorProperty =
        AvaloniaProperty.Register<SvgIcon, bool>(
            nameof(UseOriginalColor),
            false);

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<SvgIcon, Stretch>(
            nameof(Stretch),
            Stretch.Uniform);

    static SvgIcon()
    {
        AffectsMeasure<SvgIcon>(IconProperty, DefaultPackProperty, StretchProperty);
        AffectsRender<SvgIcon>(IconProperty, DefaultPackProperty, IconBrushProperty,
            StrokeThicknessProperty, UseOriginalColorProperty, StretchProperty);

        IconProperty.Changed.AddClassHandler<SvgIcon>((icon, _) => icon._ResetModel());
        DefaultPackProperty.Changed.AddClassHandler<SvgIcon>((icon, _) => icon._ResetModel());
    }

    private SvgIconModel? _model;
    private bool _modelLoaded;

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string DefaultPack
    {
        get => GetValue(DefaultPackProperty);
        set => SetValue(DefaultPackProperty, value);
    }

    public IBrush? IconBrush
    {
        get => GetValue(IconBrushProperty);
        set => SetValue(IconBrushProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public bool UseOriginalColor
    {
        get => GetValue(UseOriginalColorProperty);
        set => SetValue(UseOriginalColorProperty, value);
    }

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public IAnimation AnimateIconBrushTo(
        NColor color,
        TimeSpan? duration = null,
        IEasing? easing = null,
        string? animationKey = null)
    {
        _EnsureAnimatableIconBrush();

        var animation = new NColorFromToAnimation
        {
            Name = animationKey ?? $"SvgIconColor {RuntimeHelpers.GetHashCode(this)}",
            To = color,
            Duration = duration ?? TimeSpan.FromMilliseconds(120),
            Easing = easing ?? CubicEaseOut.Shared
        };

        return animation.RunFireAndForget(new AvaloniaAnimatable(this, IconBrushProperty));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var model = _GetModel();
        var naturalSize = model is null
            ? new Size(24D, 24D)
            : new Size(model.Width, model.Height);

        if (double.IsInfinity(availableSize.Width) && double.IsInfinity(availableSize.Height))
            return naturalSize;

        if (double.IsInfinity(availableSize.Width))
            return new Size(naturalSize.Width * availableSize.Height / naturalSize.Height, availableSize.Height);

        if (double.IsInfinity(availableSize.Height))
            return new Size(availableSize.Width, naturalSize.Height * availableSize.Width / naturalSize.Width);

        return availableSize;
    }

    public override void Render(DrawingContext drawingContext)
    {
        base.Render(drawingContext);

        var model = _GetModel();
        var renderSize = Bounds.Size;
        if (model is null || model.Elements.Count == 0 || renderSize.Width <= 0D || renderSize.Height <= 0D)
            return;

        var target = _CalculateTargetRect(new Size(model.Width, model.Height), renderSize, Stretch);
        if (target.Width <= 0D || target.Height <= 0D)
            return;

        var scaleX = target.Width / model.Width;
        var scaleY = target.Height / model.Height;

        using var _1 = drawingContext.PushTransform(Matrix.CreateTranslation(target.X, target.Y));
        using var _2 = drawingContext.PushTransform(Matrix.CreateScale(scaleX, scaleY));
        using var _3 = drawingContext.PushTransform(Matrix.CreateTranslation(-model.MinX, -model.MinY));

        var options = new SvgIconPaintOptions(IconBrush, StrokeThickness, UseOriginalColor);
        foreach (var element in model.Elements)
            element.Draw(drawingContext, options);
    }

    private SvgIconModel? _GetModel()
    {
        if (_modelLoaded)
            return _model;

        _model = SvgIconLoader.Load(Icon, DefaultPack);
        _modelLoaded = true;
        return _model;
    }

    private void _ResetModel()
    {
        _model = null;
        _modelLoaded = false;
    }

    private static void _OnIconChanged(AvaloniaObject dependencyObject, AvaloniaPropertyChangedEventArgs args)
    {
        var icon = (SvgIcon)dependencyObject;
        icon._ResetModel();
    }

    private void _EnsureAnimatableIconBrush()
    {
        if (IconBrush is SolidColorBrush)
            return;

        IconBrush = IconBrush switch
        {
            IImmutableSolidColorBrush immutable => new SolidColorBrush(immutable.Color, immutable.Opacity),
            _ => new SolidColorBrush(Colors.Black)
        };
    }

    private static Rect _CalculateTargetRect(Size sourceSize, Size renderSize, Stretch stretch)
    {
        if (sourceSize.Width <= 0D || sourceSize.Height <= 0D)
            sourceSize = new Size(24D, 24D);

        if (stretch == Stretch.None)
        {
            var x = (renderSize.Width - sourceSize.Width) / 2D;
            var y = (renderSize.Height - sourceSize.Height) / 2D;
            return new Rect(x, y, sourceSize.Width, sourceSize.Height);
        }

        var scaleX = renderSize.Width / sourceSize.Width;
        var scaleY = renderSize.Height / sourceSize.Height;

        var scale = stretch switch
        {
            Stretch.Fill => double.NaN,
            Stretch.UniformToFill => Math.Max(scaleX, scaleY),
            _ => Math.Min(scaleX, scaleY)
        };

        var width = stretch == Stretch.Fill ? renderSize.Width : sourceSize.Width * scale;
        var height = stretch == Stretch.Fill ? renderSize.Height : sourceSize.Height * scale;
        var left = (renderSize.Width - width) / 2D;
        var top = (renderSize.Height - height) / 2D;

        return new Rect(left, top, width, height);
    }
}
