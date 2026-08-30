using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public partial class PageSetupGameManage
{
    private new bool isLoaded;

    public PageSetupGameManage()
    {
        InitializeComponent();
        Loaded += PageSetupSystem_Loaded;
    }

    private void PageSetupSystem_Loaded(object sender, RoutedEventArgs e)
    {
        // 閲嶅鍔犺浇閮ㄥ垎
        PanBack.ScrollToHome();

        // 闈為噸澶嶅姞杞介儴鍒?
        if (isLoaded)
            return;
        isLoaded = true;

        ModAnimation.AniControlEnabled += 1;
        Reload();
        SliderLoad();

        if (!Lang.IsChineseMainland)
        {
            TextFilenameFormat.Visibility = Visibility.Collapsed;
            ComboDownloadTranslateV2.Visibility = Visibility.Collapsed;
            TextModManageStyle.Visibility = Visibility.Collapsed;
            ComboModLocalNameStyle.Visibility = Visibility.Collapsed;
            
            RowFilenameFormat.Height = new GridLength(0);
            RowFilenameFormatGap.Height = new GridLength(0);
            RowModManageStyle.Height = new GridLength(0);
            RowModManageStyleGap.Height = new GridLength(0);
        }

        ModAnimation.AniControlEnabled -= 1;
    }

    public void Reload()
    {
        // 涓嬭浇
        SliderDownloadThread.Value = Config.Download.ThreadLimit;
        SliderDownloadSpeed.Value = Config.Download.SpeedLimit;
        ComboDownloadSource.SelectedIndex = Config.Download.FileSource;
        ComboDownloadVersion.SelectedIndex = Config.Download.VersionListSource;
        CheckDownloadAutoSelectVersion.Checked = Config.Download.AutoSelectInstance;
        CheckFixAuthlib.Checked = Config.Download.FixAuthLib;

        // Mod 涓庢暣鍚堝寘
        ComboDownloadTranslateV2.SelectedIndex = Config.Download.Comp.NameFormatV2;
        ComboDownloadMod.SelectedIndex = Config.Download.Comp.CompSourceSolution;
        ComboModLocalNameStyle.SelectedIndex = Config.Download.Comp.UiCompNameSolution;
        ComboDownloadQuickBehavior.SelectedIndex = Config.Download.Comp.QuickDownloadBehavior;
        CheckDownloadIgnoreQuilt.Checked = Config.Download.Comp.IgnoreQuilt;
        CheckDownloadAutoInstallDependencies.Checked = Config.Download.Comp.AutoInstallDependencies;
        CheckDownloadClipboard.Checked = Config.Download.Comp.ReadClipboard;

        // Minecraft 鏇存柊鎻愮ず
        CheckUpdateRelease.Checked = Config.Tool.ReleaseNotification;
        CheckUpdateSnapshot.Checked = Config.Tool.SnapshotNotification;

        // 杈呭姪璁剧疆
        CheckHelpLauncherLanguage.Checked = Config.Tool.AutoChangeLanguage;
    }

    // 鍒濆鍖?
    public void Reset()
    {
        try
        {
            Config.Download.Reset();
            Config.Tool.Reset();
            ModBase.Log("[Setup] 宸插垵濮嬪寲鍏朵粬椤佃缃?);
            HintService.Hint(Lang.Text("Setup.GameManage.Initialized"), HintType.Success, false);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.GameManage.Error.InitFailed"),
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Setup.GameManage.Error.InitFailed"));
        }

        Reload();
    }

    // 灏嗘帶浠舵敼鍙樿矾鐢卞埌璁剧疆鏀瑰彉
    private void CheckBoxChange(object senderRaw, bool user)
    {
        var sender = (MyCheckBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Checked);
    }

    private void SliderChange(object senderRaw, bool user)
    {
        var sender = (MySlider)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Value);
    }

    private void ComboChange(object senderRaw, SelectionChangedEventArgs e)
    {
        var sender = (MyComboBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.SelectedIndex);
    }

    private static void SetByTag(string tag, object value)
        => ConfigService.TrySetValue(tag, value);

    // 婊戝姩鏉?
    private void SliderLoad()
    {
        SliderDownloadThread.getHintText = new Func<object, object>(v => (int)v + 1);
        SliderDownloadSpeed.getHintText = new Func<object, object>(v =>
        {
            int value = (int)v;
            switch (value)
            {
                case <= 14:
                    return Lang.Number((value + 1) * 0.1d, "N1") + " MiB/s";
                case <= 31:
                    return Lang.Number((value - 11) * 0.5d, "N1") + " MiB/s";
                case <= 41:
                    return Lang.Number(value - 21, "N0") + " MiB/s";
                default:
                    return Lang.Text("Setup.GameManage.Download.Unlimited");
            }
        });
    }

    private void SliderDownloadThread_PreviewChange(object sender, ModBase.RouteEventArgs e)
    {
        if (SliderDownloadThread.Value < 100)
            return;
        if (!States.Hint.LargeDownloadThread)
        {
            States.Hint.LargeDownloadThread = true;
            ModMain.MyMsgBox(
                Lang.Text("Setup.GameManage.Download.Threads.TooManyWarning.Message"),
                Lang.Text("Common.Dialog.Warning"),
                Lang.Text("Setup.GameManage.Download.Threads.TooManyWarning.Confirm"), isWarn: true);
        }
    }
}
