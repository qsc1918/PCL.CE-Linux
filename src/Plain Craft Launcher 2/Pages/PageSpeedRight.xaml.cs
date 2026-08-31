namespace PCL;

public partial class PageSpeedRight : MyPageRight
{
    public PageSpeedRight()
    {
        InitializeComponent();
        Loaded += (_, _) => Init();
    }

    private void Init()
    {
        PanBack.ScrollToHome();
    }
}