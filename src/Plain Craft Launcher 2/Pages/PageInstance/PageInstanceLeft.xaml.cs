using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceLeft : IRefreshable
{
    /// <summary>
    ///     褰撳墠鏄剧ず璁剧疆鐨?MC 瀹炰緥銆?
    /// </summary>
    public static McInstance McInstance = null;

    public PageInstanceLeft()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshModDisabled();
    }

    public void Refresh()
    {
        Refresh(ModMain.frmMain.PageCurrentSub);
    }

    public void RefreshModDisabled()
    {
        var hide = Config.Preference.Hide;

        if (McInstance is not null && McInstance.Modable)
        {
            ItemMod.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceMod
                ? Visibility.Collapsed
                : Visibility.Visible;
            ItemModDisabled.Visibility = Visibility.Collapsed;
        }
        else
        {
            ItemMod.Visibility = Visibility.Collapsed;
            ItemModDisabled.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceMod
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // 鍔熻兘闅愯棌
        if (!PageSetupUI.HiddenForceShow)
        {
            var disableCount = 0;
            if (hide.InstanceSave)
                disableCount += 1;
            if (hide.InstanceScreenshot)
                disableCount += 1;
            if (hide.InstanceMod)
                disableCount += 1;
            if (hide.InstanceResourcePack)
                disableCount += 1;
            if (hide.InstanceShader)
                disableCount += 1;
            if (hide.InstanceSchematic)
                disableCount += 1;
            if (hide.InstanceServer)
                disableCount += 1;
            if (disableCount == 7)
                TextResource.Visibility = Visibility.Collapsed;
            else
                TextResource.Visibility = Visibility.Visible;
        }
        else
        {
            TextResource.Visibility = Visibility.Visible;
        }

        ItemInstall.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceEdit
            ? Visibility.Collapsed
            : Visibility.Visible;
        ItemExport.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceExport
            ? Visibility.Collapsed
            : Visibility.Visible;
        ItemWorld.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceSave
            ? Visibility.Collapsed
            : Visibility.Visible;
        ItemScreenshot.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceScreenshot
            ? Visibility.Collapsed
            : Visibility.Visible;
        ItemResourcePack.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceResourcePack
            ? Visibility.Collapsed
            : Visibility.Visible;
        ItemShader.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceShader
            ? Visibility.Collapsed
            : Visibility.Visible;
        ItemSchematic.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceSchematic
            ? Visibility.Collapsed
            : Visibility.Visible;
        ItemServer.Visibility = !PageSetupUI.HiddenForceShow && hide.InstanceServer
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void RefreshButton_Click(object sender, EventArgs e) // 鐢辫竟鏍忔寜閽尶鍚嶈皟鐢?
    {
        Refresh((FormMain.PageSubType)ModBase.Val(((MyIconButton)sender).Tag));
    }

    public void Refresh(FormMain.PageSubType subType)
    {
        switch (subType)
        {
            case FormMain.PageSubType.VersionMod:
            {
                PageInstanceCompResource.Refresh(ModComp.CompType.Mod);
                break;
            }
            case FormMain.PageSubType.VersionScreenshot:
            {
                var ignore= PageInstanceScreenshot.RefreshAsync();
                break;
            }
            case FormMain.PageSubType.VersionWorld:
            {
                PageInstanceSaves.Refresh();
                break;
            }
            case FormMain.PageSubType.VersionResourcePack:
            {
                PageInstanceCompResource.Refresh(ModComp.CompType.ResourcePack);
                break;
            }
            case FormMain.PageSubType.VersionShader:
            {
                PageInstanceCompResource.Refresh(ModComp.CompType.Shader);
                break;
            }
            case FormMain.PageSubType.VersionSchematic:
            {
                PageInstanceCompResource.Refresh(ModComp.CompType.Schematic);
                break;
            }
            case FormMain.PageSubType.VersionInstall:
            {
                ModDownload.dlClientListLoader.Start(isForceRestart: true);
                ModDownload.dlOptiFineListLoader.Start(isForceRestart: true);
                ModDownload.dlForgeListLoader.Start(isForceRestart: true);
                ModDownload.dlNeoForgeListLoader.Start(isForceRestart: true);
                ModDownload.dlLiteLoaderListLoader.Start(isForceRestart: true);
                ModDownload.dlFabricListLoader.Start(isForceRestart: true);
                ModDownload.dlFabricApiLoader.Start(isForceRestart: true);
                ModDownload.dlOptiFabricLoader.Start(isForceRestart: true);
                ModDownload.dlLabyModListLoader.Start(isForceRestart: true);
                ItemInstall.Checked = true;
                ModMain.frmInstanceInstall.GetCurrentInfo();
                break;
            }
            case FormMain.PageSubType.VersionExport:
            {
                if (ModMain.frmInstanceExport is not null)
                    ModMain.frmInstanceExport.RefreshAll();
                ItemExport.Checked = true;
                break;
            }
            case FormMain.PageSubType.VersionServer:
            {
                if (ModMain.frmInstanceServer is not null)
                    ModMain.frmInstanceServer.RefreshServers();
                ItemServer.Checked = true;
                break;
            }
        }
    }

    public void Reset(object sender, EventArgs e)
    {
        if (ModMain.MyMsgBox(Lang.Text("Instance.Left.InitializeSettings.ConfirmMessage"),
                Lang.Text("Instance.Left.InitializeSettings.ConfirmTitle"),
                button2: Lang.Text("Common.Action.Cancel"),
                isWarn: true)
            == 1)
        {
            if (ModMain.frmInstanceSetup is null)
                ModMain.frmInstanceSetup = new PageInstanceSetup();
            ModMain.frmInstanceSetup.Reset();
            ItemSetup.Checked = true;
        }
    }

    #region 椤甸潰鍒囨崲

    /// <summary>
    ///     褰撳墠椤甸潰鐨勭紪鍙枫€備粠 0 寮€濮嬭绠椼€?
    /// </summary>
    public FormMain.PageSubType pageID = FormMain.PageSubType.Default;

    /// <summary>
    ///     鍕鹃€変簨浠舵敼鍙橀〉闈€?
    /// </summary>
    private void PageCheck(object sender, ModBase.RouteEventArgs e)
    {
        if (sender is MyListItem item && item.Tag is not null)
            PageChange((FormMain.PageSubType)ModBase.Val(item.Tag));
    }

    public object PageGet(FormMain.PageSubType id)
    {
        if ((int)id == -1)
            id = pageID;
        switch (id)
        {
            case FormMain.PageSubType.VersionOverall:
            {
                if (ModMain.frmInstanceOverall is null)
                    ModMain.frmInstanceOverall = new PageInstanceOverall();
                return ModMain.frmInstanceOverall;
            }
            case FormMain.PageSubType.VersionMod:
            {
                if (ModMain.frmInstanceMod is null)
                    ModMain.frmInstanceMod = new PageInstanceCompResource(ModComp.CompType.Mod);
                return ModMain.frmInstanceMod;
            }
            case FormMain.PageSubType.VersionModDisabled:
            {
                if (ModMain.frmInstanceModDisabled is null)
                    ModMain.frmInstanceModDisabled = new PageInstanceModDisabled();
                return ModMain.frmInstanceModDisabled;
            }
            case FormMain.PageSubType.VersionSetup:
            {
                if (ModMain.frmInstanceSetup is null)
                    ModMain.frmInstanceSetup = new PageInstanceSetup();
                return ModMain.frmInstanceSetup;
            }
            case FormMain.PageSubType.VersionWorld:
            {
                if (ModMain.frmInstanceSaves is null)
                    ModMain.frmInstanceSaves = new PageInstanceSaves();
                return ModMain.frmInstanceSaves;
            }
            case FormMain.PageSubType.VersionScreenshot:
            {
                if (ModMain.frmInstanceScreenshot is null)
                    ModMain.frmInstanceScreenshot = new PageInstanceScreenshot();
                return ModMain.frmInstanceScreenshot;
            }
            case FormMain.PageSubType.VersionResourcePack:
            {
                if (ModMain.frmInstanceResourcePack is null)
                    ModMain.frmInstanceResourcePack = new PageInstanceCompResource(ModComp.CompType.ResourcePack);
                return ModMain.frmInstanceResourcePack;
            }
            case FormMain.PageSubType.VersionShader:
            {
                if (ModMain.frmInstanceShader is null)
                    ModMain.frmInstanceShader = new PageInstanceCompResource(ModComp.CompType.Shader);
                return ModMain.frmInstanceShader;
            }
            case FormMain.PageSubType.VersionSchematic:
            {
                if (ModMain.frmInstanceSchematic is null)
                    ModMain.frmInstanceSchematic = new PageInstanceCompResource(ModComp.CompType.Schematic);
                return ModMain.frmInstanceSchematic;
            }
            case FormMain.PageSubType.VersionInstall:
            {
                if (ModMain.frmInstanceInstall is null)
                    ModMain.frmInstanceInstall = new PageInstanceInstall();
                return ModMain.frmInstanceInstall;
            }
            case FormMain.PageSubType.VersionExport:
            {
                if (ModMain.frmInstanceExport is null)
                    ModMain.frmInstanceExport = new PageInstanceExport();
                return ModMain.frmInstanceExport;
            }
            case FormMain.PageSubType.VersionServer:
            {
                if (ModMain.frmInstanceServer is null)
                    ModMain.frmInstanceServer = new PageInstanceServer();
                return ModMain.frmInstanceServer;
            }

            default:
            {
                throw new Exception("鏈煡鐨勫疄渚嬭缃瓙椤甸潰绉嶇被锛? + (int)id);
            }
        }
    }

    /// <summary>
    ///     鍒囨崲鐜版湁椤甸潰銆?
    /// </summary>
    public void PageChange(FormMain.PageSubType id)
    {
        if (pageID == id)
            return;
        ModAnimation.AniControlEnabled += 1;
        try
        {
            PageChangeRun((MyPageRight)PageGet(id));
            pageID = id;
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "鍒囨崲鍒嗛〉闈㈠け璐ワ紙ID " + (int)id + "锛?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Error.OperationFailed"));
        }
        finally
        {
            ModAnimation.AniControlEnabled -= 1;
        }
    }

    private static void PageChangeRun(MyPageRight target)
    {
        ModAnimation.AniStop("FrmMain PageChangeRight"); // 鍋滄涓婚〉闈㈢殑鍙抽〉闈㈠垏鎹㈠姩鐢伙紝闃叉瀹冧笌鏈姩鐢讳竴璧疯Е鍙戝娆?PageOnEnter
        if (target.Parent is not null)
            target.SetValue(ContentPresenter.ContentProperty, null);
        ModMain.frmMain.pageRight = target;
        ((MyPageRight)ModMain.frmMain.PanMainRight.Child).PageOnExit();
        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaCode(() =>
            {
                ((MyPageRight)ModMain.frmMain.PanMainRight.Child).PageOnForceExit();
                ModMain.frmMain.PanMainRight.Child = ModMain.frmMain.pageRight;
                ModMain.frmMain.pageRight.Opacity = 0d;
            }, 130),
            ModAnimation.AaCode(() =>
            {
                // 寤惰繜瑙﹀彂椤甸潰閫氱敤鍔ㄧ敾锛屼互浣垮緱鍦?Loaded 浜嬩欢涓姞杞界殑鎺т欢寰椾互澶勭悊
                ModMain.frmMain.pageRight.Opacity = 1d;
                ModMain.frmMain.pageRight.PageOnEnter();
            }, 30, true)
        }, "PageLeft PageChange");
    }

    #endregion
}
