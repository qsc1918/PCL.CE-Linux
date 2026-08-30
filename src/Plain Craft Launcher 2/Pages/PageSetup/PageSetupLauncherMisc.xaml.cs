using System.Diagnostics;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.IoC;
using PCL.Core.UI;
using PCL.Core.App.Localization;
using PCL.Core.Utils.OS;

namespace PCL;

public partial class PageSetupLauncherMisc
{
    private bool isFirstLoad = true;

    private new bool isLoaded;

    public PageSetupLauncherMisc()
    {
        InitializeComponent();
        Loaded += PageSetupLink_Loaded;
        Loaded += (_, _) => Reload();
    }

    private void PageSetupLink_Loaded(object sender, RoutedEventArgs e)
    {
        // 闁插秴顦查崝鐘烘祰闁劌鍨?
        PanBack.ScrollToHome();

        // 闂堢偤鍣告径宥呭鏉炰粙鍎撮崚?
        if (isLoaded)
            return;
        isLoaded = true;

        ModAnimation.AniControlEnabled += 1;
        SliderLoad();
        Reload();
        ModAnimation.AniControlEnabled -= 1;
    }

    public void Reload()
    {
        // 缁崵绮虹拋鍓х枂
        ComboSystemActivity.SelectedIndex = States.System.AnnounceSolution;
        CheckSystemDisableHardwareAcceleration.Checked = Config.System.DisableHardwareAcceleration;
        SliderAniFPS.Value = Config.System.AnimationFpsLimit;
        SliderMaxLog.Value = Config.System.MaxGameLog;
        CheckSystemTelemetry.Checked = Config.System.Telemetry;

        // 缂冩垹绮?
        TextSystemHttpProxy.Text = Config.Network.HttpProxy.CustomAddress;
        TextSystemHttpProxyCustomUsername.Text = Config.Network.HttpProxy.CustomUsername;
        TextSystemHttpProxyCustomPassword.Text = Config.Network.HttpProxy.CustomPassword;
        ((MyRadioBox)FindName($"RadioHttpProxyType{Config.Network.HttpProxy.Type}")).SetChecked(true, false);
        CheckNetDohEnable.Checked = Config.Network.EnableDoH;

        // 鐠嬪啳鐦柅澶愩€?
        SliderDebugAnim.Value = Config.Debug.AnimationSpeed;
        CheckDebugSkipCopy.Checked = Config.Debug.DontCopy;
        CheckDebugMode.Checked = Config.Debug.Enabled;
        CheckDebugDelay.Checked = Config.Debug.AddRandomDelay;
    }

    // 閸掓繂顫愰崠?
    public void Reset()
    {
        try
        {
            Config.Network.Reset();
            Config.Debug.Reset();
            Config.System.Reset();
            ModBase.Log("[Setup] 瀹告彃鍨垫慨瀣閸氼垰濮╅崳?閺夊倿銆嶆い浣冾啎缂?);
            HintService.Hint(Lang.Text("Setup.Misc.Initialized"), HintType.Success, false);
            Reload();
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.Misc.Error.InitFailed"),
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Misc.Error.InitFailed"));
        }

        Reload();
    }

    // 鐏忓棙甯舵禒鑸垫暭閸欐鐭鹃悽鍗炲煂鐠佸墽鐤嗛弨鐟板綁
    private void ComboChange(object senderRaw, SelectionChangedEventArgs e)
    {
        var sender = (MyComboBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.SelectedIndex);
    }

    private void RadioBoxChange(object senderRaw, ModBase.RouteEventArgs e)
    {
        var sender = (MyRadioBox)senderRaw;
        var gotCfg = sender.Tag?.ToString()?.Split("/") ?? Array.Empty<string>();
        if (ModAnimation.AniControlEnabled == 0 && gotCfg.Length >= 2)
            SetByTag(gotCfg[0], int.Parse(gotCfg[1]));
    }

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

    private static void SetByTag(string tag, object value)
        => ConfigService.TrySetValue(tag, value);

    // 缂冩垹绮?
    private void ApplyHttpProxyBtn_OnClicked(object sender, PointerReleasedEventArgs e)
    {
        Config.Network.HttpProxy.CustomAddress = TextSystemHttpProxy.Text;
        Config.Network.HttpProxy.CustomUsername = TextSystemHttpProxyCustomUsername.Text;
        Config.Network.HttpProxy.CustomPassword = TextSystemHttpProxyCustomPassword.Text;
    }

    // 濠婃垵濮╅弶?
    private void SliderLoad()
    {
        SliderDebugAnim.getHintText = new Func<object, object>(v =>
            (int)v > 29
                ? Lang.Text("Common.Action.Close")
                : Lang.Number(Math.Round(Convert.ToDouble(v) / 10 + 0.1d, 1), "N1") + "x");
        SliderAniFPS.getHintText = new Func<object, string>(v => Lang.Number(Convert.ToInt32(v) + 1, "N0") + " FPS");
        // y = 10x + 50 (0 <= x <= 5, 50 <= y <= 100)
        // y = 50x - 150 (5 < x <= 13, 100 < y <= 500)
        // y = 100x - 800 (13 < x <= 28, 500 < y <= 2000)
        SliderMaxLog.getHintText = new Func<object, object>(v =>
        {
            var val = Convert.ToInt32(v);
            return val switch
            {
                <= 5 => val * 10 + 50,
                <= 13 => val * 50 - 150,
                <= 28 => val * 100 - 800,
                _ => Lang.Text("Setup.Misc.System.MaxLogLines.Unlimited")
            };
        });
    }

    // 绾兛娆㈤崝鐘烩偓?
    private void Check_DisableHardwareAcceleration(object _, bool __)
    {
        HintService.Hint(Lang.Text("Setup.Misc.HardwareAcceleration.RestartNotice"));
    }

    // 鐠嬪啳鐦Ο鈥崇础
    private void CheckDebugMode_Change(object _, bool __)
    {
        if (ModAnimation.AniControlEnabled == 0)
            HintService.Hint(Lang.Text("Setup.Misc.Debug.Mode.Hint"), log: false);
    }

    // 閼奉亜濮╅弴瀛樻煀
    private void ComboSystemActivity_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        if (ComboSystemActivity.SelectedIndex != 2)
            return;
        if (ModMain.MyMsgBox(
                Lang.Text("Setup.Misc.System.Announcement.Disabled.Warning.Message"),
                Lang.Text("Common.Dialog.Warning"),
                Lang.Text("Setup.Misc.System.Announcement.Disabled.Warning.Confirm"),
                Lang.Text("Common.Action.Cancel"), isWarn: true) ==
            2) ComboSystemActivity.SelectedItem = e.RemovedItems[0];
    }

    private void CheckDebugMode_OnChange(object sender, bool user)
    {
        CheckBoxChange(sender, user);
        CheckDebugMode_Change(sender, user);
    }

    private void CheckSystemDisableHardwareAcceleration_OnChange(object sender, bool user)
    {
        CheckBoxChange(sender, user);
        Check_DisableHardwareAcceleration(sender, user);
    }

    private void ComboSystemActivity_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ComboChange(sender, e);
        ComboSystemActivity_SelectionChanged(sender, e);
    }

    #region 鐎电厧鍤?/ 鐎电厧鍙嗙拋鍓х枂

    private void BtnSystemSettingExp_Click(object sender, PointerReleasedEventArgs e)
    {
        var savePath =
            SystemDialogs.SelectSaveFile(Lang.Text("Setup.Misc.System.ExportSettings.SaveTitle"), "PCL 閸忋劌鐪柊宥囩枂.json", Lang.Text("Setup.Misc.System.ExportSettings.Filter"), ModBase.exePath);
        if (string.IsNullOrWhiteSpace(savePath))
            return;
        File.Copy(ConfigService.SharedConfigPath, savePath, true);
        HintService.Hint(Lang.Text("Setup.Misc.System.ExportSettings.Success"), HintType.Success);
        ModBase.OpenExplorer(savePath);
    }

    private void BtnSystemSettingImp_Click(object sender, PointerReleasedEventArgs e)
    {
        var sourcePath = SystemDialogs.SelectFile(Lang.Text("Setup.Misc.System.ExportSettings.Filter"), Lang.Text("Setup.Misc.System.ImportSettings.SelectTitle"));
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;
        File.Copy(sourcePath, ConfigService.SharedConfigPath, true);
        ModMain.MyMsgBox(Lang.Text("Setup.Misc.System.ImportSettings.Success.Message"), button1: Lang.Text("Setup.Misc.System.ImportSettings.Success.Restart"), forceWait: true);
        Process.Start(new ProcessStartInfo(Basics.ExecutablePath));
        FormMain.EndProgramForce();
    }

    #endregion

    #region 閸嬫粍顒涙担璺ㄦ暏 PCL CE

    private void BtnSystemStopUsingPclCe_Click(object sender, PointerReleasedEventArgs e)
    {
        var result = ModMain.MyMsgBox(
            Lang.Text("Setup.Misc.System.StopUsingPclCe.Message"),
            Lang.Text("Setup.Misc.System.StopUsingPclCe.Title"),
            Lang.Text("Common.Action.Continue"),
            Lang.Text("Setup.Misc.System.StopUsingPclCe.ContinueAndRemove"),
            Lang.Text("Common.Action.Cancel"),
            isWarn: true);

        if (result < 3)
        {
            if (ModMain.MyMsgBox(
                    Lang.Text("Setup.Misc.System.StopUsingPclCe.Message.Final"),
                    Lang.Text("Common.Dialog.Warning"),
                    Lang.Text("Common.Action.Continue"),
                    Lang.Text("Common.Action.Cancel"),
                    isWarn: true) == 1)
            {
                StopUsingPClCeCore(result == 2);
            }
        }
    }

    private void StopUsingPClCeCore(bool removeMcResources)
    {
        // 閸掔娀娅?MC 閺傚洣娆㈡径鐟板敶閻?PCL CE 闁板秶鐤?
        if (removeMcResources && States.Game.Folders != "")
        {
            foreach (var path in States.Game.Folders.Split('|'))
            {
                var realPath = path.Split('>')[1];
                Delete([Path.Combine(realPath, "PCL.ini")]);

                var versionsPath = Path.Combine(realPath, "versions");
                if (!Directory.Exists(versionsPath)) continue;
                
                Delete(
                    Directory.EnumerateDirectories(versionsPath)
                        .Select(p => Path.Combine(p, "PCL", "config.v1.yml"))
                );
            }
        }
        
        // 閻㈠彉绨?CE 閺傚洣娆㈡径瑙勵劀閸︺劋濞囬悽顭掔礉娴ｈ法鏁ゅ鎯扮箿鐠嬪啰鏁?CMD 閻ㄥ嫭鏌熷▔鏇炲灩闂?
        List<string> foldersToDelete =
        [
            Paths.Data,
            Paths.OldSharedData,
            Paths.SharedData,
            Paths.SharedLocalData,
            Paths.Temp
        ];

        var sb = new StringBuilder();
        sb.Append("/c timeout /t 5 /nobreak >nul & ");
        foreach (var folder in foldersToDelete)
        {
            sb.Append($"rmdir /s /q \"{folder}\" & ");
        }
        
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = sb.ToString(),
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false
        });

        // 瀵搫鍩楅柅鈧崙?
        KernelInterop.ExitProcess();
            
        void Delete(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    File.Delete(path);
                }
                catch (Exception)
                {
                    //
                }
            }
        }
    }

    #endregion
}
