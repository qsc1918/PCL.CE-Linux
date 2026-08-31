using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Markup;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.UI.Theme;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;

using Avalonia.Metadata;

namespace PCL;
public partial class MyHint : Border
{
    // 配色
    public enum Themes
    {
        Blue = 0,
        Red = 1,
        Yellow = 2
    }

    public static readonly AvaloniaProperty IsWarnProperty = AvaloniaProperty.Register("IsWarn", typeof(bool),
        typeof(MyHint),
        new PropertyMetadata(true,
            (d, e) =>
            {
                var f = (MyHint)d;
                f.Theme = e.NewValue is not null ? Themes.Red : Themes.Blue;
            }));

    public static readonly AvaloniaProperty TextProperty = AvaloniaProperty.Register("Text", typeof(string),
        typeof(MyHint), new PropertyMetadata("", (d, e) =>
        {
            var f = (MyHint)d;
            f.LabText.Text = (string)e.NewValue;
        }));

    // 触发点击事件
    private bool isMouseDown;
    public int Uuid = ModBase.GetUuid();

    public MyHint()
    {
        InitializeComponent();
        UpdateUI();
        Loaded += (_, _) => UpdateUI();
        Loaded += MyHint_Loaded;
        PointerReleased += MyHint_PointerReleased;
        PointerPressed += MyHint_MouseDown;
        PointerExited += (_, _) => MyHint_PointerExited();
        Unloaded += (_, _) => Dispose();
    }

    // 边框
    public bool HasBorder
    {
        get => BorderThickness.Top > 0d;
        set
        {
            if (value)
                BorderThickness = new Thickness(3d, ModBase.GetWPFSize(1d), ModBase.GetWPFSize(1d), ModBase.GetWPFSize(1d));
            else
                BorderThickness = new Thickness(3d, 0d, 0d, 0d);
        }
    }

    public Themes Theme
    {
        get => field;
        set
        {
            field = value;
            UpdateUI();
        }
    } = Themes.Red;

    [Obsolete("IsWarn 已过时。请换用 Theme 属性。")]
    public bool IsWarn
    {
        get => Theme == Themes.Red;
        set => Theme = value ? Themes.Red : Themes.Blue;
    }

    // 文本
    [Content] // [port] WPF 绫荤骇 [ContentProperty("Inlines")] 鈫?Avalonia 12 灞炴€х骇 [Content]
    public InlineCollection Inlines => LabText.Inlines;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // 关闭按钮
    public bool CanClose
    {
        get => BtnClose.Visibility == true;
        set => BtnClose.IsVisible = value ? true : false;
    }

    public string RelativeSetup { get; set; } = "";

    private void UpdateUI()
    {
        var hue = default(double);
        switch (Theme)
        {
            case Themes.Blue:
            {
                hue = 210d;
                break;
            }
            case Themes.Red:
            {
                hue = 355d;
                break;
            }
            case Themes.Yellow:
            {
                hue = 40d;
                break;
            }
        }

        var s = ThemeService.CurrentTone;
        Background = new ModBase.MyColor().FromHSL2(hue, 90, s.L7 * 100);
        BorderBrush = new ModBase.MyColor().FromHSL2(hue, 90, s.L2 * 100);
        LabText.Foreground = new ModBase.MyColor().FromHSL2(hue, 90, s.L2 * 100);
        BtnClose.Foreground = new ModBase.MyColor().FromHSL2(hue, 90, s.L2 * 100);

        // 根据提示气泡对齐方向刷新边框
        // 此处依赖 HasBorder 的副作用进行范围检查
        HasBorder = HasBorder;
    }

    private void MyHint_Loaded(object sender, RoutedEventArgs e)
    {
        ThemeService.ColorModeChanged += (v, theme) => _ThemeChanged(v, theme);
        if (CanClose && ConfigService.TryGetConfigItemNoType(RelativeSetup, out var item) && item.GetValueNoType() is not null)
            Visibility = false;
    }

    private void BtnClose_Click(object sender, EventArgs e)
    {
        if (ConfigService.TryGetConfigItemNoType(RelativeSetup, out var item))
            item.SetValueNoType(true);
        ModAnimation.AniDispose(this, false);
    }

    private void MyHint_PointerReleased(object sender, PointerPressedEventArgs e)
    {
        if (!isMouseDown)
            return;
        isMouseDown = false;
        ModBase.Log("[Control] 按下提示条" + (string.IsNullOrEmpty(Name) ? "" : "：" + Name));
        e.Handled = true;
        ModMain.RaiseCustomEvent(this);
    }

    private void MyHint_MouseDown(object sender, PointerPressedEventArgs e)
    {
        isMouseDown = true;
    }

    private void MyHint_PointerExited()
    {
        isMouseDown = false;
    }

    private void _ThemeChanged(bool isDarkMode, ColorTheme theme)
    {
        UpdateUI();
    }

    private void Dispose()
    {
        ThemeService.ColorModeChanged -= _ThemeChanged;
    }
}

public static partial class ModAnimation
{
    public static void AniDispose(MyHint control, bool removeFromChildren, ParameterizedThreadStart callBack = null)
    {
        if (!control.IsHitTestVisible)
            return;
        control.IsHitTestVisible = false;
        AniStart(new[]
        {
            AaScaleTransform(control, -0.08d, 200, ease: new AniEaseInFluent()),
            AaOpacity(control, -1, 200, ease: new AniEaseOutFluent()),
            AaHeight(control, -control.Bounds.Height, 150, 100, new AniEaseOutFluent()),
            AaCode(() =>
            {
                if (removeFromChildren)
                    ((Panel)control.Parent).Children.Remove(control);
                else
                    control.IsVisible = false;
                if (callBack is not null)
                    callBack(control);
            }, after: true)
        }, "MyCard Dispose " + control.Uuid);
    }
}
