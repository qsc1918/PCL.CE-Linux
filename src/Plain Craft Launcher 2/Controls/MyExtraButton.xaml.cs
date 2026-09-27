using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;

namespace PCL;

public partial class MyExtraButton : Grid
{
    public delegate void ClickEventHandler(object sender, PointerReleasedEventArgs e); // 自定义事件

    public delegate void RightClickEventHandler(object sender, PointerReleasedEventArgs e);

    public delegate bool ShowCheckDelegate();

    // 自定义事件
    // 务必放在 IsMouseDown 更新之后
    private const int animationColorIn = 120;
    private const int animationColorOut = 150;

    // 鼠标点击判定（务必放在点击事件之后，以使得 Button_PointerReleased 先于 Button_PointerExited 执行）
    private bool isLeftMouseHeld;
    private bool isRightMouseHeld;
    public ShowCheckDelegate showCheck = null;

    // 自定义属性
    public int Uuid = ModBase.GetUuid();

    // [port] Avalonia 不为此 XAML Clip 内 RectangleGeometry 生成命名字段，改经名称作用域获取
    private RectangleGeometry RectProgress;

    // [port] WPF ToolTip property -> Avalonia ToolTip.SetTip wrapper（对象初始化器 new MyExtraButton { ToolTip = "x" }）
    public object ToolTip
    {
        get => Avalonia.Controls.ToolTip.GetTip(this);
        set => Avalonia.Controls.ToolTip.SetTip(this, value);
    }

    public MyExtraButton()
    {
        Loaded += (_, _) => RefreshColor();
        // [port] IsEnabledChanged -> Avalonia PropertyChanged 上的 IsEnabledProperty
        this.PropertyChanged += (_, e) => { if (e.Property == IsEnabledProperty) RefreshColor(); };
        InitializeComponent();
        // [port] Avalonia 不为 XAML Clip 内的 RectangleGeometry 生成命名字段，且几何对象不在名称作用域中，
        //       故从所属 Border 的 Clip 属性取回（与 MyButton 的 (RectangleGeometry)PanBack.Clip 一致）
        RectProgress = PanProgress.Clip as RectangleGeometry;
        // [port] WPF MouseLeft/RightButtonDown/Up 与 MouseEnter/Leave（独立事件）→ Avalonia 统一指针事件，在此重新接线
        PanClick.PointerPressed += Button_PointerPressed;
        PanClick.PointerReleased += Button_PointerReleased;
        PanClick.PointerEntered += PanClick_MouseEvent;
        PanClick.PointerExited += PanClick_MouseEvent;
        PanClick.PointerExited += (_, _) => Button_PointerExited();
    }

    public double Progress
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            if (value < 0.0001d)
            {
                PanProgress.IsVisible = false;
            }
            else
            {
                PanProgress.IsVisible = true;
                // [port] 几何对象为属性取值，初始化时序上可能尚未就绪；加空保护避免刷新时报 NRE
                if (RectProgress is not null)
                    RectProgress.Rect = new Rect(0d, 40d * (1d - value), 40d, 40d * value);
            }
        }
    }

    public string Logo
    {
        get;
        set
        {
            if ((value ?? "") == (field ?? ""))
                return;
            field = value;
            Path.Data = Geometry.Parse(value);
            SvgIconControlHelper.ApplyVisibility(Path, ShapeSvgIcon, IsUsingSvgIcon);
        }
    } = "";

    public string SvgIcon
    {
        get;
        set
        {
            value ??= string.Empty;
            if (value == field)
                return;
            field = value;
            if (Path is null || ShapeSvgIcon is null)
                return;
            SvgIconControlHelper.ApplyIcon(Path, ShapeSvgIcon, field);
            ApplyLogoScale();
        }
    } = string.Empty;

    private bool IsUsingSvgIcon => SvgIconControlHelper.HasSvgIcon(SvgIcon);

    private double EffectiveLogoScale => IsUsingSvgIcon ? 1D : LogoScale;

    public double LogoScale
    {
        get;
        set
        {
            field = value;
            ApplyLogoScale();
        }
    } = 1d;

    public bool Show
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            ModBase.RunInUi(() =>
            {
                if (value)
                {
                    // 有了
                    IsVisible = true;
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaScaleTransform(this, 0.3d - ((ScaleTransform)RenderTransform).ScaleX, 500,
                                60, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaScaleTransform(this, 0.7d, 500, 60,
                                new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaHeight(this, 50d - Height, 200,
                                ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
                        }, "MyExtraButton MainScale " + Uuid);
                }
                else
                {
                    // 没了
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaScaleTransform(this, -((ScaleTransform)RenderTransform).ScaleX, 100,
                                ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaHeight(this, -Height, 400, 100, new ModAnimation.AniEaseOutFluent()),
                            ModAnimation.AaCode(() => IsVisible = false, after: true)
                        }, "MyExtraButton MainScale " + Uuid);
                }

                IsHitTestVisible = value; // 防止缩放动画中依然可以点进去
            });
        }
    }

    public bool CanRightClick { get; set; }

    private void ApplyLogoScale()
    {
        IconHost?.RenderTransform = new ScaleTransform { ScaleX = EffectiveLogoScale, ScaleY = EffectiveLogoScale };
    }

    // 声明
    public event ClickEventHandler? Click;
    public event RightClickEventHandler? RightClick;

    private void StartScaleAnimation(double targetScale, double reboundScale, int reboundDuration = 60)
    {
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScaleTransform(PanScale, targetScale - ((ScaleTransform)PanScale.RenderTransform).ScaleX,
                    800, ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
                ModAnimation.AaScaleTransform(PanScale, reboundScale, reboundDuration,
                    ease: new ModAnimation.AniEaseOutFluent())
            }, "MyExtraButton Scale " + Uuid);
    }

    private void RefreshScaleAfterRelease()
    {
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScaleTransform(PanScale, 1d - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 300,
                    ease: new ModAnimation.AniEaseOutBack())
            }, "MyExtraButton Scale " + Uuid);
    }

    public void ShowRefresh()
    {
        if (showCheck is not null)
            Show = showCheck();
    }

    // 触发点击事件
    private void Button_LeftPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (isLeftMouseHeld)
        {
            ModBase.Log("[Control] 按下附加按钮" +
                        (ToolTip is null or "" ? "" : "：" + ToolTip));
            Click?.Invoke(sender, e);
            e.Handled = true;
            Button_LeftPointerReleased();
        }
    }

    private void Button_RightPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (isRightMouseHeld)
        {
            ModBase.Log("[Control] 右键按下附加按钮" +
                        (ToolTip is null or "" ? "" : "：" + ToolTip));
            RightClick?.Invoke(sender, e);
            e.Handled = true;
            Button_RightPointerReleased();
        }
    }

    // [port] WPF MouseLeftButtonDown/MouseRightButtonDown（独立事件）→ Avalonia 统一 PointerPressed，按按下按钮分发
    private void Button_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            Button_RightMouseDown(sender, e);
        else
            Button_LeftMouseDown(sender, e);
    }

    // [port] WPF MouseLeftButtonUp/MouseRightButtonUp（独立事件）→ Avalonia 统一 PointerReleased，按释放按钮分发
    private void Button_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
            Button_RightPointerReleased(sender, e);
        else
            Button_LeftPointerReleased(sender, e);
    }

    private void Button_LeftMouseDown(object sender, PointerPressedEventArgs e)
    {
        if (!isLeftMouseHeld && !isRightMouseHeld)
            StartScaleAnimation(0.85d, -0.05d);
        isLeftMouseHeld = true;
        Focus();
    }

    private void Button_RightMouseDown(object sender, PointerPressedEventArgs e)
    {
        if (!CanRightClick)
            return;
        if (!isLeftMouseHeld && !isRightMouseHeld)
            StartScaleAnimation(0.85d, -0.05d);
        isRightMouseHeld = true;
        Focus();
    }

    private void Button_LeftPointerReleased()
    {
        if (!isRightMouseHeld)
            RefreshScaleAfterRelease();
        if (isLeftMouseHeld) ModMain.RaiseCustomEvent(this);
        isLeftMouseHeld = false;
        RefreshColor(); // 直接刷新颜色以判断是否已触发 PointerExited
    }

    private void Button_RightPointerReleased()
    {
        if (!CanRightClick)
            return;
        if (!isLeftMouseHeld)
            RefreshScaleAfterRelease();
        isRightMouseHeld = false;
        RefreshColor(); // 直接刷新颜色以判断是否已触发 PointerExited
    }

    private void Button_PointerExited()
    {
        isLeftMouseHeld = false;
        isRightMouseHeld = false;
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScaleTransform(PanScale, 1d - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 500,
                    ease: new ModAnimation.AniEaseOutFluent())
            }, "MyExtraButton Scale " + Uuid);
        RefreshColor(); // 直接刷新颜色以判断是否已触发 PointerExited
    }

    public void RefreshColor()
    {
        try
        {
            if (ControlVisualHelpers.ShouldAnimate(this)) // 防止默认属性变更触发动画
            {
                if (!IsEnabled)
                    // 禁用
                    ModAnimation.AniStart(
                        ModAnimation.AaColor(PanColor, BackgroundProperty, "ColorBrushGray4", animationColorIn),
                        "MyExtraButton Color " + Uuid);
                else if (IsPointerOver)
                    // 指向
                    ModAnimation.AniStart(
                        ModAnimation.AaColor(PanColor, BackgroundProperty, "ColorBrush4", animationColorIn),
                        "MyExtraButton Color " + Uuid);
                else
                    // 普通
                    ModAnimation.AniStart(
                        ModAnimation.AaColor(PanColor, BackgroundProperty, "ColorBrush3", animationColorOut),
                        "MyExtraButton Color " + Uuid);
            }

            else
            {
                ControlVisualHelpers.AnimateColorOrSetResource(PanColor, BackgroundProperty,
                    !IsEnabled ? "ColorBrushGray4" : IsPointerOver ? "ColorBrush4" : "ColorBrush3",
                    !IsEnabled || IsPointerOver ? animationColorIn : animationColorOut,
                    "MyExtraButton Color " + Uuid, false);
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "刷新图标按钮颜色出错");
        }
    }

    /// <summary>
    ///     发出一圈波浪效果提示。
    /// </summary>
    public void Ribble()
    {
        ModBase.RunInUi(() =>
        {
            var shape = new Border
            {
                CornerRadius = new CornerRadius(1000d), BorderThickness = new Thickness(0.001d), Opacity = 0.5d,
                RenderTransformOrigin = new RelativePoint(new Point(0.5d, 0.5d), RelativeUnit.Relative), RenderTransform = new ScaleTransform()
            };
            shape.SetResourceReference(Border.BackgroundProperty, "ColorBrush5");
            PanScale.Children.Insert(0, shape);
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaScaleTransform(shape, 13d, 1000,
                        ease: new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Strong, 0.3d)),
                    ModAnimation.AaOpacity(shape, -shape.Opacity, 1000),
                    ModAnimation.AaCode(() => PanScale.Children.Remove(shape), after: true)
                }, "ExtraButton Ribble " + ModBase.GetUuid());
        });
    }

    private void PanClick_MouseEvent(object sender, PointerEventArgs e)
    {
        RefreshColor();
    }
}
