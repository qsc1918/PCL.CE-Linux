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

        // [port] 收敛保护（关键）。
        //        BackgroundBrush 走的是动态资源（例如主窗口 PanTitle 的
        //        BackgroundBrush="{DynamicResource ColorBrush3}"），Avalonia 的属性系统在主题/
        //        布局刷新时会**反复**触发 Changed，而原实现每次都无条件排一个
        //        Dispatcher.InvokeAsync + 300ms 动画。任务队列被彻底淹没后：
        //        UI 线程再也排不到渲染，动画/主时钟线程的 Dispatcher.Invoke 也永远得不到响应
        //        → CPU 100%+、内存数百 MB/s 增长、窗口永不绘制。
        //        （实测：隐藏承载它的 PanBack 后，内存增速立刻从 ~125MB/s 降到 0。）
        //        因此：颜色没有实际变化、或已有动画在跑时，直接同步落到目标值即可。
        try
        {
            var delta = new ModBase.MyColor(brush) - new ModBase.MyColor(grid.AnimatableBrush);
            var noChange = Math.Abs(delta.a) < 0.5d && Math.Abs(delta.r) < 0.5d &&
                           Math.Abs(delta.g) < 0.5d && Math.Abs(delta.b) < 0.5d;
            if (noChange || grid.IsAnimating)
            {
                grid.AnimatableBrush = brush;
                return;
            }
        }
        catch
        {
            // 取当前画刷失败（例如尚不可用时）→ 直接落到目标值，不做动画
            grid.AnimatableBrush = brush;
            return;
        }

        // [port] Dispatcher.InvokeAsync(Func<Task>) → InvokeAsync（Avalonia 12）
        grid.Dispatcher.InvokeAsync(async () =>
        {
            try
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
            }
            finally
            {
                grid.IsAnimating = false;
            }
        });
    }

    private void Init()
    {
        AnimatableBrush = BackgroundBrush;
    }
}
