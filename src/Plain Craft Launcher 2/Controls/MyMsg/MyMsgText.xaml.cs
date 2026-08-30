using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Input;
// [port] Avalonia.Interop removed
using PCL.Core.UI.Controls;

using PCL.Core.App.Localization;
namespace PCL;

public partial class MyMsgText
{
    private readonly ModMain.MyMsgBoxConverter myConverter;
    private readonly int uuid = ModBase.GetUuid();

    public MyMsgText(ModMain.MyMsgBoxConverter converter)
    {
        try
        {
            InitializeComponent();
            AppendUniqueNameSuffix(Btn1);
            AppendUniqueNameSuffix(Btn2);
            AppendUniqueNameSuffix(Btn3);
            myConverter = converter;
            LabTitle.Text = converter.Title;
            LabCaption.Text = converter.Text;
            ConfigurePrimaryButton(converter.Button1, converter.IsWarn);
            ConfigureSecondaryButton(Btn2, converter.Button2);
            ConfigureSecondaryButton(Btn3, converter.Button3);
            ShapeLine.StrokeThickness = ModBase.GetWPFSize(1d);
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "普通弹窗初始化失败",
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Application.Control.MessageBox.Error.OperationFailed"));
        }

        Loaded += Load;
    }

    private void AppendUniqueNameSuffix(Control element)
    {
        element.Name += ModBase.GetUuid();
    }

    private void ConfigurePrimaryButton(string text, bool isWarn)
    {
        Btn1.Text = text;
        if (isWarn)
        {
            Btn1.ColorType = MyButton.ColorState.Red;
            LabTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight");
        }
    }

    private static void ConfigureSecondaryButton(MyButton button, string text)
    {
        button.Text = text;
        button.IsVisible = string.IsNullOrEmpty(text) ? false : true;
    }

    private void Load(object sender, RoutedEventArgs e)
    {
        try
        {
            // UI 初始化
            if (Btn2.IsVisible && !(Btn1.ColorType == MyButton.ColorState.Red))
                Btn1.ColorType = MyButton.ColorState.Highlight;
            Btn1.Focus();
            // 动画
            Opacity = 0d;
            ModAnimation.AniStart(
                ModAnimation.AaColor(ModMain.frmMain.PanMsgBackground, BlurBorder.BackgroundProperty,
                    (myConverter.IsWarn
                        ? new ModBase.MyColor(140d, 80d, 0d, 0d)
                        : new ModBase.MyColor(90d, 0d, 0d, 0d)) - ModMain.frmMain.PanMsgBackground.Background, 200),
                "PanMsgBackground Background");
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaOpacity(this, 1d, 120, 60),
                    ModAnimation.AaDouble(i => TransformPos.Y += (double)i,
                        -TransformPos.Y, 300, 60, new ModAnimation.AniEaseOutBack(ModAnimation.AniEasePower.Weak)),
                    ModAnimation.AaDouble(i => TransformRotate.Angle += (double)i,
                        -TransformRotate.Angle, 300, 60,
                        new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak))
                }, "MyMsgBox " + uuid);
            // 记录日志
            ModBase.Log("[Control] 普通弹窗：" + LabTitle.Text + "\r\n" + LabCaption.Text);
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "普通弹窗加载失败",
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Application.Control.MessageBox.Error.OperationFailed"));
        }
    }

    private void Close()
    {
        // 结束线程阻塞
        if (myConverter.ForceWait || !string.IsNullOrEmpty(myConverter.Button2))
            myConverter.WaitFrame.Continue = false;
        // [port] 移除 WPF 的 ComponentDispatcher.PopModal()：Avalonia 无 ComponentDispatcher（WPF 模态消息环）。
        //       模态阻塞由调用方 ModMain 的 Dispatcher.PushFrame(converter.WaitFrame) 提供，
        //       并由上文 WaitFrame.Continue = false 解除；本弹窗是主窗体上的覆盖网格，不直接参与模态环控制。
        // 动画
        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaCode(() =>
            {
                if (!ModMain.WaitingMyMsgBox.Any())
                    ModAnimation.AniStart(ModAnimation.AaColor(ModMain.frmMain.PanMsgBackground,
                        BlurBorder.BackgroundProperty,
                        new ModBase.MyColor(0d, 0d, 0d, 0d) - ModMain.frmMain.PanMsgBackground.Background, 200,
                        ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)));
            }, 30),
            ModAnimation.AaOpacity(this, -Opacity, 80, 20),
            ModAnimation.AaDouble(i => TransformPos.Y += (double)i, 20d - TransformPos.Y,
                150, 0, new ModAnimation.AniEaseOutFluent()),
            ModAnimation.AaDouble(i => TransformRotate.Angle += (double)i,
                6d - TransformRotate.Angle, 150, 0, new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
            ModAnimation.AaCode(() => ((Grid)Parent).Children.Remove(this), after: true)
        }, "MyMsgBox " + uuid);
    }

    public void Btn1_Click(object? sender = null, PointerPressedEventArgs? e = null)
    {
        if (myConverter.IsExited)
            return;
        if (myConverter.Button1Action is not null)
        {
            myConverter.Button1Action();
        }
        else
        {
            myConverter.IsExited = true;
            myConverter.Result = 1;
            Close();
        }
    }

    public void Btn2_Click(object sender, PointerPressedEventArgs e)
    {
        if (myConverter.IsExited)
            return;
        if (myConverter.Button2Action is not null)
        {
            myConverter.Button2Action();
        }
        else
        {
            myConverter.IsExited = true;
            myConverter.Result = 2;
            Close();
        }
    }

    public void Btn3_Click(object sender, PointerPressedEventArgs e)
    {
        if (myConverter.IsExited)
            return;
        if (myConverter.Button3Action is not null)
        {
            myConverter.Button3Action();
        }
        else
        {
            myConverter.IsExited = true;
            myConverter.Result = 3;
            Close();
        }
    }

    private void Drag(object sender, PointerPressedEventArgs e)
    {
        try
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                if (e.GetPosition(ShapeLine).Y <= 2d)
                    ModMain.frmMain.DragMove();
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "拖拽移动失败",
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Application.Control.MessageBox.Error.OperationFailed"));
        }
    }
}
