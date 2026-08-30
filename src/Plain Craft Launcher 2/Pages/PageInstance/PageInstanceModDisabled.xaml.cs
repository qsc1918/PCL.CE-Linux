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
            .Launch); // 鍦ㄥ疄渚嬮€夋嫨椤甸潰閫夊畾瀹炰緥鐨勬椂鍊欏彧浼氳繑鍥炰竴灞傦紝鍥犳濡傛灉涓嶅厛閿氬畾 Launch锛屽湪閫夋嫨瀹炰緥鍚庝細鍥為€€鍒板疄渚嬭缃殑杩欎釜椤甸潰
        ModMain.frmMain.PageChange(FormMain.PageType.InstanceSelect);
    }

    public void BtnDownload_Loaded(object? sender = null, RoutedEventArgs? e = null)
    {
        var newVisibility =
            (Config.Preference.Hide.PageDownload && !PageSetupUI.HiddenForceShow) ||
            (ModMain.frmSelectRight is not null && ModMain.frmSelectRight.showHidden)
                ? Visibility.Collapsed
                : Visibility.Visible;
        if (BtnDownload.Visibility != newVisibility)
        {
            BtnDownload.Visibility = newVisibility;
            PanMain.TriggerForceResize();
        }
    }
}
