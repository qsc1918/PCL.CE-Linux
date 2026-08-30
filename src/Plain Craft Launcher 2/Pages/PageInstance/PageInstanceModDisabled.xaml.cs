using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using PCL.Core.App;

namespace PCL;

public partial class PageInstanceModDisabled
{
    public PageInstanceModDisabled()
    {
        InitializeComponent();
        BtnDownload.Click += BtnDownload_Click;
        BtnVersion.Click += BtnVersion_Click;
        BtnDownload.Loaded += BtnDownload_Loaded;
    }

    private void BtnDownload_Click(object sender, EventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall);
    }

    private void BtnVersion_Click(object sender, EventArgs e)
    {
        ModMain.frmMain.PageChange(FormMain.PageType
            .Launch); // 閸︺劌鐤勬笟瀣偓澶嬪妞ょ敻娼伴柅澶婄暰鐎圭偘绶ラ惃鍕閸婃瑥褰ф导姘崇箲閸ョ偘绔寸仦鍌︾礉閸ョ姵顒濇俊鍌涚亯娑撳秴鍘涢柨姘暰 Launch閿涘苯婀柅澶嬪鐎圭偘绶ラ崥搴濈窗閸ョ偤鈧偓閸掓澘鐤勬笟瀣啎缂冾喚娈戞潻娆庨嚋妞ょ敻娼?
        ModMain.frmMain.PageChange(FormMain.PageType.InstanceSelect);
    }

    public void BtnDownload_Loaded(object? sender = null, RoutedEventArgs? e = null)
    {
        var newVisibility =
            (Config.Preference.Hide.PageDownload && !PageSetupUI.HiddenForceShow) ||
            (ModMain.frmSelectRight is not null && ModMain.frmSelectRight.showHidden)
                ? false
                : true;
        if (BtnDownload.IsVisible != newVisibility)
        {
            BtnDownload.IsVisible = newVisibility;
            PanMain.TriggerForceResize();
        }
    }
}
