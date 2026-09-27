using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;
using Avalonia.Animation;
using Avalonia.VisualTree;
using PCL.Core.App;
using PCL.Core.UI.Theme;

namespace PCL;

public partial class MyToast : Border
{
    public int Uuid = ModBase.GetUuid();

    /// <summary>判定为拖动而非点击的最小水平位移（像素）。</summary>
    private const double DragDeadzone = 4d;

    /// <summary>拖动时透明度下限，确保控件始终可见。</summary>
    private const double DragOpacityFloor = 0.35d;

    /// <summary>触发关闭的位移占控件宽度的比例。</summary>
    private const double DismissThresholdRatio = 0.12d;

    /// <summary>触发关闭的最小绝对位移（像素）。</summary>
    private const double DismissThresholdMin = 24d;

    /// <summary>拖动释放后，若剩余显示时间不足此值则直接关闭。</summary>
    private const double MinRemainingMs = 300d;

    /// <summary>拖动释放后回到原位的动画时长（毫秒）。</summary>
    private const int ReturnAnimationMs = 150;

    // 拖动状态
    private bool _dragPending;
    private bool _isDragging;
    private Point _dragStartPoint;
    private double _dragStartTranslateX;
    private Control? _dragReference;

    // 进度条状态
    private double _progressStartWidth;
    private double _progressTotalMs;

    // [port] 指针捕获（WPF CaptureMouse/ReleaseMouseCapture → Avalonia pointer.Capture）
    private IPointer? _capturedPointer;

    public MyToast()
    {
        InitializeComponent();
        BtnClose.Click += (_, _) => Dismiss();
        // [port] WPF Preview(隧道)鼠标事件 → Avalonia AddHandler(..., Tunnel, handledEventsToo:true)
        AddHandler(InputElement.PointerPressedEvent, new EventHandler<PointerPressedEventArgs>(Toast_PreviewPointerPressed), RoutingStrategies.Tunnel, true);
        AddHandler(InputElement.PointerMovedEvent, new EventHandler<PointerEventArgs>(Toast_PreviewMouseMove), RoutingStrategies.Tunnel, true);
        AddHandler(InputElement.PointerReleasedEvent, new EventHandler<PointerReleasedEventArgs>(Toast_PreviewPointerReleased), RoutingStrategies.Tunnel, true);
        // [port] WPF LostMouseCapture → Avalonia PointerCaptureLost
        PointerCaptureLost += Toast_LostMouseCapture;
        Loaded += (_, _) =>
        {
            UpdateColors();
            ThemeService.ColorModeChanged += OnThemeChanged;
            ThemeService.ColorThemeChanged += OnColorThemeChanged;
        };
        Unloaded += (_, _) =>
        {
            ThemeService.ColorModeChanged -= OnThemeChanged;
            ThemeService.ColorThemeChanged -= OnColorThemeChanged;
            ModAnimation.AniStop($"Toast Show {Uuid}");
            ModAnimation.AniStop($"Toast Hide {Uuid}");
            ModAnimation.AniStop($"Toast Dismiss {Uuid}");
            ModAnimation.AniStop($"Toast Emphasize {Uuid}");
            ModAnimation.AniStop($"Toast Drag Return {Uuid}");
            // [port] WPF BeginAnimation(WidthProperty, null) 停止动画 → 停止项目动画框架
            ModAnimation.AniStop($"Toast Progress {Uuid}");
        };
    }

    private void OnThemeChanged(bool isDarkMode, ColorTheme theme) => UpdateColors();
    private void OnColorThemeChanged(ColorTheme theme) => UpdateColors();

    public string Context
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    public string Icon { get; set; } = "lucide/info";

    public HintType ToastType { get; set; } = HintType.Info;

    public double DisplayDuration { get; set; } = 5000;

    public bool IsDismissing { get; private set; }

    private double _targetHeight;

    public void Show()
    {
        if (Parent is not Panel)
            return;
        if (ModMain.frmMain is not null)
            MaxWidth = ModMain.frmMain.Bounds.Width * 0.9;
        Margin = new Thickness(0, 0, 16, 4);
        Opacity = 0;

        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Arrange(new Rect(0, 0, DesiredSize.Width, DesiredSize.Height));
        _targetHeight = Math.Max(Bounds.Height, 45d);
        Height = 0;

        RenderTransform = new TranslateTransform(60, 0);

        ModAnimation.AniStop($"Toast Drag Return {Uuid}");
        var enterAnimations = new List<ModAnimation.AniData>
        {
            ModAnimation.AaTranslateX(this, -60, 400, ease: new ModAnimation.AniEaseOutFluent()),
            ModAnimation.AaHeight(this, _targetHeight, 150, ease: new ModAnimation.AniEaseOutFluent()),
            ModAnimation.AaOpacity(this, 1, 100)
        };
        ModAnimation.AniStart(enterAnimations, $"Toast Show {Uuid}");

        RestartHideAnimation();
    }

    public void Emphasize()
    {
        ModAnimation.AniStop($"Toast Show {Uuid}");
        ModAnimation.AniStop($"Toast Hide {Uuid}");
        ModAnimation.AniStop($"Toast Emphasize {Uuid}");
        ModAnimation.AniStop($"Toast Drag Return {Uuid}");
        // [port] WPF BeginAnimation(null) → 停止进度动画
        ModAnimation.AniStop($"Toast Progress {Uuid}");
        if (RenderTransform is TranslateTransform tt) tt.X = 0;
        Opacity = 1;
        Height = _targetHeight;
        ModAnimation.AniStart(new List<ModAnimation.AniData>
        {
            ModAnimation.AaTranslateX(this, -8, 70, ease: new ModAnimation.AniEaseOutFluent()),
            ModAnimation.AaTranslateX(this, 16, 70, after: true),
            ModAnimation.AaTranslateX(this, -8, 60, after: true, ease: new ModAnimation.AniEaseOutFluent()),
            ModAnimation.AaCode(RestartHideAnimation, after: true),
        }, $"Toast Emphasize {Uuid}");
    }

    private void RestartHideAnimation()
    {
        StartHideAnimation(DisplayDuration);
        StartProgressAnimation(DisplayDuration);
    }

    private void StartHideAnimation(double delayMs)
    {
        var delay = (int)Math.Round(delayMs);
        ModAnimation.AniStart(new List<ModAnimation.AniData>
        {
            ModAnimation.AaTranslateX(this, 60, 200, delay, new ModAnimation.AniEaseInFluent()),
            ModAnimation.AaOpacity(this, -1, 150, delay),
            ModAnimation.AaHeight(this, -_targetHeight, 100, ease: new ModAnimation.AniEaseOutFluent(), after: true),
            ModAnimation.AaCode(() =>
            {
                if (Parent is Panel p)
                    p.Children.Remove(this);
            }, after: true)
        }, $"Toast Hide {Uuid}");
    }

    public void Dismiss()
    {
        if (IsDismissing) return;
        IsDismissing = true;
        _isDragging = false;
        _dragPending = false;
        if (_capturedPointer is not null) { _capturedPointer.Capture(null); _capturedPointer = null; }
        ModAnimation.AniStop($"Toast Show {Uuid}");
        ModAnimation.AniStop($"Toast Hide {Uuid}");
        ModAnimation.AniStop($"Toast Emphasize {Uuid}");
        ModAnimation.AniStop($"Toast Drag Return {Uuid}");
        // [port] WPF BeginAnimation(null) → 停止进度动画
        ModAnimation.AniStop($"Toast Progress {Uuid}");
        ModAnimation.AniStart(new List<ModAnimation.AniData>
        {
            ModAnimation.AaTranslateX(this, 60, 150, ease: new ModAnimation.AniEaseInFluent()),
            ModAnimation.AaOpacity(this, -1, 100),
            ModAnimation.AaCode(() =>
            {
                if (Parent is Panel p)
                    p.Children.Remove(this);
            }, after: true)
        }, $"Toast Dismiss {Uuid}");
    }

    private void StartProgressAnimation(double duration)
    {
        var totalMs = (int)Math.Round(duration);
        if (totalMs <= 0)
            return;
        var w = ProgressBar.Bounds.Width;
        if (w <= 0) w = 300;
        ProgressBar.HorizontalAlignment = HorizontalAlignment.Left;
        ProgressBar.Width = w;
        _progressStartWidth = w;
        _progressTotalMs = totalMs;
        // [port] WPF DoubleAnimation + BeginAnimation → 项目动画框架
        ModAnimation.AniStart(new List<ModAnimation.AniData>
        {
            ModAnimation.AaWidth(ProgressBar, -w, totalMs)
        }, $"Toast Progress {Uuid}");
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RootGrid.Clip = new RectangleGeometry(new Rect(0, 0, RootGrid.Bounds.Width, RootGrid.Bounds.Height), 8, 8);
    }

    private void UpdateColors()
    {
        var baseHue = ToastType switch
        {
            HintType.Success => 145d,
            HintType.Error => 355d,
            HintType.Warning => 40d,
            _ => 210d
        };
        var res = Avalonia.Application.Current.Resources;
        var accent = new ModBase.MyColor().FromHSL2(baseHue, 75, 60);
        var bg = ThemeService.IsDarkMode
            ? new SolidColorBrush(LabColor.FromLch(0.35))
            : (IBrush)res["ColorBrushBackground"];
        var text = (ISolidColorBrush)res["ColorBrushGray1"];
        var accentBrush = new SolidColorBrush(accent);

        Root.Background = bg;
        Root.BorderBrush = bg;
        TitleText.Foreground = text;
        ProgressBar.Fill = accentBrush;
        BtnClose.Foreground = text;
        ToastIcon.Icon = Icon;
        ToastIcon.IconBrush = accentBrush;
        ToastIcon.StrokeThickness = 0;
    }

    #region 拖动关闭

    private void Toast_PreviewPointerPressed(object sender, PointerPressedEventArgs e)
    {
        _dragPending = false;
        if (IsDismissing)
            return;
        if (IsDescendantOf(e.Source as AvaloniaObject, BtnClose))
            return;
        _dragReference = Parent as Control;
        if (_dragReference is null)
            return;
        _dragPending = true;
        _isDragging = false;
        _dragStartPoint = e.GetPosition(_dragReference);
    }

    private void Toast_PreviewMouseMove(object sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            if (_dragReference is null || !e.GetCurrentPoint(_dragReference).Properties.IsLeftButtonPressed)
            {
                _isDragging = false;
                _dragPending = false;
                if (_capturedPointer is not null) { _capturedPointer.Capture(null); _capturedPointer = null; }
                ReturnFromDrag();
                return;
            }
            var dragCurrent = e.GetPosition(_dragReference);
            UpdateDragPosition(dragCurrent.X - _dragStartPoint.X);
            e.Handled = true;
            return;
        }

        if (!_dragPending)
            return;
        if (_dragReference is null || !e.GetCurrentPoint(_dragReference).Properties.IsLeftButtonPressed)
        {
            _dragPending = false;
            return;
        }

        var current = e.GetPosition(_dragReference);
        var delta = current.X - _dragStartPoint.X;

        if (delta < DragDeadzone)
            return;

        BeginDrag(delta, e.Pointer);
    }

    private void Toast_PreviewPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (_dragPending && !_isDragging)
        {
            _dragPending = false;
            return;
        }

        if (!_isDragging)
            return;

        _isDragging = false;
        _dragPending = false;
        e.Handled = true;

        if (_capturedPointer is not null) { _capturedPointer.Capture(null); _capturedPointer = null; }

        var currentX = (RenderTransform as TranslateTransform)?.X ?? 0d;
        if (currentX - _dragStartTranslateX >= GetDismissThreshold())
        {
            Dismiss();
            return;
        }

        ReturnFromDrag();
    }

    private void Toast_LostMouseCapture(object sender, PointerCaptureLostEventArgs e)
    {
        if (!_isDragging)
            return;
        _isDragging = false;
        _dragPending = false;
        ReturnFromDrag();
    }

    private void BeginDrag(double initialDelta, IPointer pointer)
    {
        _isDragging = true;
        _dragPending = false;

        ModAnimation.AniStop($"Toast Show {Uuid}");
        ModAnimation.AniStop($"Toast Hide {Uuid}");
        ModAnimation.AniStop($"Toast Emphasize {Uuid}");
        ModAnimation.AniStop($"Toast Drag Return {Uuid}");

        PauseProgress();

        Height = _targetHeight;
        _dragStartTranslateX = (RenderTransform as TranslateTransform)?.X ?? 0d;

        _capturedPointer = pointer;
        pointer.Capture(this);

        UpdateDragPosition(initialDelta);
    }

    private void UpdateDragPosition(double delta)
    {
        var newX = _dragStartTranslateX + ApplyDragResistance(delta);
        if (RenderTransform is TranslateTransform tt)
            tt.X = newX;
        Opacity = GetDragOpacity(newX);
    }

    private void ReturnFromDrag()
    {
        if (Parent is null || IsDismissing)
            return;
        var currentX = (RenderTransform as TranslateTransform)?.X ?? 0d;
        var currentOpacity = Opacity;

        var remaining = GetProgressRemainingMs();
        if (remaining < MinRemainingMs)
        {
            Dismiss();
            return;
        }

        ResumeProgress(remaining);

        ModAnimation.AniStart(new List<ModAnimation.AniData>
        {
            ModAnimation.AaTranslateX(this, -currentX, ReturnAnimationMs, ease: new ModAnimation.AniEaseOutFluent()),
            ModAnimation.AaOpacity(this, 1d - currentOpacity, ReturnAnimationMs),
            ModAnimation.AaCode(() => StartHideAnimation(GetProgressRemainingMs()), after: true)
        }, $"Toast Drag Return {Uuid}");
    }

    private static double ApplyDragResistance(double delta)
    {
        return Math.Max(0d, delta);
    }

    private double GetDragOpacity(double translateX)
    {
        if (translateX <= 0d)
            return 1d;
        var width = Bounds.Width > 0 ? Bounds.Width : 1d;
        return Math.Max(DragOpacityFloor, 1d - (translateX / width) * (1d - DragOpacityFloor));
    }

    private double GetDismissThreshold()
    {
        return Math.Max(DismissThresholdMin, Bounds.Width * DismissThresholdRatio);
    }

    private static bool IsDescendantOf(AvaloniaObject? descendant, AvaloniaObject ancestor)
    {
        while (descendant is not null)
        {
            if (ReferenceEquals(descendant, ancestor))
                return true;
            descendant = (descendant as Avalonia.Visual)?.GetVisualParent();
        }
        return false;
    }

    #endregion

    #region 进度条暂停与恢复

    private void PauseProgress()
    {
        var currentWidth = ProgressBar.Width;
        // [port] WPF BeginAnimation(null) → 停止进度动画
        ModAnimation.AniStop($"Toast Progress {Uuid}");
        ProgressBar.Width = currentWidth;
    }

    private void ResumeProgress(double remainingMs)
    {
        if (remainingMs <= 0)
            return;
        var currentWidth = ProgressBar.Width;
        if (currentWidth <= 0)
            return;
        // [port] WPF DoubleAnimation + BeginAnimation → 项目动画框架
        ModAnimation.AniStart(new List<ModAnimation.AniData>
        {
            ModAnimation.AaWidth(ProgressBar, -currentWidth, (int)Math.Round(remainingMs))
        }, $"Toast Progress {Uuid}");
    }

    private double GetProgressRemainingMs()
    {
        if (_progressStartWidth <= 0)
            return 0d;
        var currentWidth = ProgressBar.Width;
        return _progressTotalMs * (currentWidth / _progressStartWidth);
    }

    #endregion
}
