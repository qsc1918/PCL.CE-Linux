using System.Collections;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.UI;
using PCL.Core.Utils;

namespace PCL;

public partial class PageInstanceInstall
{
    private enum InstallAction
    {
        Modify,
        Reset
    }

    private bool isLoad;
    private string lastVersionName;
    private InstallAction _installAction;

    public PageInstanceInstall()
    {
        Initialized += (a, b) => LoaderInit();
        Loaded += (a, b) => Init();
        InitializeComponent();
        LoadMinecraft.Text = Lang.Text("Download.Version.LoadingList");
    }

    private void LoaderInit()
    {
        disabledPageAnimControls.Add(BtnSelectStart);
        // PageLoaderInit(LoadMinecraft, PanLoad, PanBack, Nothing, DlClientListLoader, AddressOf LoadMinecraft_OnFinish)
        PageLoaderInit(LoadMinecraft, PanLoad, PanAllBack, null, ModDownload.dlClientListLoader, _ => GetCurrentInfo());
        LoadOptiFine.StateChanged += (_, _, _) => { OptiFine_Loaded(); ReloadSelected(); };
        LoadLiteLoader.StateChanged += (_, _, _) => { LiteLoader_Loaded(); ReloadSelected(); };
        LoadForge.StateChanged += (_, _, _) => { Forge_Loaded(); ReloadSelected(); };
        LoadNeoForge.StateChanged += (_, _, _) => { NeoForge_Loaded(); ReloadSelected(); };
        LoadCleanroom.StateChanged += (_, _, _) => { Cleanroom_Loaded(); ReloadSelected(); };
        LoadFabric.StateChanged += (_, _, _) => { Fabric_Loaded(); ReloadSelected(); };
        LoadFabricApi.StateChanged += (_, _, _) => { FabricApi_Loaded(); ReloadSelected(); };
        LoadLegacyFabric.StateChanged += (_, _, _) => { LegacyFabric_Loaded(); ReloadSelected(); };
        LoadLegacyFabricApi.StateChanged += (_, _, _) => { LegacyFabricApi_Loaded(); ReloadSelected(); };
        LoadOptiFabric.StateChanged += (_, _, _) => { OptiFabric_Loaded(); ReloadSelected(); };
        LoadLabyMod.StateChanged += (_, _, _) => { LabyMod_Loaded(); ReloadSelected(); };
        PageExit += () => isInSelectPage = false;
    }

    private void Init()
    {
        PanBack.ScrollToHome();

        GetCurrentInfo();

        var needRefresh = lastVersionName is null || (lastVersionName ?? "") != (_vanillaName ?? "");
        lastVersionName = _vanillaName;

        ModDownload.dlOptiFineListLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlLiteLoaderListLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlFabricListLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlNeoForgeListLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlCleanroomListLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlLabyModListLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlLegacyFabricListLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlFabricApiLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlLegacyFabricApiLoader.Start(isForceRestart: needRefresh);
        ModDownload.dlOptiFabricLoader.Start(isForceRestart: needRefresh);

        // 闂堢偤鍣告径宥呭鏉炰粙鍎撮崚?
        if (isLoad)
        {
            ReloadSelected();
            return;
        }
        isLoad = true;

        ModDownloadLib.McDownloadForgeRecommendedRefresh();

        LoadOptiFine.State = ModDownload.dlOptiFineListLoader;
        LoadLiteLoader.State = ModDownload.dlLiteLoaderListLoader;
        LoadFabric.State = ModDownload.dlFabricListLoader;
        LoadFabricApi.State = ModDownload.dlFabricApiLoader;
        LoadNeoForge.State = ModDownload.dlNeoForgeListLoader;
        LoadCleanroom.State = ModDownload.dlCleanroomListLoader;
        LoadOptiFabric.State = ModDownload.dlOptiFabricLoader;
        LoadLabyMod.State = ModDownload.dlLabyModListLoader;
        LoadLegacyFabric.State = ModDownload.dlLegacyFabricListLoader;
        LoadLegacyFabricApi.State = ModDownload.dlLegacyFabricApiLoader;
    }

    #region 鐎瑰顥?

    private void BtnSelectStart_Click(object sender, PointerReleasedEventArgs mouseButtonEventArgs)
    {
        // Quilt 鐎圭偘绶ラ弮鐘崇《闁俺绻冪€瑰顥婄粻锛勫殠闁插秷顥?娣囶喗鏁奸敍鍫濆嚒缁夊娅?Quilt 鐎瑰顥婇弨顖涘瘮閿?
        if (PageInstanceLeft.McInstance.Info.HasQuilt)
        {
            HintService.Hint(Lang.Text("Instance.Overall.Reset.QuiltUnsupported"));
            return;
        }

        // 绾喛顓婚悧鍫熸拱闂呮梻顬?
        if (selectedLoaderName is not null &&
            (Config.Launch.IndieSolutionV2 == 0 ||
             Config.Launch.IndieSolutionV2 == 2))
            if (ModMain.MyMsgBox(
                    Lang.Text("Download.Install.InstanceIsolation.Warning.Message"), Lang.Text("Download.Install.InstanceIsolation.Warning.Title"), Lang.Text("Download.Install.InstanceIsolation.Warning.Cancel"), Lang.Text("Download.Install.InstanceIsolation.Warning.Continue")) == 1)
                return;

        if (_installAction == InstallAction.Reset)
            if (ModMain.MyMsgBox(
                    Lang.Text("Instance.Install.Reset.Message"),
                    Lang.Text("Instance.Install.Reset.Title"),
                    Lang.Text("Common.Action.Continue"),
                    Lang.Text("Common.Action.Cancel")
                ) == 2)
                return;

        // 閸掔娀娅?LabyMod Neo 閺傚洣娆?
        if ((PageInstanceLeft.McInstance.PathIndie ?? "") != (PageInstanceLeft.McInstance.PathInstance ?? "") &&
            PageInstanceLeft.McInstance.Info.HasLabyMod)
            Directory.Delete(System.IO.Path.Combine(PageInstanceLeft.McInstance.PathIndie, "labymod-neo"), true);
        // 婢跺洣鍞ょ€圭偘绶ラ弽绋跨妇閺傚洣娆?
        ModBase.CopyFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".json",
            PageInstanceLeft.McInstance.PathInstance + @"PCLInstallBackups\" + PageInstanceLeft.McInstance.Name + ".json");
        if (File.Exists(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".jar"))
            ModBase.CopyFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".jar",
                PageInstanceLeft.McInstance.PathInstance + @"PCLInstallBackups\" + PageInstanceLeft.McInstance.Name +
                ".jar");
        // 绾喛顓婚悪顒傜彌 API (婵?Fabric API 缁? 閺勵垰鎯侀棁鈧憰浣筋潶娣囶喗鏁?
        if (selectedFabricApi?.Equals(_currentFabricApi) == true)
            selectedFabricApi = null;
        if (selectedLegacyFabricApi?.Equals(_currentLegacyFabricApi) == true)
            selectedLegacyFabricApi = null;
        if (selectedOptiFabric?.Equals(_currentOptiFabric) == true)
            selectedOptiFabric = null;
        // 閹绘劒姘︾€瑰顥婇悽瀹狀嚞
        var request = new ModDownloadLib.McInstallRequest
        {
            targetInstanceName = PageInstanceLeft.McInstance.Name,
            targetInstanceFolder = $@"{ModFolder.mcFolderSelected}versions\{PageInstanceLeft.McInstance.Name}\",
            minecraftJson = _vanillaData?["url"].ToString(),
            minecraftName = _vanillaName,
            optiFineEntry = selectedOptiFine,
            forgeEntry = selectedForge,
            neoForgeEntry = selectedNeoForge,
            neoForgeVersion = selectedNeoForgeVersion,
            cleanroomEntry = selectedCleanroom,
            cleanroomVersion = selectedCleanroomVersion,
            fabricVersion = selectedFabric,
            fabricApi = selectedFabricApi,
            optiFabric = selectedOptiFabric,
            liteLoaderEntry = selectedLiteLoader,
            labyModChannel = selectedLabyModChannel,
            labyModCommitRef = selectedLabyModCommitRef,
            legacyFabricVersion = selectedLegacyFabric,
            legacyFabricApi = selectedLegacyFabricApi
        };
        BtnSelectStart.IsEnabled = false;
        if (!ModDownloadLib.McInstall(request, _installAction == InstallAction.Modify ? Lang.Text("Instance.Install.Action.ModifyLabel") : Lang.Text("Common.Action.Reset")))
            return;
        // 閸掔娀娅庨弮褏娈戦悪顒傜彌 API 閺傚洣娆?
        if (selectedFabricApi is not null && _currentFabricApiPath is not null)
            File.Delete(_currentFabricApiPath);
        if (selectedLegacyFabricApi is not null && _currentLegacyFabricApiPath is not null)
            File.Delete(_currentLegacyFabricApiPath);
        if (selectedOptiFabric is not null && _currentOptiFabricPath is not null)
            File.Delete(_currentOptiFabricPath);
        // 鏉╂柨娲栨稉濠氥€?
        ModMain.frmMain.PageChange(new FormMain.PageStackData { page = FormMain.PageType.Launch });
    }

    #endregion

    private string GetLoaderError(MyLoading loader)
    {
        if (loader is null || !loader.State.IsLoader)
            return Lang.Text("Download.Install.State.Getting");
        switch (loader.State.LoadingState)
        {
            case MyLoading.MyLoadingState.Run:
            {
                return Lang.Text("Download.Install.State.Getting");
            }
            case MyLoading.MyLoadingState.Error:
            {
                var message = ((ModLoader.LoaderBase)loader.State).Error.Message;
                return message == Lang.Text("Download.Install.State.NoVersion") ? Lang.Text("Download.Install.State.NoVersion") : Lang.Text("Download.Install.State.GetFailed", message);
            }
            case MyLoading.MyLoadingState.Unloaded:
            {
                return Lang.Text("Download.Install.State.UnknownUnloaded");
            }

            default:
            {
                return null;
            }
        }
    }

    #region 妞ょ敻娼伴崚鍥ㄥ床

    // 妞ょ敻娼伴崚鍥ㄥ床閸斻劎鏁?
    public bool isInSelectPage;
    private bool isFirstLoaded;

    private void EnterSelectPage()
    {
        if (isInSelectPage)
            return;
        isInSelectPage = true;

        disabledPageAnimControls.Remove(BtnSelectStart);
        BtnSelectStart.Show = true;
        autoSelectedFabricApi = false;
        autoSelectedOptiFabric = false;
        PanSelect.Visibility = true;
        PanSelect.IsHitTestVisible = true;
        PanMinecraft.IsHitTestVisible = false;
        PanBack.IsHitTestVisible = false;
        PanBack.ScrollToHome();

        CardMinecraft.IsSwapped = true;
        CardOptiFine.IsSwapped = true;
        CardLiteLoader.IsSwapped = true;
        CardForge.IsSwapped = true;
        CardNeoForge.IsSwapped = true;
        CardCleanroom.IsSwapped = true;
        CardFabric.IsSwapped = true;
        CardFabricApi.IsSwapped = true;
        CardOptiFabric.IsSwapped = true;
        CardLabyMod.IsSwapped = true;
        CardLegacyFabric.IsSwapped = true;
        CardLegacyFabricApi.IsSwapped = true;

        if (!(bool)States.Hint.InstallPageBack)
        {
            States.Hint.InstallPageBack = true;
            HintService.Hint(Lang.Text("Download.Install.Hint.MinecraftBack"));
        }

        // 婵″倹鐏夐崷銊┾偓澶嬪妞ょ敻娼伴幐澶夌啊閸掗攱鏌婇柨顕嗙礉闁瀚ㄦい鐢垫畱娑撴粏銈块崣顖濆厴娴兼氨鏁辨禍搴″З閻㈡槒顫﹂梾鎰閿涘奔绲炬稉宥勭窗閻㈠彉绨崝鐘烘祰缂佹挻娼懓灞藉晙濞嗏剝妯夌粈鐚寸礉閸ョ姵顒濇潻娆撳櫡闂団偓鐟曚焦澧滈崝銊︿划婢?
        foreach (var Card in GetAllAnimControls(PanSelect))
        {
            Card.Opacity = 1d;
            Card.RenderTransform = new TranslateTransform();
        }

        // 閸氼垰濮?Forge 閸旂姾娴?
        if (McInstanceInfo.IsFormatFit(_vanillaName))
        {
            var forgeLoader =
                new ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>(
                    "DlForgeVersion " + _vanillaName, ModDownload.DlForgeVersionMain);
            LoadForge.State = forgeLoader;
            forgeLoader.Start(_vanillaName);
        }

        // 閸氼垰濮?Fabric API閵嗕俯egacy Fabric API閵嗕副ptiFabric 閸旂姾娴?
        ModDownload.dlFabricApiLoader.Start();
        ModDownload.dlLegacyFabricApiLoader.Start();
        ModDownload.dlOptiFabricLoader.Start();

        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaOpacity(PanMinecraft, -PanMinecraft.Opacity, 100, 10),
            ModAnimation.AaCode(() =>
            {
                PanBack.ScrollToHome();
                OptiFine_Loaded();
                LiteLoader_Loaded();
                Forge_Loaded();
                NeoForge_Loaded();
                Cleanroom_Loaded();
                Fabric_Loaded();
                LegacyFabric_Loaded();
                FabricApi_Loaded();
                LegacyFabricApi_Loaded();
                LabyMod_Loaded();
                OptiFabric_Loaded();
                ReloadSelected();
            }, after: true),
            ModAnimation.AaOpacity(PanSelect, 1d - PanSelect.Opacity, 250, 150),
            ModAnimation.AaCode(() =>
            {
                PanMinecraft.Visibility = false;
                PanBack.IsHitTestVisible = true;
                // 閸掓繂顫愰崠?Binding
                if (isFirstLoaded)
                    return;
                isFirstLoaded = true;
                BtnOptiFineClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardOptiFine.MainTextBlock, Mode = BindingMode.OneWay });
                BtnLiteLoaderClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardLiteLoader.MainTextBlock, Mode = BindingMode.OneWay });
                BtnForgeClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardForge.MainTextBlock, Mode = BindingMode.OneWay });
                BtnLegacyFabricClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardLegacyFabric.MainTextBlock, Mode = BindingMode.OneWay });
                BtnNeoForgeClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardNeoForge.MainTextBlock, Mode = BindingMode.OneWay });
                BtnCleanroomClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardCleanroom.MainTextBlock, Mode = BindingMode.OneWay });
                BtnFabricClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardFabric.MainTextBlock, Mode = BindingMode.OneWay });
                BtnLegacyFabricApiClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground")
                        { Source = CardLegacyFabricApi.MainTextBlock, Mode = BindingMode.OneWay });
                BtnFabricApiClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardFabricApi.MainTextBlock, Mode = BindingMode.OneWay });
                BtnLabyModClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardLabyMod.MainTextBlock, Mode = BindingMode.OneWay });
                BtnOptiFabricClearInner.SetBinding(Shape.FillProperty,
                    new Binding("Foreground") { Source = CardOptiFabric.MainTextBlock, Mode = BindingMode.OneWay });
            }, after: true)
        }, "FrmInstanceInstall SelectPageSwitch", true);
    }

    public void ExitSelectPage()
    {
        if (!isInSelectPage)
            return;
        isInSelectPage = false;

        LoadMinecraft_OnFinish();

        disabledPageAnimControls.Add(BtnSelectStart);
        BtnSelectStart.Show = false;

        ClearSelected(); // 濞撳懘娅庡鏌モ偓澶嬪妞?
        PanMinecraft.Visibility = true;
        PanSelect.IsHitTestVisible = false;
        PanMinecraft.IsHitTestVisible = true;
        PanBack.IsHitTestVisible = false;
        PanBack.ScrollToHome();

        ModAnimation.AniStart(new[]
        {
            ModAnimation.AaOpacity(PanSelect, -PanSelect.Opacity, 90, 10),
            ModAnimation.AaCode(() => PanBack.ScrollToHome(), after: true),
            ModAnimation.AaOpacity(PanMinecraft, 1d - PanMinecraft.Opacity, 150, 100),
            ModAnimation.AaCode(() =>
            {
                PanSelect.Visibility = false;
                PanBack.IsHitTestVisible = true;
            }, after: true)
        }, "FrmInstanceInstall SelectPageSwitch");
    }

    // 妞ょ敻娼伴崚鍥ㄥ床鐟欙箑褰?
    public void MinecraftSelected(MyListItem sender, PointerReleasedEventArgs e)
    {
        _vanillaName = sender.Title;
        _vanillaData = (JsonObject)sender.Tag;
        _vanillaIcon = sender.Logo;
        EnterSelectPage();
    }

    private void CardMinecraft_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        ExitSelectPage();
        e.handled = true;
    }

    #endregion

    #region 闁瀚?

    // Minecraft
    private string? _vanillaName;
    private JsonObject? _vanillaData;
    private string? _vanillaIcon;
    private int VanillaDrop => McInstanceInfo.VersionToDrop(_vanillaName, true);

    // OptiFine
    private ModDownload.DlOptiFineListEntry? selectedOptiFine;

    /// <summary>
    ///     闁鐣鹃惃?Mod Loader 閸氬秶袨閿涘苯鍞寸€圭懓绨叉稉?Forge / NeoForge / Fabric / Cleanroom / LabyMod / LegacyFabric
    /// </summary>
    private string? selectedLoaderName;

    /// <summary>
    ///     闁鐣鹃惃?Mod Loader API 閸氬秶袨閿涘苯鍞寸€圭懓绨叉稉?Fabric API
    /// </summary>
    private string? selectedAPIName;

    // LiteLoader
    private ModDownload.DlLiteLoaderListEntry? selectedLiteLoader;

    // Forge
    private ModDownload.DlForgeVersionEntry? selectedForge;

    // Cleanroom
    private ModDownload.DlCleanroomListEntry? selectedCleanroom;
    private string? selectedCleanroomVersion;

    // NeoForge
    private ModDownload.DlNeoForgeListEntry? selectedNeoForge;
    private string? selectedNeoForgeVersion;

    // Fabric
    private string? selectedFabric;

    // FabricApi
    private ModComp.CompFile? selectedFabricApi;

    // LegacyFabric
    private string? selectedLegacyFabric;

    // Legacy FabricApi
    private ModComp.CompFile? selectedLegacyFabricApi;

    // LabyMod
    private string? selectedLabyModChannel;
    private string? selectedLabyModCommitRef;
    private string? selectedLabyModVersion;

    // OptiFabric
    private ModComp.CompFile? selectedOptiFabric;

    private bool _ReloadSelected_Ongoing; // #3742 娑擃叏绱滾oadOptiFineGetError 娴兼艾鍨垫慨瀣 LoadOptiFine閿涘矁袝閸欐垳绨ㄦ禒?LoadOptiFine.StateChanged閿涘苯顕遍懛鏉戝晙濞喡ょ殶閻?SelectReload

    /// <summary>
    ///     闁插秷娴囧鏌モ偓澶嬪閻ㄥ嫰銆嶉惄顔炬畱閺勫墽銇氶妴?
    /// </summary>
    private void ReloadSelected()
    {
        if (_vanillaName is null || _ReloadSelected_Ongoing)
            return;
        _ReloadSelected_Ongoing = true;
        try
        {
        var selectedInfo = GetSelectInfo();
        // 娑撳顣╃憴?
        ItemSelect.Title = PageInstanceLeft.McInstance.Name;
        ItemSelect.Logo = GetSelectLogo();
        BtnSelectStart.IsEnabled = true;
        if ((selectedInfo ?? "") == (currentInfo ?? ""))
        {
            ItemSelect.Info = selectedInfo;
            BtnSelectStart.Text = Lang.Text("Instance.Install.Action.StartReset");
            _installAction = InstallAction.Reset;
            BtnSelectStart.SvgIcon = "lucide/rotate-ccw";
        }
        else
        {
            ItemSelect.Info = currentInfo + " 閳?" + selectedInfo;
            BtnSelectStart.Text = Lang.Text("Instance.Install.Action.StartModify");
            _installAction = InstallAction.Modify;
            BtnSelectStart.SvgIcon = "lucide/pencil";
        }

        // Minecraft
        ImgMinecraft.Source = new MyBitmap(_vanillaIcon);
        LabMinecraft.Text = _vanillaName;
        LabMinecraft.Foreground = ThemeManager.colorGray1;
        // OptiFine
        if (!McVersionComparer.CompareVersionGe(_vanillaName, "1.7.2"))
        {
            CardOptiFine.Visibility = false;
        }
        else
        {
            CardOptiFine.Visibility = true;
            var optiFineError = LoadOptiFineGetError();
            CardOptiFine.MainSwap.Visibility = optiFineError is null ? true : false;
            if (optiFineError is not null)
                CardOptiFine.IsSwapped = true;
            SetPanelVisibility(PanOptiFineInfo, CardOptiFine.IsSwapped);
            if (selectedOptiFine is null)
            {
                BtnOptiFineClear.Visibility = false;
                ImgOptiFine.Visibility = false;
                LabOptiFine.Text = optiFineError ?? Lang.Text("Download.Install.State.CanAdd");
                LabOptiFine.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnOptiFineClear.Visibility = true;
                ImgOptiFine.Visibility = true;
                LabOptiFine.Text = selectedOptiFine.DisplayName.Replace(_vanillaName + " ", "");
                LabOptiFine.Foreground = ThemeManager.colorGray1;
            }
        }

        // LiteLoader
        if (!McInstanceInfo.IsFormatFit(_vanillaName)
            || !McVersionComparer.CompareVersionGe(_vanillaName, "1.5.2")
            || !McVersionComparer.CompareVersionGe("1.12.2", _vanillaName))
        {
            CardLiteLoader.Visibility = false;
        }
        else
        {
            CardLiteLoader.Visibility = true;
            var liteLoaderError = LoadLiteLoaderGetError();
            CardLiteLoader.MainSwap.Visibility = liteLoaderError is null ? true : false;
            if (liteLoaderError is not null)
                CardLiteLoader.IsSwapped = true; // 娓氬顩ч崷銊ユ倱閺冭泛鐫嶅鈧崡锛勫閺冨爼鈧瀚ㄦ禍鍡曠瑝閸忕厧顔愭い鐟板灟瀵搫鍩楅幎妯哄綌
            SetPanelVisibility(PanLiteLoaderInfo, CardLiteLoader.IsSwapped);
            if (selectedLiteLoader is null)
            {
                BtnLiteLoaderClear.Visibility = false;
                ImgLiteLoader.Visibility = false;
                LabLiteLoader.Text = liteLoaderError ?? Lang.Text("Download.Install.State.CanAdd");
                LabLiteLoader.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnLiteLoaderClear.Visibility = true;
                ImgLiteLoader.Visibility = true;
                LabLiteLoader.Text = selectedLiteLoader.Inherit;
                LabLiteLoader.Foreground = ThemeManager.colorGray1;
            }
        }

        // Forge
        if (!McInstanceInfo.IsFormatFit(_vanillaName)
            || !McVersionComparer.CompareVersionGe(_vanillaName, "1.1"))
        {
            CardForge.Visibility = false;
        }
        else
        {
            CardForge.Visibility = true;
            var forgeError = LoadForgeGetError();
            CardForge.MainSwap.Visibility = forgeError is null ? true : false;
            if (forgeError is not null)
                CardForge.IsSwapped = true;
            SetPanelVisibility(PanForgeInfo, CardForge.IsSwapped);
            if (selectedForge is null)
            {
                BtnForgeClear.Visibility = false;
                ImgForge.Visibility = false;
                LabForge.Text = forgeError ?? Lang.Text("Download.Install.State.CanAdd");
                LabForge.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnForgeClear.Visibility = true;
                ImgForge.Visibility = true;
                LabForge.Text = selectedForge.VersionName;
                LabForge.Foreground = ThemeManager.colorGray1;
            }
        }

        // Cleanroom
        if (_vanillaName == "1.12.2")
        {
            CardCleanroom.Visibility = true;
            var cleanroomError = LoadCleanroomGetError();
            CardCleanroom.MainSwap.Visibility = cleanroomError is null ? true : false;
            if (cleanroomError is not null)
                CardCleanroom.IsSwapped = true;
            SetPanelVisibility(PanCleanroomInfo, CardCleanroom.IsSwapped);
            if (selectedCleanroom is null)
            {
                BtnCleanroomClear.Visibility = false;
                ImgCleanroom.Visibility = false;
                LabCleanroom.Text = cleanroomError ?? Lang.Text("Download.Install.State.CanAdd");
                LabCleanroom.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnCleanroomClear.Visibility = true;
                ImgCleanroom.Visibility = true;
                LabCleanroom.Text = selectedCleanroom.VersionName;
                LabCleanroom.Foreground = ThemeManager.colorGray1;
            }
        }
        else
        {
            CardCleanroom.Visibility = false;
        }

        // NeoForge
        if (!McVersionComparer.CompareVersionGe(_vanillaName, "1.20.1"))
        {
            CardNeoForge.Visibility = false;
        }
        else
        {
            CardNeoForge.Visibility = true;
            var neoForgeError = LoadNeoForgeGetError();
            CardNeoForge.MainSwap.Visibility = neoForgeError is null ? true : false;
            if (neoForgeError is not null)
                CardNeoForge.IsSwapped = true;
            SetPanelVisibility(PanNeoForgeInfo, CardNeoForge.IsSwapped);
            if (selectedNeoForge is null)
            {
                BtnNeoForgeClear.Visibility = false;
                ImgNeoForge.Visibility = false;
                LabNeoForge.Text = neoForgeError ?? Lang.Text("Download.Install.State.CanAdd");
                LabNeoForge.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnNeoForgeClear.Visibility = true;
                ImgNeoForge.Visibility = true;
                LabNeoForge.Text = selectedNeoForge.VersionName;
                LabNeoForge.Foreground = ThemeManager.colorGray1;
            }
        }

        // Fabric
        if (VanillaDrop < 130
            || (VanillaDrop == 130 && !McVersionComparer.CompareVersionGe(_vanillaName, "18w43b")))
        {
            CardFabric.Visibility = false;
        }
        else
        {
            CardFabric.Visibility = true;
            var fabricError = LoadFabricGetError();
            CardFabric.MainSwap.Visibility = fabricError is null ? true : false;
            if (fabricError is not null)
                CardFabric.IsSwapped = true;
            SetPanelVisibility(PanFabricInfo, CardFabric.IsSwapped);
            if (selectedFabric is null)
            {
                BtnFabricClear.Visibility = false;
                ImgFabric.Visibility = false;
                LabFabric.Text = fabricError ?? Lang.Text("Download.Install.State.CanAdd");
                LabFabric.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnFabricClear.Visibility = true;
                ImgFabric.Visibility = true;
                LabFabric.Text = selectedFabric.Replace("+build", "");
                LabFabric.Foreground = ThemeManager.colorGray1;
            }
        }

        // FabricApi
        if (selectedFabric is null)
        {
            CardFabricApi.Visibility = false;
        }
        else
        {
            CardFabricApi.Visibility = true;
            var fabricApiError = LoadFabricApiGetError();
            CardFabricApi.MainSwap.Visibility = fabricApiError is null ? true : false;
            if (fabricApiError is not null || selectedFabric is null)
                CardFabricApi.IsSwapped = true;
            SetPanelVisibility(PanFabricApiInfo, CardFabricApi.IsSwapped);
            if (selectedFabricApi is null)
            {
                BtnFabricApiClear.Visibility = false;
                ImgFabricApi.Visibility = false;
                LabFabricApi.Text = fabricApiError ?? Lang.Text("Download.Install.State.CanAdd");
                LabFabricApi.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnFabricApiClear.Visibility = true;
                ImgFabricApi.Visibility = true;
                LabFabricApi.Text = selectedFabricApi.DisplayName.Split("]")[1].Replace("Fabric API ", "")
                    .Replace(" build ", ".").Split("+").First().Trim();
                LabFabricApi.Foreground = ThemeManager.colorGray1;
            }
        }

        // LegacyFabric
        if (VanillaDrop < 30 || VanillaDrop > 130)
        {
            CardLegacyFabric.Visibility = false;
        }
        else
        {
            CardLegacyFabric.Visibility = true;
            var legacyFabricError = LoadLegacyFabricGetError();
            CardLegacyFabric.MainSwap.Visibility =
                legacyFabricError is null ? true : false;
            if (legacyFabricError is not null)
                CardLegacyFabric.IsSwapped = true;
            SetPanelVisibility(PanLegacyFabricInfo, CardLegacyFabric.IsSwapped);
            if (selectedLegacyFabric is null)
            {
                BtnLegacyFabricClear.Visibility = false;
                ImgLegacyFabric.Visibility = false;
                LabLegacyFabric.Text = legacyFabricError ?? Lang.Text("Download.Install.State.CanAdd");
                LabLegacyFabric.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnLegacyFabricClear.Visibility = true;
                ImgLegacyFabric.Visibility = true;
                LabLegacyFabric.Text = selectedLegacyFabric.Replace("+build", "");
                LabLegacyFabric.Foreground = ThemeManager.colorGray1;
            }
        }

        // LegacyFabricApi
        if (selectedLegacyFabric is null)
        {
            CardLegacyFabricApi.Visibility = false;
        }
        else
        {
            CardLegacyFabricApi.Visibility = true;
            var legacyFabricApiError = LoadLegacyFabricApiGetError();
            CardLegacyFabricApi.MainSwap.Visibility =
                legacyFabricApiError is null ? true : false;
            if (legacyFabricApiError is not null || selectedLegacyFabric is null)
                CardLegacyFabricApi.IsSwapped = true;
            SetPanelVisibility(PanLegacyFabricApiInfo, CardLegacyFabricApi.IsSwapped);
            if (selectedLegacyFabricApi is null)
            {
                BtnLegacyFabricApiClear.Visibility = false;
                ImgLegacyFabricApi.Visibility = false;
                LabLegacyFabricApi.Text = legacyFabricApiError ?? Lang.Text("Download.Install.State.CanAdd");
                LabLegacyFabricApi.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnLegacyFabricApiClear.Visibility = true;
                ImgLegacyFabricApi.Visibility = true;
                LabLegacyFabricApi.Text = selectedLegacyFabricApi.DisplayName.Replace("Legacy Fabric API ", "");
                LabLegacyFabricApi.Foreground = ThemeManager.colorGray1;
            }
        }

        // LabyMod
        if (!McInstanceInfo.IsFormatFit(_vanillaName)
            || !McVersionComparer.CompareVersionGe(_vanillaName, "1.8.9"))
        {
            CardLabyMod.Visibility = false;
        }
        else
        {
            CardLabyMod.Visibility = true;
            var labyModError = LoadLabyModGetError();
            CardLabyMod.MainSwap.Visibility = labyModError is null ? true : false;
            if (labyModError is not null)
                CardLabyMod.IsSwapped = true;
            SetPanelVisibility(PanLabyModInfo, CardLabyMod.IsSwapped);
            if (selectedLabyModVersion is null)
            {
                BtnLabyModClear.Visibility = false;
                ImgLabyMod.Visibility = false;
                LabLabyMod.Text = labyModError ?? Lang.Text("Download.Install.State.CanAdd");
                LabLabyMod.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnLabyModClear.Visibility = true;
                ImgLabyMod.Visibility = true;
                LabLabyMod.Text = selectedLabyModVersion;
                LabLabyMod.Foreground = ThemeManager.colorGray1;
            }
        }

        // OptiFabric
        if (selectedFabric is null || selectedOptiFine is null)
        {
            CardOptiFabric.Visibility = false;
        }
        else
        {
            CardOptiFabric.Visibility = true;
            var optiFabricError = LoadOptiFabricGetError();
            CardOptiFabric.MainSwap.Visibility = optiFabricError is null ? true : false;
            if (optiFabricError is not null || selectedFabric is null)
                CardOptiFabric.IsSwapped = true;
            SetPanelVisibility(PanOptiFabricInfo, CardOptiFabric.IsSwapped);
            if (selectedOptiFabric is null)
            {
                BtnOptiFabricClear.Visibility = false;
                ImgOptiFabric.Visibility = false;
                LabOptiFabric.Text = optiFabricError ?? Lang.Text("Download.Install.State.CanAdd");
                LabOptiFabric.Foreground = ThemeManager.colorGray4;
            }
            else
            {
                BtnOptiFabricClear.Visibility = true;
                ImgOptiFabric.Visibility = true;
                LabOptiFabric.Text = selectedOptiFabric.DisplayName.ToLower().Replace("optifabric-", "")
                    .Replace(".jar", "").Trim().TrimStart('v');
                LabOptiFabric.Foreground = ThemeManager.colorGray1;
            }
        }

        // 娑撴槒顒熼崨?
        if (selectedFabric is not null && selectedFabricApi is null)
            HintFabricAPI.Visibility = true;
        else
            HintFabricAPI.Visibility = false;
        if (selectedLegacyFabric is not null && selectedLegacyFabricApi is null)
            HintLegacyFabricAPI.Visibility = true;
        else
            HintLegacyFabricAPI.Visibility = false;

        if ((selectedFabric is not null || selectedLegacyFabric is not null) && selectedOptiFine is not null &&
            selectedOptiFabric is null)
        {
            if (VanillaDrop >= 140 && VanillaDrop <= 150)
            {
                HintOptiFabric.Visibility = false;
                HintLegacyOptiFabric.Visibility = false;
                HintOptiFabricOld.Visibility = true;
            }
            else if (selectedLegacyFabric is not null)
            {
                HintOptiFabric.Visibility = false;
                HintLegacyOptiFabric.Visibility = true;
                HintOptiFabricOld.Visibility = false;
            }
            else
            {
                HintOptiFabric.Visibility = true;
                HintOptiFabricOld.Visibility = false;
                HintLegacyOptiFabric.Visibility = false;
            }
        }
        else
        {
            HintOptiFabric.Visibility = false;
            HintOptiFabricOld.Visibility = false;
            HintLegacyOptiFabric.Visibility = false;
        }

        if (VanillaDrop >= 160 && selectedOptiFine is not null &&
            (selectedForge is not null || selectedFabric is not null))
            HintModOptiFine.Visibility = true;
        else
            HintModOptiFine.Visibility = false;
        // 缂佹挻娼?
        }
        finally
        {
            _ReloadSelected_Ongoing = false;
        }
    }

    /// <summary>
    ///     濞撳懐鈹栧鏌モ偓澶嬪閻ㄥ嫰銆嶉惄顔衡偓?
    /// </summary>
    private void ClearSelected()
    {
        _vanillaName = null;
        _vanillaData = null;
        _vanillaIcon = null;
        selectedOptiFine = null;
        selectedLiteLoader = null;
        selectedLoaderName = null;
        selectedAPIName = null;
        selectedForge = null;
        selectedNeoForge = null;
        selectedNeoForgeVersion = null;
        selectedCleanroom = null;
        selectedCleanroomVersion = null;
        selectedFabric = null;
        selectedFabricApi = null;
        selectedOptiFabric = null;
        selectedLabyModCommitRef = null;
        selectedLabyModVersion = null;
        selectedLabyModChannel = null;
        selectedLegacyFabric = null;
        selectedLegacyFabricApi = null;
    }

    // 娣団剝浼呴弽蹇撳З閻?
    private void SetPanelVisibility(Grid panel, bool visible)
    {
        if (Equals(panel.Tag, visible.ToString()))
            return;
        panel.Tag = visible.ToString();
        if (visible)
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaTranslateY(panel, -((TranslateTransform)panel.RenderTransform).Y, 150,
                        ease: new ModAnimation.AniEaseOutFluent()),
                    ModAnimation.AaOpacity(panel, 1d - panel.Opacity, 60)
                }, "PageDownloadInstall Visibility " + panel.Name);
        else
            ModAnimation.AniStart(
                new[]
                {
                    ModAnimation.AaTranslateY(panel, 6d - ((TranslateTransform)panel.RenderTransform).Y, 60),
                    ModAnimation.AaOpacity(panel, -panel.Opacity, 60)
                }, "PageDownloadInstall Visibility " + panel.Name);
    }

    /// <summary>
    ///     閼惧嘲褰囩€圭偘绶ラ崶鐐垼閵?
    /// </summary>
    private string GetSelectLogo()
    {
        if (selectedFabric is not null) return "avares://PCL/Images/Blocks/Fabric.png";

        if (selectedLegacyFabric is not null) return "avares://PCL/Images/Blocks/Fabric.png";

        if (selectedForge is not null) return "avares://PCL/Images/Blocks/Anvil.png";

        if (selectedNeoForge is not null) return "avares://PCL/Images/Blocks/NeoForge.png";

        if (selectedLiteLoader is not null) return "avares://PCL/Images/Blocks/Egg.png";

        if (selectedOptiFine is not null) return "avares://PCL/Images/Blocks/GrassPath.png";

        if (selectedCleanroom is not null) return "avares://PCL/Images/Blocks/Cleanroom.png";

        if (selectedLabyModVersion is not null) return "avares://PCL/Images/Blocks/LabyMod.png";

        return _vanillaIcon;
    }

    /// <summary>
    ///     閼惧嘲褰囩€圭偘绶ラ幓蹇氬牚娣団剝浼呴妴?
    /// </summary>
    private string GetSelectInfo()
    {
        var parts = new List<string>
        {
            _vanillaName
        };

        var loaderInfos = new (string NameKey, string? Version)[]
        {
            ("Common.Installation.Fabric", selectedFabric?.Replace("+build", "")),
            ("Common.Installation.LegacyFabric", selectedLegacyFabric),
            ("Common.Installation.Forge", selectedForge?.VersionName),
            ("Common.Installation.NeoForge",
                selectedNeoForge?.VersionName ?? VersionOrNull(selectedNeoForgeVersion)),
            ("Common.Installation.Cleanroom",
                selectedCleanroom?.VersionName ?? VersionOrNull(selectedCleanroomVersion)),
            ("Common.Installation.LabyMod", selectedLabyModVersion),
            ("Common.Installation.OptiFine",
                selectedOptiFine?.DisplayName.Replace(_vanillaName + " ", ""))
        };

        parts.AddRange(
            loaderInfos
                .Where(info => !string.IsNullOrWhiteSpace(info.Version))
                .Select(info => $"{Lang.Text(info.NameKey)} {info.Version}")
        );

        if (selectedLiteLoader is not null) parts.Add(Lang.Text("Common.Installation.LiteLoader"));

        if (parts.Count == 1) parts.Add(Lang.Text("Instance.Install.NoExtraInstall"));

        return string.Join("  |  ", parts);
    }

    private static string? VersionOrNull<T>(T version)
    {
        return EqualityComparer<T>.Default.Equals(version, default!)
            ? null
            : version?.ToString();
    }

    #endregion

    #region 瑜版挸澧犳穱鈩冧紖閼惧嘲褰?

    private ModComp.CompFile _currentFabricApi; // 閸旂姾娴囩€瑰本鍨氶崥搴ｆ纯閹恒儴鐨熼悽銊や簰閹绘劙鐝幀褑鍏?
    private string _currentFabricApiPath;

    private object GetCurrentFabricApi() // 鏉╂稑鍙嗘い鐢告桨閸滃矁浠堢純鎴濆鏉炶姤妞傜拫鍐暏
    {
        var loaderOutput = ModDownload.dlFabricApiLoader.output;
        if (loaderOutput is null)
            return null; // 绾喕绻氶懕鏃傜秹娣団剝浼呭鎻掑鏉?
        var localComp = ModLocalComp.GetModLocalCompByKeywords(PageInstanceLeft.McInstance,
            new[] { "fabric-api", "fabric" }, "fabric", "api");
        if (localComp is null)
            return null;
        var result = loaderOutput.FirstOrDefault(comp => (comp.Hash ?? "") == (localComp.ModrinthHash ?? ""));
        if (result is not null)
        {
            _currentFabricApi = result;
            _currentFabricApiPath = localComp.path;
        }

        return result;
    }

    private ModComp.CompFile _currentLegacyFabricApi; // 閸旂姾娴囩€瑰本鍨氶崥搴ｆ纯閹恒儴鐨熼悽銊や簰閹绘劙鐝幀褑鍏?
    private string _currentLegacyFabricApiPath;

    private object GetCurrentLegacyFabricApi() // 鏉╂稑鍙嗘い鐢告桨閸滃矁浠堢純鎴濆鏉炶姤妞傜拫鍐暏
    {
        var loaderOutput = ModDownload.dlLegacyFabricApiLoader.output;
        if (loaderOutput is null)
            return null; // 绾喕绻氶懕鏃傜秹娣団剝浼呭鎻掑鏉?
        var localComp = ModLocalComp.GetModLocalCompByKeywords(PageInstanceLeft.McInstance,
            new[] { "legacy-fabric-api", "legacy-fabric" }, "legacy-fabric", "api");
        if (localComp is null)
            return null;
        var result = loaderOutput.FirstOrDefault(comp => (comp.Hash ?? "") == (localComp.ModrinthHash ?? ""));
        if (result is not null)
        {
            _currentLegacyFabricApi = result;
            _currentLegacyFabricApiPath = localComp.path;
        }

        return result;
    }

    private ModComp.CompFile _currentOptiFabric;
    private string _currentOptiFabricPath;

    private object GetCurrentOptiFabric()
    {
        var loaderOutput = ModDownload.dlOptiFabricLoader.output;
        if (loaderOutput is null)
            return null;
        var localComp =
            ModLocalComp.GetModLocalCompByKeywords(PageInstanceLeft.McInstance, "optifabric", "optifabric", "opti");
        if (localComp is null)
            return null;
        var result = loaderOutput.FirstOrDefault(comp => (comp.Hash ?? "") == (localComp.ModrinthHash ?? ""));
        if (result is not null)
        {
            _currentOptiFabric = result;
            _currentOptiFabricPath = localComp.path;
        }

        return result;
    }

    // 瑜版挸澧犳穱鈩冧紖閼惧嘲褰?
    public void GetCurrentInfo()
    {
        ClearSelected();
        BtnSelectStart.IsEnabled = true;
        var currentInstance = PageInstanceLeft.McInstance.Info;
        _vanillaName = currentInstance.VanillaName;
        if (currentInstance.HasLiteLoader)
            selectedLiteLoader = new ModDownload.DlLiteLoaderListEntry { Inherit = currentInstance.VanillaName };
        if (currentInstance.HasOptiFine)
            selectedOptiFine = new ModDownload.DlOptiFineListEntry
            {
                DisplayName = currentInstance.VanillaName + " " + currentInstance.OptiFine.Replace("_", " "),
                IsPreview = currentInstance.OptiFine.ContainsF("pre"), Inherit = currentInstance.VanillaName,
                NameVersion = currentInstance.VanillaName + "-OptiFine_HD_U_" + currentInstance.OptiFine
            };
        if (currentInstance.HasCleanroom)
        {
            selectedLoaderName = "Cleanroom";
            selectedAPIName = "Cleanroom";
            selectedCleanroomVersion = currentInstance.Cleanroom;
            selectedCleanroom = new ModDownload.DlCleanroomListEntry(selectedCleanroomVersion);
        }
        else if (currentInstance.HasForge)
        {
            selectedLoaderName = "Forge";
            selectedForge =
                new ModDownload.DlForgeVersionEntry(currentInstance.Forge, null, currentInstance.VanillaName)
                {
                    Category = "installer", forgeType = ModDownload.DlForgelikeEntry.ForgelikeType.Forge,
                    Inherit = currentInstance.VanillaName
                };
        }
        else if (currentInstance.HasLegacyFabric)
        {
            selectedLoaderName = "LegacyFabric";
            selectedLegacyFabric = currentInstance.LegacyFabric;
            selectedLegacyFabricApi = (ModComp.CompFile)GetCurrentLegacyFabricApi();
        }
        else if (currentInstance.HasFabric)
        {
            selectedLoaderName = "Fabric";
            selectedFabric = currentInstance.Fabric;
            selectedFabricApi = (ModComp.CompFile)GetCurrentFabricApi();
        }
        else if (currentInstance.HasLabyMod)
        {
            selectedLoaderName = "LabyMod";
            selectedLabyModVersion = currentInstance.LabyMod;
        }
        else if (currentInstance.HasNeoForge)
        {
            selectedLoaderName = "NeoForge";
            selectedNeoForgeVersion = currentInstance.NeoForge;
            selectedNeoForge = new ModDownload.DlNeoForgeListEntry(currentInstance.NeoForge)
            {
                VersionName = currentInstance.NeoForge, Inherit = currentInstance.VanillaName,
                forgeType = ModDownload.DlForgelikeEntry.ForgelikeType.NeoForge
            };
        }

        if (currentInstance.HasFabric && currentInstance.HasOptiFine)
            selectedOptiFabric = (ModComp.CompFile)GetCurrentOptiFabric();
        _vanillaIcon = "avares://PCL/Images/Blocks/Grass.png"; // TODO: 闂団偓鐟曚礁鍨介弬?Icon
        currentInfo = GetSelectInfo();
        EnterSelectPage();
    }

    private string currentInfo;

    #endregion

    #region 閸旂姾娴囬崳?

    // 缂佹挻鐏夐弫鐗堝祦閸?
    private static string GetVersionTypeTitle(string key) => key switch
    {
        "濮濓絽绱￠悧? => Lang.Text("Download.Version.Type.Release"),
        "妫板嫯顫嶉悧? => Lang.Text("Download.Version.Type.Development"),
        "鏉╂粌褰滈悧? => Lang.Text("Download.Version.Type.BeforeRelease"),
        "閹版矮姹夐懞鍌滃" => Lang.Text("Download.Version.Type.AprilFools"),
        _ => key
    };

    private void LoadMinecraft_OnFinish()
    {
        ExitSelectPage(); // 鏉╂柨娲?
        do
        {
            try
            {
                var dict = new Dictionary<string, List<JsonObject>>
                {
                    { "濮濓絽绱￠悧?, new List<JsonObject>() }, { "妫板嫯顫嶉悧?, new List<JsonObject>() }, { "鏉╂粌褰滈悧?, new List<JsonObject>() },
                    { "閹版矮姹夐懞鍌滃", new List<JsonObject>() }
                };
                var versions = (JsonArray)ModDownload.dlClientListLoader.output.Value["versions"];
                foreach (JsonObject Version in versions)
                {
                    // 绾喖鐣鹃崚鍡欒
                    var type = Version["type"].ToString();
                    var versionId = Version["id"].ToString().ToLower();
                    switch (type ?? "")
                    {
                        case "release":
                        {
                            type = "濮濓絽绱￠悧?;
                            break;
                        }
                        case "snapshot":
                        case "pending":
                        {
                            type = "妫板嫯顫嶉悧?;
                            // Mojang 鐠囶垰鍨庣猾?
                            if (versionId.StartsWith("1.") && !versionId.Contains("combat") &&
                                !versionId.Contains("rc") && !versionId.Contains("experimental") &&
                                !versionId.Equals("1.2") && !versionId.Contains("pre"))
                            {
                                type = "濮濓絽绱￠悧?;
                                Version["type"] = "release";
                            }

                            // 閹版矮姹夐懞鍌滃閺?
                            switch (Version["id"].ToString().ToLower() ?? "")
                            {
                                case "2point0_blue":
                                case "2point0_red":
                                case "2point0_purple":
                                case "2.0_blue":
                                case "2.0_red":
                                case "2.0_purple":
                                case "2.0":
                                {
                                    type = "閹版矮姹夐懞鍌滃";
                                    Version["id"] = Version["id"].ToString().Replace("point", ".");
                                    Version["type"] = "special";
                                    Version.Add("lore", McVersionClassifier.GetMcFoolName((string)Version["id"]));
                                    break;
                                }
                                case "20w14infinite":
                                case "20w14閳?:
                                {
                                    type = "閹版矮姹夐懞鍌滃";
                                    Version["id"] = "20w14閳?;
                                    Version["type"] = "special";
                                    Version.Add("lore", McVersionClassifier.GetMcFoolName((string)Version["id"]));
                                    break;
                                }
                                case "3d shareware v1.34":
                                case "1.rv-pre1":
                                case "15w14a":
                                case var @case when @case == "2.0":
                                case "22w13oneblockatatime":
                                case "23w13a_or_b":
                                case "24w14potato":
                                case "25w14craftmine":
                                case "26w14a":
                                {
                                    type = "閹版矮姹夐懞鍌滃";
                                    Version["type"] = "special";
                                    Version.Add("lore",
                                        McVersionClassifier.GetMcFoolName((string)Version["id"])); // 4/1 閼奉亜濮╃憴鍡曠稊閹版矮姹夐懞鍌滃
                                    break;
                                }

                                default:
                                {
                                    var releaseDate = McVersionClassifier.GetReleaseTime(Version).ToUniversalTime().AddHours(2d);
                                    if (releaseDate.Month == 4 && releaseDate.Day == 1)
                                    {
                                        type = "閹版矮姹夐懞鍌滃";
                                        Version["type"] = "special";
                                    }

                                    break;
                                }
                            }

                            break;
                        }
                        case "special":
                        {
                            // 瀹歌尪顫︽径鍕倞閻ㄥ嫭鍓兼禍楦垮Ν閻?
                            type = "閹版矮姹夐懞鍌滃";
                            break;
                        }

                        default:
                        {
                            type = "鏉╂粌褰滈悧?;
                            break;
                        }
                    }

                    // 閸旂姴鍙嗘潏鐐插悁
                    dict[type].Add(Version);
                }

                // 閹烘帒绨?
                foreach (var Pair in dict.ToList())
                    dict[Pair.Key] = Pair.Value.OrderByDescending(McVersionClassifier.GetReleaseTime).ToList();
                // 濞撳懐鈹栬ぐ鎾冲
                PanMinecraft.Children.Clear();
                // 濞ｈ濮為張鈧弬鎵閺?
                var cardInfo = new MyCard { Title = Lang.Text("Download.Version.Latest.Title"), Margin = new Thickness(0d, 15d, 0d, 15d) };
                var topestVersions = new List<JsonObject>();
                var release = (JsonObject)dict["濮濓絽绱￠悧?][0].DeepClone();
                release["lore"] = Lang.Text("Download.Version.Latest.Release", Lang.Date(release["releaseTime"].ToObject<DateTime>(), "g"));
                topestVersions.Add(release);
                if (dict["濮濓絽绱￠悧?][0]["releaseTime"].ToObject<DateTime>() < dict["妫板嫯顫嶉悧?][0]["releaseTime"].ToObject<DateTime>())
                {
                    var snapshot = (JsonObject)dict["妫板嫯顫嶉悧?][0].DeepClone();
                    snapshot["lore"] = Lang.Text("Download.Version.Latest.Development", Lang.Date(snapshot["releaseTime"].ToObject<DateTime>(), "g"));
                    topestVersions.Add(snapshot);
                }

                var panInfo = new StackPanel
                {
                    Margin = new Thickness(20d, MyCard.SwapedHeight, 18d, 0d),
                    VerticalAlignment = VerticalAlignment.Top, RenderTransform = new TranslateTransform(0d, 0d),
                    Tag = topestVersions
                };

                void StackInstall(StackPanel stack)
                {
                    foreach (var item in (IEnumerable)stack.Tag)
                        stack.Children.Add(ModDownloadLib.McDownloadListItem((JsonObject)item,
                            (sender, e) => MinecraftSelected((MyListItem)sender, e), false));
                }

                ;
                MyCard.StackInstall(ref panInfo, StackInstall);
                cardInfo.Children.Add(panInfo);
                PanMinecraft.Children.Insert(0, cardInfo);
                // 濞ｈ濮為崗鏈电铂閻楀牊婀?
                foreach (var Pair in dict)
                {
                    if (!Pair.Value.Any())
                        continue;
                    // 婢х偛濮為崡锛勫
                    var newCard = new MyCard
                        { Title = GetVersionTypeTitle(Pair.Key) + " (" + Pair.Value.Count + ")", Margin = new Thickness(0d, 0d, 0d, 15d) };
                    var newStack = new StackPanel
                    {
                        Margin = new Thickness(20d, MyCard.SwapedHeight, 18d, 0d),
                        VerticalAlignment = VerticalAlignment.Top, RenderTransform = new TranslateTransform(0d, 0d),
                        Tag = Pair.Value
                    };
                    newCard.Children.Add(newStack);
                    newCard.SwapControl = newStack;
                    // 娑撳秷鍏樻担璺ㄦ暏 AddressOf閿涘矁绻栫€佃壈鍤ф禍?#535閿涘苯甯崶鐘茬暚閸忋劋绗夐弰搴礉閻ゆ垳鎶€閺勵垳绱拠鎴濇珤 Bug
                    newCard.InstallMethod = StackInstall;
                    newCard.IsSwapped = true;
                    PanMinecraft.Children.Add(newCard);
                }

                // 閼奉亜濮╅柅澶嬪閻楀牊婀?
                if (mcVersionWaitingForSelect is null)
                    break;
                ModBase.Log("[Download] 閼奉亜濮╅柅澶嬪 MC 閻楀牊婀伴敍? + mcVersionWaitingForSelect);
                foreach (JsonObject Version in versions)
                {
                    if ((Version["id"].ToString() ?? "") != (mcVersionWaitingForSelect ?? ""))
                        continue;
                    var item = ModDownloadLib.McDownloadListItem(Version, (_, _) => { }, false);
                    MinecraftSelected(item, null);
                }
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "閸欘垵顫嬮崠鏍х暔鐟佸懐澧楅張顒€鍨悰銊ュ毉闁?,
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
            }
        } while (false);
    }

    /// <summary>
    ///     瑜?MC 閻楀牊婀伴崚妤勩€冮崝鐘烘祰鐎瑰本妞傞敍宀€鐝涢崡瀹犲殰閸斻劑鈧瀚ㄩ惃鍕閺堫兙鈧倻鏁ゆ禍搴☆樆闁劏鐨熼悽銊ｂ偓?
    /// </summary>
    public static string mcVersionWaitingForSelect = null;

    #endregion

    #region OptiFine 閸掓銆?

    /// <summary>
    ///     閼惧嘲褰?OptiFine 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadOptiFineGetError()
    {
        if (selectedLoaderName == "NeoForge" || selectedLoaderName == "LabyMod" || selectedLoaderName == "Cleanroom")
            return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
        if (LoadOptiFine is null || LoadOptiFine.State.LoadingState == MyLoading.MyLoadingState.Run)
            return Lang.Text("Download.Install.State.Loading");
        if (LoadOptiFine.State.LoadingState == MyLoading.MyLoadingState.Error)
            return $"{Lang.Text("Download.Install.State.GetVersionListFailed")}{((ModLoader.LoaderBase)LoadOptiFine.State).Error.Message}";
        // 濡偓閺?Forge 1.13 - 1.14.3閿涙艾鍙忛柈銊ょ瑝閸忕厧顔?
        if (selectedLoaderName == "Forge" && McVersionComparer.CompareVersion(_vanillaName, "1.13") >= 0 &&
            McVersionComparer.CompareVersion("1.14.3", _vanillaName) >= 0) return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
        // 濡偓閺?Fabric 1.20.5+: 閸忋劑鍎存稉宥呭悑鐎?
        if (selectedFabric is not null && McVersionComparer.CompareVersion(_vanillaName, "1.20.4") > 0)
            return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
        // 濡偓閺?Loader
        if (GetLoaderError(LoadOptiFine) is not null)
            return GetLoaderError(LoadOptiFine);
        // 濡偓閺?Forge 閻楀牊婀?
        var hasAny = false;
        var hasRequiredVersion = false;
        foreach (var OptiFineVersion in ModDownload.dlOptiFineListLoader.output.Value)
        {
            if (!OptiFineVersion.DisplayName.StartsWith(_vanillaName + " "))
                continue; // 娑撳秵妲搁崥灞肩娑擃亜銇囬悧鍫熸拱
            hasAny = true;
            if (selectedForge is null)
                return null; // 閺堫亪鈧瀚?Forge
            if ((bool)IsOptiFineSuitForForge(OptiFineVersion, selectedForge))
                return null; // 鐠囥儳澧楅張顒€褰查悽?
            if (OptiFineVersion.RequiredForgeVersion is not null)
                hasRequiredVersion = true;
        }

        if (!hasAny) return Lang.Text("Download.Install.State.NoVersion");

        if (hasRequiredVersion) return Lang.Text("Download.Install.Compat.CompatForgeSpecificOnly");

        return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
    }

    // 濡偓閺屻儲鐓囨稉?OptiFine 閺勵垰鎯佹稉搴㈢厙娑?Forge 閸忕厧顔?
    private object IsOptiFineSuitForForge(ModDownload.DlOptiFineListEntry optiFine,
        ModDownload.DlForgeVersionEntry forge)
    {
        if ((forge.Inherit ?? "") != (optiFine.Inherit ?? ""))
            return false; // 娑撳秵妲搁崥灞肩娑擃亜銇囬悧鍫熸拱
        if (optiFine.RequiredForgeVersion is null)
            return false; // 娑撳秴鍚嬬€?Forge
        if (string.IsNullOrWhiteSpace(optiFine.RequiredForgeVersion))
            return true; // #4183
        if (optiFine.RequiredForgeVersion.Contains(".")) // XX.X.XXX
            return McVersionComparer.CompareVersion(forge.version.ToString(), optiFine.RequiredForgeVersion) == 0;

        // XXXX
        return forge.version.Revision == Convert.ToDouble(optiFine.RequiredForgeVersion);
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardOptiFine_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadOptiFineGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?OptiFine 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void OptiFine_Loaded()
    {
        try
        {
            if (ModDownload.dlOptiFineListLoader.State != ModBase.LoadState.Finished)
                return;

            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            var versions = new List<ModDownload.DlOptiFineListEntry>();
            foreach (var Version in ModDownload.dlOptiFineListLoader.output.Value)
            {
                if (selectedForge is not null &&
                                          !(bool)IsOptiFineSuitForForge(Version, selectedForge))
                    continue;
                if (Version.DisplayName.StartsWith(_vanillaName + " "))
                    versions.Add(Version);
            }

            if (!versions.Any())
                return;
            // 閹烘帒绨?
            versions.Sort((left, right) =>
            {
                if (!left.IsPreview && right.IsPreview)
                    return true;
                if (left.IsPreview && !right.IsPreview)
                    return false;
                return McVersionComparer.CompareVersion(left.DisplayName, right.DisplayName) != 0;
            });
            // 閸欘垵顫嬮崠?
            PanOptiFine.Children.Clear();
            foreach (var Version in versions)
                PanOptiFine.Children.Add(
                    ModDownloadLib.OptiFineDownloadListItem(Version, (a, b) =>
                        this.OptiFine_Selected((dynamic)a, b), false));
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?OptiFine 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    private void OptiFine_Selected(MyListItem sender, EventArgs e)
    {
        selectedOptiFine = (ModDownload.DlOptiFineListEntry)sender.Tag;
        if (selectedForge is not null &&
                                  !(bool)IsOptiFineSuitForForge(selectedOptiFine, selectedForge))
            selectedForge = null;
        OptiFabric_Loaded();
        Forge_Loaded();
        NeoForge_Loaded();
        CardOptiFine.IsSwapped = true;
        ReloadSelected();
    }

    private void OptiFine_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedOptiFine = null;
        selectedOptiFabric = null;
        autoSelectedOptiFabric = false;
        CardOptiFine.IsSwapped = true;
        e.Handled = true;
        Forge_Loaded();
        NeoForge_Loaded();
        ReloadSelected();
    }

    #endregion

    #region LiteLoader 閸掓銆?

    /// <summary>
    ///     閼惧嘲褰?LiteLoader 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadLiteLoaderGetError()
    {
        // 濡偓閺?Loader
        if (GetLoaderError(LoadLiteLoader) is not null)
            return GetLoaderError(LoadLiteLoader);
        if (selectedLoaderName == "NeoForge" || selectedLoaderName == "LegacyFabric" || selectedLoaderName == "LabyMod" || selectedLoaderName == "Cleanroom")
            return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
        // 濡偓閺屻儳澧楅張?
        return ModDownload.dlLiteLoaderListLoader.output.Value.Any(v => (v.Inherit ?? "") == (_vanillaName ?? ""))
            ? null
            : Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardLiteLoader_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadLiteLoaderGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?LiteLoader 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void LiteLoader_Loaded()
    {
        try
        {
            if (ModDownload.dlLiteLoaderListLoader.State != ModBase.LoadState.Finished)
                return;
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            var versions = new List<ModDownload.DlLiteLoaderListEntry>();
            foreach (var Version in ModDownload.dlLiteLoaderListLoader.output.Value)
                if ((Version.Inherit ?? "") == (_vanillaName ?? ""))
                    versions.Add(Version);
            if (!versions.Any())
                return;
            // 閸欘垵顫嬮崠?
            PanLiteLoader.Children.Clear();
            foreach (var Version in versions)
                PanLiteLoader.Children.Add(ModDownloadLib.LiteLoaderDownloadListItem(Version,
                    (a, b) => this.LiteLoader_Selected((dynamic)a, b), false));
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?LiteLoader 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    private void LiteLoader_Selected(MyListItem sender, EventArgs e)
    {
        selectedLiteLoader = (ModDownload.DlLiteLoaderListEntry)sender.Tag;
        CardLiteLoader.IsSwapped = true;
        ReloadSelected();
    }

    private void LiteLoader_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedLiteLoader = null;
        CardLiteLoader.IsSwapped = true;
        e.Handled = true;
        ReloadSelected();
    }

    #endregion

    #region Forge 閸掓銆?

    /// <summary>
    ///     閼惧嘲褰?Forge 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadForgeGetError()
    {
        if (McVersionComparer.CompareVersionGe("1.5.1", _vanillaName) && McVersionComparer.CompareVersionGe(_vanillaName, "1.1"))
            return Lang.Text("Download.Install.State.NoVersion");
                
        if (selectedLoaderName is not null && !ReferenceEquals(selectedLoaderName, "Forge"))
            return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);

        // 濡偓閺?Loader
        if (GetLoaderError(LoadForge) is not null)
            return GetLoaderError(LoadForge);
        var loader = (ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>)LoadForge.State;
        if ((_vanillaName ?? "") != (loader.input ?? ""))
            return Lang.Text("Download.Install.State.Getting");
        // 濡偓閺屻儳澧楅張?
        foreach (var Version in loader.output)
        {
            if (Version.Category == "universal" || Version.Category == "client")
                continue; // 鐠哄疇绻冮弮鐘崇《閼奉亜濮╃€瑰顥婇惃鍕閺?
            if (selectedLoaderName is not null && selectedLoaderName != "Forge")
                return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
            if (selectedOptiFine is not null && McVersionComparer.CompareVersionGe(_vanillaName, "1.13") &&
                McVersionComparer.CompareVersionGe("1.14.3", _vanillaName))
                return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine"); // 1.13 ~ 1.14.3 OptiFine 濡偓閺?
            if (selectedOptiFine is not null && !(bool)IsOptiFineSuitForForge(selectedOptiFine, Version))
                continue;
            return null;
        }

        return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardForge_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadForgeGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?Forge 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void Forge_Loaded()
    {
        try
        {
            if (!LoadForge.State.IsLoader)
                return;
            var loader = (ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>)LoadForge.State;
            if ((_vanillaName ?? "") != (loader.input ?? ""))
                return;
            if (loader.State != ModBase.LoadState.Finished)
                return;
            // 閼惧嘲褰囩憰浣规▔缁€铏规畱閻楀牊婀?
            var versions = loader.output.ToList(); // 婢跺秴鍩楅弫鎵矋閿涘奔浜掗崗?Output 閸︺劌鐤勬笟瀣閸氬骸褰夌粚?
            if (!loader.output.Any())
                return;
            PanForge.Children.Clear();
            versions = versions.Where(v =>
            {
                if (v.Category == "universal" || v.Category == "client")
                    return false; // 鐠哄疇绻冮弮鐘崇《閼奉亜濮╃€瑰顥婇惃鍕閺?
                if (selectedOptiFine is not null &&
                                          !(bool)IsOptiFineSuitForForge(selectedOptiFine, v))
                    return false;
                return true;
            }).OrderByDescending(v => v).ToList();
            ModDownloadLib.ForgeDownloadListItemPreload(PanForge, versions,
                (a, b) => this.Forge_Selected((dynamic)a, b), false);
            foreach (var Version in versions)
                PanForge.Children.Add(
                    ModDownloadLib.ForgeDownloadListItem(Version, (a, b) => this.Forge_Selected((dynamic)a, b), false));
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?Forge 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    private void Forge_Selected(MyListItem sender, EventArgs e)
    {
        selectedForge = (ModDownload.DlForgeVersionEntry)sender.Tag;
        selectedLoaderName = "Forge";
        CardForge.IsSwapped = true;
        if (selectedOptiFine is not null &&
                                  !(bool)IsOptiFineSuitForForge(selectedOptiFine, selectedForge))
            selectedOptiFine = null;
        OptiFine_Loaded();
        ReloadSelected();
    }

    private void Forge_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedForge = null;
        selectedLoaderName = null;
        CardForge.IsSwapped = true;
        e.Handled = true;
        OptiFine_Loaded();
        ReloadSelected();
    }

    #endregion

    #region NeoForge 閸掓銆?

    /// <summary>
    ///     閼惧嘲褰?NeoForge 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadNeoForgeGetError()
    {
        if (selectedOptiFine is not null)
            return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
        if (selectedLoaderName is not null && !ReferenceEquals(selectedLoaderName, "NeoForge"))
            return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
        // 濡偓閺?Loader
        if (GetLoaderError(LoadNeoForge) is not null)
            return GetLoaderError(LoadNeoForge);
        // 濡偓閺屻儳澧楅張?
        return ModDownload.dlNeoForgeListLoader.output.Value.Any(v => (v.Inherit ?? "") == (_vanillaName ?? ""))
            ? null
            : Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardNeoForge_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadNeoForgeGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?NeoForge 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void NeoForge_Loaded()
    {
        try
        {
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            if (ModDownload.dlNeoForgeListLoader.State != ModBase.LoadState.Finished)
                return;
            var versions = ModDownload.dlNeoForgeListLoader.output.Value
                .Where(v => (v.Inherit ?? "") == (_vanillaName ?? "")).ToList();
            if (!versions.Any())
                return;
            // 閸欘垵顫嬮崠?
            PanNeoForge.Children.Clear();
            ModDownloadLib.NeoForgeDownloadListItemPreload(PanNeoForge, versions,
                (a, b) => this.NeoForge_Selected((dynamic)a, b),
                false);
            foreach (var Version in versions)
                PanNeoForge.Children.Add(
                    ModDownloadLib.NeoForgeDownloadListItem(Version, (a, b) => this.NeoForge_Selected((dynamic)a, b),
                        false));
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?NeoForge 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    private void NeoForge_Selected(MyListItem sender, EventArgs e)
    {
        selectedNeoForge = (ModDownload.DlNeoForgeListEntry)sender.Tag;
        selectedLoaderName = "NeoForge";
        CardNeoForge.IsSwapped = true;
        OptiFine_Loaded();
        ReloadSelected();
    }

    private void NeoForge_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedNeoForge = null;
        selectedLoaderName = null;
        CardNeoForge.IsSwapped = true;
        e.Handled = true;
        OptiFine_Loaded();
        ReloadSelected();
    }

    #endregion

    #region Cleanroom 閸掓銆?

    /// <summary>
    ///     閼惧嘲褰?Cleanroom 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadCleanroomGetError()
    {
        if (!_vanillaName.StartsWith("1."))
            return Lang.Text("Download.Install.State.NoAvailableVersion");
        if (selectedOptiFine is not null)
            return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
        if (selectedLoaderName is not null && selectedLoaderName != "Cleanroom")
            return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
        if (selectedLiteLoader is not null) 
            return Lang.Text("Download.Install.Compat.IncompatibleWithLiteLoader");
        // 濡偓閺?Loader
        if (GetLoaderError(LoadCleanroom) is not null)
            return GetLoaderError(LoadCleanroom);
        // 濡偓閺屻儳澧楅張?
        return ModDownload.dlCleanroomListLoader.output.Value.Any(v => (v.Inherit ?? "") == (_vanillaName ?? ""))
            ? null
            : Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardCleanroom_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadCleanroomGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?Cleanroom 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void Cleanroom_Loaded()
    {
        try
        {
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            if (ModDownload.dlCleanroomListLoader.State != ModBase.LoadState.Finished)
                return;
            var versions = ModDownload.dlCleanroomListLoader.output.Value
                .Where(v => (v.Inherit ?? "") == (_vanillaName ?? "")).ToList();
            if (!versions.Any())
                return;
            // 閸欘垵顫嬮崠?
            PanCleanroom.Children.Clear();
            ModDownloadLib.CleanroomDownloadListItemPreload(PanCleanroom, versions,
                (a, b) => this.Cleanroom_Selected((dynamic)a, b), false);
            foreach (var Version in versions)
                PanCleanroom.Children.Add(
                    ModDownloadLib.CleanroomDownloadListItem(Version, (a, b) => this.Cleanroom_Selected((dynamic)a, b),
                        false));
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?Cleanroom 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    private void Cleanroom_Selected(MyListItem sender, EventArgs e)
    {
        selectedCleanroom = (ModDownload.DlCleanroomListEntry)sender.Tag;
        selectedLoaderName = "Cleanroom";
        CardCleanroom.IsSwapped = true;
        OptiFine_Loaded();
        ReloadSelected();
    }

    private void Cleanroom_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedCleanroom = null;
        selectedCleanroomVersion = null;
        selectedLoaderName = null;
        selectedAPIName = null;
        CardCleanroom.IsSwapped = true;
        e.Handled = true;
        OptiFine_Loaded();
        ReloadSelected();
    }

    #endregion

    #region Fabric 閸掓銆?

    /// <summary>
    ///     閼惧嘲褰?Fabric 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadFabricGetError()
    {
        // 濡偓閺?OptiFine 1.20.5+閿涙碍鐥呴張?OptiFabric 閺佸懎鍙忛柈銊ょ瑝閸忕厧顔?
        if (selectedOptiFine is not null && McVersionComparer.CompareVersionGe(_vanillaName, "1.20.5"))
            return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
        // 濡偓閺?Loader
        if (GetLoaderError(LoadFabric) is not null)
            return GetLoaderError(LoadFabric);
        // 濡偓閺屻儳澧楅張?
        foreach (JsonObject version in ModDownload.dlFabricListLoader.output.Value["game"].AsArray())
            if ((version["version"].ToString() ?? "") ==
                (_vanillaName.Replace("閳?, "infinite").Replace("Combat Test 7c", "1.16_combat-3") ?? ""))
            {
                if (selectedLoaderName is not null && !ReferenceEquals(selectedLoaderName, "Fabric"))
                    return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
                return null;
            }

        return Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardFabric_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadFabricGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?Fabric 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void Fabric_Loaded()
    {
        try
        {
            if (ModDownload.dlFabricListLoader.State != ModBase.LoadState.Finished)
                return;
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            var versions = (JsonArray)ModDownload.dlFabricListLoader.output.Value["loader"];
            if (!versions.Any())
                return;
            // 閸欘垵顫嬮崠?
            PanFabric.Children.Clear();
            PanFabric.Tag = versions;
            CardFabric.SwapControl = PanFabric;
            CardFabric.InstallMethod = stack =>
            {
                foreach (var item in (IEnumerable)stack.Tag)
                    stack.Children.Add(
                        ModDownloadLib.FabricDownloadListItem((JsonObject)item,
                            (a, b) => this.Fabric_Selected((dynamic)a, b)));
            };
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?Fabric 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    public void Fabric_Selected(MyListItem sender, EventArgs e)
    {
        selectedFabric = ((dynamic)sender.Tag)["version"].ToString();
        selectedLoaderName = "Fabric";
        FabricApi_Loaded();
        OptiFabric_Loaded();
        CardFabric.IsSwapped = true;
        ReloadSelected();
    }

    private void Fabric_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedFabric = null;
        selectedFabricApi = null;
        autoSelectedFabricApi = false;
        selectedOptiFabric = null;
        autoSelectedOptiFabric = false;
        selectedLoaderName = null;
        selectedAPIName = null;
        CardFabric.IsSwapped = true;
        e.Handled = true;
        ReloadSelected();
    }

    #endregion

    #region Fabric API 閸掓銆?

    /// <summary>
    ///     閸掋倖鏌囬弻?Fabric API 閺勵垰鎯侀柅鍌炲帳瑜版挸澧犻柅澶嬪閻ㄥ嫬甯悧鍫㈠閺堫兙鈧?
    /// </summary>
    public bool IsFabricApiCompatible(ModComp.CompFile fabricApi)
    {
        var fabricApiName = fabricApi.DisplayName;
        try
        {
            if (fabricApiName is null || _vanillaName is null)
                return false;
            fabricApiName = fabricApiName.ToLower();
            _vanillaName = _vanillaName.Replace("閳?, "infinite").Replace("Combat Test 7c", "1.16_combat-3").ToLower();
            if (fabricApiName.StartsWith("[" + _vanillaName + "]"))
                return true;
            if (!fabricApiName.Contains("/") || !fabricApiName.Contains("]"))
                return false;
            // 閻╁瓨甯撮惃鍕灲閺傤叏绱欐笟瀣洤 1.18.1/22w03a閿?
            foreach (var part in fabricApiName.BeforeFirst("]").TrimStart('[').Split("/"))
                if ((part ?? "") == (_vanillaName ?? ""))
                    return true;
            // 鐏忓棛澧楅張顒€鎮曢崚鍡楀鐠囶厾绀岄敍鍫滅伐婵?1.16.4/5閿?
            var lefts = fabricApiName.BeforeFirst("]").RegexSearch("[a-z/]+|[0-9/]+");
            var rights = _vanillaName.BeforeFirst("]").RegexSearch("[a-z/]+|[0-9/]+");
            // 鐎佃鐦″▓浣冪箻鐞涘苯鍨介弬?
            var i = 0;
            while (true)
            {
                // 娑撱倛绔熼崸鍥╁繁婢舵唻绱濋幇鐔活潕閺勵垯绔存稉顏冪鐟?
                if (lefts.Count - 1 < i && rights.Count - 1 < i)
                    return true;
                // 绾喖鐣炬稉銈堢珶閺勵垰鎯佹稉鈧懛?
                var leftValue = lefts.Count - 1 < i ? "-1" : lefts[i];
                var rightValue = rights.Count - 1 < i ? "-1" : rights[i];
                if (!leftValue.Contains("/"))
                {
                    if ((leftValue ?? "") != (rightValue ?? ""))
                        return false;
                }
                // 瀹革箒绔熺€涙ê婀弬婊勬浆
                else if (!leftValue.Contains(rightValue))
                {
                    return false;
                }

                i += 1;
            }

            return true;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "閸掋倖鏌?Fabric API 閻楀牊婀伴柅鍌炲帳閹冨毉闁挎瑱绱? + fabricApiName + ", " + _vanillaName + "閿?);
            return false;
        }
    }

    /// <summary>
    ///     閼惧嘲褰?FabricApi 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadFabricApiGetError()
    {
        // 濡偓閺?Loader
        if (GetLoaderError(LoadFabricApi) is not null)
            return GetLoaderError(LoadFabricApi);
        if (ModDownload.dlFabricApiLoader.output is null)
            return selectedFabric is null ? Lang.Text("Download.Install.Compat.RequiresFabric") : Lang.Text("Download.Install.State.Getting");
        // 濡偓閺屻儳澧楅張?
        if (ModDownload.dlFabricApiLoader.output.Any(f => IsFabricApiCompatible(f)))
            return selectedFabric is null ? Lang.Text("Download.Install.Compat.RequiresFabric") : null;

        return Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardFabricApi_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadFabricApiGetError() is not null)
            e.handled = true;
    }

    private bool autoSelectedFabricApi;

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?FabricApi 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void FabricApi_Loaded()
    {
        try
        {
            if (ModDownload.dlFabricApiLoader.State != ModBase.LoadState.Finished)
                return;
            if (_vanillaName is null || selectedFabric is null)
                return;
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            var versions = new List<ModComp.CompFile>();
            foreach (var version in ModDownload.dlFabricApiLoader.output)
                if (IsFabricApiCompatible(version))
                {
                    if (!version.DisplayName.StartsWith("["))
                    {
                        ModBase.Log("[Download] 瀹歌尙澹掗崚銈勬叏閺€?Fabric API 閺勫墽銇氶崥宥忕窗" + version.DisplayName, ModBase.LogLevel.Debug);
                        version.DisplayName = "[" + _vanillaName + "] " + version.DisplayName;
                    }

                    versions.Add(version);
                }

            if (!versions.Any())
                return;
            versions = versions.OrderByDescending(v => v.ReleaseDate).ToList();
            // 閸欘垵顫嬮崠?
            PanFabricApi.Children.Clear();
            foreach (var version in versions)
            {
                if (!IsFabricApiCompatible(version))
                    continue;
                PanFabricApi.Children.Add(
                    ModDownloadLib.FabricApiDownloadListItem(version,
                        (a, b) => this.FabricApi_Selected((dynamic)a, b)));
            }

            // 閼奉亜濮╅柅澶嬪 Fabric API
            if (!autoSelectedFabricApi)
            {
                autoSelectedFabricApi = true;
                ModBase.Log($"[Download] 瀹歌尪鍤滈崝銊┾偓澶嬪 Fabric API閿涙((MyListItem)PanFabricApi.Children[0]).Title}");
                FabricApi_Selected((MyListItem)PanFabricApi.Children[0], null);
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?Fabric API 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    private void FabricApi_Selected(MyListItem sender, EventArgs e)
    {
        selectedFabricApi = (ModComp.CompFile)sender.Tag;
        selectedAPIName = "Fabric API";
        CardFabricApi.IsSwapped = true;
        ReloadSelected();
    }

    private void FabricApi_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedFabricApi = null;
        selectedAPIName = null;
        CardFabricApi.IsSwapped = true;
        e.Handled = true;
        ReloadSelected();
    }

    #endregion

    #region LegacyFabric 閸掓銆?

    /// <summary>
    ///     閼惧嘲褰?LegacyFabric 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadLegacyFabricGetError()
    {
        if (LoadLegacyFabric is null || LoadLegacyFabric.State.LoadingState == MyLoading.MyLoadingState.Run)
            return Lang.Text("Download.Install.State.Loading");
        if (LoadLegacyFabric.State.LoadingState == MyLoading.MyLoadingState.Error)
            return Lang.Text("Download.Install.State.GetVersionListFailed", ((ModLoader.LoaderBase)LoadLegacyFabric.State).Error.Message);
        foreach (JsonObject Version in ModDownload.dlLegacyFabricListLoader.output.Value["game"].AsArray())
            if ((Version["version"].ToString() ?? "") == (_vanillaName ?? ""))
            {
                if (selectedLiteLoader is not null)
                    return Lang.Text("Download.Install.Compat.IncompatibleWithLiteLoader");
                if (selectedLoaderName is not null && !ReferenceEquals(selectedLoaderName, "LegacyFabric"))
                    return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
                return null;
            }

        return Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardLegacyFabric_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadLegacyFabricGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?LegacyFabric 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void LegacyFabric_Loaded()
    {
        try
        {
            if (ModDownload.dlLegacyFabricListLoader.State != ModBase.LoadState.Finished)
                return;
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            var versions = (JsonArray)ModDownload.dlLegacyFabricListLoader.output.Value["loader"];
            if (!versions.Any())
                return;
            // 閸欘垵顫嬮崠?
            PanLegacyFabric.Children.Clear();
            PanLegacyFabric.Tag = versions;
            CardLegacyFabric.SwapControl = PanLegacyFabric;
            CardLegacyFabric.InstallMethod = stack =>
            {
                foreach (var item in (IEnumerable)stack.Tag)
                    stack.Children.Add(ModDownloadLib.LegacyFabricDownloadListItem((JsonObject)item,
                        (a, b) => this.LegacyFabric_Selected((dynamic)a, b)));
            };
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?LegacyFabric 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    public void LegacyFabric_Selected(MyListItem sender, EventArgs e)
    {
        selectedLegacyFabric = ((dynamic)sender.Tag)["version"].ToString();
        selectedLoaderName = "LegacyFabric";
        LegacyFabricApi_Loaded();
        CardLegacyFabric.IsSwapped = true;
        ReloadSelected();
    }

    private void LegacyFabric_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedLegacyFabric = null;
        selectedLegacyFabricApi = null;
        autoSelectedLegacyFabricApi = false;
        selectedLoaderName = null;
        selectedAPIName = null;
        CardLegacyFabric.IsSwapped = true;
        e.Handled = true;
        ReloadSelected();
    }

    #endregion

    #region Legacy Fabric API 閸掓銆?

    /// <summary>
    ///     娴犲孩妯夌粈鍝勬倳閸掋倖鏌囩拠?API 閺勵垰鎯佹稉搴㈢厙閻楀牊婀伴柅鍌炲帳閵?
    /// </summary>
    public static bool IsSuitableLegacyFabricApi(List<string> supportVersions, string minecraftVersion)
    {
        try
        {
            if (supportVersions.Contains(minecraftVersion)) return true;

            return false;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "閸掋倖鏌?Legacy Fabric API 閻楀牊婀伴柅鍌炲帳閹冨毉闁挎瑱绱? + supportVersions + ", " + minecraftVersion + "閿?);
            return false;
        }
    }

    /// <summary>
    ///     閼惧嘲褰?LegacyFabricApi 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadLegacyFabricApiGetError()
    {
        if (LoadLegacyFabricApi is null || LoadLegacyFabricApi.State.LoadingState == MyLoading.MyLoadingState.Run)
            return Lang.Text("Download.Install.State.Loading");
        if (LoadLegacyFabricApi.State.LoadingState == MyLoading.MyLoadingState.Error)
            return Lang.Text("Download.Install.State.GetVersionListFailed", ((ModLoader.LoaderBase)LoadLegacyFabricApi.State).Error.Message);
        if (selectedAPIName is not null && !ReferenceEquals(selectedAPIName, "Legacy Fabric API"))
            return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedAPIName);
        if (ModDownload.dlLegacyFabricApiLoader.output is null)
        {
            if (selectedLegacyFabric is null)
                return Lang.Text("Download.Install.Compat.RequiresLegacyFabric");
            return Lang.Text("Download.Install.State.Loading");
        }

        foreach (var Version in ModDownload.dlLegacyFabricApiLoader.output)
        {
            if (!IsSuitableLegacyFabricApi(Version.GameVersions, _vanillaName))
                continue;
            if (selectedLegacyFabric is null)
                return Lang.Text("Download.Install.Compat.RequiresLegacyFabric");
            return null;
        }

        return Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardLegacyFabricApi_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadLegacyFabricApiGetError() is not null)
            e.handled = true;
    }

    private bool autoSelectedLegacyFabricApi;

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?LegacyFabricApi 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void LegacyFabricApi_Loaded()
    {
        try
        {
            if (ModDownload.dlLegacyFabricApiLoader.State != ModBase.LoadState.Finished)
                return;
            if (_vanillaName is null || selectedLegacyFabric is null)
                return;
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            var versions = new List<ModComp.CompFile>();
            foreach (var Version in ModDownload.dlLegacyFabricApiLoader.output)
                if (IsSuitableLegacyFabricApi(Version.GameVersions, _vanillaName))
                    versions.Add(Version);

            if (!versions.Any())
                return;
            versions = versions.OrderByDescending(v => v.ReleaseDate).ToList();
            // 閸欘垵顫嬮崠?
            PanLegacyFabricApi.Children.Clear();
            foreach (var Version in versions)
            {
                if (!IsSuitableLegacyFabricApi(Version.GameVersions, _vanillaName))
                    continue;
                PanLegacyFabricApi.Children.Add(
                    ModDownloadLib.LegacyFabricApiDownloadListItem(Version,
                        (a, b) => this.LegacyFabricApi_Selected((dynamic)a, b)));
            }

            // 閼奉亜濮╅柅澶嬪 Legacy Fabric API
            if (!autoSelectedLegacyFabricApi)
            {
                autoSelectedLegacyFabricApi = true;
                ModBase.Log($"[Download] 瀹歌尪鍤滈崝銊┾偓澶嬪 Legacy Fabric API閿涙((MyListItem)PanLegacyFabricApi.Children[0]).Title}");
                LegacyFabricApi_Selected((MyListItem)PanLegacyFabricApi.Children[0], null);
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?Legacy Fabric API 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    private void LegacyFabricApi_Selected(MyListItem sender, EventArgs e)
    {
        selectedLegacyFabricApi = (ModComp.CompFile)sender.Tag;
        selectedAPIName = "Legacy Fabric API";
        CardLegacyFabricApi.IsSwapped = true;
        ReloadSelected();
    }

    private void LegacyFabricApi_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedLegacyFabricApi = null;
        selectedAPIName = null;
        CardLegacyFabricApi.IsSwapped = true;
        e.Handled = true;
        ReloadSelected();
    }

    #endregion


    #region OptiFabric 閸掓銆?

    /// <summary>
    ///     閸掋倖鏌囬弻?OptiFabric 閺勵垰鎯侀柅鍌炲帳瑜版挸澧犻柅澶嬪閻ㄥ嫬甯悧鍫㈠閺堫兙鈧?
    /// </summary>
    private bool IsOptiFabricCompatible(ModComp.CompFile modFile)
    {
        try
        {
            if (_vanillaName is null)
                return false;
            return modFile.GameVersions.Contains(_vanillaName);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "閸掋倖鏌?OptiFabric 閻楀牊婀伴柅鍌炲帳閹冨毉闁挎瑱绱? + _vanillaName + "閿?);
            return false;
        }
    }

    private bool autoSelectedOptiFabric;

    /// <summary>
    ///     閼惧嘲褰?OptiFabric 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadOptiFabricGetError()
    {
        if (VanillaDrop >= 140 && VanillaDrop <= 150)
            return Lang.Text("Download.Install.Compat.OptiFabricOriginsRequired");
        // 濡偓閺?Loader
        if (GetLoaderError(LoadOptiFabric) is not null)
            return GetLoaderError(LoadOptiFabric);
        // 濡偓閺屻儳澧楅張?
        if (ModDownload.dlOptiFabricLoader.output is null)
        {
            if (selectedFabric is null && selectedOptiFine is null)
                return Lang.Text("Download.Install.Compat.RequiresOptiFineAndFabric");
            if (selectedFabric is null)
                return Lang.Text("Download.Install.Compat.RequiresFabric");
            if (selectedOptiFine is null)
                return Lang.Text("Download.Install.Compat.RequiresOptiFine");
            return Lang.Text("Download.Install.State.Getting");
        }

        foreach (var version in ModDownload.dlOptiFabricLoader.output)
        {
            if (!IsOptiFabricCompatible(version))
                continue; // 2135#
            if (selectedFabric is null && selectedOptiFine is null)
                return Lang.Text("Download.Install.Compat.RequiresOptiFineAndFabric");
            if (selectedFabric is null)
                return Lang.Text("Download.Install.Compat.RequiresFabric");
            if (selectedOptiFine is null)
                return Lang.Text("Download.Install.Compat.RequiresOptiFine");
            return null; // 闁俺绻冨Λ鈧弻?
        }

        return Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardOptiFabric_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadOptiFabricGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?OptiFabric 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void OptiFabric_Loaded()
    {
        try
        {
            if (ModDownload.dlOptiFabricLoader.State != ModBase.LoadState.Finished)
                return;
            if (_vanillaName is null || selectedFabric is null || selectedOptiFine is null)
                return;
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            var versions = new List<ModComp.CompFile>();
            foreach (var Version in ModDownload.dlOptiFabricLoader.output)
                if (IsOptiFabricCompatible(Version))
                    versions.Add(Version);
            if (!versions.Any())
                return;
            // 閹烘帒绨?
            versions = versions.OrderByDescending(v => v.ReleaseDate).ToList();
            // 閸欘垵顫嬮崠?
            PanOptiFabric.Children.Clear();
            foreach (var Version in versions)
            {
                if (!IsOptiFabricCompatible(Version))
                    continue;
                PanOptiFabric.Children.Add(
                    ModDownloadLib.OptiFabricDownloadListItem(Version,
                        (a, b) => this.OptiFabric_Selected((dynamic)a, b)));
            }

            // 閼奉亜濮╅柅澶嬪 OptiFabric
            if (autoSelectedOptiFabric || (VanillaDrop >= 140 && VanillaDrop <= 150))
                return; // 1.14~15 娑撳秷鍤滈崝銊┾偓澶嬪
            autoSelectedOptiFabric = true;
            ModBase.Log($"[Download] 瀹歌尪鍤滈崝銊┾偓澶嬪 OptiFabric閿涙((MyListItem)PanOptiFabric.Children[0]).Title}");
            OptiFabric_Selected((MyListItem)PanOptiFabric.Children[0], null);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?OptiFabric 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    private void OptiFabric_Selected(MyListItem sender, EventArgs e)
    {
        selectedOptiFabric = (ModComp.CompFile)sender.Tag;
        CardOptiFabric.IsSwapped = true;
        ReloadSelected();
    }

    private void OptiFabric_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedOptiFabric = null;
        CardOptiFabric.IsSwapped = true;
        e.Handled = true;
        ReloadSelected();
    }

    #endregion

    #region LabyMod 閸掓銆?

    /// <summary>
    ///     閼惧嘲褰?LabyMod 閻ㄥ嫬濮炴潪钘夌磽鐢晲淇婇幁顖樷偓鍌濆濮濓絽鐖堕崚娆掔箲閸?Nothing閵?
    /// </summary>
    private string LoadLabyModGetError()
    {
        if (LoadLabyMod is null || LoadLabyMod.State.LoadingState == MyLoading.MyLoadingState.Run)
            return Lang.Text("Download.Install.State.Loading");
        if (LoadLabyMod.State.LoadingState == MyLoading.MyLoadingState.Error)
            return Lang.Text("Download.Install.State.GetVersionListFailed", ((ModLoader.LoaderBase)LoadLabyMod.State).Error.Message);
        // 濡偓閺?Loader
        if (GetLoaderError(LoadLabyMod) is not null)
            return GetLoaderError(LoadLabyMod);
        if (selectedOptiFine is not null)
            return Lang.Text("Download.Install.Compat.IncompatibleWithOptiFine");
        if (selectedLoaderName is not null && selectedLoaderName != "LabyMod")
            return Lang.Text("Download.Install.Compat.IncompatibleWithLoader", selectedLoaderName);
        if (selectedLiteLoader is not null)
            return Lang.Text("Download.Install.Compat.IncompatibleWithLiteLoader");
        foreach (JsonObject Version in ModDownload.dlLabyModListLoader.output.Value["production"]["minecraftVersions"].AsArray())
            if ((Version["version"].ToString() ?? "") == (_vanillaName ?? ""))
                return null;
        foreach (JsonObject Version in ModDownload.dlLabyModListLoader.output.Value["snapshot"]["minecraftVersions"].AsArray())
            if ((Version["version"].ToString() ?? "") == (_vanillaName ?? ""))
                return null;
        return Lang.Text("Download.Install.State.NoVersion");
    }

    // 闂勬劕鍩楃仦鏇炵磻
    private void CardLabyMod_PreviewSwap(object sender, ModBase.RouteEventArgs e)
    {
        if (LoadLabyModGetError() is not null)
            e.handled = true;
    }

    /// <summary>
    ///     鐏忔繆鐦柌宥嗘煀閸欘垵顫嬮崠?LabyMod 閻楀牊婀伴崚妤勩€冮妴?
    /// </summary>
    private void LabyMod_Loaded()
    {
        try
        {
            if (LoadLabyMod.State.LoadingState == MyLoading.MyLoadingState.Run)
                return;
            // 閼惧嘲褰囬悧鍫熸拱閸掓銆?
            var versions = ModDownload.dlLabyModListLoader.output.Value;
            if (versions is null || versions["production"] is null || versions["snapshot"] is null)
                return;
            // 閸欘垵顫嬮崠?
            var processedVersions = new JsonArray();
            foreach (JsonObject Production in versions["production"]["minecraftVersions"].AsArray())
                if ((Production["version"].ToString() ?? "") == (_vanillaName ?? ""))
                {
                    var productionVersion = new JsonObject();
                    productionVersion.Add("version", versions["production"]["labyModVersion"].ToString());
                    productionVersion.Add("channel", "production");
                    productionVersion.Add("commitReference", versions["production"]["commitReference"].ToString());
                    processedVersions.Add(productionVersion);
                }

            foreach (JsonObject Snapshot in versions["snapshot"]["minecraftVersions"].AsArray())
                if ((Snapshot["version"].ToString() ?? "") == (_vanillaName ?? ""))
                {
                    var snapshotVersion = new JsonObject();
                    snapshotVersion.Add("version", versions["snapshot"]["labyModVersion"].ToString());
                    snapshotVersion.Add("channel", "snapshot");
                    snapshotVersion.Add("commitReference", versions["snapshot"]["commitReference"].ToString());
                    processedVersions.Add(snapshotVersion);
                }

            // MyMsgBox(If(ProcessedVersions.ToString, "Nothing"))
            PanLabyMod.Children.Clear();
            PanLabyMod.Tag = processedVersions;
            CardLabyMod.SwapControl = PanLabyMod;
            CardLabyMod.InstallMethod = stack =>
            {
                foreach (JsonObject item in (IEnumerable)stack.Tag)
                    stack.Children.Add(
                        ModDownloadLib.LabyModDownloadListItem(item, (a, b) => this.LabyMod_Selected((dynamic)a, b)));
            };
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸欘垵顫嬮崠?LabyMod 鐎瑰顥婇悧鍫熸拱閸掓銆冮崙娲晩",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Install.Error.OperationFailed"));
        }
    }

    // 闁瀚ㄦ稉搴㈢闂?
    public void LabyMod_Selected(MyListItem sender, EventArgs e)
    {
        selectedLabyModChannel = ((dynamic)sender.Tag)("channel").ToString();
        selectedLabyModCommitRef = ((dynamic)sender.Tag)("commitReference").ToString();
        selectedLabyModVersion =
            ((dynamic)sender.Tag)("version").ToString() + (selectedLabyModChannel == "snapshot" ? " " + Lang.Text("Download.Version.Type.Snapshot") : " " + Lang.Text("Download.Version.Type.Stable"));
        selectedLoaderName = "LabyMod";
        CardLabyMod.IsSwapped = true;
        ReloadSelected();
    }

    private void LabyMod_Clear(object sender, PointerReleasedEventArgs e)
    {
        selectedLabyModCommitRef = null;
        selectedLabyModVersion = null;
        selectedLabyModChannel = null;
        
        if (selectedLoaderName == "LabyMod")
        {
            selectedLoaderName = null;
        }    

        selectedAPIName = null;
        CardLabyMod.IsSwapped = true;
        e.Handled = true;
        ReloadSelected();
    }

    #endregion
}
