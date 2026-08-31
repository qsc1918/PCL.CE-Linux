using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Markup;

using Avalonia.Metadata;

namespace PCL;
public partial class MyExtraTextButton : Grid
{
    public delegate void ClickEventHandler(object sender, PointerPressedEventArgs e); // 自定义事件

    // 自定义事件
    // 务必放在 IsMouseDown 更新之后
    private const int animationColorIn = 120;
    private const int animationColorOut = 150;

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MyExtraTextButton, string>(
        nameof(Text), string.Empty);

    static MyExtraTextButton()
    {
        TextProperty.Changed.AddClassHandler<MyExtraTextButton>((d, e) => d.LabText.Text = (string)e.NewValue);
    }

    // 鼠标点击判定（务必放在点击事件之后，以使得 Button_PointerReleased 先于 Button_PointerExited 执行）
    private bool isLeftMouseHeld;

    // 自定义属性
    public int Uuid = ModBase.GetUuid();

    public MyExtraTextButton()
    {
        InitializeComponent();
        RefreshIconHostVisibility();

        Loaded += (_, _) => RefreshColor();
        IsEnabledChanged += (_, _) => RefreshColor();
        PanClick.PointerPressed += Button_LeftMouseDown;
        PanClick.PointerReleased += Button_LeftPointerReleased;
        PanClick.PointerExited += Button_PointerExited;
        PanClick.MouseRightButtonUp += Button_RightPointerReleased;
        PanClick.PointerEntered += (sender, e) => RefreshColor();
    }

    public string Logo
    {
        get;
        set
        {
            if ((value ?? "") == (field ?? ""))
                return;
            field = value ?? string.Empty;
            Path.Data = string.IsNullOrWhiteSpace(value)
                ? null
                : (Geometry)new GeometryConverter().ConvertFromString(value);
            SvgIconControlHelper.ApplyVisibility(Path, ShapeSvgIcon, IsUsingSvgIcon);
            RefreshIconHostVisibility();
        }
    } = string.Empty;

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
            RefreshIconHostVisibility();
        }
    } = string.Empty;

    private bool IsUsingSvgIcon => SvgIconControlHelper.HasSvgIcon(SvgIcon);

    private double EffectiveLogoScale => IsUsingSvgIcon ? 1D : LogoScale;

    private bool HasAnyIcon => IsUsingSvgIcon || !string.IsNullOrWhiteSpace(Logo);

    private void ApplyLogoScale()
    {
        IconHost?.RenderTransform = new ScaleTransform
        {
            ScaleX = EffectiveLogoScale,
            ScaleY = EffectiveLogoScale
        };
    }

    private void RefreshIconHostVisibility()
    {
        if (IconHost is null || LabText is null)
            return;

        if (HasAnyIcon)
        {
            IconHost.IsVisible = true;
            IconHost.Width = 16;
            IconHost.Margin = new Thickness(2, 12, 0, 12);
            LabText.Margin = new Thickness(12, 0, 0, 0.8);
        }
        else
        {
            IconHost.IsVisible = false;
            IconHost.Width = 0;
            IconHost.Margin = new Thickness(0, 12, 0, 12);
            LabText.Margin = new Thickness(0, 0, 0, 0.8);
        }
    }

    public double LogoScale
    {
        get;
        set
        {
            field = value;
            ApplyLogoScale();
        }
    } = 1d;

    // 显示文本
    [Content] // [port] WPF 绫荤骇 [ContentProperty("Inlines")] 鈫?Avalonia 12 灞炴€х骇 [Content]
    public InlineCollection Inlines => LabText.Inlines;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set
        {
            if (value is null) return;
            SetValue(TextProperty, value);
        }
    }

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
                    Opacity = 0d;
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(this, 1d - Opacity, 80, 50),
                            ModAnimation.AaScaleTransform(this, 0.15d - ((ScaleTransform)RenderTransform).ScaleX, 400,
                                50, new ModAnimation.AniEaseOutBack()),
                            ModAnimation.AaScaleTransform(this, 0.85d, 160, 50, new ModAnimation.AniEaseOutFluent())
                        }, "MyExtraTextButton MainScale " + Uuid);
                }
                else
                {
                    // 没了
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(this, -Opacity, 50, 50),
                            ModAnimation.AaScaleTransform(this, -((ScaleTransform)RenderTransform).ScaleX, 100,
                                ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak))
                        }, "MyExtraTextButton MainScale " + Uuid);
                }

                IsHitTestVisible = value; // 防止缩放动画中依然可以点进去
            });
        }
    }

    // 声明
    public event ClickEventHandler? Click;

    private void StartScaleAnimation(double targetScale, double reboundScale, int reboundDuration = 60)
    {
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScaleTransform(PanScale, targetScale - ((ScaleTransform)PanScale.RenderTransform).ScaleX,
                    800, ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)),
                ModAnimation.AaScaleTransform(PanScale, reboundScale, reboundDuration,
                    ease: new ModAnimation.AniEaseOutFluent())
            }, "MyExtraTextButton Scale " + Uuid);
    }

    private void RefreshScaleAfterRelease()
    {
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScaleTransform(PanScale, 1d - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 300,
                    ease: new ModAnimation.AniEaseOutBack())
            }, "MyExtraTextButton Scale " + Uuid);
    }

    // 触发点击事件
    private void Button_LeftPointerReleased(object sender, PointerPressedEventArgs e)
    {
        if (!isLeftMouseHeld) return;
        ModBase.Log("[Control] 按下附加图标按钮：" + Text);
        Click?.Invoke(sender, e);
        e.Handled = true;
        ModMain.RaiseCustomEvent(this);
        Button_LeftPointerReleased();
    }

    private void Button_LeftMouseDown(object sender, PointerPressedEventArgs e)
    {
        if (!isLeftMouseHeld)
            StartScaleAnimation(0.85d, -0.05d);
        isLeftMouseHeld = true;
        Focus();
    }

    private void Button_LeftPointerReleased()
    {
        RefreshScaleAfterRelease();
        isLeftMouseHeld = false;
        RefreshColor(); // 直接刷新颜色以判断是否已触发 PointerExited
    }

    private void Button_RightPointerReleased(object sender, PointerEventArgs e)
    {
        if (!isLeftMouseHeld)
            RefreshScaleAfterRelease();
        RefreshColor(); // 直接刷新颜色以判断是否已触发 PointerExited
    }

    private void Button_PointerExited(object sender, PointerEventArgs e)
    {
        isLeftMouseHeld = false;
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScaleTransform(PanScale, 1d - ((ScaleTransform)PanScale.RenderTransform).ScaleX, 500,
                    ease: new ModAnimation.AniEaseOutFluent())
            }, "MyExtraTextButton Scale " + Uuid);
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
                        "MyExtraTextButton Color " + Uuid);
                else if (IsPointerOver)
                    // 指向
                    ModAnimation.AniStart(
                        ModAnimation.AaColor(PanColor, BackgroundProperty, "ColorBrush4", animationColorIn),
                        "MyExtraTextButton Color " + Uuid);
                else
                    // 普通
                    ModAnimation.AniStart(
                        ModAnimation.AaColor(PanColor, BackgroundProperty, "ColorBrush3", animationColorOut),
                        "MyExtraTextButton Color " + Uuid);
            }

            else
            {
                ControlVisualHelpers.AnimateColorOrSetResource(PanColor, BackgroundProperty,
                    !IsEnabled ? "ColorBrushGray4" : IsPointerOver ? "ColorBrush4" : "ColorBrush3",
                    !IsEnabled || IsPointerOver ? animationColorIn : animationColorOut,
                    "MyExtraTextButton Color " + Uuid, false);
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "刷新附加图标按钮颜色出错");
        }
    }
}
