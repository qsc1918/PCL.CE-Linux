using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;

namespace PCL;

public partial class MySearchBox : MyCard
{
    public delegate void SearchEventHandler(object sender, EventArgs e);

    public delegate void TextChangedEventHandler(object sender, EventArgs e);

    public MySearchBox()
    {
        InitializeComponent();

        Loaded += MySearchBox_Loaded;
    }
    
    private void MySearchBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ModMain.RaiseCustomEvent(this);
    }
    
    // 属性
    public string HintText
    {
        get => (string)GetValue(HintTextProperty);
        set => SetValue(HintTextProperty, value);
    }

    public static readonly StyledProperty<string> HintTextProperty =
        AvaloniaProperty.Register<MySearchBox, string>(nameof(HintText), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MySearchBox, string>(nameof(Text), string.Empty);

    static MySearchBox()
    {
        HintTextProperty.Changed.AddClassHandler<MySearchBox>((d, e) => d.TextBox.HintText = (string)e.NewValue);
        TextProperty.Changed.AddClassHandler<MySearchBox>((d, e) => d.TextBox.Text = (string)e.NewValue);
    }

    public bool SearchButtonVisibility
    {
        get => BtnSearch.IsVisible;
        set
        {
            BtnClear.Margin = new Thickness(0d, 0d, value == true ? 70 : 10, 0d);
            BtnSearch.IsVisible = value;
        }
    }

    public event TextChangedEventHandler? TextChanged;

    private void MySearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        TextBox.Focus();
    }

    private void Text_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateClearButtonState();
        SetCurrentValue(TextProperty, TextBox.Text);

        TextChanged?.Invoke(sender, e);
    }

    private void BtnClear_Click(object sender, EventArgs e)
    {
        TextBox.Text = "";
        TextBox.Focus();
    }

    public event SearchEventHandler? Search;

    private void BtnSearch_Click(object sender, PointerPressedEventArgs e)
    {
        Search?.Invoke(sender, e);
    }

    private void UpdateClearButtonState()
    {
        var hasText = !string.IsNullOrEmpty(TextBox.Text);
        ModAnimation.AniStart(ModAnimation.AaOpacity(BtnClear, hasText ? 1d - BtnClear.Opacity : -BtnClear.Opacity, 90),
            "MySearchBox ClearBtn " + uuid);
        BtnClear.IsHitTestVisible = hasText;
    }
}
