using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace PCL;

public class AnimatedBackgroundGrid : Grid
{
    public static readonly AvaloniaProperty BackgroundBrushProperty = AvaloniaProperty.Register("BackgroundBrush",
        typeof(SolidColorBrush), typeof(AnimatedBackgroundGrid),
        new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), _BackgroundBrushChanged));

    private readonly AvaloniaProperty _animatableBrushProperty;

    public readonly int uuid = ModBase.GetUuid();

    public AnimatedBackgroundGrid(AvaloniaProperty brushDp)
    {
        _animatableBrushProperty = brushDp;
        Loaded += (_, _) => Init();
    }

    public AnimatedBackgroundGrid() : this(BackgroundProperty)
    {
    }

    protected virtual Control AnimatableElement => this;

    protected virtual SolidColorBrush AnimatableBrush
    {
        get => (SolidColorBrush)Background;
        set => Background = value;
    }

    protected bool IsAnimating
    {
        get => field;
        private set => field = value;
    }

    public SolidColorBrush BackgroundBrush
    {
        get => (SolidColorBrush)GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    private static void _BackgroundBrushChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
    {
        var grid = (AnimatedBackgroundGrid)d;
        var brush = (SolidColorBrush)e.NewValue;
        if (!(grid.IsLoaded && grid.IsVisible))
        {
            grid.AnimatableBrush = brush;
            return;
        }

        grid.Dispatcher.BeginInvoke(new Func<Task>(async () =>
        {
            grid.IsAnimating = true;
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaColor(grid.AnimatableElement, grid._animatableBrushProperty,
                        new ModBase.MyColor(brush) - grid.AnimatableBrush, 300)
                }, "MyCard Theme " + grid.uuid);
            await Task.Delay(300);
            grid.AnimatableBrush = brush;
            grid.IsAnimating = false;
        }));
    }

    private void Init()
    {
        AnimatableBrush = BackgroundBrush;
    }
}
