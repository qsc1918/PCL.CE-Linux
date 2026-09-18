using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Controls.Documents;
using Avalonia.Markup;

using Avalonia.Metadata;

namespace PCL;
public partial class MyCheckBox : Grid
{
    public delegate void ChangeEventHandler(object sender, bool user);

    public delegate void PreviewChangeEventHandler(object sender, ModBase.RouteEventArgs e);

    private const int animationTimeOfCheck = 150; // 勾选状态变更动画长度

    // 指向动画

    private const int animationTimeOfMouseIn = 100;

    private const int animationTimeOfMouseOut = 200;

    // 在使用 XAML 设置 Checked 属性时，不会触发 Checked_Set 方法，所以需要在这里手动触发 UI 改变
    public static readonly StyledProperty<bool?> CheckedProperty = AvaloniaProperty.Register<MyCheckBox, bool?>(
        nameof(Checked), false);

    /// <summary>
    ///     是否为三态复选框。
    /// </summary>
    public static readonly StyledProperty<bool> IsThreeStateProperty =
        AvaloniaProperty.Register<MyCheckBox, bool>("IsThreeState", false);

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MyCheckBox, string>(
        nameof(Text), string.Empty);

    static MyCheckBox()
    {
        CheckedProperty.Changed.AddClassHandler<MyCheckBox>((d, e) =>
        {
            if (!d.IsLoaded) d.SyncUI();
        });
        TextProperty.Changed.AddClassHandler<MyCheckBox>((d, e) => d.LabText.Text = (string)e.NewValue);
    }

    private bool? _previousState = false; // 上一次的勾选状态
    private bool allowMouseDown = true;

    // 点击事件

    private bool mouseDowned;

    // 基础

    public int Uuid = ModBase.GetUuid();

    public MyCheckBox()
    {
        InitializeComponent();

        PointerReleased += (_, _) => Checkbox_PointerReleased();
        PointerPressed += (_, _) => Checkbox_MouseDown();
        PointerExited += (_, _) => Checkbox_PointerExited();
        // [port] IsEnabledChanged -> Avalonia PropertyChanged 上的 IsEnabledProperty
        this.PropertyChanged += (_, e) => { if (e.Property == IsEnabledProperty) Checkbox_IsEnabledChanged(); };
        PointerEntered += (_, _) => Checkbox_PointerEnteredAnimation();
        PointerExited += (_, _) => Checkbox_PointerExitedAnimation();
    }

    // 自定义属性
    public bool? Checked
    {
        get => (bool?)GetValue(CheckedProperty);
        set => SetChecked(value, false);
    }

    public bool IsThreeState
    {
        get => (bool)GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    } // 是否为三态复选框
    [Content] // [port] WPF 绫荤骇 [ContentProperty("Inlines")] 鈫?Avalonia 12 灞炴€х骇 [Content]

    public InlineCollection Inlines => LabText.Inlines;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    } // 内容

    /// <summary>
    ///     复选框勾选状态改变。
    /// </summary>
    /// <param name="user">是否为用户手动改变的勾选状态。</param>
    public event ChangeEventHandler? Change;

    public event PreviewChangeEventHandler? PreviewChange;

    /// <summary>
    ///     手动设置 Checked 属性。
    /// </summary>
    /// <param name="value">新的 Checked 属性。</param>
    /// <param name="user">是否由用户引发。</param>
    public void SetChecked(bool? value, bool user)
    {
        try
        {
            if (Checked.HasValue && value.HasValue && Checked.Value == value.Value)
                return;

            // Preview 事件
            if (value.HasValue && value.Value && user)
            {
                var e = new ModBase.RouteEventArgs(user);
                PreviewChange?.Invoke(this, e);
                if (e.handled)
                {
                    mouseDowned = true;
                    Checkbox_PointerExited();
                    mouseDowned = false;
                    return;
                }
            }

            // 判断真实勾选状态
            var isChecked = GetFinalState(value, IsThreeState);

            _previousState = Checked; // 记录上一次的勾选状态
            SetValue(CheckedProperty, isChecked);
            if (IsLoaded)
                Change?.Invoke(this, user);

            // 更改动画
            SyncUI();
            ModMain.RaiseCustomEvent(this);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "设置 Checked 失败");
        }
    }

    private void SyncUI()
    {
        if (ControlVisualHelpers.ShouldAnimate(this)) // 防止默认属性变更触发动画
        {
            allowMouseDown = false;

            var isChecked = GetFinalState(Checked, IsThreeState);

            switch (isChecked, _previousState)
            {
                case (true, null):
                    AniBackgroundScale();
                    AniIndeterminateHide();
                    AniCheckShow();
                    AniColorChecked();
                    AniAllowMouseDown();
                    break;

                case (true, false):
                    AniBackgroundScale();
                    AniCheckShow();
                    AniColorChecked();
                    AniAllowMouseDown();
                    break;

                case (false, true):
                    AniBackgroundScale();
                    AniCheckHide();
                    AniColorUnchecked();
                    AniAllowMouseDown();
                    break;

                case (false, null):
                    AniBackgroundScale();
                    AniIndeterminateHide();
                    AniCheckHide();
                    AniColorUnchecked();
                    AniAllowMouseDown();
                    break;

                case (null, true):
                    AniBackgroundScale();
                    AniCheckHide();
                    AniIndeterminateShow();
                    AniColorUnchecked();
                    AniAllowMouseDown();
                    break;

                case (null, false):
                    AniBackgroundScale();
                    AniIndeterminateShow();
                    AniColorUnchecked();
                    AniAllowMouseDown();
                    break;
            }
        }
        else
        {
            // 不使用动画
            ModAnimation.AniStop("MyCheckBox Background Scale " + Uuid);
            ModAnimation.AniStop("MyCheckBox Check Scale Show" + Uuid);
            ModAnimation.AniStop("MyCheckBox Check Scale Hide" + Uuid);
            ModAnimation.AniStop("MyCheckBox Indeterminate Scale Show" + Uuid);
            ModAnimation.AniStop("MyCheckBox Indeterminate Scale Hide" + Uuid);
            ModAnimation.AniStop("MyCheckBox BorderColor " + Uuid);
            ModAnimation.AniStop("MyCheckBox AllowMouseDown " + Uuid);
            if (Checked == true)
            {
                ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX = 1d;
                ((ScaleTransform)ShapeCheck.RenderTransform).ScaleY = 1d;
                ShapeBorder.SetResourceReference(Border.BorderBrushProperty,
                    IsEnabled ? "ColorBrush2" : "ColorBrushGray4");
            }
            else
            {
                ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX = 0d;
                ((ScaleTransform)ShapeCheck.RenderTransform).ScaleY = 0d;
                ShapeBorder.SetResourceReference(Border.BorderBrushProperty,
                    IsEnabled ? "ColorBrush1" : "ColorBrushGray4");
            }
        }
    }

    private void Checkbox_PointerReleased()
    {
        if (!mouseDowned)
            return;
        ModBase.Log("[Control] 按下复选框（" + !Checked + "）：" + Text);
        mouseDowned = false;
        if (IsThreeState)
        {
            switch (Checked)
            {
                case true:
                    SetChecked(null, true);
                    break;
                case false:
                    SetChecked(true, true);
                    break;
                case null:
                    SetChecked(false, true);
                    break;
            }

            ModAnimation.AniStart(
                ModAnimation.AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100),
                "MyCheckBox Background " + Uuid);
            return;
        }

        SetChecked(!Checked, true);
        ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100),
            "MyCheckBox Background " + Uuid);
    }

    private void Checkbox_MouseDown()
    {
        if (!allowMouseDown)
            return;
        mouseDowned = true;
        Focus();
        ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushBg1", 100),
            "MyCheckBox Background " + Uuid);
        var scaleAnims = new List<ModAnimation.AniData>
        {
            ModAnimation.AaScale(ShapeBorder, 16.5d - ShapeBorder.Width, 1000,
                ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong), absolute: true)
        };
        if (Checked == true)
            scaleAnims.Add(ModAnimation.AaScaleTransform(ShapeCheck,
                0.9d - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX, 1000,
                ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)));
        ModAnimation.AniStart(scaleAnims.ToArray(), "MyCheckBox Scale " + Uuid);
    }

    private void Checkbox_PointerExited()
    {
        if (!mouseDowned)
            return;
        mouseDowned = false;
        ModAnimation.AniStart(ModAnimation.AaColor(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite", 100),
            "MyCheckBox Background " + Uuid);
        var scaleAnims = new List<ModAnimation.AniData>
        {
            ModAnimation.AaScale(ShapeBorder, 18d - ShapeBorder.Width,
                ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong), absolute: true)
        };
        if (Checked == true)
            scaleAnims.Add(ModAnimation.AaScaleTransform(ShapeCheck, 1d - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX,
                500, ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Strong)));
        ModAnimation.AniStart(scaleAnims.ToArray(), "MyCheckBox Scale " + Uuid);
    }

    private void Checkbox_IsEnabledChanged()
    {
        if (IsLoaded && ModAnimation.AniControlEnabled == 0) // 防止默认属性变更触发动画
        {
            // 有动画
            if (IsEnabled)
            {
                // 可用
                Checkbox_PointerExitedAnimation();
            }
            else
            {
                // 不可用
                ModAnimation.AniStart(
                    ModAnimation.AaColor(ShapeBorder, Border.BorderBrushProperty,
                        ThemeManager.colorGray4 - new ModBase.MyColor(ShapeBorder.BorderBrush), animationTimeOfMouseOut),
                    "MyCheckBox BorderColor " + Uuid);
                ModAnimation.AniStart(
                    ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty,
                        ThemeManager.colorGray4 - new ModBase.MyColor(LabText.Foreground), animationTimeOfMouseOut),
                    "MyCheckBox TextColor " + Uuid);
            }
        }
        else
        {
            // 无动画
            ModAnimation.AniStop("MyCheckBox TextColor " + Uuid);
            ModAnimation.AniStop("MyCheckBox BorderColor " + Uuid);
            LabText.SetResourceReference(TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray4");
            ShapeBorder.SetResourceReference(Border.BorderBrushProperty,
                IsEnabled ? Checked == true ? "ColorBrush2" : "ColorBrush1" : "ColorBrushGray4");
        }
    }

    private void Checkbox_PointerEnteredAnimation()
    {
        ModAnimation.AniStart(
            ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty, "ColorBrush3", animationTimeOfMouseIn),
            "MyCheckBox TextColor " + Uuid);
        ModAnimation.AniStart(
            ModAnimation.AaColor(ShapeBorder, Border.BorderBrushProperty, "ColorBrush3", animationTimeOfMouseIn),
            "MyCheckBox BorderColor " + Uuid);
    }

    private void Checkbox_PointerExitedAnimation()
    {
        if (!IsEnabled)
            return; // PointerExited 比 IsEnabledChanged 后执行，所以如果自定义事件修改了 IsEnabled，将导致显示错误
        ModAnimation.AniStart(
            ModAnimation.AaColor(LabText, TextBlock.ForegroundProperty,
                IsEnabled ? "ColorBrush1" : "ColorBrushGray4", animationTimeOfMouseOut),
            "MyCheckBox TextColor " + Uuid);
        ModAnimation.AniStart(
            ModAnimation.AaColor(ShapeBorder, Border.BorderBrushProperty,
                IsEnabled ? Checked == true ? "ColorBrush2" : "ColorBrush1" : "ColorBrushGray4",
                animationTimeOfMouseOut),
            "MyCheckBox BorderColor " + Uuid);
    }

    // 动画
    private void AniBackgroundScale()
    {
        ModAnimation.AniStart(
            new[]
            {
                ModAnimation.AaScale(ShapeBorder, 12d - ShapeBorder.Width, animationTimeOfCheck,
                    ease: new ModAnimation.AniEaseOutFluent(), absolute: true),
                ModAnimation.AaScale(ShapeBorder, 6d, animationTimeOfCheck * 2,
                    (int)Math.Round(animationTimeOfCheck * 0.7d), new ModAnimation.AniEaseOutBack(), absolute: true)
            }, "MyCheckBox Background Scale " + Uuid);
    }

    private void AniCheckShow()
    {
        ModAnimation.AniStart(
            ModAnimation.AaScaleTransform(ShapeCheck, 1d - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX,
                animationTimeOfCheck * 2, (int)Math.Round(animationTimeOfCheck * 0.7d),
                new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
            "MyCheckBox Check Scale Show" + Uuid);
    }

    private void AniCheckHide()
    {
        ModAnimation.AniStart(
            ModAnimation.AaScaleTransform(ShapeCheck, -((ScaleTransform)ShapeCheck.RenderTransform).ScaleX,
                (int)Math.Round(animationTimeOfCheck * 0.9d),
                ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
            "MyCheckBox Check Scale Hide" + Uuid);
    }

    private void AniIndeterminateShow()
    {
        ModAnimation.AniStart(
            ModAnimation.AaScaleTransform(ShapeIndeterminate,
                1d - ((ScaleTransform)ShapeIndeterminate.RenderTransform).ScaleX, animationTimeOfCheck * 2,
                (int)Math.Round(animationTimeOfCheck * 0.7d),
                new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
            "MyCheckBox Indeterminate Scale Show" + Uuid);
    }

    private void AniIndeterminateHide()
    {
        ModAnimation.AniStart(
            ModAnimation.AaScaleTransform(ShapeIndeterminate,
                -((ScaleTransform)ShapeIndeterminate.RenderTransform).ScaleX,
                (int)Math.Round(animationTimeOfCheck * 0.9d),
                ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
            "MyCheckBox Indeterminate Scale Hide" + Uuid);
    }

    private void AniAllowMouseDown()
    {
        ModAnimation.AniStart(ModAnimation.AaCode(() => allowMouseDown = true, animationTimeOfCheck * 2),
            "MyCheckBox AllowMouseDown " + Uuid);
    }

    private void AniColorChecked()
    {
        ModAnimation.AniStart(
            ModAnimation.AaColor(ShapeBorder, Border.BorderBrushProperty,
                IsEnabled ? IsPointerOver ? "ColorBrush3" : "ColorBrush2" : "ColorBrushGray4", animationTimeOfCheck),
            "MyCheckBox BorderColor " + Uuid);
    }

    private void AniColorUnchecked()
    {
        ModAnimation.AniStart(
            ModAnimation.AaColor(ShapeBorder, Border.BorderBrushProperty,
                IsEnabled ? IsPointerOver ? "ColorBrush3" : "ColorBrush1" : "ColorBrushGray4", animationTimeOfCheck),
            "MyCheckBox BorderColor " + Uuid);
    }

    private static bool? GetFinalState(bool? value, bool isThreeState)
    {
        return isThreeState ? value : value == true;
    }
}
