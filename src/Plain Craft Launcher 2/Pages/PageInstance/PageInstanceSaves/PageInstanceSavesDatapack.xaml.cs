using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App;
using PCL.Core.UI;
using PCL.Core.UI.Theme;
using PCL.Network;
using PCL.Network.Loaders;
using FileSystem = Microsoft.VisualBasic.FileSystem;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceSavesDatapack : IRefreshable
{
    #region 閺佺増宓侀崠鍛繆閹垳绱︾€?

    private readonly Dictionary<string, (DateTime CreationTime, long Length)> datapackFileInfoCache = new();

    // 閼惧嘲褰囬弫鐗堝祦閸栧懍淇婇幁顖ょ礄鐢妇绱︾€涙﹫绱?
    private (DateTime CreationTime, long Length) GetDatapackFileInfo(string path)
    {
        (DateTime CreationTime, long Length) cacheItem;
        if (datapackFileInfoCache.TryGetValue(path, out cacheItem)) return cacheItem;

        try
        {
            var fileInfo = new FileInfo(path);
            var newItem = (fileInfo.CreationTime, fileInfo.Length);
            if (!datapackFileInfoCache.ContainsKey(path)) datapackFileInfoCache.Add(path, newItem);
            return newItem;
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "閼惧嘲褰囬弫鐗堝祦閸栧懍淇婇幁顖氥亼鐠? " + path);
            return (DateTime.MinValue, 0L);
        }
    }

    // 妞ょ敻娼伴崗鎶芥４閺冭埖绔婚悶鍡欑处鐎?
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        datapackFileInfoCache.Clear();
    }

    #endregion

    #region 閸掓繂顫愰崠?

    private readonly MyLocalCompItem.SwipeSelect currentSwipSelect;

    public PageInstanceSavesDatapack()
    {
        currentSwipSelect = new MyLocalCompItem.SwipeSelect { TargetFrm = this };

        InitializeComponent();
        Unloaded += Page_Unloaded;
        Loaded += (_, _) => PageOther_Loaded();
        LoaderInit();
        PageExit += UnselectedAllWithAnimation;
        // Handles
        Load.Click += Load_Click;
        BtnManageOpen.Click += BtnManageOpen_Click;
        BtnHintOpen.Click += BtnManageOpen_Click;
        BtnManageSelectAll.Click += BtnManageSelectAll_Click;
        BtnManageInstall.Click += BtnManageInstall_Click;
        BtnHintInstall.Click += BtnManageInstall_Click;
        BtnManageDownload.Click += BtnManageDownload_Click;
        BtnHintDownload.Click += BtnManageDownload_Click;
        BtnManageInfoExport.Click += BtnManageInfoExport_Click;
        Load.StateChanged += (_, _, _) => UnselectedAllWithAnimation();
        SearchBox.PreviewKeyDown += SearchBox_PreviewKeyDown;
        BtnFilterAll.Check += ChangeFilter;
        BtnFilterCanUpdate.Check += ChangeFilter;
        BtnFilterDisabled.Check += ChangeFilter;
        BtnFilterEnabled.Check += ChangeFilter;
        BtnFilterError.Check += ChangeFilter;
        BtnSort.Click += BtnSortClick;
        BtnSelectEnable.Click += BtnSelectEnable_Click;
        BtnSelectDisable.Click += BtnSelectDisable_Click;
        BtnSelectUpdate.Click += BtnSelectUpdate_Click;
        BtnSelectDelete.Click += BtnSelectDelete_Click;
        BtnSelectCancel.Click += BtnSelectCancel_Click;
        BtnSelectFavorites.Click += BtnSelectFavorites_Click;
        BtnSelectShare.Click += BtnSelectShare_Click;
        SearchBox.TextChanged += SearchRun;
    }

    private ModLocalComp.CompLocalLoaderData GetRequireLoaderData()
    {
        var res = new ModLocalComp.CompLocalLoaderData();
        res.gameVersion = PageInstanceLeft.McInstance;
        res.frm = null;
        res.loaders = new[] { ModComp.CompLoaderType.Minecraft }.ToList();
        res.compPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
        res.compType = ModComp.CompType.DataPack;
        return res;
    }

    private bool isLoad;

    public void PageOther_Loaded()
    {
        if (ModMain.frmMain.pageLast.page != FormMain.PageType.CompDetail)
            PanBack.ScrollToHome();
        ModAnimation.AniControlEnabled += 1;
        selectedDatapacks.Clear();
        ReloadDatapackFileList();
        ChangeAllSelected(false);
        ModAnimation.AniControlEnabled -= 1;

        // 闂堢偤鍣告径宥呭鏉炰粙鍎撮崚?
        if (isLoad)
            return;
        isLoad = true;

        ModMain.frmMain.KeyDown += FrmMain_KeyDown;
        // 鐠嬪啯鏆ｉ幐澶愭尦鏉堢绐涢敍鍫ｇ箹閻溾晜鍓伴崕鎸庣梾濞夋洑绮?XAML 閺€鐧哥礆
        foreach (MyRadioButton Btn in PanFilter.Children)
            Btn.LabText.Margin = new Thickness(-2, 0d, 8d, 0d);
    }

    /// <summary>
    ///     閸掗攱鏌婇弫鐗堝祦閸栧懎鍨悰銊ｂ偓?
    /// </summary>
    public void ReloadDatapackFileList(bool forceReload = false)
    {
        if (LoaderRun(forceReload
                ? ModLoader.LoaderFolderRunType.ForceRun
                : ModLoader.LoaderFolderRunType.RunOnUpdated))
        {
            ModBase.Log("[System] 瀹告彃鍩涢弬鐗堟殶閹诡喖瀵橀崚妤勩€?);
            datapackFileInfoCache.Clear();

            ModBase.RunInUi(() =>
            {
                Filter = FilterType.All;
                PanBack.ScrollToHome();
                SearchBox.Text = "";
            });
        }
    }

    // 瀵搫鍩楅崚閿嬫煀
    private void RefreshSelf()
    {
        Refresh();
    }

    void IRefreshable.Refresh()
    {
        RefreshSelf();
    }

    public void Refresh()
    {
        ModMain.frmInstanceSavesDatapack.ReloadDatapackFileList(true);
        ModBase.Log("[Datapack] 閸掗攱鏌婇弫鐗堝祦閸栧懎鍨悰?);
    }

    private void LoaderInit()
    {
        PageLoaderInit(Load, PanLoad, PanAllBack, null, ModLocalComp.compResourceListLoader,
            _ => LoadUIFromLoaderOutput(), () => ModComp.CompType.DataPack, false);
    }

    private void Load_Click(object sender, PointerReleasedEventArgs e)
    {
        if (ModLocalComp.compResourceListLoader.State == ModBase.LoadState.Failed)
            LoaderRun(ModLoader.LoaderFolderRunType.ForceRun);
    }

    public bool LoaderRun(ModLoader.LoaderFolderRunType type)
    {
        var loadPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
        return ModLoader.LoaderFolderRun(ModLocalComp.compResourceListLoader, loadPath, type,
            loaderInput: GetRequireLoaderData());
    }

    #endregion

    #region UI 閸?

    /// <summary>
    ///     瀹告彃濮炴潪鐣屾畱閺佺増宓侀崠?UI 缂傛挸鐡ㄩ妴渚筫y 娑撶儤鏆熼幑顔煎瘶閻?RawPath閵?
    /// </summary>
    public Dictionary<string, MyLocalCompItem> datapackItems = new();

    /// <summary>
    ///     鐏忓棗濮炴潪钘夋珤缂佹挻鐏夐惃鍕殶閹诡喖瀵橀崚妤勩€冮崝鐘烘祰娑?UI閵?
    /// </summary>
    private void LoadUIFromLoaderOutput()
    {
        try
        {
            // 閸掋倖鏌囨惔鏃囶嚉閺勫墽銇氶崫顏冪娑擃亪銆夐棃?
            if (ModLocalComp.compResourceListLoader.output.Any())
            {
                PanBack.IsVisible = true;
                PanEmpty.IsVisible = false;
            }
            else
            {
                // 閺嶈宓佺紒鍕缁鐎风拋鍓х枂 PanEmpty 閻ㄥ嫭鏋冮張顒€鍞寸€?
                TxtEmptyTitle.Text = Lang.Text("Instance.Resource.Datapack.Empty.Title");
                TxtEmptyDescription.Text = Lang.Text("Instance.Resource.Datapack.Empty.Description");

                PanEmpty.IsVisible = true;
                PanBack.IsVisible = false;
                return;
            }

            // 娣囶喗鏁肩紓鎾崇摠
            datapackItems.Clear();
            var itemsToShow = ModLocalComp.compResourceListLoader.output.ToList();

            foreach (var DatapackEntity in itemsToShow)
                datapackItems[DatapackEntity.RawPath] = BuildLocalCompItem(DatapackEntity);

            // 閺勫墽銇氱紒鎾寸亯
            ModBase.RunInUi(() =>
            {
                Filter = FilterType.All;
                SearchBox.Text = ""; // 鏉╂瑤绱扮憴锕€褰傜紒鎾寸亯閸掗攱鏌婇敍灞惧娴犮儵娓剁憰浣告躬 DatapackItems 閺囧瓨鏌婃稊瀣倵
                RefreshUI();
                SetSortMethod(SortMethod.CompName);
            });
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸旂姾娴囬弫鐗堝祦閸栧懎鍨悰?UI 婢惰精瑙?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    private MyLocalCompItem BuildLocalCompItem(ModLocalComp.LocalCompFile entry)
    {
        try
        {
            ModAnimation.AniControlEnabled += 1;
            var newItem = new MyLocalCompItem
            {
                Entry = entry,
                buttonHandler = BuildLocalCompItemBtnHandler,
                Checked = selectedDatapacks.Contains(entry.RawPath)
            };
            newItem.CurrentSwipe = currentSwipSelect;
            newItem.Tags = entry.Tags;
            entry.OnCompUpdate += _ => newItem.Refresh();
            newItem.Refresh();
            ModAnimation.AniControlEnabled -= 1;
            return newItem;
        }
        catch (Exception ex)
        {
            ModAnimation.AniControlEnabled -= 1;
            ModBase.Log(ex, $"閸掓稑缂?UI 妞ょ懓銇戠拹銉窗{entry.RawPath}");
            throw;
        }
    }

    private void BuildLocalCompItemBtnHandler(MyLocalCompItem sender, EventArgs e)
    {
        // 閻愮懓鍤禍瀣╂
        sender.Changed += (ss, e) => CheckChanged((MyLocalCompItem)ss, e);

        // 閺傚洣娆㈡い鍦畱閻愮懓鍤禍瀣╂閿涙艾鍨忛幑銏も偓澶夎厬閻樿埖鈧?
        sender.Click += (ss, e) =>
        {
            var s = (MyLocalCompItem)ss;
            s.Checked = !s.Checked;
        };

        // 閸ョ偓鐖ｉ幐澶愭尦
        var btnOpen = new MyIconButton { LogoScale = 1.05d, SvgIcon = "lucide/folder-open", Tag = sender };
        btnOpen.ToolTip = Lang.Text("Instance.Saves.OpenFileLocation");
        ToolTipService.SetPlacement(btnOpen, PlacementMode.Center);
        ToolTipService.SetVerticalOffset(btnOpen, 30d);
        ToolTipService.SetHorizontalOffset(btnOpen, 2d);
        btnOpen.Click += (sender, e) => Open_Click((MyIconButton)sender, e);

        var btnCont = new MyIconButton { LogoScale = 1d, SvgIcon = "lucide/info", Tag = sender };
        btnCont.ToolTip = Lang.Text("Instance.Saves.Detail");
        ToolTipService.SetPlacement(btnCont, PlacementMode.Center);
        ToolTipService.SetVerticalOffset(btnCont, 30d);
        ToolTipService.SetHorizontalOffset(btnCont, 2d);
        btnCont.Click += Info_Click;
        sender.MouseRightButtonUp += Info_Click;

        var btnDelete = new MyIconButton { LogoScale = 1d, SvgIcon = "lucide/trash-2", Tag = sender };
        btnDelete.ToolTip = Lang.Text("Common.Action.Delete");
        ToolTipService.SetPlacement(btnDelete, PlacementMode.Center);
        ToolTipService.SetVerticalOffset(btnDelete, 30d);
        ToolTipService.SetHorizontalOffset(btnDelete, 2d);
        btnDelete.Click += (sender, e) => Delete_Click((MyIconButton)sender, e);

        if (sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
        {
            var btnDisable = new MyIconButton { LogoScale = 1d, SvgIcon = "lucide/circle-minus", Tag = sender };
            btnDisable.ToolTip = Lang.Text("Instance.Resource.Disable");
            ToolTipService.SetPlacement(btnDisable, PlacementMode.Center);
            ToolTipService.SetVerticalOffset(btnDisable, 30d);
            ToolTipService.SetHorizontalOffset(btnDisable, 2d);
            btnDisable.Click += (ss, e) => Disable_Click((MyIconButton)ss, e);
            sender.Buttons = new[] { btnCont, btnOpen, btnDisable, btnDelete };
        }
        else if (sender.Entry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled)
        {
            var btnEnable = new MyIconButton { LogoScale = 1d, SvgIcon = "lucide/circle-check", Tag = sender };
            btnEnable.ToolTip = Lang.Text("Instance.Resource.Enable");
            ToolTipService.SetPlacement(btnEnable, PlacementMode.Center);
            ToolTipService.SetVerticalOffset(btnEnable, 30d);
            ToolTipService.SetHorizontalOffset(btnEnable, 2d);
            btnEnable.Click += (ss, e) => Enable_Click((MyIconButton)ss, e);
            sender.Buttons = new[] { btnCont, btnOpen, btnEnable, btnDelete };
        }
        else
        {
            sender.Buttons = new[] { btnCont, btnOpen, btnDelete };
        }
    }

    /// <summary>
    ///     閸掗攱鏌婇弫缈犻嚋 UI閵?
    /// </summary>
    public void RefreshUI()
    {
        if (PanList is null)
            return;
        var showingDatapacks = (IsSearching ? searchResult : datapackItems.Values.Select(i => i.Entry))
            .Where(m => CanPassFilter(m)).ToList();

        // 鐎佃妯夌粈铏规畱閺佺増宓侀崠鍛扮箻鐞涘本甯撴惔?
        if (showingDatapacks.Any())
        {
            var sortMethod = GetSortMethod(currentSortMethod);
            showingDatapacks.Sort((a, b) => sortMethod(a, b));
        }

        // 闁插秵鏌婇崚妤€鍤崚妤勩€?
        ModAnimation.AniControlEnabled += 1;
        if (showingDatapacks.Any())
        {
            PanList.Visibility = true;
            PanList.Children.Clear();
            foreach (var TargetDatapack in showingDatapacks)
            {
                if (!datapackItems.ContainsKey(TargetDatapack.RawPath))
                    continue;
                var item = datapackItems[TargetDatapack.RawPath];

                // 绾喕绻氶崗鍐濞屸剝婀侀悥璺侯啇閸ｎ煉绱濋柆鍨帳闁插秴顦插ǎ璇插瀵倸鐖?
                if (item.Parent is not null) ((Panel)item.Parent).Children.Remove(item);

                ModStyle.MinecraftFormatter.SetColorfulTextLab(item.LabTitle.Text, item.LabTitle,
                    ThemeService.IsDarkMode);
                ModStyle.MinecraftFormatter.SetColorfulTextLab(item.LabInfo.Text, item.LabInfo,
                    ThemeService.IsDarkMode);
                item.Checked = selectedDatapacks.Contains(TargetDatapack.RawPath); // 閺囧瓨鏌婇柅澶夎厬閻樿埖鈧?
                PanList.Children.Add(item);
            }
        }
        else
        {
            PanList.Visibility = false;
        }

        ModAnimation.AniControlEnabled -= 1;
        selectedDatapacks =
            new HashSet<string>(selectedDatapacks.Where(m =>
                showingDatapacks.Any(s => (s.RawPath ?? "") == (m ?? ""))));
        RefreshBars();
    }

    /// <summary>
    ///     閸掗攱鏌婃い鑸电埉閸滃苯绨抽弽蹇旀▔缁€鎭掆偓?
    /// </summary>
    public void RefreshBars()
    {
        Dispatcher.BeginInvoke(new Func<Task>(async () =>
        {
            // -----------------
            // 妞ゅ爼鍎撮弽?
            // -----------------

            // 鐠佲剝鏆?
            var anyCount = 0;
            var enabledCount = 0;
            var disabledCount = 0;
            var updateCount = 0;
            var unavalialeCount = 0;
            var itemSource = (IsSearching ? searchResult : datapackItems.Values.Select(i => i.Entry)).ToArray();
            await Task.Run(() =>
            {
                foreach (var item in itemSource)
                {
                    anyCount += 1;
                    if (item.CanUpdate) updateCount += 1;
                    if (item.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine) enabledCount += 1;
                    if (item.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled) disabledCount += 1;
                    if (item.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable) unavalialeCount += 1;
                }
            });
            // 閺勫墽銇?
            BtnFilterAll.Text = IsSearching ? Lang.Text("Instance.Resource.Filter.SearchResult") : Lang.Text("Instance.Resource.Filter.AllWithCount", anyCount);
            BtnFilterCanUpdate.Text = Lang.Text("Instance.Resource.Filter.UpdatableWithCount", updateCount);
            BtnFilterCanUpdate.Visibility = Filter == FilterType.CanUpdate || updateCount > 0
                ? true
                : false;
            BtnFilterEnabled.Text = Lang.Text("Instance.Resource.Filter.EnabledWithCount", enabledCount);
            BtnFilterEnabled.Visibility = Filter == FilterType.Enabled || (enabledCount > 0 && enabledCount < anyCount)
                ? true
                : false;
            BtnFilterDisabled.Text = Lang.Text("Instance.Resource.Filter.DisabledWithCount", disabledCount);
            BtnFilterDisabled.Visibility = Filter == FilterType.Disabled || disabledCount > 0
                ? true
                : false;
            BtnFilterError.Text = Lang.Text("Instance.Resource.Filter.ErrorWithCount", unavalialeCount);
            BtnFilterError.Visibility = Filter == FilterType.Unavailable || unavalialeCount > 0
                ? true
                : false;

            // -----------------
            // 鎼存洟鍎撮弽?
            // -----------------

            // 鐠佲剝鏆?
            var newCount = selectedDatapacks.Count;
            var selected = newCount > 0;
            if (selected)
                LabSelect.Text = Lang.Text("Instance.Resource.SelectedCount", newCount);

            // 閹稿鎸抽崣顖滄暏閹?
            if (selected)
            {
                var hasUpdate = false;
                var hasEnabled = false;
                var hasDisabled = false;
                var canFavoriteAndShare = true;


                // 濡偓閺屻儲妲搁崥锔藉閺堝鈧鑵戦惃鍕殶閹诡喖瀵橀柈鑺ユ箒閺堝鏅ラ惃鍕€嶉惄顔讳繆閹?
                await Task.Run(() =>
                {
                    foreach (var DatapackEntity in ModLocalComp.compResourceListLoader.output)
                        if (selectedDatapacks.Contains(DatapackEntity.RawPath))
                        {
                            if (DatapackEntity.CanUpdate) hasUpdate = true;
                            if (DatapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
                                hasEnabled = true;
                            else if (DatapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled)
                                hasDisabled = true;
                            if (DatapackEntity.Comp is null || string.IsNullOrEmpty(DatapackEntity.Comp.Id))
                                canFavoriteAndShare = false;
                        }
                });

                BtnSelectDisable.IsEnabled = hasEnabled;
                BtnSelectEnable.IsEnabled = hasDisabled;
                BtnSelectUpdate.IsEnabled = hasUpdate;
                BtnSelectFavorites.IsEnabled = canFavoriteAndShare;
                BtnSelectShare.IsEnabled = canFavoriteAndShare;
            }

            // 閺囧瓨鏌婇弰鍓с仛閻樿埖鈧?
            if (ModAnimation.AniControlEnabled == 0)
            {
                PanListBack.Margin = new Thickness(0d, 0d, 0d, selected ? 95 : 15);
                if (selected)
                {
                    // 娴犲懎婀弫浼村櫤婢х偛濮為弮鑸垫尡閺€鎯у毉閻?鐠哄疇绌崝銊ф暰
                    if (bottomBarShownCount >= newCount)
                    {
                        bottomBarShownCount = newCount;
                        return;
                    }

                    bottomBarShownCount = newCount;
                    // 閸戣櫣骞?鐠哄疇绌崝銊ф暰
                    CardSelect.Visibility = true;
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(CardSelect, 1d - CardSelect.Opacity, 60),
                            ModAnimation.AaTranslateY(CardSelect, -27 - TransSelect.Y, 120,
                                ease: new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaTranslateY(CardSelect, 3d, 150, 120,
                                new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaTranslateY(CardSelect, -1, 90, 270,
                                new ModAnimation.AniEaseInoutFluent(ModAnimation.AniEasePower.Weak))
                        }, "Datapack Sidebar");
                }
                else
                {
                    // 娑撳秹鍣告径宥嗘尡閺€楣冩閽樺繐濮╅悽?
                    if (bottomBarShownCount == 0)
                        return;
                    bottomBarShownCount = 0;
                    // 闂呮劘妫岄崝銊ф暰
                    ModAnimation.AniStart(
                        new[]
                        {
                            ModAnimation.AaOpacity(CardSelect, -CardSelect.Opacity, 90),
                            ModAnimation.AaTranslateY(CardSelect, -10 - TransSelect.Y, 90,
                                ease: new ModAnimation.AniEaseInFluent(ModAnimation.AniEasePower.Weak)),
                            ModAnimation.AaCode(() => CardSelect.Visibility = false, after: true)
                        }, "Datapack Sidebar");
                }
            }
            else
            {
                ModAnimation.AniStop("Datapack Sidebar");
                bottomBarShownCount = newCount;
                if (selected)
                {
                    CardSelect.Visibility = true;
                    CardSelect.Opacity = 1d;
                    TransSelect.Y = -25;
                }
                else
                {
                    CardSelect.Visibility = false;
                    CardSelect.Opacity = 0d;
                    TransSelect.Y = -10;
                }
            }
        }));
    }

    private int bottomBarShownCount;

    #endregion

    #region 缁狅紕鎮?

    /// <summary>
    ///     閹垫挸绱?datapacks 閺傚洣娆㈡径骞库偓?
    /// </summary>
    private void BtnManageOpen_Click(object sender, EventArgs e)
    {
        try
        {
            var datapackPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
            Directory.CreateDirectory(datapackPath);
            ModBase.OpenExplorer(datapackPath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閹垫挸绱?datapacks 閺傚洣娆㈡径鐟般亼鐠?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     閸忋劑鈧鈧?
    /// </summary>
    private void BtnManageSelectAll_Click(object sender, PointerReleasedEventArgs e)
    {
        ChangeAllSelected(selectedDatapacks.Count < PanList.Children.Count);
    }

    /// <summary>
    ///     鐎瑰顥婇弫鐗堝祦閸栧懌鈧?
    /// </summary>
    private void BtnManageInstall_Click(object sender, PointerReleasedEventArgs e)
    {
        var fileList = SystemDialogs.SelectFiles(
            Lang.Text("Instance.Saves.Datapack.Install.FileDialog.Filter"),
            Lang.Text("Instance.Saves.Datapack.Install.FileDialog.Title"));
        if (fileList is null || fileList.Length == 0)
            return;
        InstallDatapackFiles(fileList);
        Refresh();
    }

    /// <summary>
    ///     鐎瑰顥婇弫鐗堝祦閸栧懏鏋冩禒韬测偓?
    /// </summary>
    public static void InstallDatapackFiles(IEnumerable<string> filePathList)
    {
        if (!filePathList.Any())
            return;

        var extension = filePathList.First().AfterLast(".").ToLower();

        // 濡偓閺屻儲鏋冩禒鑸靛⒖鐏炴洖鎮?
        if (extension != "zip")
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.UnsupportedFormat", extension, Lang.Text("Download.Comp.Type.DataPack"), "zip"), HintType.Error);
            return;
        }

        // 濡偓閺屻儱娲栭弨鍓佺彲
        if (filePathList.First().Contains(@":\$RECYCLE.BIN\"))
        {
            HintService.Hint(Lang.Text("Instance.Resource.Install.RestoreFromRecycleBin"), HintType.Error);
            return;
        }

        ModBase.Log($"[System] 閺傚洣娆㈡稉?{extension} 閺嶇厧绱￠敍灞界毦鐠囨洑缍旀稉鐑樻殶閹诡喖瀵樼€瑰顥?);

        // 绾喛顓荤€瑰顥?
        if (!(ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup &&
              ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.VersionSavesDatapack))
            if (ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Install.Message"),
                    Lang.Text("Instance.Saves.Datapack.Install.Title"), Lang.Text("Common.Action.Confirm"),
                    Lang.Text("Common.Action.Cancel")) != 1)
                return;

        // 閹笛嗩攽鐎瑰顥?
        try
        {
            var datapackFolder = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
            Directory.CreateDirectory(datapackFolder);

            foreach (var FilePath in filePathList)
            {
                var newFileName = ModBase.GetFileNameFromPath(FilePath);
                var destFile = datapackFolder + newFileName;

                if (File.Exists(destFile))
                    if (ModMain.MyMsgBox(Lang.Text("Instance.Resource.Install.OverwriteConfirm.Message", newFileName), Lang.Text("Instance.Resource.Install.OverwriteConfirm.Title"), Lang.Text("Common.Action.Overwrite"), Lang.Text("Common.Action.Cancel")) != 1)
                        continue;

                ModBase.CopyFile(FilePath, destFile);
            }

            if (filePathList.Count() == 1)
                HintService.Hint(Lang.Text("Instance.Resource.Install.SuccessSingle", ModBase.GetFileNameFromPath(filePathList.First())), HintType.Success);
            else
                HintService.Hint(Lang.Text("Instance.Resource.Install.SuccessMultiple", filePathList.Count(), Lang.Text("Download.Comp.Type.DataPack")), HintType.Success);

            // 閸掗攱鏌婇崚妤勩€?
            if (ModMain.frmMain.pageCurrent == FormMain.PageType.InstanceSetup &&
                ModMain.frmMain.PageCurrentSub == FormMain.PageSubType.VersionSavesDatapack)
                if (ModMain.frmInstanceSavesDatapack is not null)
                    ModMain.frmInstanceSavesDatapack.ReloadDatapackFileList(true);
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "婢跺秴鍩楅弫鐗堝祦閸栧懏鏋冩禒璺恒亼鐠?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    /// <summary>
    ///     娑撳娴囬弫鐗堝祦閸栧懌鈧?
    /// </summary>
    private void BtnManageDownload_Click(object sender, PointerReleasedEventArgs e)
    {
        var datapackPath = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
        Directory.CreateDirectory(datapackPath);
        PageDownloadCompDetail.cachedFolder[ModComp.CompType.DataPack] = datapackPath;
        ModMain.frmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadDataPack);
        PageComp.targetVersion = PageInstanceLeft.McInstance; // 鐏忓棗缍嬮崜宥呯杽娓氬顔曠純顔昏礋缁涙盯鈧娅?
    }

    /// <summary>
    ///     鐎电厧鍤穱鈩冧紖閵?
    /// </summary>
    private void BtnManageInfoExport_Click(object sender, PointerReleasedEventArgs e)
    {
        var choice =
            ModMain.MyMsgBox(
                Lang.Text("Instance.Saves.Datapack.Export.Mode.Message"),
                Lang.Text("Instance.Resource.Export.Mode.Title"), Lang.Text("Instance.Resource.Export.Mode.Txt"), Lang.Text("Instance.Resource.Export.Mode.Csv"), Lang.Text("Common.Action.Cancel"));

        void ExportText(string content, string fileName)
        {
            try
            {
                var savePath =
                    SystemDialogs.SelectSaveFile(Lang.Text("Instance.Resource.Export.SelectSaveLocation"), fileName, Lang.Text("Instance.Resource.Export.FilesFilter"));
                if (string.IsNullOrWhiteSpace(savePath)) return;
                File.WriteAllText(savePath, content, Encoding.UTF8);
                ModBase.OpenExplorer(savePath);
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "鐎电厧鍤弫鐗堝祦閸栧懍淇婇幁顖氥亼鐠?,
                    ModBase.LogLevel.Msgbox,
                    userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
            }
        }

        ;
        switch (choice)
        {
            case 1: // TXT
            {
                var exportContent = new List<string>();
                foreach (var DatapackEntity in ModLocalComp.compResourceListLoader.output)
                    exportContent.Add(DatapackEntity.FileName);
                ExportText(exportContent.Join("\r\n"),
                    ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave) + "閻ㄥ嫭鏆熼幑顔煎瘶娣団剝浼?txt");
                break;
            }

            case 2: // CSV
            {
                var exportContent = new List<string>();
                exportContent.Add("閺傚洣娆㈤崥?閺佺増宓侀崠鍛倳缁?閺佺増宓侀崠鍛閺?濮濄倗澧楅張顒佹纯閺傜増妞傞梻?瀹搞儳鈻?ID,閺傚洣娆㈡径褍鐨敍鍫濈摟閼哄偊绱?閺傚洣娆㈢捄顖氱窞");
                foreach (var DatapackEntity in ModLocalComp.compResourceListLoader.output)
                    exportContent.Add(
                        $"{DatapackEntity.FileName},{DatapackEntity.Comp?.TranslatedName},{DatapackEntity.Version},{DatapackEntity.compFile?.ReleaseDate},{DatapackEntity.Comp?.Id},{GetDatapackFileInfo(DatapackEntity.path).Length},{DatapackEntity.path}");
                ExportText(exportContent.Join("\r\n"),
                    ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave) + "閻ㄥ嫭鏆熼幑顔煎瘶娣団剝浼?csv");
                break;
            }
        }
    }

    #endregion

    #region 闁瀚?

    /// <summary>
    ///     闁瀚ㄩ惃鍕殶閹诡喖瀵橀惃鍕熅瀵板嫨鈧?
    /// </summary>
    public HashSet<string> selectedDatapacks = new();

    // 閸楁洟銆嶉崚鍥ㄥ床闁瀚ㄩ悩鑸碘偓?
    public void CheckChanged(MyLocalCompItem sender, ModBase.RouteEventArgs e)
    {
        if (ModAnimation.AniControlEnabled != 0)
            return;
        // 閺囧瓨鏌婇柅澶嬪娴滃棛娈戦崘鍛啇
        var selectedKey = sender.Entry.RawPath;
        if (sender.Checked)
            selectedDatapacks.Add(selectedKey);
        else
            selectedDatapacks.Remove(selectedKey);
        RefreshBars();
    }

    // 閸掑洦宕查幍鈧張澶愩€嶉惃鍕偓澶嬪閻樿埖鈧?
    private void ChangeAllSelected(bool value)
    {
        ModAnimation.AniControlEnabled += 1;
        selectedDatapacks.Clear();
        foreach (var Item in datapackItems.Values)
        {
            var shouldSelected = value && PanList.Children.Contains(Item);
            Item.Checked = shouldSelected;
            if (shouldSelected)
                selectedDatapacks.Add(Item.Entry.RawPath);
        }

        ModAnimation.AniControlEnabled -= 1;
        RefreshBars();
    }

    private void UnselectedAllWithAnimation()
    {
        var cacheAniControlEnabled = ModAnimation.AniControlEnabled;
        ModAnimation.AniControlEnabled = 0;
        ChangeAllSelected(false);
        ModAnimation.AniControlEnabled += cacheAniControlEnabled;
    }

    private void FrmMain_KeyDown(object sender, KeyEventArgs e)
    {
        if (!ReferenceEquals(ModMain.frmMain.pageRight, this))
            return;
        if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.A)
            ChangeAllSelected(true);
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl + A 娴兼俺顫﹂幖婊呭偍濡楀棙宕熼懢鍑ょ礉鐎佃壈鍤ч弮鐘崇《閸忋劑鈧绱濋幍鈧禒銉ユ躬閹稿绗?Ctrl + A 閺冩儼娴嗙粔鑽ゅ妽閻愰€涗簰娓氭寧宕熼懢?
        if (SearchBox.Text.Any())
            return;
        if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.A)
            PanBack.Focus();
    }

    #endregion

    #region 缁涙盯鈧?

    public FilterType Filter
    {
        get => field;
        set
        {
            if (field == value)
                return;
            field = value;
            switch (value)
            {
                case FilterType.All:
                {
                    BtnFilterAll.Checked = true;
                    break;
                }
                case FilterType.Enabled:
                {
                    BtnFilterEnabled.Checked = true;
                    break;
                }
                case FilterType.Disabled:
                {
                    BtnFilterDisabled.Checked = true;
                    break;
                }
                case FilterType.CanUpdate:
                {
                    BtnFilterCanUpdate.Checked = true;
                    break;
                }

                default:
                {
                    BtnFilterError.Checked = true;
                    break;
                }
            }

            RefreshUI();
        }
    } = FilterType.All;

    public enum FilterType
    {
        All = 0,
        Enabled = 1,
        Disabled = 2,
        CanUpdate = 3,
        Unavailable = 4
    }

    /// <summary>
    ///     濡偓閺屻儴顕氶弫鐗堝祦閸栧懘銆嶉弰顖氭儊缁楋箑鎮庤ぐ鎾冲缁涙盯鈧娈戠猾璇插焼閵?
    /// </summary>
    private bool CanPassFilter(ModLocalComp.LocalCompFile checkingDatapack)
    {
        switch (Filter)
        {
            case FilterType.All:
            {
                return true;
            }
            case FilterType.Enabled:
            {
                return checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine;
            }
            case FilterType.Disabled:
            {
                return checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled;
            }
            case FilterType.CanUpdate:
            {
                return checkingDatapack.CanUpdate;
            }
            case FilterType.Unavailable:
            {
                return checkingDatapack.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable;
            }

            default:
            {
                return false;
            }
        }
    }

    // 閻愮懓鍤粵娑⑩偓澶愩€嶇憴锕€褰傞惃鍕暭閸?
    private void ChangeFilter(MyRadioButton sender, bool raiseByMouse)
    {
        Filter = (FilterType)Convert.ToInt32(sender.Tag);
        RefreshUI();
        DoSort();
    }

    #endregion

    #region 閹烘帒绨?

    private SortMethod currentSortMethod = SortMethod.CompName;

    private void SetSortMethod(SortMethod target)
    {
        currentSortMethod = target;
        BtnSort.Text = Lang.Text("Instance.Resource.Sort.Text", GetSortName(target));
        DoSort();
    }

    private enum SortMethod
    {
        FileName,
        CompName,
        CreateTime,
        DatapackFileSize
    }

    private string GetSortName(SortMethod method)
    {
        switch (method)
        {
            case SortMethod.FileName:
            {
                return Lang.Text("Instance.Resource.Sort.FileName");
            }
            case SortMethod.CompName:
            {
                return Lang.Text("Instance.Resource.Sort.ResourceName");
            }
            case SortMethod.CreateTime:
            {
                return Lang.Text("Instance.Resource.Sort.AddTime");
            }
            case SortMethod.DatapackFileSize:
            {
                return Lang.Text("Instance.Resource.Sort.FileSize");
            }

            default:
            {
                return Lang.Text("Instance.Resource.Sort.ResourceName");
            }
        }

        return "";
    }

    private void BtnSortClick(object sender, ModBase.RouteEventArgs e)
    {
        var body = new ContextMenu();
        foreach (SortMethod i in Enum.GetValues(typeof(SortMethod)))
        {
            var item = new MyMenuItem();
            item.Header = GetSortName(i);
            item.Click += (_, _) => SetSortMethod(i);
            body.Items.Add(item);
        }

        body.PlacementTarget = (Control)sender;
        body.Placement = PlacementMode.Bottom;
        body.IsOpen = true;
    }

    private readonly object sortLock = new();

    private void DoSort()
    {
        lock (sortLock)
        {
            try
            {
                if (PanList is null || PanList.Children.Count < 2)
                    return;

                // 鐏忓棗鐡欓崗鍐鏉烆剚宕叉稉鍝勫讲閹烘帒绨惃鍕灙鐞?
                var items = PanList.Children.OfType<MyLocalCompItem>().ToList();
                var method = GetSortMethod(currentSortMethod);

                // 閸掑棛顬囬張澶嬫櫏閸滃本妫ら弫鍫ャ€嶉敍鍫滅箽閹镐礁甯慨瀣祲鐎靛綊銆庢惔蹇ョ礆
                var invalid = items.Where(i => i.Entry is null).ToList();
                var valid = items.Except(invalid).ToList();
                // 娴犲懎顕張澶嬫櫏妞ょ绻樼悰灞惧笓鎼?
                valid.Sort((x, y) => method(x.Entry, y.Entry));
                // 閸氬牆鑻熸穱婵囧瘮閺冪姵鏅ユい鍦畱閸樼喎顫愭い鍝勭碍
                items = valid.Concat(invalid).ToList();

                // 閹靛綊鍣洪弴瀛樻煀UI閸忓啰绀?
                PanList.Children.Clear();
                items.ForEach(i => PanList.Children.Add(i));
            }

            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    "閹笛嗩攽閹烘帒绨弮璺哄毉闁?,
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
            }
        }
    }

    private Func<ModLocalComp.LocalCompFile, ModLocalComp.LocalCompFile, int> GetSortMethod(SortMethod method)
    {
        switch (method)
        {
            case SortMethod.FileName:
            {
                return (a, b) => string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
            }
            case SortMethod.CompName:
            {
                return (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            }
            case SortMethod.CreateTime:
            {
                return (a, b) =>
                {
                    var aDate = GetDatapackFileInfo(a.path).CreationTime;
                    var bDate = GetDatapackFileInfo(b.path).CreationTime;
                    if (aDate == DateTime.MinValue && bDate == DateTime.MinValue)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

                    if (aDate == DateTime.MinValue) return 1;

                    if (bDate == DateTime.MinValue) return -1;
                    return bDate.CompareTo(aDate);
                };
            }
            case SortMethod.DatapackFileSize:
            {
                return (a, b) =>
                {
                    var aSize = GetDatapackFileInfo(a.path).Length;
                    var bSize = GetDatapackFileInfo(b.path).Length;
                    if (aSize == 0L && bSize == 0L)
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

                    if (aSize == 0L) return 1;

                    if (bSize == 0L) return -1;
                    return bSize.CompareTo(aSize);
                };
            }

            default:
            {
                return (a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    #endregion

    #region 娑撳绔熼弽?

    // 閸氼垳鏁?
    private void BtnSelectEnable_Click(object sender, ModBase.RouteEventArgs e)
    {
        ToggleDatapacks(
            ModLocalComp.compResourceListLoader.output.Where(m => selectedDatapacks.Contains(m.RawPath)).ToList(),
            true);
        ChangeAllSelected(false);
    }

    // 缁備胶鏁?
    private void BtnSelectDisable_Click(object sender, ModBase.RouteEventArgs e)
    {
        ToggleDatapacks(
            ModLocalComp.compResourceListLoader.output.Where(m => selectedDatapacks.Contains(m.RawPath)).ToList(),
            false);
        ChangeAllSelected(false);
    }

    /// <summary>
    ///     閸氼垳鏁?缁備胶鏁ら弫鐗堝祦閸栧拑绱欓柅姘崇箖闁插秴鎳￠崥宥嗘瀮娴犺泛銇欐稉?.disabled閿?
    /// </summary>
    private void ToggleDatapacks(IEnumerable<ModLocalComp.LocalCompFile> datapackList, bool isEnable)
    {
        var isSuccessful = true;
        foreach (var DatapackE in datapackList)
        {
            var datapackEntity = DatapackE;
            string newPath = null;

            if (datapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine && !isEnable)
                // 缁備胶鏁?- 濞ｈ濮?.disabled 閸氬海绱?
                newPath = datapackEntity.path + ".disabled";
            else if (datapackEntity.State == ModLocalComp.LocalCompFile.LocalFileStatus.Disabled && isEnable)
                // 閸氼垳鏁?- 缁夊娅?.disabled 閸氬海绱?
                newPath = datapackEntity.RawPath;
            else
                continue;

            // 闁插秴鎳￠崥?
            try
            {
                if (File.Exists(newPath))
                {
                    ModMain.MyMsgBox(Lang.Text("Instance.Saves.Datapack.Replace.FileNameConflict", ModBase.GetFileNameFromPath(newPath)));
                    continue;
                }

                FileSystem.Rename(datapackEntity.path, newPath);
            }
            catch (FileNotFoundException ex)
            {
                ModBase.Log(
                    ex,
                    $"閺堫亝澹橀崚浼存付鐟曚線鍣搁崨钘夋倳閻ㄥ嫭鏆熼幑顔煎瘶閿涘澖datapackEntity.path ?? "null"}閿?,
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
                ReloadDatapackFileList(true);
                return;
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"闁插秴鎳￠崥宥嗘殶閹诡喖瀵樻径杈Е閿涘澖datapackEntity.path ?? "null"}閿?);
                isSuccessful = false;
            }

            // 閺囧瓨鏁?Loader 娑擃厾娈戦崚妤勩€?
            var newDatapackEntity = new ModLocalComp.LocalCompFile(newPath);
            newDatapackEntity.FromJson(datapackEntity.ToJson());
            if (ModLocalComp.compResourceListLoader.output.Contains(datapackEntity))
            {
                var indexOfLoader = ModLocalComp.compResourceListLoader.output.IndexOf(datapackEntity);
                ModLocalComp.compResourceListLoader.output.RemoveAt(indexOfLoader);
                ModLocalComp.compResourceListLoader.output.Insert(indexOfLoader, newDatapackEntity);
            }

            if (searchResult is not null && searchResult.Contains(datapackEntity))
            {
                var indexOfResult = searchResult.IndexOf(datapackEntity);
                searchResult.Remove(datapackEntity);
                searchResult.Insert(indexOfResult, newDatapackEntity);
            }

            // 閺囧瓨鏁?UI 娑擃厾娈戦崚妤勩€?
            try
            {
                var newItem = BuildLocalCompItem(newDatapackEntity);
                datapackItems[datapackEntity.RawPath] = newItem;
                var indexOfUi = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>()
                    .FirstOrDefault(i => ReferenceEquals(i.Entry, datapackEntity)));
                if (indexOfUi == -1)
                    continue;
                PanList.Children.RemoveAt(indexOfUi);
                PanList.Children.Insert(indexOfUi, newItem);
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    $"閺囧瓨鏌?UI 閸掓銆冩い鐟般亼鐠愩儻绱皗datapackEntity.FileName}",
                    ModBase.LogLevel.Hint,
                    userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
            }
        }

        Dispatcher.Invoke(() => PanList.UpdateLayout(), DispatcherPriority.Background);

        if (isSuccessful)
        {
            RefreshBars();
        }
        else
        {
            HintService.Hint(Lang.Text("Instance.Saves.Datapack.ToggleWarning"), HintType.Error);
            ReloadDatapackFileList(true);
        }

        LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
    }

    // 閺囧瓨鏌?
    private void BtnSelectUpdate_Click(object sender, ModBase.RouteEventArgs e)
    {
        var updateList = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedDatapacks.Contains(m.RawPath) && m.CanUpdate).ToList();
        if (!updateList.Any())
            return;
        UpdateResource(updateList);
        ChangeAllSelected(false);
    }

    /// <summary>
    ///     鐠佹澘缍嶅锝呮躬鏉╂稖顢戦弫鐗堝祦閸栧懏娲块弬鎵畱 datapacks 閺傚洣娆㈡径纭呯熅瀵板嫨鈧?
    /// </summary>
    public static List<string> updatingVersions = new();

    private static bool TryGetSafeDatapackUpdateFileName(ModComp.CompFile file, out string fileName)
    {
        fileName = file.FileName?.Trim() ?? "";
        if (string.IsNullOrEmpty(fileName))
            return false;

        if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return false;

        if (fileName.IndexOfAny(new[] { '\\', '/', ':' }) >= 0)
            return false;

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;

        return fileName == Path.GetFileName(fileName) && fileName != "." && fileName != "..";
    }

    private static bool TryBuildDatapackUpdatePath(string rootPath, string fileName, out string fullPath)
    {
        var fullRootPath = Path.GetFullPath(rootPath);
        if (!fullRootPath.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !fullRootPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            fullRootPath += Path.DirectorySeparatorChar;

        fullPath = Path.GetFullPath(Path.Combine(fullRootPath, fileName));
        return fullPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateResource(IEnumerable<ModLocalComp.LocalCompFile> datapackList)
    {
        // 閺囧瓨鏌婇崜宥堫劅閸?
        if (!States.Hint.FunctionDatapackUpdate || datapackList.Count() >= 15)
        {
            if (ModMain.MyMsgBox(
                    Lang.Text("Instance.Saves.Datapack.Update.Warning.Message"),
                    Lang.Text("Instance.Saves.Datapack.Update.Warning.Title"), Lang.Text("Instance.Saves.Datapack.Update.Warning.Confirm"), Lang.Text("Common.Action.Cancel"), isWarn: true) == 1)
                States.Hint.FunctionDatapackUpdate = true;
            else
                return;
        }

        try
        {
            // 閺嬪嫰鈧姳绗呮潪鎴掍繆閹?
            datapackList = datapackList.ToList(); // 闂冨弶顒涢崚閿嬫煀瑜板崬鎼锋潻顓濆敩閸?
            var fileList = new List<DownloadFile>();
            var fileCopyList = new Dictionary<string, string>();
            var updateEntryList = new List<ModLocalComp.LocalCompFile>();
            var tempRoot = Path.Combine(ModBase.pathTemp, "DownloadedComp");
            var datapackRoot = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");
            var skippedUnsafeFileCount = 0;
            foreach (var Entry in datapackList)
            {
                var file = Entry.UpdateFile;
                if (!file.Available)
                    continue;
                if (!TryGetSafeDatapackUpdateFileName(file, out var safeFileName) ||
                    !TryBuildDatapackUpdatePath(tempRoot, safeFileName, out var tempAddress) ||
                    !TryBuildDatapackUpdatePath(datapackRoot, safeFileName, out var realAddress))
                {
                    skippedUnsafeFileCount++;
                    ModBase.Log($"[DatapackUpdate] 瀹歌尪鐑︽潻鍥︾瑝鐎瑰鍙忛惃鍕殶閹诡喖瀵橀弴瀛樻煀閺傚洣娆㈤崥宥忕窗{file.FileName}", ModBase.LogLevel.Debug);
                    continue;
                }

                // 濞ｈ濮為崚棰佺瑓鏉炶棄鍨悰?
                fileList.Add(file.ToNetFile(tempAddress, ModComp.DownloadReason.Update,
                    file.RawGameVersions.FirstOrDefault()));
                fileCopyList[tempAddress] = realAddress;
                updateEntryList.Add(Entry);
            }

            if (skippedUnsafeFileCount > 0)
                HintService.Hint(
                    Lang.Text("Instance.Saves.Datapack.Update.UnsafeFilesSkipped", skippedUnsafeFileCount),
                    HintType.Error);
            if (!fileList.Any())
                return;

            // 閺嬪嫰鈧姴濮炴潪钘夋珤
            var installLoaders = new List<ModLoader.LoaderBase>();
            var finishedFileNames = new List<string>();
            installLoaders.Add(
                new LoaderDownload(Lang.Text("Instance.Saves.Datapack.Update.Task.DownloadFiles"), fileList)
                    { ProgressWeight = updateEntryList.Count * 1.5d });

            installLoaders.Add(new ModLoader.LoaderTask<int, int>(
                Lang.Text("Instance.Saves.Datapack.Update.Task.ReplaceFiles"), _ =>
                {
                    try
                    {
                        foreach (var Entry in updateEntryList)
                            if (File.Exists(Entry.path))
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Entry.path, UIOption.AllDialogs,
                                    RecycleOption.SendToRecycleBin);
                            else
                                ModBase.Log($"[DatapackUpdate] 閺堫亝澹橀崚鐗堟纯閺傛澘澧犻惃鍕殶閹诡喖瀵橀弬鍥︽閿涘矁鐑︽潻鍥ь嚠鐎瑰啰娈戦崚鐘绘珟閿涙Entry.path}",
                                    ModBase.LogLevel.Debug);

                        foreach (var Entry in fileCopyList)
                        {
                            if (File.Exists(Entry.Value))
                            {
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Entry.Value, UIOption.AllDialogs,
                                    RecycleOption.SendToRecycleBin);
                                ModBase.Log($"[Datapack] 閺囧瓨鏌婇崥搴ｆ畱閺佺増宓侀崠鍛瀮娴犺泛鍑＄€涙ê婀敍灞界殺娴兼碍濡哥€瑰啯鏂侀崗銉ユ礀閺€鍓佺彲閿涙Entry.Value}", ModBase.LogLevel.Debug);
                            }

                            if (Directory.Exists(ModBase.GetPathFromFullPath(Entry.Value)))
                            {
                                File.Move(Entry.Key, Entry.Value);
                                finishedFileNames.Add(ModBase.GetFileNameFromPath(Entry.Value));
                            }
                            else
                            {
                                ModBase.Log($"[Datapack] 閺囧瓨鏌婇崥搴ｆ畱閻╊喗鐖ｉ弬鍥︽婢剁懓鍑＄悮顐㈠灩闂勩倧绱皗Entry.Value}", ModBase.LogLevel.Debug);
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        ModBase.Log(ex, "閺囨寧宕查弮褏澧楅弫鐗堝祦閸栧懏鏋冩禒鑸垫鐞氼偂瀵岄崝銊ュ絿濞?);
                    }
                }));

            // 缂佹挻娼径鍕倞
            var loader = new ModLoader.LoaderCombo<IEnumerable<ModLocalComp.LocalCompFile>>(
                Lang.Text("Instance.Saves.Datapack.Update.Task.Title",
                    ModBase.GetFolderNameFromPath(PageInstanceSavesLeft.currentSave)), installLoaders);
            var pathDatapacks = Path.Combine(PageInstanceSavesLeft.currentSave, "datapacks");

            loader.OnStateChanged = _ =>
            {
                switch (loader.State)
                {
                    case ModBase.LoadState.Finished:
                    {
                        switch (finishedFileNames.Count)
                        {
                            case 0:
                            {
                                ModBase.Log("[DatapackUpdate] 濞屸剝婀侀弫鐗堝祦閸栧懓顫﹂幋鎰閺囧瓨鏌?);
                                break;
                            }
                            case 1:
                            {
                                HintService.Hint(Lang.Text("Instance.Resource.Update.SuccessSingle", finishedFileNames.Single()), HintType.Success);
                                break;
                            }

                            default:
                            {
                                HintService.Hint(Lang.Text("Instance.Resource.Update.SuccessMultiple", finishedFileNames.Count), HintType.Success);
                                break;
                            }
                        }

                        break;
                    }
                    case ModBase.LoadState.Failed:
                    {
                        HintService.Hint(Lang.Text("Instance.Resource.Update.Failed", loader.Error.Message), HintType.Error);
                        break;
                    }
                    case ModBase.LoadState.Aborted:
                    {
                        HintService.Hint(Lang.Text("Instance.Resource.Update.Aborted"));
                        break;
                    }

                    default:
                    {
                        return;
                    }
                }

                ModBase.Log($"[DatapackUpdate] 瀹歌弓绮犲锝呮躬鏉╂稖顢戦弫鐗堝祦閸栧懏娲块弬鎵畱閺傚洣娆㈡径鐟板灙鐞涖劎些闂勩倧绱皗pathDatapacks}");
                updatingVersions.Remove(pathDatapacks);

                // 濞撳懐鎮婄紓鎾崇摠
                ModBase.RunInNewThread(() =>
                {
                    try
                    {
                        foreach (var TempFile in fileCopyList.Keys)
                            if (File.Exists(TempFile))
                                File.Delete(TempFile);
                    }
                    catch (Exception ex)
                    {
                        ModBase.Log(ex, "濞撳懐鎮婇弫鐗堝祦閸栧懏娲块弬鎵处鐎涙ê銇戠拹?);
                    }
                }, "Clean Datapack Update Cache", ThreadPriority.BelowNormal);
            };

            // 閸氼垰濮╅崝鐘烘祰閸?
            ModBase.Log($"[DatapackUpdate] 瀵偓婵娲块弬?{datapackList.Count()} 娑擃亝鏆熼幑顔煎瘶閿涙pathDatapacks}");
            updatingVersions.Add(pathDatapacks);
            loader.Start();
            ModLoader.LoaderTaskbarAdd(loader);
            ModMain.frmMain.BtnExtraDownload.ShowRefresh();
            ModMain.frmMain.BtnExtraDownload.Ribble();
            ReloadDatapackFileList(true);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "閸掓繂顫愰崠鏍ㄦ殶閹诡喖瀵橀弴瀛樻煀婢惰精瑙?);
        }
    }

    // 閸掔娀娅?
    private void BtnSelectDelete_Click(object sender, ModBase.RouteEventArgs e)
    {
        DeleteDatapacks(ModLocalComp.compResourceListLoader.output.Where(m => selectedDatapacks.Contains(m.RawPath)));
        ChangeAllSelected(false);
    }

    private void DeleteDatapacks(IEnumerable<ModLocalComp.LocalCompFile> datapackList)
    {
        try
        {
            var isSuccessful = true;
            var isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            // 绾喛顓婚棁鈧憰浣稿灩闂勩倗娈戦弬鍥︽
            datapackList = datapackList.SelectMany(target =>
            {
                if (target.State == ModLocalComp.LocalCompFile.LocalFileStatus.Fine)
                    return new[] { target.path, target.path + ".disabled" };

                return new[] { target.path, target.RawPath };
            }).Distinct().Where(m => File.Exists(m)).Select(m => new ModLocalComp.LocalCompFile(m)).ToList();

            // 鐎圭偤妾崚鐘绘珟閺傚洣娆?
            foreach (var DatapackEntity in datapackList)
            {
                try
                {
                    if (isShiftPressed)
                        File.Delete(DatapackEntity.path);
                    else
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(DatapackEntity.path,
                            UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (OperationCanceledException ex)
                {
                    ModBase.Log(ex, "閸掔娀娅庨弫鐗堝祦閸栧懓顫︽稉璇插З閸欐牗绉?);
                    ReloadDatapackFileList(true);
                    return;
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        $"閸掔娀娅庨弫鐗堝祦閸栧懎銇戠拹銉礄{DatapackEntity.path}閿?,
                        ModBase.LogLevel.Msgbox,
                        userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
                    isSuccessful = false;
                }

                // 閸欐牗绉烽柅澶夎厬
                selectedDatapacks.Remove(DatapackEntity.RawPath);
                // 閺囧瓨鏁?Loader 閸?UI 娑擃厾娈戦崚妤勩€?
                ModLocalComp.compResourceListLoader.output.Remove(DatapackEntity);
                searchResult?.Remove(DatapackEntity);
                datapackItems.Remove(DatapackEntity.RawPath);
                var indexOfUi = PanList.Children.IndexOf(PanList.Children.OfType<MyLocalCompItem>()
                    .FirstOrDefault(i => i.Entry.Equals(DatapackEntity)));
                if (indexOfUi >= 0)
                    PanList.Children.RemoveAt(indexOfUi);
            }

            RefreshBars();
            if (!isSuccessful)
            {
                HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.FileOccupied"), HintType.Error);
                ReloadDatapackFileList(true);
            }
            else if (PanList.Children.Count == 0)
            {
                ReloadDatapackFileList(true);
            }
            else
            {
                RefreshBars();
            }

            if (!isSuccessful)
                return;
            if (isShiftPressed)
            {
                if (datapackList.Count() == 1)
                    HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.PermanentSingle", datapackList.Single().FileName), HintType.Success);
                else
                    HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.PermanentMultiple", datapackList.Count()), HintType.Success);
            }
            else if (datapackList.Count() == 1)
            {
                HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.RecycleSingle", datapackList.Single().FileName), HintType.Success);
            }
            else
            {
                HintService.Hint(Lang.Text("Instance.Saves.Datapack.Delete.RecycleMultiple", datapackList.Count()), HintType.Success);
            }
        }
        catch (OperationCanceledException ex)
        {
            ModBase.Log(ex, "閸掔娀娅庨弫鐗堝祦閸栧懓顫︽稉璇插З閸欐牗绉?);
            ReloadDatapackFileList(true);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閸掔娀娅庨弫鐗堝祦閸栧懎鍤悳鐗堟弓閻儵鏁婄拠?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
            ReloadDatapackFileList(true);
        }

        LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
    }

    // 閸欐牗绉烽柅澶嬪
    private void BtnSelectCancel_Click(object sender, ModBase.RouteEventArgs e)
    {
        ChangeAllSelected(false);
    }

    // 閺€鎯版
    private void BtnSelectFavorites_Click(object sender, ModBase.RouteEventArgs e)
    {
        var selected = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedDatapacks.Contains(m.RawPath) && m.Comp is not null).Select(i => i.Comp).ToList();
        ModComp.CompFavorites.ShowMenu(selected, (Control)sender);
    }

    // 閸掑棔闊?
    private void BtnSelectShare_Click(object sender, ModBase.RouteEventArgs e)
    {
        var shareList = ModLocalComp.compResourceListLoader.output
            .Where(m => selectedDatapacks.Contains(m.RawPath) && m.Comp is not null).Select(i => i.Comp.Id).ToHashSet();
        ModBase.ClipboardSet(ModComp.CompFavorites.GetShareCode(shareList));
        ChangeAllSelected(false);
    }

    #endregion

    #region 閸楁洑閲滅挧鍕爱妞?

    // 鐠囷附鍎?
    public void Info_Click(object sender, EventArgs e)
    {
        try
        {
            var datapackEntry = ((MyLocalCompItem)(sender is MyIconButton iconBtn ? iconBtn.Tag : sender)).Entry;

            // 閸旂姾娴囨径杈Е娣団剝浼?
            if (datapackEntry.State == ModLocalComp.LocalCompFile.LocalFileStatus.Unavailable)
            {
                ModMain.MyMsgBox(
                    Lang.Text(
                        "Instance.Saves.Datapack.Info.ReadFailed.WithDetail",
                        datapackEntry.FileUnavailableReason.ToString()),
                    Lang.Text("Instance.Saves.Datapack.Info.ReadFailedTitle"));
                return;
            }

            if (datapackEntry.Comp is not null)
            {
                // 鐠哄疇娴嗛崚鐗堟殶閹诡喖瀵樻稉瀣祰妞ょ敻娼?
                ModMain.frmMain.PageChange(new FormMain.PageStackData
                {
                    page = FormMain.PageType.CompDetail,
                    additional = (datapackEntry.Comp, new List<string>(), PageInstanceLeft.McInstance.Info.VanillaName,
                        ModComp.CompLoaderType.Minecraft, ModComp.CompType.DataPack, null)
                });
            }
            else
            {
                // 閼惧嘲褰囨穱鈩冧紖
                var contentLines = new List<string>();

                if (datapackEntry.Description is not null)
                    contentLines.Add(datapackEntry.Description + "\r\n");
                if (datapackEntry.Authors is not null)
                    contentLines.Add(Lang.Text("Instance.Saves.Datapack.Info.Author") + datapackEntry.Authors);
                contentLines.Add(Lang.Text("Instance.Saves.Datapack.Info.File") + datapackEntry.FileName + "閿? +
                                 ModBase.GetString(GetDatapackFileInfo(datapackEntry.path).Length) + "閿?);
                if (datapackEntry.Version is not null)
                    contentLines.Add(Lang.Text("Instance.Saves.Datapack.Info.Version") + datapackEntry.Version);

                var debugInfo = new List<string>();
                if (datapackEntry.ModId is not null) debugInfo.Add(Lang.Text("Instance.Saves.Datapack.Info.DatapackId") + datapackEntry.ModId);
                if (debugInfo.Any())
                {
                    contentLines.Add("");
                    contentLines.AddRange(debugInfo);
                }

                // 閺勫墽銇氱拠锔藉剰娣団剝浼?
                if (datapackEntry.Url is null)
                    ModMain.MyMsgBox(contentLines.Join("\r\n"), datapackEntry.Name, Lang.Text("Instance.Resource.Item.Info.Return"));
                else if (ModMain.MyMsgBox(contentLines.Join("\r\n"), datapackEntry.Name, Lang.Text("Instance.Resource.Item.Info.OpenWebsite"), Lang.Text("Instance.Resource.Item.Info.Return")) == 1)
                    ModBase.OpenWebsite(datapackEntry.Url);
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閼惧嘲褰囬弫鐗堝祦閸栧懓顕涢幆鍛亼鐠?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    // 閹垫挸绱戦弬鍥︽閹碘偓閸︺劎娈戞担宥囩枂
    public void Open_Click(MyIconButton sender, EventArgs e)
    {
        try
        {
            var listItem = (MyLocalCompItem)sender.Tag;
            ModBase.OpenExplorer(listItem.Entry.path);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閹垫挸绱戦弫鐗堝祦閸栧懏鏋冩禒鏈电秴缂冾喖銇戠拹?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Saves.Error.OperationFailed"));
        }
    }

    // 閸掔娀娅?
    public void Delete_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        DeleteDatapacks(new[] { listItem.Entry });
    }

    // 閸氼垳鏁?
    public void Enable_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        ToggleDatapacks(new[] { listItem.Entry }, true);
    }

    // 缁備胶鏁?
    public void Disable_Click(MyIconButton sender, EventArgs e)
    {
        var listItem = (MyLocalCompItem)sender.Tag;
        ToggleDatapacks(new[] { listItem.Entry }, false);
    }

    #endregion

    #region 閹兼粎鍌?

    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchBox.Text);
    private List<ModLocalComp.LocalCompFile> searchResult;

    public void SearchRun(object sender, EventArgs e)
    {
        try
        {
            if (IsSearching)
            {
                // 閺嬪嫰鈧姾顕Ч?
                var queryList = new List<ModBase.SearchEntry<ModLocalComp.LocalCompFile>>();
                foreach (var Entry in ModLocalComp.compResourceListLoader.output)
                {
                    var searchSource = new List<ModBase.SearchSource>();
                    searchSource.Add(new ModBase.SearchSource(Entry.Name, 1d));
                    searchSource.Add(new ModBase.SearchSource(Entry.FileName, 1d));
                    if (Entry.Version is not null)
                        searchSource.Add(new ModBase.SearchSource(Entry.Version, 0.2d));
                    if (Entry.Description is not null && !string.IsNullOrEmpty(Entry.Description))
                        searchSource.Add(new ModBase.SearchSource(Entry.Description, 0.4d));
                    if (Entry.Comp is not null)
                    {
                        if ((Entry.Comp.RawName ?? "") != (Entry.Name ?? ""))
                            searchSource.Add(new ModBase.SearchSource(Entry.Comp.RawName, 1d));
                        if ((Entry.Comp.TranslatedName ?? "") != (Entry.Comp.RawName ?? ""))
                            searchSource.Add(new ModBase.SearchSource(Entry.Comp.TranslatedName, 1d));
                        if ((Entry.Comp.Description ?? "") != (Entry.Description ?? ""))
                            searchSource.Add(new ModBase.SearchSource(Entry.Comp.Description, 0.4d));
                        searchSource.Add(new ModBase.SearchSource(string.Join("", Entry.Comp.Tags), 0.2d));
                    }

                    queryList.Add(new ModBase.SearchEntry<ModLocalComp.LocalCompFile>
                        { item = Entry, searchSource = searchSource });
                }

                // 鏉╂稖顢戦幖婊呭偍
                searchResult = ModBase.Search(queryList, SearchBox.Text, ModBase.MaxLocalSearchDepth, 0.35d).Select(r => r.item).ToList();
            }

            RefreshUI();
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "閹兼粎鍌ㄦ潻鍥┾柤娑擃厼褰傞悽鐔风磽鐢?);
        }
    }

    #endregion
}
