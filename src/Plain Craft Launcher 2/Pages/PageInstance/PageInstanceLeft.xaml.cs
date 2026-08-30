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
    ///     瑜版挸澧犻弰鍓с仛鐠佸墽鐤嗛惃?MC 鐎圭偘绶ラ妴?
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
            ItemMod.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceMod
                ? false
                : true;
            ItemModDisabled.IsVisible = false;
        }
        else
        {
            ItemMod.IsVisible = false;
            ItemModDisabled.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceMod
                ? false
                : true;
        }

        // 閸旂喕鍏橀梾鎰
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
                TextResource.IsVisible = false;
            else
                TextResource.IsVisible = true;
        }
        else
        {
            TextResource.IsVisible = true;
        }

        ItemInstall.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceEdit
            ? false
            : true;
        ItemExport.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceExport
            ? false
            : true;
        ItemWorld.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceSave
            ? false
            : true;
        ItemScreenshot.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceScreenshot
            ? false
            : true;
        ItemResourcePack.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceResourcePack
            ? false
            : true;
        ItemShader.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceShader
            ? false
            : true;
        ItemSchematic.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceSchematic
            ? false
            : true;
        ItemServer.IsVisible = !PageSetupUI.HiddenForceShow && hide.InstanceServer
            ? false
            : true;
    }

    private void RefreshButton_Click(object sender, EventArgs e) // 閻㈣精绔熼弽蹇斿瘻闁筋喖灏堕崥宥堢殶閻?
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

    #region 妞ょ敻娼伴崚鍥ㄥ床

    /// <summary>
    ///     瑜版挸澧犳い鐢告桨閻ㄥ嫮绱崣鏋偓鍌欑矤 0 瀵偓婵顓哥粻妞尖偓?
    /// </summary>
    public FormMain.PageSubType pageID = FormMain.PageSubType.Default;

    /// <summary>
    ///     閸曢箖鈧绨ㄦ禒鑸垫暭閸欐﹢銆夐棃顫偓?
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
                throw new Exception("閺堫亞鐓￠惃鍕杽娓氬顔曠純顔肩摍妞ょ敻娼扮粔宥囪閿? + (int)id);
            }
        }
    }

    /// <summary>
    ///     閸掑洦宕查悳鐗堟箒妞ょ敻娼伴妴?
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
                "閸掑洦宕查崚鍡涖€夐棃銏犮亼鐠愩儻绱橧D " + (int)id + "閿?,
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
        ModAnimation.AniStop("FrmMain PageChangeRight"); // 閸嬫粍顒涙稉濠氥€夐棃銏㈡畱閸欐娊銆夐棃銏犲瀼閹广垹濮╅悽浼欑礉闂冨弶顒涚€瑰啩绗岄張顒€濮╅悽璁崇鐠х柉袝閸欐垵顦垮▎?PageOnEnter
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
                // 瀵ゆ儼绻滅憴锕€褰傛い鐢告桨闁氨鏁ら崝銊ф暰閿涘奔浜掓担鍨繁閸?Loaded 娴滃娆㈡稉顓炲鏉炵晫娈戦幒褌娆㈠妞句簰婢跺嫮鎮?
                ModMain.frmMain.pageRight.Opacity = 1d;
                ModMain.frmMain.pageRight.PageOnEnter();
            }, 30, true)
        }, "PageLeft PageChange");
    }

    #endregion
}
