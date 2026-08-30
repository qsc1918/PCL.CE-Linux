using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Input;
using FluentValidation;
using fNbt;
using PCL.Core.Link.McPing;
using PCL.Core.Link.McPing.Model;
using PCL.Core.Minecraft;
using PCL.Core.Utils.Validate;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceServer : MyPageRight
{
    private const int debounceInterval = 2000;

    public static readonly List<MinecraftServerInfo> serverList = new();
    private static readonly List<ServerCard> serverCardList = new();

    private CancellationTokenSource _cts;

    private DateTime _lastRefresh = DateTime.MinValue;

    public PageInstanceServer()
    {
        InitializeComponent();
        Loaded += PageLoaded;
        IsVisibleChanged += PageInstanceServer_IsVisibleChanged;
    }

    private async void PageLoaded(object e, RoutedEventArgs sender)
    {
        serverList.Clear();
        serverCardList.Clear();
        PanServers.Children.Clear();

        await LoadServersFromFileAsync();
        RefreshTip();

        foreach (var server in serverList)
        {
            var serverCard = new ServerCard();
            serverCard.RemoveServer += RemoveServerEvent;
            serverCard.EditServer += (a, b) => this.EditServer(a, (ServerCard.ResultEventArgs)b);
            serverCard.UpdateServerInfo(server);
            serverCardList.Add(serverCard);
            PanServers.Children.Add(serverCard);
        }

        PingAllServers();
    }

    private void PageInstanceServer_IsVisibleChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!IsVisible)
            if (_cts is not null)
            {
                _cts.Cancel();
                _cts.Dispose(); // 娓呯悊鏃х殑 CancellationTokenSource
                _cts = null;
            }
    }

    private async void RemoveServerEvent(object sender, EventArgs e)
    {
        // Get server index
        var index = PanServers.Children.IndexOf((Control)sender);
        if (index < 0)
        {
            HintService.Hint(Lang.Text("Instance.Server.IndexNotFound"), HintType.Error);
            return;
        }

        // Read NBT file
        var nbtData =
            await NbtFileHandler.ReadTagInNbtFileAsync<NbtList>(
                Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat"), "servers");
        if (nbtData is null)
        {
            HintService.Hint(Lang.Text("Instance.Server.ReadDataFailed"), HintType.Error);
            return;
        }

        // Remove server from NBT data
        nbtData.RemoveAt(index);
        var clonedNbtData = (NbtList)nbtData.Clone();

        // Write back to NBT file
        if (!await NbtFileHandler.WriteTagInNbtFileAsync(clonedNbtData,
                Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat")))
        {
            HintService.Hint(Lang.Text("Instance.Server.WriteDataFailed"), HintType.Error);
            return;
        }

        // Remove server from list and UI
        serverList.RemoveAt(index);
        serverCardList.Remove((ServerCard)sender);
        if (serverList.Count == 0) RefreshTip();

        // Remove UI element
        PanServers.Children.Remove((Control)sender);

        // Success message
        HintService.Hint(Lang.Text("Instance.Server.Removed"), HintType.Success);
    }

    private async void EditServer(object sender, ServerCard.ResultEventArgs e)
    {
        // Read NBT file
        var nbtData =
            await NbtFileHandler.ReadTagInNbtFileAsync<NbtList>(Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat"),
                "servers");
        if (nbtData is null)
        {
            HintService.Hint(Lang.Text("Instance.Server.ReadDataFailed"), HintType.Error);
            return;
        }

        // Get server index
        var index = PanServers.Children.IndexOf((Control)sender);
        if (index < 0 || index >= nbtData.Count)
        {
            HintService.Hint(Lang.Text("Instance.Server.IndexNotFound"), HintType.Error);
            return;
        }

        // Verify server data
        var server = nbtData[index] as NbtCompound;

        // Update server data
        server["name"] = new NbtString("name", e.Param1);
        server["ip"] = new NbtString("ip", e.Param2);

        // Write updated NBT data
        var clonedNbtData = (NbtList)nbtData.Clone();
        if (!await NbtFileHandler.WriteTagInNbtFileAsync(clonedNbtData,
                Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat")))
        {
            HintService.Hint(Lang.Text("Instance.Server.WriteDataFailed"), HintType.Error);
            return;
        }

        var serverCard = sender as ServerCard;

        serverCard.server.Name = e.Param1;
        serverCard.server.Address = e.Param2;

        await serverCard.RefreshServerStatusAsync(true);

        // Success message
        HintService.Hint(Lang.Text("Instance.Server.Updated"), HintType.Success);
    }

    /// <summary>
    ///     鍒锋柊鏈嶅姟鍣ㄥ垪琛?
    /// </summary>
    public async void RefreshServers()
    {
        ModBase.Log("鍒锋柊鏈嶅姟鍣ㄥ垪琛?);
        try
        {
            // 璇诲彇鏈嶅姟鍣ㄤ俊鎭?
            await LoadServersFromFileAsync();

            // 鍦║I绾跨▼涓洿鏂扮晫闈?
            ModBase.RunInUi(() => UpdateServerUi());

            // 寮傛ping鎵€鏈夋湇鍔″櫒
            PingAllServers();
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Instance.Server.RefreshFailed"),
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Server.RefreshFailed"));
            ModBase.RunInUi(() => HintService.Hint(
                Lang.Text("Instance.Server.RefreshFailed.WithDetail", ex.ToString()),
                HintType.Error));
        }
    }

    private void BtnRefresh_Click(object sender, MouseButtonEventArgs e)
    {
        if ((DateTime.Now - _lastRefresh).TotalMilliseconds < debounceInterval)
        {
            HintService.Hint(Lang.Text("Instance.Server.NoFrequentRefresh"));
            return;
        }

        _lastRefresh = DateTime.Now;
        HintService.Hint(Lang.Text("Instance.Server.RefreshingList"));
        try
        {
            RefreshServers();
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                Lang.Text("Instance.Server.RefreshFailed"),
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Server.RefreshFailed"));
            HintService.Hint(
                Lang.Text("Instance.Server.RefreshFailed.WithDetail", ex.ToString()),
                HintType.Error);
        }
    }

    private async void BtnAddServer_Click(object sender, MouseButtonEventArgs e)
    {
        var result = GetServerInfo(new MinecraftServerInfo { Name = Lang.Text("Instance.Server.DefaultName"), Address = "" });
        if (result.Success)
        {
            var newServer = new MinecraftServerInfo
            {
                Name = result.Name,
                Address = result.Address,
                Status = ServerStatus.Unknown
            };
            serverList.Add(newServer);

            RefreshTip();

            var serverCard = new ServerCard();
            serverCard.RemoveServer += RemoveServerEvent;
            serverCard.EditServer += (a, b) => this.EditServer(a, (ServerCard.ResultEventArgs)b);
            serverCard.UpdateServerInfo(newServer);
            serverCardList.Add(serverCard);
            PanServers.Children.Add(serverCard);

            await serverCard.RefreshServerStatusAsync(false);

            var serversDatPath = Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat");

            NbtList nbtData;
            if (!File.Exists(serversDatPath))
            {
                nbtData = new NbtList("servers", NbtTagType.Compound);
                RefreshTip();
            }
            else
            {
                nbtData = await NbtFileHandler.ReadTagInNbtFileAsync<NbtList>(serversDatPath, "servers");
            }

            if (nbtData is not null)
            {
                var server = new NbtCompound();
                server["name"] = new NbtString("name", result.Name);
                server["ip"] = new NbtString("ip", result.Address);
                if (nbtData.Count == 0) nbtData.ListType = NbtTagType.Compound;
                nbtData.Add(server);
                var clonedNbtData = (NbtList)nbtData.Clone();
                await NbtFileHandler.WriteTagInNbtFileAsync(clonedNbtData, serversDatPath);
            }
        }
    }

    public static (string Name, string Address, bool Success) GetServerInfo(MinecraftServerInfo server)
    {
        var newName = ModMain.MyMsgBoxInput(Lang.Text("Instance.Server.EditTitle"), Lang.Text("Instance.Server.NamePrompt"), server.Name,
            [new NullOrWhiteSpaceValidator()]);

        if (string.IsNullOrEmpty(newName)) return (string.Empty, string.Empty, false);

        var newAddress = ModMain.MyMsgBoxInput(Lang.Text("Instance.Server.EditTitle"), Lang.Text("Instance.Server.AddressPrompt"), server.Address,
            [new NullOrWhiteSpaceValidator()]);
        if (string.IsNullOrEmpty(newAddress)) return (string.Empty, string.Empty, false);
        return (newName, newAddress, true);
    }

    /// <summary>
    ///     浠巗ervers.dat鏂囦欢璇诲彇鏈嶅姟鍣ㄤ俊鎭?
    /// </summary>
    private async Task LoadServersFromFileAsync()
    {
        serverList.Clear();

        var serversFile = Path.Combine(PageInstanceLeft.McInstance.PathIndie, "servers.dat");
        if (!File.Exists(serversFile))
            return;

        try
        {
            // 璇诲彇NBT鏍煎紡鐨剆ervers.dat鏂囦欢
            var nbtData = await NbtFileHandler.ReadTagInNbtFileAsync<NbtList>(serversFile, "servers");
            ParseServersFromNBT(nbtData);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, Lang.Text("Instance.Server.ReadFileFailed"));
        }
    }

    /// <summary>
    ///     瑙ｆ瀽NBT鏍煎紡鐨勬湇鍔″櫒鏁版嵁
    /// </summary>
    private void ParseServersFromNBT(NbtList serversList)
    {
        if (serversList is not null)
        {
            ModBase.Log($"Found {serversList.Count} servers:");

            // 閬嶅巻 servers 鍒楄〃涓殑姣忎釜鏈嶅姟鍣?
            for (int i = 0, loopTo = serversList.Count - 1; i <= loopTo; i++)
            {
                var server = serversList[i] as NbtCompound;
                if (server is not null)
                {
                    // 鎻愬彇鏈嶅姟鍣ㄤ俊鎭?
                    // Dim hidden As Byte = If(server.Get(Of NbtByte)("hidden")?.Value, 0)
                    var ip = server.Get<NbtString>("ip")?.Value ?? "Unknown";
                    var name = server.Get<NbtString>("name")?.Value ?? "Unknown";
                    var iconBase64 = server.Get<NbtString>("icon")?.Value;

                    ModBase.Log($"鏈嶅姟鍣?{i + 1}:");
                    ModBase.Log($"  鍚嶅瓧: {name}");
                    ModBase.Log($"  IP: {ip}");
                    // Log($"  Hidden: {If(hidden = 1, "Yes", "No")}")
                    serverList.Add(new MinecraftServerInfo
                    {
                        Name = name,
                        Address = ip,
                        Status = ServerStatus.Unknown,
                        Icon = iconBase64
                    });
                }
            }
        }
        else
        {
            ModBase.Log("No 'servers' list found in servers.dat.");
        }
    }

    /// <summary>
    ///     鏇存柊鏈嶅姟鍣║I鏄剧ず
    /// </summary>
    private void UpdateServerUi()
    {
        PanServers.Children.Clear();

        RefreshTip();

        foreach (var server in serverList)
        {
            var serverCard = new ServerCard();
            serverCard.RemoveServer += RemoveServerEvent;
            serverCard.EditServer += (a, b) => this.EditServer(a, (ServerCard.ResultEventArgs)b);
            serverCard.UpdateServerInfo(server);
            serverCardList.Add(serverCard);
            PanServers.Children.Add(serverCard);
        }
    }

    private void RefreshTip()
    {
        if (serverList.Count == 0)
        {
            ModBase.Log(Lang.Text("Instance.Server.NoServersFound"));
            PanNoServer.Visibility = Visibility.Visible;
            PanContent.Visibility = Visibility.Collapsed;
            PanServers.Visibility = Visibility.Collapsed;
            return;
        }

        ModBase.Log(Lang.Text("Instance.Server.FoundServers"));
        PanNoServer.Visibility = Visibility.Collapsed;
        PanContent.Visibility = Visibility.Visible;
        PanServers.Visibility = Visibility.Visible;
    }

    private async void PingAllServers()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var semaphore = new SemaphoreSlim(5); // 闄愬埗鏈€澶?5 涓苟鍙戜换鍔?

        var tasks = new List<Task>();
        try
        {
            var snapshot = serverCardList.ToList();
            foreach (var server in snapshot)
            {
                var currentServer = server;
                await semaphore.WaitAsync(token);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await currentServer.RefreshServerStatusAsync(false, token);
                    }
                    catch (Exception ex)
                    {
                        ModBase.Log(ex, $"Ping 鏈嶅姟鍣ㄥけ璐? {currentServer}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, token));
            }

            await Task.WhenAll(tasks); // 绛夊緟鎵€鏈変换鍔″畬鎴?
        }
        catch (OperationCanceledException ex)
        {
            ModBase.Log("PingAllServers 琚彇娑?, ModBase.LogLevel.Debug);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "PingAllServers 澶辫触");
        }
    }

    /// <summary>
    ///     ping鍗曚釜鏈嶅姟鍣?
    /// </summary>
    public static async Task<MinecraftServerInfo> PingServerAsync(MinecraftServerInfo server, CancellationToken token)
    {
        try
        {
            var addr = await ServerAddressResolver.GetResolvedServerAddressAsync(server.Address, token);
            using (var query = McPingServiceFactory.CreateService(addr.Host, addr.Ip, addr.Port))
            {
                McPingResult? result;
                ModBase.Log("Pinging server: " + server.Address + ":" + addr.Port);
                result = await query.PingAsync(token); // 浼犻€?token
                ModBase.Log("Ping result: " + (result is not null ? "Success" : "Failed"));
                if (result is not null)
                {
                    server.Status = ServerStatus.Online;
                    server.PlayerCount = result.Players.Online;
                    server.MaxPlayers = result.Players.Max;
                    server.Description = result.Description ?? string.Empty;
                    server.Version = result.Version.Name;
                    server.Ping = (int)result.Latency;
                    server.Icon = result.Favicon;
                }
                else
                {
                    server.Status = ServerStatus.Offline;
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            server.Status = ServerStatus.Offline;
            ModBase.Log("Ping 鏈嶅姟鍣ㄨ鍙栨秷: " + server.Address, ModBase.LogLevel.Debug);
        }
        catch (Exception ex)
        {
            server.Status = ServerStatus.Offline;
            ModBase.Log(ex, $"Ping 鏈嶅姟鍣ㄥけ璐? {server.Address}:{server.Port}");
        }

        return server;
    }
}

/// <summary>
///     Minecraft鏈嶅姟鍣ㄤ俊鎭被
/// </summary>
public class MinecraftServerInfo
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int Port { get; set; } = 25565;
    public ServerStatus Status { get; set; } = ServerStatus.Unknown;
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public string Description { get; set; } = "";
    public string Version { get; set; } = "";
    public int Ping { get; set; }
    public string Icon { get; set; } = "";
}

/// <summary>
///     鏈嶅姟鍣ㄧ姸鎬佹灇涓?
/// </summary>
public enum ServerStatus
{
    Unknown,
    Online,
    Offline,
    Pinging
}
