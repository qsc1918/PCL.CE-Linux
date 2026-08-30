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
        // 闁插秴顦查崝鐘烘祰闁劌鍨?
        PanBack.ScrollToHome();

        // 闂堢偤鍣告径宥呭鏉炰粙鍎撮崚?
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

        // TextRelays.Text = "濮濓絽婀懢宄板絿娣団剝浼?.."
        // Do While Not (PageLinkLobby.LobbyAnnouncementLoader.State = LoadState.Finished OrElse PageLinkLobby.LobbyAnnouncementLoader.State = LoadState.Failed)
        // Thread.Sleep(500)
        // Loop
        // If ETRelay.RelayList.Count > 0 Then
        // TextRelays.Text = ""
        // For Each Relay In ETRelay.RelayList
        // Select Case Relay.Type
        // Case ETRelayType.Community
        // TextRelays.Text += "[缁€鎯у隘] "
        // Case ETRelayType.Selfhosted
        // TextRelays.Text += "[閼奉亝婀乚 "
        // Case Else 'ETRelayType.Custom
        // TextRelays.Text += "[閼奉亜鐣炬稊濉?"
        // End Select
        // TextRelays.Text += Relay.Name & "閿?
        // Next
        // TextRelays.Text = TextRelays.Text.BeforeLast("閿?)
        // Else
        // TextRelays.Text = "閺嗗倹妫ら敍灞肩稑閸欘垵鍏橀棁鈧憰浣瑰閸斻劍鍧婇崝鐘辫厬缂佈勬箛閸斺€虫珤"
        // End If
    }

    // 閸掓繂顫愰崠?
    public void Reset()
    {
        try
        {
            Config.Link.Reset();
            ModBase.Log("[Setup] 瀹告彃鍨垫慨瀣閼辨梹婧€妞や絻顔曠純?);
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

    // 鐏忓棙甯舵禒鑸垫暭閸欐鐭鹃悽鍗炲煂鐠佸墽鐤嗛弨鐟板綁
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

    // 缂冩垹绮跺ù瀣槸
    private void BtnNetTest_Click(object sender, PointerReleasedEventArgs e)
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
                "[Link] 閼惧嘲褰囩純鎴犵捕濞村鐦紒鎾寸亯婢惰精瑙?,
                ModBase.LogLevel.Hint,
                userSummary: Lang.Text("Setup.GameLink.Error.NetworkTestFailed"));
            BtnNetTest.IsEnabled = true;
            BtnNetTest.Text = Lang.Text("Setup.GameLink.NetworkTest.Start");
        }
    }
}
