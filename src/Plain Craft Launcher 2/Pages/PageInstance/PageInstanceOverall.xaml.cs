using System.Collections.ObjectModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using FluentValidation;
using Microsoft.VisualBasic.FileIO;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Configuration.Storage;
using PCL.Core.Minecraft;
using PCL.Core.UI;
using PCL.Core.Utils.Validate;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using PCL.Core.App.Localization;

namespace PCL;

public partial class PageInstanceOverall
{
    private ModLoader.LoaderCombo<int> instanceInfoLoader;

    private bool isLoad;

    public MyListItem itemVersion;
    private MyCompItem modpackCompItem;

    public PageInstanceOverall()
    {
        InitializeComponent();
        Loaded += PageSetupLaunch_Loaded;
        LabInfoLoading.Text = Lang.Text("Instance.Overall.Info.Loading");
        // Handles
        ComboDisplayType.SelectionChanged += ComboDisplayType_SelectionChanged;
        BtnDisplayDesc.Click += BtnDisplayDesc_Click;
        BtnDisplayRename.Click += BtnDisplayRename_Click;
        ComboDisplayLogo.SelectionChanged += ComboDisplayLogo_SelectionChanged;
        BtnDisplayStar.Click += BtnDisplayStar_Click;
        BtnFolderVersion.Click += BtnFolderVersion_Click;
        BtnFolderSaves.Click += BtnFolderSaves_Click;
        BtnFolderMods.Click += BtnFolderMods_Click;
        BtnManageScript.Click += BtnManageScript_Click;
        BtnManageCheck.Click += BtnManageCheck_Click;
        BtnManageRestore.Click += BtnManageRestore_Click;
        BtnManageTest.Click += BtnManageTest_Click;
        BtnManageDelete.Click += BtnManageDelete_Click;
        BtnManagePatch.Click += BtnManagePatch_Click;
    }

    private void PageSetupLaunch_Loaded(object sender, RoutedEventArgs e)
    {
        // 閲嶅鍔犺浇閮ㄥ垎
        PanBack.ScrollToHome();

        // 鏇存柊璁剧疆
        ItemDisplayLogoCustom.Tag = @"PCL\Logo.png";
        Reload();

        // 闈為噸澶嶅姞杞介儴鍒?
        if (isLoad)
            return;
        isLoad = true;
        PanDisplay.TriggerForceResize();
    }

    /// <summary>
    ///     纭繚褰撳墠椤甸潰涓婄殑淇℃伅宸叉纭樉绀恒€?
    /// </summary>
    private void Reload()
    {
        ModAnimation.AniControlEnabled += 1;

        var instance = PageInstanceLeft.McInstance;
        // 鍒锋柊璁剧疆椤圭洰
        ComboDisplayType.SelectedIndex = States.Instance.CardType[instance.PathInstance];
        BtnDisplayStar.Text = instance.IsStar ? Lang.Text("Instance.Overall.Unfavorite") : Lang.Text("Instance.Overall.Favorite");
        BtnFolderMods.Visibility = instance.Modable ? Visibility.Visible : Visibility.Collapsed;
        // 鍒锋柊瀹炰緥鏄剧ず
        PanDisplayItem.Children.Clear();
        itemVersion = PageSelectRight.McVersionListItem(instance);
        itemVersion.IsHitTestVisible = false;
        PanDisplayItem.Children.Add(itemVersion);
        ModMain.frmMain.PageNameRefresh();
        // 鍒锋柊瀹炰緥淇℃伅
        GetInstanceInfo();
        // 鍒锋柊瀹炰緥鍥炬爣
        ComboDisplayLogo.SelectedIndex = 0;
        var logo = States.Instance.LogoPath[instance.PathInstance];
        var logoCustom = States.Instance.IsLogoCustom[instance.PathInstance];
        if (logoCustom)
            foreach (MyComboBoxItem Selection in ComboDisplayLogo.Items)
                if (Equals(Selection.Tag, logo) ||
                    (Equals(Selection.Tag, @"PCL\Logo.png") &&
                     logo.EndsWith(@"PCL\Logo.png")))
                {
                    ComboDisplayLogo.SelectedItem = Selection;
                    break;
                }

        ModAnimation.AniControlEnabled -= 1;
    }

    private void GetInstanceInfo()
    {
        modpackCompItem = null;
        ModBase.RunInUi(() =>
        {
            PanInfo.Children.Clear();
            PanInfo.Children.Add(new MyLoading { Text = Lang.Text("Instance.Overall.Info.Loading"), Margin = new Thickness(0d, 0d, 0d, 10d) });
        });
        var loaders = new List<ModLoader.LoaderBase>();
        loaders.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Instance.Overall.Info.LoadModpackInfoTask"), _ =>
        {
            var modpackId = States.Instance.ModpackId[PageInstanceLeft.McInstance.PathInstance];
            if (!string.IsNullOrWhiteSpace(modpackId))
            {
                var compProjects = ModComp.CompRequest.GetCompProjectsByIds(new List<string> { modpackId });
                if (compProjects.Count > 0)
                    ModBase.RunInUi(() =>
                    {
                        modpackCompItem = compProjects.First().ToCompItem(false, false);
                        modpackCompItem.Tag = compProjects.First();
                    });
            }
        })
        {
            block = true
        });
        loaders.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Instance.Overall.Info.LoadInstanceInfoTask"), _ => ModBase.RunInUi(() =>
        {
            var instance = PageInstanceLeft.McInstance;
            var instanceInfo = instance.Info;
            List<MyListItem> items = [];
            var launchCount = States.Instance.LaunchCount[instance.PathInstance];
            if (launchCount == 0)
                items.Add(new MyListItem
                {
                    Title = Lang.Text("Instance.Overall.Info.LaunchCount.Title"), Info = Lang.Text("Instance.Overall.Info.LaunchCount.Never"), Logo = "pack://application:,,,/images/Blocks/RedstoneLampOff.png"
                });
            else
                items.Add(new MyListItem
                {
                    Title = Lang.Text("Instance.Overall.Info.LaunchCount.Title"),
                    Info = Lang.Text("Instance.Overall.Info.LaunchCount.Count", States.Instance.LaunchCount[instance.PathInstance]),
                    Logo = "pack://application:,,,/images/Blocks/RedstoneLampOn.png"
                });
            if (!string.IsNullOrWhiteSpace(States.Instance.ModpackVersion[instance.PathInstance]))
                items.Add(new MyListItem
                {
                    Title = Lang.Text("Instance.Overall.Info.ModpackVersion"), Info = States.Instance.ModpackVersion[instance.PathInstance],
                    Logo = "pack://application:,,,/images/Blocks/CommandBlock.png"
                });
            items.Add(new MyListItem
            {
                Title = "Minecraft", Info = instanceInfo.VanillaName,
                Logo = "pack://application:,,,/images/Blocks/Grass.png"
            });
            if (instanceInfo.HasForge)
                items.Add(new MyListItem
                {
                    Title = "Forge", Info = instanceInfo.Forge, Logo = "pack://application:,,,/images/Blocks/Anvil.png"
                });
            if (instanceInfo.HasNeoForge)
                items.Add(new MyListItem
                {
                    Title = "NeoForge", Info = instanceInfo.NeoForge,
                    Logo = "pack://application:,,,/images/Blocks/NeoForge.png"
                });
            if (instanceInfo.HasCleanroom)
                items.Add(new MyListItem
                {
                    Title = "Cleanroom", Info = instanceInfo.Cleanroom,
                    Logo = "pack://application:,,,/images/Blocks/Cleanroom.png"
                });
            if (instanceInfo.HasFabric)
                items.Add(new MyListItem
                {
                    Title = "Fabric", Info = instanceInfo.Fabric,
                    Logo = "pack://application:,,,/images/Blocks/Fabric.png"
                });
            if (instanceInfo.HasQuilt)
                items.Add(new MyListItem
                {
                    Title = "Quilt", Info = instanceInfo.Quilt, Logo = "pack://application:,,,/images/Blocks/Quilt.png"
                });
            if (instanceInfo.HasOptiFine)
                items.Add(new MyListItem
                {
                    Title = "OptiFine", Info = instanceInfo.OptiFine,
                    Logo = "pack://application:,,,/images/Blocks/GrassPath.png"
                });
            if (instanceInfo.HasLiteLoader)
                items.Add(new MyListItem
                    { Title = "LiteLoader", Info = Lang.Text("Instance.Overall.Info.Installed"), Logo = "pack://application:,,,/images/Blocks/Egg.png" });
            if (instanceInfo.HasLegacyFabric)
                items.Add(new MyListItem
                {
                    Title = "Legacy Fabric", Info = instanceInfo.LegacyFabric,
                    Logo = "pack://application:,,,/images/Blocks/Fabric.png"
                });
            if (instanceInfo.HasLabyMod)
                items.Add(new MyListItem
                {
                    Title = "LabyMod", Info = instanceInfo.LabyMod,
                    Logo = "pack://application:,,,/images/Blocks/LabyMod.png"
                });
            var wrapPanel = new WrapPanel { Margin = new Thickness(0, -5, -20, 7) };
            foreach (var item in items)
            {
                wrapPanel.Children.Add(item);
                wrapPanel.Children.Add(new TextBlock { Width = 2d });
            }

            PanInfo.Children.Clear();
            if (modpackCompItem is not null)
            {
                PanInfo.Children.Add(modpackCompItem);
                PanInfo.Children.Add(new TextBlock());
            }

            PanInfo.Children.Add(wrapPanel);
        })));
        instanceInfoLoader = new ModLoader.LoaderCombo<int>("Instance Info Loader", loaders) { show = false };
        instanceInfoLoader.Start();
    }

    #region 鍗＄墖锛氫釜鎬у寲

    // 瀹炰緥鍒嗙被
    private void ComboDisplayType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!(isLoad && ModAnimation.AniControlEnabled == 0))
            return;
        if (ComboDisplayType.SelectedIndex != 1)
        {
            // 鏀逛负涓嶉殣钘?
            try
            {
                // 鑻ヨ缃垎绫讳负鍙畨瑁?Mod锛屽垯鏄剧ず姝ｅ父鐨?Mod 绠＄悊椤甸潰
                States.Instance.CardType[PageInstanceLeft.McInstance.PathInstance] = ComboDisplayType.SelectedIndex;
                PageInstanceLeft.McInstance.displayType = (McInstanceCardType)States.Instance.CardType[PageInstanceLeft.McInstance.PathInstance];
                ModMain.frmInstanceLeft.RefreshModDisabled();

                ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", ""); // 瑕佹眰鍒锋柊缂撳瓨
                ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                    ModLoader.LoaderFolderRunType.ForceRun, 1, @"versions\");
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    $"淇敼瀹炰緥鍒嗙被澶辫触锛坽PageInstanceLeft.McInstance.Name}锛?,
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
            }

            Reload(); // 鏇存柊 鈥滄墦寮€ Mod 鏂囦欢澶光€?鎸夐挳
        }
        else
        {
            // 鏀逛负闅愯棌
            try
            {
                if (!States.Hint.HideGameInstance)
                {
                if (ModMain.MyMsgBox(
                        Lang.Text("Instance.Overall.Hide.ConfirmMessage"), Lang.Text("Instance.Overall.Hide.ConfirmTitle"), button2: Lang.Text("Common.Action.Cancel")) != 1)
                    {
                        ComboDisplayType.SelectedIndex = 0;
                        return;
                    }

                    States.Hint.HideGameInstance = true;
                }

                States.Instance.CardType[PageInstanceLeft.McInstance.PathInstance] =
                    (int)McInstanceCardType.Hidden;
                ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", ""); // 瑕佹眰鍒锋柊缂撳瓨
                ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                    ModLoader.LoaderFolderRunType.ForceRun, 1, @"versions\");
            }
            catch (Exception ex)
            {
                ModBase.Log(
                    ex,
                    $"闅愯棌瀹炰緥 {PageInstanceLeft.McInstance.Name} 澶辫触",
                    ModBase.LogLevel.Feedback,
                    userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
            }
        }
    }

    // 鏇存敼鎻忚堪
    private void BtnDisplayDesc_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var oldInfo = States.Instance.CustomInfo[PageInstanceLeft.McInstance.PathInstance];
            var newInfo = ModMain.MyMsgBoxInput(Lang.Text("Instance.Overall.Description.EditTitle"), Lang.Text("Instance.Overall.Description.EditMessage"), oldInfo,
                [], Lang.Text("Instance.Overall.Description.Default"));
            if (newInfo is not null && (oldInfo ?? "") != (newInfo ?? ""))
                States.Instance.CustomInfo[PageInstanceLeft.McInstance.PathInstance] = newInfo;
            PageInstanceLeft.McInstance = new McInstance(PageInstanceLeft.McInstance.Name).Load();
            Reload();
            ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                ModLoader.LoaderFolderRunType.ForceRun, 1, @"versions\");
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"瀹炰緥 {PageInstanceLeft.McInstance.Name} 鎻忚堪鏇存敼澶辫触",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    // 閲嶅懡鍚嶅疄渚?
    private void BtnDisplayRename_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            // 纭杈撳叆鐨勬柊鍚嶇О
            var oldName = PageInstanceLeft.McInstance.Name;
            var oldPath = PageInstanceLeft.McInstance.PathInstance;
            // 淇敼姝ら儴鍒嗙殑鍚屾椂淇敼蹇€熷畨瑁呯殑瀹炰緥鍚嶆娴?
            var newName = ModMain.MyMsgBoxInput(Lang.Text("Instance.Overall.Name.EditTitle"), "", oldName,
                [new FolderNameValidator(ModFolder.mcFolderSelected + "versions", ignoreCase: false)]);
            if (string.IsNullOrWhiteSpace(newName))
                return;
            var newPath = Path.Combine(ModFolder.mcFolderSelected, "versions", newName);
            // 鑾峰彇涓存椂涓棿鍚嶏紝浠ラ槻姝粎淇敼澶у皬鍐欑殑閲嶅懡鍚嶅け璐?
            var tempName = newName + "_temp";
            var tempPath = Path.Combine(ModFolder.mcFolderSelected, "versions", tempName);
            var isCaseChangedOnly = (newName.ToLower() ?? "") == (oldName.ToLower() ?? "");
            // 閲嶆柊鍔犺浇瀹炰緥 Json 淇℃伅锛岄伩鍏?HMCL 椤硅鍚堝苟
            JsonObject jsonObject;
            try
            {
                jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(PageInstanceLeft.McInstance.PathInstance +
                                                                       PageInstanceLeft.McInstance.Name + ".json"));
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, "閲嶅懡鍚嶈鍙?Json 鏃跺け璐?);
                jsonObject = PageInstanceLeft.McInstance.JsonObject;
            }

            // 閲嶅懡鍚嶄富鏂囦欢澶?
            FileSystem.RenameDirectory(oldPath, tempName);
            FileSystem.RenameDirectory(tempPath, newName);
            // 娓呯悊 ini 缂撳瓨
            ModBase.IniClearCache(Path.Combine(PageInstanceLeft.McInstance.PathIndie, "options.txt"));
            // 閲嶅懡鍚?Jar 鏂囦欢涓?natives 鏂囦欢澶?
            // 涓嶈兘杩涜閬嶅巻閲嶅懡鍚嶏紝鍚﹀垯鍦ㄥ疄渚嬪悕寰堢煭鐨勬椂鍊欏鏄撹浼ゅ叾浠栨枃浠讹紙Meloong-Git/#6443锛?
            if (Directory.Exists(Path.Combine(newPath, $"{oldName}-natives")))
            {
                if (isCaseChangedOnly)
                {
                    FileSystem.RenameDirectory(Path.Combine(newPath, $"{oldName}-natives"), $"{oldName}natives_temp");
                    FileSystem.RenameDirectory(Path.Combine(newPath, $"{oldName}-natives_temp"), $"{newName}-natives");
                }
                else
                {
                    ModBase.DeleteDirectory(Path.Combine(newPath, $"{newName}-natives"));
                    FileSystem.RenameDirectory(Path.Combine(newPath, $"{oldName}-natives"), $"{newName}-natives");
                }
            }

            if (File.Exists(Path.Combine(newPath, $"{oldName}.jar")))
            {
                if (isCaseChangedOnly)
                {
                    FileSystem.RenameFile(Path.Combine(newPath, $"{oldName}.jar"), $"{oldName}_temp.jar");
                    FileSystem.RenameFile(Path.Combine(newPath, $"{oldName}_temp.jar"), $"{newName}.jar");
                }
                else
                {
                    File.Delete(Path.Combine(newPath, $"{newName}.jar"));
                    FileSystem.RenameFile(Path.Combine(newPath, $"{oldName}.jar"), $"{newName}.jar");
                }
            }

            // 鏇挎崲瀹炰緥璁剧疆鏂囦欢涓殑璺緞
            if (File.Exists(Path.Combine(newPath, "PCL", "Setup.ini")))
                ModBase.WriteFile(Path.Combine(newPath, "PCL", "Setup.ini"),
                    ModBase.ReadFile(Path.Combine(newPath, "PCL", "Setup.ini")).Replace(oldPath, newPath));
            // 鏇存敼宸查€変腑鐨勫疄渚?
            if ((ModBase.ReadIni(ModFolder.mcFolderSelected + "PCL.ini", "Version") ?? "") == (oldName ?? ""))
                ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", newName);
            // 鍐欏叆瀹炰緥 Json锛屽苟鍒犻櫎鏃х殑 Json
            try
            {
                jsonObject["id"] = newName;
                ModBase.WriteFile(Path.Combine(newPath, $"{newName}.json"), jsonObject.ToString());
                if (!isCaseChangedOnly)
                    File.Delete(Path.Combine(newPath, $"{oldName}.json"));
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, "閲嶅懡鍚嶅疄渚?Json 澶辫触");
            }

            // 鍒锋柊涓庢彁绀?
            HintService.Hint(Lang.Text("Instance.Overall.Name.RenameSuccess"), HintType.Success);
            PageInstanceLeft.McInstance = new McInstance(newName).Load();
            if (ModInstanceList.McMcInstanceSelected is not null &&
                ModInstanceList.McMcInstanceSelected.Equals(PageInstanceLeft.McInstance))
                ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", newName);
            Reload();
            ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                ModLoader.LoaderFolderRunType.ForceRun, 1, @"versions\");
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閲嶅懡鍚嶅疄渚嬪け璐?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    // 瀹炰緥鍥炬爣
    private void ComboDisplayLogo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!(isLoad && ModAnimation.AniControlEnabled == 0))
            return;
        // 閫夋嫨 鑷畾涔?鏃朵慨鏀瑰浘鐗?
        try
        {
            if (ReferenceEquals(ComboDisplayLogo.SelectedItem, ItemDisplayLogoCustom))
            {
                var fileName = SystemDialogs.SelectFile(Lang.Text("Instance.Overall.Icon.SelectFile.Filter"), Lang.Text("Instance.Overall.Icon.SelectFile.Title"));
                if (string.IsNullOrEmpty(fileName))
                {
                    Reload(); // 杩樺師閫夐」
                    return;
                }

                ModBase.CopyFile(fileName, PageInstanceLeft.McInstance.PathInstance + @"PCL\Logo.png");
            }
            else
            {
                File.Delete(PageInstanceLeft.McInstance.PathInstance + @"PCL\Logo.png");
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"鏇存敼鑷畾涔夊疄渚嬪浘鏍囧け璐ワ紙{PageInstanceLeft.McInstance.Name}锛?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }

        // 杩涜鏇存敼
        try
        {
            string newLogo = ((MyComboBoxItem)ComboDisplayLogo.SelectedItem).Tag?.ToString();
            States.Instance.LogoPath[PageInstanceLeft.McInstance.PathInstance] = newLogo;
            States.Instance.IsLogoCustom[PageInstanceLeft.McInstance.PathInstance] = !string.IsNullOrEmpty(newLogo);
            // 鍒锋柊鏄剧ず
            ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", ""); // 瑕佹眰鍒锋柊缂撳瓨
            PageInstanceLeft.McInstance = new McInstance(PageInstanceLeft.McInstance.Name).Load();
            Reload();
            ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                ModLoader.LoaderFolderRunType.ForceRun, 1, @"versions\");
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"鏇存敼瀹炰緥鍥炬爣澶辫触锛坽PageInstanceLeft.McInstance.Name}锛?,
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    // 鏀惰棌澶?
    private void BtnDisplayStar_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            States.Instance.Starred[PageInstanceLeft.McInstance.PathInstance] = !PageInstanceLeft.McInstance.IsStar;
            PageInstanceLeft.McInstance = new McInstance(PageInstanceLeft.McInstance.Name).Load();
            Reload();
            ModInstanceList.mcInstanceListForceRefresh = true;
            ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                ModLoader.LoaderFolderRunType.ForceRun, 1, @"versions\");
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"瀹炰緥 {PageInstanceLeft.McInstance.Name} 鏀惰棌鐘舵€佹洿鏀瑰け璐?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    #endregion

    #region 鍗＄墖锛氬揩鎹锋柟寮?

    // 瀹炰緥鏂囦欢澶?
    private void BtnFolderVersion_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        OpenVersionFolder(PageInstanceLeft.McInstance);
    }

    public static void OpenVersionFolder(McInstance version)
    {
        ModBase.OpenExplorer(version.PathInstance);
    }

    // 瀛樻。鏂囦欢澶?
    private void BtnFolderSaves_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        var folderPath = PageInstanceLeft.McInstance.PathIndie + @"saves\";
        Directory.CreateDirectory(folderPath);
        ModBase.OpenExplorer(folderPath);
    }

    // Mod 鏂囦欢澶?
    private void BtnFolderMods_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        var folderPath = PageInstanceLeft.McInstance.PathIndie + @"mods\";
        Directory.CreateDirectory(folderPath);
        ModBase.OpenExplorer(folderPath);
    }

    #endregion

    #region 鍗＄墖锛氱鐞?

    // 瀵煎嚭鍚姩鑴氭湰
    private void BtnManageScript_Click(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        try
        {
            // 寮圭獥瑕佹眰鎸囧畾鑴氭湰鐨勪繚瀛樹綅缃?
            var savePath = SystemDialogs.SelectSaveFile(Lang.Text("Instance.Overall.Script.SelectSaveTitle"), "鍚姩 " + PageInstanceLeft.McInstance.Name + ".bat",
                Lang.Text("Instance.Overall.Script.FileFilter"));
            if (string.IsNullOrEmpty(savePath))
                return;
            // 妫€鏌ヤ腑鏂紙绛夌帺瀹堕€夊畬寮圭獥鎸囦笉瀹氫换鍔″氨缁撴潫浜嗗憿鈥︹€︼級
            if (ModLaunch.mcLaunchLoader.State == ModBase.LoadState.Loading)
            {
                HintService.Hint(Lang.Text("Instance.Overall.Script.WaitForLaunchTask"), HintType.Error);
                return;
            }

            // 鐢熸垚鑴氭湰
            if (ModLaunch.McLaunchStart(new ModLaunch.McLaunchOptions
                    { SaveBatch = savePath, instance = PageInstanceLeft.McInstance }))
            {
                if (ModProfile.selectedProfile.Type == ModLaunch.McLoginType.Legacy)
                    HintService.Hint(Lang.Text("Instance.Overall.Script.Exporting"));
                else
                    HintService.Hint(Lang.Text("Instance.Overall.Script.ExportingWarning"));
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"瀵煎嚭鍚姩鑴氭湰澶辫触锛坽PageInstanceLeft.McInstance.Name}锛?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    // 琛ュ叏鏂囦欢
    private void BtnManageCheck_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            // 蹇界暐鏂囦欢妫€鏌ユ彁绀?
            if ((bool)ModLibrary.ShouldIgnoreFileCheck(PageInstanceLeft.McInstance))
            {
                HintService.Hint(Lang.Text("Instance.Overall.Repair.DisableVerificationHint"));
                return;
            }

            // 閲嶅浠诲姟妫€鏌?
            var taskName = PageInstanceLeft.McInstance.Name + " " + Lang.Text("Instance.Overall.Repair.TaskName");
            foreach (var OngoingLoader in ModLoader.loaderTaskbar)
            {
                if ((OngoingLoader.name ?? "") != (taskName ?? ""))
                    continue;
                HintService.Hint(Lang.Text("Instance.Overall.Repair.Processing"), HintType.Error);
                return;
            }

            // 鍚姩
            var loader = new ModLoader.LoaderCombo<string>(taskName,
                ModDownload.DlClientFix(PageInstanceLeft.McInstance, true,
                    ModDownload.AssetsIndexExistsBehaviour.AlwaysDownload));
            loader.OnStateChanged = _ =>
            {
                switch (loader.State)
                {
                    case ModBase.LoadState.Finished:
                    {
                        HintService.Hint(
                            Lang.Text("Instance.Overall.Repair.Success.WithTaskName", taskName), HintType.Success);
                        break;
                    }
                    case ModBase.LoadState.Failed:
                    {
                        HintService.Hint(
                            Lang.Text("Instance.Overall.Repair.Failed.WithDetail", taskName, loader.Error.ToString()),
                            HintType.Error);
                        break;
                    }
                    case ModBase.LoadState.Aborted:
                    {
                        HintService.Hint(
                            Lang.Text("Instance.Overall.Repair.Cancelled.WithTaskName", taskName));
                        break;
                    }
                }
            };
            loader.Start(PageInstanceLeft.McInstance.Name);
            ModLoader.LoaderTaskbarAdd(loader);
            ModMain.frmMain.BtnExtraDownload.ShowRefresh();
            ModMain.frmMain.BtnExtraDownload.Ribble();
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"灏濊瘯琛ュ叏鏂囦欢澶辫触锛坽PageInstanceLeft.McInstance.Name}锛?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    // 閲嶇疆
    private void BtnManageRestore_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var currentVersion = PageInstanceLeft.McInstance.Info;
            if (!(currentVersion.Drop == 99) &&
                McVersionComparer.CompareVersion(currentVersion.VanillaName, "1.5.2") == -1 && currentVersion.HasForge)
            {
                HintService.Hint(Lang.Text("Instance.Overall.Reset.NotSupported"));
                return;
            }

            if (currentVersion.HasQuilt)
            {
                HintService.Hint(Lang.Text("Instance.Overall.Reset.QuiltUnsupported"));
                return;
            }

            // 纭鎿嶄綔
            if (ModMain.MyMsgBox(
                    Lang.Text("Instance.Overall.Reset.ConfirmMessage", PageInstanceLeft.McInstance.Name), Lang.Text("Instance.Overall.Reset.ConfirmTitle"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel")) == 2)
                return;

            // 澶囦唤瀹炰緥鏍稿績鏂囦欢
            ModBase.CopyFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".json",
                PageInstanceLeft.McInstance.PathInstance + @"PCLInstallBackups\" + PageInstanceLeft.McInstance.Name +
                ".json");
            ModBase.CopyFile(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name + ".jar",
                PageInstanceLeft.McInstance.PathInstance + @"PCLInstallBackups\" + PageInstanceLeft.McInstance.Name +
                ".jar");
            // 鎻愪氦瀹夎鐢宠
            var request = new ModDownloadLib.McInstallRequest
            {
                targetInstanceName = PageInstanceLeft.McInstance.Name,
                targetInstanceFolder = $@"{ModFolder.mcFolderSelected}versions\{PageInstanceLeft.McInstance.Name}\",
                minecraftName = currentVersion.VanillaName,
                optiFineEntry = currentVersion.HasOptiFine
                    ? new ModDownload.DlOptiFineListEntry
                    {
                        Inherit = currentVersion.VanillaName,
                        DisplayName = currentVersion.VanillaName + " " + currentVersion.OptiFine
                    }
                    : null,
                forgeEntry = currentVersion.HasForge
                    ? new ModDownload.DlForgeVersionEntry(currentVersion.Forge, null, currentVersion.VanillaName)
                        { Category = "installer" }
                    : null,
                forgeVersion = currentVersion.HasForge ? currentVersion.Forge : null,
                neoForgeVersion = currentVersion.HasNeoForge ? currentVersion.NeoForge : null,
                cleanroomVersion = currentVersion.HasCleanroom ? currentVersion.Cleanroom : null,
                fabricVersion = currentVersion.HasFabric ? currentVersion.Fabric : null,
                liteLoaderEntry = currentVersion.HasLiteLoader
                    ? new ModDownload.DlLiteLoaderListEntry { Inherit = currentVersion.VanillaName }
                    : null,
                legacyFabricVersion = currentVersion.HasLegacyFabric ? currentVersion.LegacyFabric : null
            };
            // .MinecraftJson = CurrentVersion.McName,
            if (!ModDownloadLib.McInstall(request, Lang.Text("Common.Action.Reset")))
                return;
            ModMain.frmMain.PageChange(new FormMain.PageStackData { page = FormMain.PageType.Launch });
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"閲嶇疆瀹炰緥 {PageInstanceLeft.McInstance.Name} 澶辫触",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    // 娴嬭瘯娓告垙
    private void BtnManageTest_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            ModLaunch.McLaunchStart(new ModLaunch.McLaunchOptions
                { instance = PageInstanceLeft.McInstance, IsTest = true });
            ModMain.frmMain.PageChange(FormMain.PageType.Launch);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "娴嬭瘯娓告垙澶辫触",
                ModBase.LogLevel.Feedback,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    // 鍒犻櫎瀹炰緥
    // 淇敼姝や唬鐮佹椂锛屽悓鏃朵慨鏀?PageSelectRight 涓殑浠ｇ爜
    private void BtnManageDelete_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            var isIsolatedInstance =
                PageInstanceLeft.McInstance.state != McInstanceState.Error &&
                !string.Equals(
                    PageInstanceLeft.McInstance.PathIndie,
                    ModFolder.mcFolderSelected,
                    StringComparison.OrdinalIgnoreCase
                );

            var confirmMessageKey = (isIsolatedInstance, isShiftPressed) switch
            {
                (true, true) => "Instance.Overall.Delete.ConfirmMessageIsolatedPermanent",
                (true, false) => "Instance.Overall.Delete.ConfirmMessageIsolated",
                (false, true) => "Instance.Overall.Delete.ConfirmMessagePermanent",
                (false, false) => "Instance.Overall.Delete.ConfirmMessage"
            };

            var confirmResult = ModMain.MyMsgBox(
                Lang.Text(confirmMessageKey, PageInstanceLeft.McInstance.Name),
                Lang.Text("Instance.Overall.Delete.ConfirmTitle"),
                button2: Lang.Text("Common.Action.Cancel"),
                isWarn: isIsolatedInstance || isShiftPressed
            );

            switch (confirmResult)
            {
                case 1:
                {
                    var instancePath = PageInstanceLeft.McInstance.PathInstance;
                    var instanceName = PageInstanceLeft.McInstance.Name;
                    ModBase.IniClearCache(Path.Combine(PageInstanceLeft.McInstance.PathIndie, "options.txt"));
                    ((DynamicCacheConfigStorage)ConfigService.GetProvider(ConfigSource.GameInstance)).InvalidateCache(
                        instancePath);
                    if (isShiftPressed)
                    {
                        ModBase.DeleteDirectory(instancePath);
                        HintService.Hint(Lang.Text("Instance.Overall.Delete.PermanentSuccess", instanceName),
                            HintType.Success);
                    }
                    else
                    {
                        FileSystem.DeleteDirectory(instancePath, UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin);
                        HintService.Hint(Lang.Text("Instance.Overall.Delete.RecycleBinSuccess", instanceName),
                            HintType.Success);
                    }

                    break;
                }
                case 2:
                {
                    return;
                }
            }

            ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected,
                ModLoader.LoaderFolderRunType.ForceRun, 1, @"versions\");
            ModMain.frmMain.PageBack();
        }
        catch (OperationCanceledException ex)
        {
            ModBase.Log(ex, "鍒犻櫎瀹炰緥 " + PageInstanceLeft.McInstance.Name + " 琚富鍔ㄥ彇娑?);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"鍒犻櫎瀹炰緥 {PageInstanceLeft.McInstance.Name} 澶辫触",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Overall.Error.OperationFailed"));
        }
    }

    // 淇ˉ鏍稿績
    private void BtnManagePatch_Click(object sender, MouseButtonEventArgs e)
    {
        switch (ModMain.MyMsgBox(
                    Lang.Text("Instance.Overall.Patch.ConfirmMessage", PageInstanceLeft.McInstance.Name),
                    Lang.Text("Instance.Overall.Patch.ConfirmTitle"), button2: Lang.Text("Common.Action.Cancel")))
        {
            case 1:
            {
                var userInput = SystemDialogs.SelectFile(Lang.Text("Instance.Overall.Patch.SelectFile.Filter"), Lang.Text("Instance.Overall.Patch.SelectFile.Title"));
                if (userInput is null || string.IsNullOrWhiteSpace(userInput))
                    return;
                HintService.Hint(Lang.Text("Instance.Overall.Patch.Patching"));
                ModBase.RunInNewThread(() =>
                {
                    var core = new GameCore(PageInstanceLeft.McInstance.PathInstance + PageInstanceLeft.McInstance.Name +
                                            ".jar");
                    core.AddToCore(userInput);
                    HintService.Hint(Lang.Text("Instance.Overall.Patch.Success"), HintType.Success);
                    Config.Instance.DisableAssetVerifyV2[PageInstanceLeft.McInstance.PathInstance] = true;
                });
                break;
            }
            case 2:
            {
                return;
            }
        }
    }

    #endregion
}
