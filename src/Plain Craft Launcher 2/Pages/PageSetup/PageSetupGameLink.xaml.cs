using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.Link.Scaffolding.EasyTier;

namespace PCL;

public partial class PageSetupGameLink
{
    private bool isFirstLoad = true;

    private new bool isLoaded;

    public PageSetupGameLink()
    {
        InitializeComponent();
        TextUdpNatType.Text = Lang.Text("Setup.GameLink.NetworkTest.UdpNatType", Lang.Text("Setup.GameLink.NetworkTest.NotTested"));
        TextTcpNatType.Text = Lang.Text("Setup.GameLink.NetworkTest.TcpNatType", Lang.Text("Setup.GameLink.NetworkTest.NotTested"));
        TextIpv6Status.Text = Lang.Text("Setup.GameLink.NetworkTest.Ipv6Status", Lang.Text("Setup.GameLink.NetworkTest.NotTested"));
        Loaded += PageSetupLink_Loaded;
        Loaded += (_, _) => Reload();
    }

    private void PageSetupLink_Loaded(object sender, RoutedEventArgs e)
    {
        // 閲嶅鍔犺浇閮ㄥ垎
        PanBack.ScrollToHome();

        // 闈為噸澶嶅姞杞介儴鍒?
        if (isLoaded)
            return;
        isLoaded = true;

        ModAnimation.AniControlEnabled += 1;
        Reload();
        ModAnimation.AniControlEnabled -= 1;
    }

    public void Reload()
    {
        TextLinkUsername.Text = Config.Link.Username;
        // TextLinkRelay.Text = Config.Link.RelayServer
        // ComboRelayType.SelectedIndex = Config.Link.RelayType
        // ComboServerType.SelectedIndex = Config.Link.ServerType
        CheckLatencyFirstMode.Checked = Config.Link.UseLatencyFirstMode;
        ComboPreferProtocol.SelectedIndex = (int)Config.Link.ProtocolPreference;
        CheckTryPunchSym.Checked = Config.Link.TryPunchSym;
        CheckEnableIPv6.Checked = Config.Link.EnableIPv6;
        CheckEnableCliOutput.Checked = Config.Link.EnableCliOutput;

        // TextRelays.Text = "姝ｅ湪鑾峰彇淇℃伅..."
        // Do While Not (PageLinkLobby.LobbyAnnouncementLoader.State = LoadState.Finished OrElse PageLinkLobby.LobbyAnnouncementLoader.State = LoadState.Failed)
        // Thread.Sleep(500)
        // Loop
        // If ETRelay.RelayList.Count > 0 Then
        // TextRelays.Text = ""
        // For Each Relay In ETRelay.RelayList
        // Select Case Relay.Type
        // Case ETRelayType.Community
        // TextRelays.Text += "[绀惧尯] "
        // Case ETRelayType.Selfhosted
        // TextRelays.Text += "[鑷湁] "
        // Case Else 'ETRelayType.Custom
        // TextRelays.Text += "[鑷畾涔塢 "
        // End Select
        // TextRelays.Text += Relay.Name & "锛?
        // Next
        // TextRelays.Text = TextRelays.Text.BeforeLast("锛?)
        // Else
        // TextRelays.Text = "鏆傛棤锛屼綘鍙兘闇€瑕佹墜鍔ㄦ坊鍔犱腑缁ф湇鍔″櫒"
        // End If
    }

    // 鍒濆鍖?
    public void Reset()
    {
        try
        {
            Config.Link.Reset();
            ModBase.Log("[Setup] 宸插垵濮嬪寲鑱旀満椤佃缃?);
            HintService.Hint(Lang.Text("Setup.GameLink.Initialized"), HintType.Success, false);
            Reload();
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Setup.GameLink.Error.InitFailed"),
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Setup.GameLink.Error.InitFailed"));
        }

        Reload();
    }

    // 灏嗘帶浠舵敼鍙樿矾鐢卞埌璁剧疆鏀瑰彉
    private void TextBoxChange(object senderRaw, TextChangedEventArgs e)
    {
        var sender = (MyTextBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Text);
    }

    private static void ComboBoxChange(MyComboBox sender, object e)
    {
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.SelectedIndex);
    }

    private void CheckBoxChange(object senderRaw, bool user)
    {
        var sender = (MyCheckBox)senderRaw;
        if (ModAnimation.AniControlEnabled == 0)
            SetByTag(sender.Tag?.ToString(), sender.Checked);
    }

    private static void SetByTag(string tag, object value)
        => ConfigService.TrySetValue(tag, value);

    private void LinkProtocolPerferenceChange(object sender, SelectionChangedEventArgs e)
    {
        if (ModAnimation.AniControlEnabled == 0)
            try
            {
                var selection = (LinkProtocolPreference)((MyComboBox)sender).SelectedIndex;
                Config.Link.ProtocolPreference = selection;
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    Lang.Text("Setup.GameLink.Error.ConfigChangeFailed"),
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Setup.GameLink.Error.ConfigChangeFailed"));
            }
    }

    // 缃戠粶娴嬭瘯
    private void BtnNetTest_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            BtnNetTest.IsEnabled = false;
            BtnNetTest.Text = Lang.Text("Setup.GameLink.NetworkTest.Testing");
            ModBase.RunInNewThread(() =>
            {
                var status = CliNetTest.GetNetStatusAsync().GetAwaiter().GetResult();
                ModBase.RunInUi(() =>
                {
                    TextUdpNatType.Text =
                        Lang.Text("Setup.GameLink.NetworkTest.UdpNatType", CliNetTest.GetNatTypeString(status.UdpNatType));
                    TextTcpNatType.Text =
                        Lang.Text("Setup.GameLink.NetworkTest.TcpNatType", CliNetTest.GetNatTypeString(status.TcpNatType));
                    TextIpv6Status.Text = Lang.Text("Setup.GameLink.NetworkTest.Ipv6Status",
                        status.SupportIPv6
                            ? Lang.Text("Setup.GameLink.NetworkTest.Supported")
                            : Lang.Text("Setup.GameLink.NetworkTest.Unsupported"));
                    BtnNetTest.IsEnabled = true;
                    BtnNetTest.Text = Lang.Text("Setup.GameLink.NetworkTest.Start");
                });
            });
        }
        catch (Exception ex)
        {
            ModBase.Log(ex,
                "[Link] 鑾峰彇缃戠粶娴嬭瘯缁撴灉澶辫触",
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Setup.GameLink.Error.NetworkTestFailed"));
            BtnNetTest.IsEnabled = true;
            BtnNetTest.Text = Lang.Text("Setup.GameLink.NetworkTest.Start");
        }
    }
}
