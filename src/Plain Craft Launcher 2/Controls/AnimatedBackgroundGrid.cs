using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;

namespace PCL;

public class AnimatedBackgroundGrid : Grid
{
    // [port] WPF 式 Register + PropertyMetadata(默认值, 回调) → Avalonia Register<TOwner,TValue> + Changed 钩子
    public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
        AvaloniaProperty.Register<AnimatedBackgroundGrid, IBrush>(
            "BackgroundBrush", new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)));

    private readonly AvaloniaProperty _animatableBrushProperty;

    public readonly int uuid = ModBase.GetUuid();

    static AnimatedBackgroundGrid()
    {
        BackgroundBrushProperty.Changed.AddClassHandler<AnimatedBackgroundGrid>((d, e) => _BackgroundBrushChanged(d, e));
    }

    public AnimatedBackgroundGrid(AvaloniaProperty brushDp)
    {
        _animatableBrushProperty = brushDp;
        Loaded += (_, _) => Init();
    }

    public AnimatedBackgroundGrid() : this(BackgroundProperty)
    {
    }

    protected virtual Control AnimatableElement => this;

    protected virtual IBrush AnimatableBrush
    {
        get => Background;
        set => Background = value;
    }

    protected bool IsAnimating
    {
        get => field;
        private set => field = value;
    }

    public IBrush BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    private static void _BackgroundBrushChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
    {
        var grid = (AnimatedBackgroundGrid)d;
        var brush = (IBrush)e.NewValue;
        if (!(grid.IsLoaded && grid.IsVisible))
        {
            grid.AnimatableBrush = brush;
            return;
        }

        // [port] Dispatcher.InvokeAsync(Func<Task>) → InvokeAsync（Avalonia 12）
        grid.Dispatcher.InvokeAsync(async () =>
        {
            grid.IsAnimating = true;
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaColor(grid.AnimatableElement, grid._animatableBrushProperty,
                        new ModBase.MyColor(brush) - new ModBase.MyColor(grid.AnimatableBrush), 300)
                }, "MyCard Theme " + grid.uuid);
            await Task.Delay(300);
            grid.AnimatableBrush = brush;
            grid.IsAnimating = false;
        });
    }

    private void Init()
    {
        AnimatableBrush = BackgroundBrush;
    }
}
