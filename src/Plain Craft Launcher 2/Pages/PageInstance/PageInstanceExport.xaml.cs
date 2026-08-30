using System.IO;
using System.IO.Compression;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using DotNet.Globbing;
using PCL.Core.App;
using PCL.Core.UI;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public class ExportOption : AvaloniaObject
{
    public static readonly AvaloniaProperty TitleProperty = AvaloniaProperty.Register(
        nameof(Title), typeof(string), typeof(ExportOption)
    );

    public static readonly AvaloniaProperty DescriptionProperty = AvaloniaProperty.Register(
        nameof(Description), typeof(string), typeof(ExportOption)
    );

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string Rules { get; set; }

    /// <summary>
    ///     濡傛灉 Rules 涓虹┖锛屽垯鏍规嵁 ShowRules 鐨勫唴瀹瑰垽鏂槸鍚﹀簲璇ユ樉绀鸿繖涓閫夋銆?
    ///     濡傛灉 ShowRules 涔熶负绌猴紝鍒欏缁堟樉绀恒€?
    /// </summary>
    public string ShowRules { get; set; }

    public bool DefaultChecked { get; set; }
    public bool RequireModLoader { get; set; }
    public bool RequireOptiFine { get; set; }
    public bool RequireModLoaderOrOptiFine { get; set; }
}

public partial class PageInstanceExport : IRefreshable
{
    private string currentVersion = "";

    public PageInstanceExport()
    {
        InitializeComponent();
        Loaded += (_, _) => PageInstanceExport_Loaded();
        CardOptions.MouseLeftButtonDown += CardOptions_MouseLeftButtonDown;
        BtnAdvancedExport.Click += ExportConfig;
        BtnAdvancedImport.Click += ImportConfig;
        BtnExport.Click += StartExport;
        TextExportName.GotFocus += TextExportName_GotFocus;
        CheckAdvancedModrinth.Change += CheckAdvancedModrinth_Change;
        CheckAdvancedInclude.Change += CheckAdvancedInclude_Change;
    }

    void IRefreshable.Refresh()
    {
        RefreshAll();
    }

    private void PageInstanceExport_Loaded()
    {
        ModAnimation.AniControlEnabled += 1;
        if ((currentVersion ?? "") != (PageInstanceLeft.McInstance.PathInstance ?? ""))
            RefreshAll(); // 鍒囨崲鍒颁簡鍙︿竴涓疄渚嬶紝閲嶇疆椤甸潰
        ModAnimation.AniControlEnabled -= 1;
    }

    public void RefreshAll()
    {
        ModBase.Log("[Export] 鍒锋柊瀵煎嚭椤甸潰");
        HintOptiFine.Visibility =
            PageInstanceLeft.McInstance.Info.HasOptiFine ? Visibility.Visible : Visibility.Collapsed;
        currentVersion = PageInstanceLeft.McInstance.PathInstance;
        TextExportName.Text = "";
        TextExportName.HintText = PageInstanceLeft.McInstance.Name;
        TextExportVersion.Text = "";
        TextExportVersion.HintText = "1.0.0";
        CheckAdvancedInclude.Checked = false;
        CheckAdvancedModrinth.Checked = false;
        GetExportOption(CheckOptionsBasic).Description = PageInstanceLeft.McInstance.GetDefaultDescription();
        ResetConfigOverrides();
        ReloadAllSubOptions();
        RefreshAllOptionsUI();
        PanBack.ScrollToHome();
    }

    // 鑷姩濉啓鏁村悎鍖呭悕绉?
    private void TextExportName_GotFocus(object sender, RoutedEventArgs routedEventArgs)
    {
        if (string.IsNullOrEmpty(TextExportName.Text))
        {
            TextExportName.Text = TextExportName.HintText;
            TextExportName.SelectionStart = TextExportName.Text.Length;
        }
    }

    // 鍕鹃€?Modrinth 涓婁紶妯″紡鏃讹紝绂佹鎵撳寘 PCL
    private void CheckAdvancedModrinth_Change(object sender, bool user)
    {
        if (CheckAdvancedModrinth.Checked == true)
            CheckOptionsPcl.Checked = false;
        CheckOptionsPcl.IsEnabled = (bool)!CheckAdvancedModrinth.Checked;
    }

    // 鍕鹃€?鍏朵粬鏂囦欢澶?鏃讹紝鍚屾鍕鹃€?鍙栨秷鎵€鏈夊瓙閫夐」
    private void CheckOptionsOtherFolders_Change(object sender, bool user)
    {
        if (!user) return;
        foreach (var child in PanOptionsOtherFolders.Children)
            if (child is MyCheckBox childBox)
                childBox.Checked = CheckOptionsOtherFolders.Checked;
    }

    // 鍕鹃€夋墦鍖呰祫婧愭枃浠舵椂锛岀姝㈠紑鍚?Modrinth 涓婁紶妯″紡
    private void CheckAdvancedInclude_Change(object sender, bool user)
    {
        if (CheckAdvancedInclude.Checked == true)
            CheckAdvancedModrinth.Checked = false;
        CheckAdvancedModrinth.IsEnabled = (bool)!CheckAdvancedInclude.Checked;
    }

    #region 瀛愰€夐」

    private readonly string[] subOptionBlackList = new[] { "Quark Programmer Art.zip", "+ EuphoriaPatches_" };

    /// <summary>
    ///     鍔ㄦ€佺敓鎴愬瓙鏂囦欢澶逛笅鐨勯€夐」锛屼緥濡傝祫婧愬寘銆佸瓨妗ｇ瓑銆?
    /// </summary>
    private void ReloadAllSubOptions()
    {
        ReloadSubOptions(PanOptionsResourcePacks, true, true, "resourcepacks", "texturepacks");
        ReloadSubOptions(PanOptionsSaves, false, true, "saves");
        ReloadSubOptions(PanOptionsShaderPacks, true, true, "shaderpacks");
        ReloadOtherFolders();
    }

    /// <summary>
    ///     鎵弿瀹炰緥鏍圭洰褰曚笅鏈宸叉湁閫夐」瑕嗙洊鐨勬枃浠跺す锛岀敓鎴愮嫭绔嬬殑澶嶉€夋銆?
    /// </summary>
    private void ReloadOtherFolders()
    {
        PanOptionsOtherFolders.Children.Clear();

        var pathIndie = PageInstanceLeft.McInstance.PathIndie;
        var rootDir = new DirectoryInfo(pathIndie);
        if (!rootDir.Exists)
        {
            CheckOptionsOtherFolders.Visibility = Visibility.Collapsed;
            return;
        }

        var coveredFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // 妯＄粍
            "mods", "coremods", "lib",
            // 鏁村悎鍖呴噸瑕佹暟鎹?
            "addons", "multiblocked", "modpack-update-checker", "global_packs",
            "global_resource_packs", "global_data_packs", "optional_data_packs", "maps",
            "mods-resourcepacks", "matmos", "resource_assorts",
            "patchouli_books", "datapacks",
            "openloader", "worldshape", "resources", "scripts", "structures",
            "fontfiles", "oresources", "packmenu", "craftpresence", "pointblanks",
            // 妯＄粍璁剧疆
            "config", "defaultconfigs", "journeymap", "local", "essential", "gg.essential.mod",
            "CustomSkinLoader",
            // 鍦板浘
            "xaero", "XaeroWaypoints", "XaeroWorldMap",
            // 璧勬簮鍖?
            "resourcepacks", "texturepacks",
            // 鍏夊奖
            "shaderpacks",
            // 鎴浘 / 缁撴瀯 / 褰曞儚
            "screenshots", "schematics",
            "replay_recordings", "replay_videos",
            // 瀛樻。 / 璁剧疆鏂囦欢澶?
            "saves", "configureddefaults",
            // 濮嬬粓璺宠繃锛堝ぇ閲忔枃浠舵垨鏃犵敤缂撳瓨锛?
            "assets", "versions", "libraries", "structureCacheV1",
            ".fabric", ".git", "avatar-cache", "cosmetic-cache",
            // PCL 鍗曠嫭澶勭悊
            "PCL",
        };

        var coveredPrefixes = new[] { "kubejs", "template" };
        var coveredSuffixes = new[] { "-natives" };

        foreach (var subDir in rootDir.EnumerateDirectories())
        {
            if (coveredFolders.Contains(subDir.Name))
                continue;
            if (coveredPrefixes.Any(p => subDir.Name.StartsWithF(p)))
                continue;
            if (coveredSuffixes.Any(s => subDir.Name.EndsWithF(s, true)))
                continue;

            PanOptionsOtherFolders.Children.Add(new MyCheckBox
            {
                Tag = new ExportOption
                {
                    Title = subDir.Name,
                    DefaultChecked = false,
                    Rules = ModBase.EscapeLikePattern($"{subDir.Name}/")
                }
            });
        }

        CheckOptionsOtherFolders.Visibility = PanOptionsOtherFolders.Children.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ReloadSubOptions(StackPanel panel, bool acceptCompressedFile, bool acceptFolder,
        params string[] folders)
    {
        panel.Children.Clear();
        foreach (var Folder in folders)
        {
            var targetFolder = new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie + Folder);
            if (!targetFolder.Exists)
                continue;
            // 鏌ユ壘鏂囦欢澶逛笅鐨勫搴旈」
            if (acceptCompressedFile)
                foreach (var File in targetFolder.EnumerateFiles("*.zip").Concat(targetFolder.EnumerateFiles("*.rar")))
                {
                    if (subOptionBlackList.Any(b => File.Name.ContainsF(b)))
                        continue;
                    panel.Children.Add(new MyCheckBox
                    {
                        Tag = new ExportOption
                        {
                            Title = File.Name, DefaultChecked = true,
                            Rules = ModBase.EscapeLikePattern($"{Folder}/{File.Name}")
                        }
                    });
                    if (Folder == "shaderpacks") // 澶勭悊鍏夊奖鍖呯殑閰嶇疆鏂囦欢
                    {
                        var shaderConfig = new FileInfo(Path.Combine(File.Directory.FullName,
                            $"{File.Name}.txt"));
                        if (shaderConfig.Exists)
                            panel.Children.Add(new MyCheckBox
                            {
                                Margin = new Thickness(30, 0, 0, 0),
                                Tag = new ExportOption
                                {
                                    Title = $"{shaderConfig.Name}", DefaultChecked = true,
                                    Description = Lang.Text("Instance.Export.Config.ShaderConfigSuffix"),
                                    Rules = ModBase.EscapeLikePattern($"{Folder}/{shaderConfig.Name}")
                                }
                            });
                    }
                }

            if (acceptFolder)
                foreach (var SubFolder in targetFolder.EnumerateDirectories().OrderByDescending(f => f.LastWriteTime))
                {
                    if (subOptionBlackList.Any(b => SubFolder.Name.ContainsF(b)))
                        continue;
                    if (!SubFolder.EnumerateFileSystemInfos().Any())
                        continue;
                    var newCheckBox = new MyCheckBox
                    {
                        Tag = new ExportOption
                        {
                            Title = SubFolder.Name, DefaultChecked = true,
                            Rules = ModBase.EscapeLikePattern($"{Folder}/{SubFolder.Name}/")
                        }
                    };
                    if (ReferenceEquals(panel, PanOptionsSaves))
                        GetExportOption(newCheckBox).Description =
                            Lang.Date(SubFolder.LastWriteTime, "g");
                    panel.Children.Add(newCheckBox);
                    if (Folder == "shaderpacks") // 澶勭悊鏂囦欢澶瑰舰寮忓厜褰卞寘鐨勯厤缃枃浠?
                    {
                        var shaderConfig = new FileInfo(Path.Combine(targetFolder.FullName,
                            $"{SubFolder.Name}.txt"));
                        if (shaderConfig.Exists)
                            panel.Children.Add(new MyCheckBox
                            {
                                Margin = new Thickness(30, 0, 0, 0),
                                Tag = new ExportOption
                                {
                                    Title = $"{shaderConfig.Name}", DefaultChecked = true,
                                    Description = Lang.Text("Instance.Export.Config.ShaderConfigSuffix"),
                                    Rules = ModBase.EscapeLikePattern($"{Folder}/{shaderConfig.Name}")
                                }
                            });
                    }
                }
        }
    }

    #endregion

    #region 閫夐」

    /// <summary>
    ///     閲嶆柊纭鏄惁搴旇鏄剧ず姣忎釜閫夐」锛屽苟灏?ExportOption 鍚屾鍒?UI銆?
    /// </summary>
    private void RefreshAllOptionsUI()
    {
        // 棰勫厛褰掔撼鎵€鏈夎嚦澶氫簩绾х殑鏂囦欢/鏂囦欢澶?
        var allEntries = new List<string>();

        bool IsValidDirectory(DirectoryInfo folder)
        {
            try
            {
                return folder.Exists && folder.EnumerateFileSystemInfos()
                    .Any(i => !subOptionBlackList.Any(b => i.Name.ContainsF(b)));
            }
            catch
            {
                return false;
            }
        }

        ; // 妫€鏌ユ枃浠跺す涓嶄负绌?
        // 涓€鑸槸鐢变簬鏃犳硶璁块棶锛屾垨鏄竴涓寚鍚戝凡涓嶅瓨鍦ㄧ殑鏂囦欢澶圭殑閾炬帴锛堜緥濡備娇鐢?mklink 鍒涢€犵殑 resource 鏂囦欢澶归摼鎺ワ級
        var pathInfo = new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie);
        allEntries.AddRange(pathInfo.EnumerateFiles().Select(f => f.Name));
        foreach (var SubFolder in pathInfo.EnumerateDirectories().Where(IsValidDirectory))
        {
            allEntries.Add($@"{SubFolder.Name}\");
            allEntries.AddRange(SubFolder.EnumerateFiles().Select(f => $@"{SubFolder.Name}\{f.Name}"));
            allEntries.AddRange(SubFolder.EnumerateDirectories().Where(IsValidDirectory)
                .Select(d => $@"{SubFolder.Name}\{d.Name}\"));
        }

        ModBase.Log($"[Export] 鍏卞彂鐜?{allEntries.Count} 涓彲琛岀殑浜岀骇鏂囦欢/鏂囦欢澶?);

        // 纭閫夐」鏄惁搴旇琚樉绀?
        bool IsVisible(ExportOption targetOption)
        {
            // 妫€鏌ラ渶瑕?OptiFine 鎴?Mod 鍔犺浇鍣?
            if (targetOption.RequireOptiFine && !PageInstanceLeft.McInstance.Info.HasOptiFine)
                return false;
            if (targetOption.RequireModLoader && !PageInstanceLeft.McInstance.Modable)
                return false;
            if (targetOption.RequireModLoaderOrOptiFine && !PageInstanceLeft.McInstance.Info.HasOptiFine &&
                !PageInstanceLeft.McInstance.Modable)
                return false;
            // 绮楃暐妫€鏌ユ槸鍚﹀彲鑳芥湁绗﹀悎瑙勫垯鐨勬枃浠?鏂囦欢澶?
            return StandardizeLines((targetOption.Rules ?? targetOption.ShowRules).Split('|'), true).Any(rule =>
            {
                if (rule.StartsWithF("!"))
                    return false; // 鍙湅姝ｅ悜瑙勫垯
                // 妫€鏌ュ墠涓ょ骇
                try
                {
                    if (allEntries.Any(entry => LikeString(entry, rule)))
                        return true;
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        $"閿欒鐨勮鍒欙細{rule}",
                        ModBase.LogLevel.Hint,
                        userSummary: Lang.Text("Instance.Export.Error.OperationFailed"));
                    return false;
                }

                // 绮楃暐妫€鏌ユ墍鏈夌骇
                rule = rule.Trim("*?".ToCharArray());
                if (rule.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Count() >= 3)
                {
                    if (rule.EndsWithF(@"\"))
                        return IsValidDirectory(new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie + rule)); // 鏂囦欢澶规湁鏁?

                    return File.Exists(PageInstanceLeft.McInstance.PathIndie + rule);
                    // 鏂囦欢鏈夋晥
                }

                return false;
            });
        }

        ;
        // 閫愪釜妫€鏌ラ€夐」
        foreach (var CheckBox in GetAllOptions(true))
        {
            var targetOption = GetExportOption(CheckBox);
            // 鍚嶇О涓庣畝浠?
            CheckBox.Inlines.Clear();
            CheckBox.Inlines.Add(new Run(targetOption.Title));
            if (!string.IsNullOrEmpty(targetOption.Description))
                CheckBox.Inlines.Add(new Run("   " + targetOption.Description) { Foreground = ThemeManager.colorGray5 });
            // 鍙鎬с€侀粯璁ゅ嬀閫?
            if (string.IsNullOrEmpty(targetOption.Rules) && string.IsNullOrEmpty(targetOption.ShowRules))
            {
                CheckBox.Visibility = Visibility.Visible;
                CheckBox.Checked = targetOption.DefaultChecked;
            }
            else
            {
                var pass = IsVisible(targetOption);
                CheckBox.Visibility = pass ? Visibility.Visible : Visibility.Collapsed;
                CheckBox.Checked = targetOption.DefaultChecked && pass;
            }
        }
    }

    /// <summary>
    ///     瀵规枃鏈杩涜鏍囧噯鍖栧鐞嗭紝浠ヤ究浣跨敤 Like 杩涜鍖归厤銆?
    /// </summary>
    private IEnumerable<string> StandardizeLines(IEnumerable<string> raw, bool addSuffixStarToFolderPath)
    {
        foreach (var IgnoreLineRaw in raw)
        {
            var ignoreLine = IgnoreLineRaw;
            ignoreLine = ignoreLine.Trim();
            if (string.IsNullOrEmpty(ignoreLine) || ignoreLine.StartsWithF("#") || ignoreLine.StartsWithF("="))
                continue;
            ignoreLine = ignoreLine.Replace("/", @"\");
            yield return ignoreLine + (ignoreLine.EndsWithF(@"\") && addSuffixStarToFolderPath ? "**" : "");
        }
    }

    /// <summary>
    ///     鑾峰彇鎵€鏈夊彲浣滀负閫夐」鐨?CheckBox銆?
    /// </summary>
    private IEnumerable<MyCheckBox> GetAllOptions(bool includeHidden)
    {
        foreach (var Element in PanOptions.Children)
        {
            if (!includeHidden &&
                ((Control)Element).Visibility != Visibility.Visible)
                continue;
            if (Element is MyCheckBox)
                yield return (MyCheckBox)Element;
            else if (Element is StackPanel)
                foreach (var SubElement in ((StackPanel)Element).Children)
                {
                    if (!includeHidden && ((Control)SubElement).Visibility != Visibility.Visible)
                        continue;
                    if (SubElement is MyCheckBox)
                        yield return (MyCheckBox)SubElement;
                }
        }
    }

    /// <summary>
    ///     鑾峰彇璇?CheckBox 瀵瑰簲鐨?ExportOption銆?
    /// </summary>
    private ExportOption GetExportOption(MyCheckBox checkBox)
    {
        return (ExportOption)checkBox.Tag;
    }

    #endregion

    #region 閰嶇疆鏂囦欢

    private const string sperator = "==============================================================";

    // ================ 瀵煎嚭鍐呭娈?================

    /// <summary>
    ///     浠庨厤缃枃浠朵腑璇诲彇鐨勮鍒欍€?
    ///     濡傛灉涓嶄负 Nothing锛屽垯浼氳鍐欏綋鍓嶅嬀閫夌殑瑙勫垯骞剁鐢ㄥ搴?UI銆?
    /// </summary>
    private List<string> RulesOverrides
    {
        get => field;
        set
        {
            field = value;
            if (value is null)
            {
                BtnOverrideCancel.Visibility = Visibility.Collapsed;
                PanOptions.Visibility = Visibility.Visible;
                CardOptions.Inlines.Clear();
                CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionListTitle")) { FontWeight = FontWeights.Bold });
            }
            else
            {
                BtnOverrideCancel.Visibility = Visibility.Visible;
                PanOptions.Visibility = Visibility.Collapsed;
                CardOptions.Inlines.Clear();
                CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionListTitle") + ":鈥娾€娾€娾€?) { FontWeight = FontWeights.Bold });
                CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionList.FromConfig")) { FontWeight = FontWeights.Normal });
            }
        }
    }

    /// <summary>
    ///     鑾峰彇褰撳墠瀹為檯鐢熸晥鐨勬墍鏈夎鍒欍€?
    /// </summary>
    private IEnumerable<string> GetAllRules()
    {
        if (RulesOverrides is not null)
        {
            // 杩斿洖瑕嗙洊鐨勫垪琛?
            foreach (var Rule in RulesOverrides)
                yield return Rule;
        }
        else
        {
            // 浠庡綋鍓嶅嬀閫夌殑鎵€鏈夐€夐」涓幏鍙栨墍鏈夎鍒欒
            yield return "";
            yield return "# " + Lang.Text("Instance.Export.Config.Comment.ModifyRules");
            yield return "# " + Lang.Text("Instance.Export.Config.Comment.ReverseMatch");
            yield return "";
            foreach (var CheckBox in GetAllOptions(false))
            {
                if (CheckBox.Checked == false)
                    continue;
                var targetOption = GetExportOption(CheckBox);
                if (targetOption.Rules is null)
                    continue;
                yield return $"# {targetOption.Title}";
                foreach (var Rule in targetOption.Rules.Split('|'))
                    yield return Rule;
                yield return "";
            }

            yield return "# " + Lang.Text("Instance.Export.Config.Comment.ExcludedFiles");
            yield return "!*.log";
            yield return "!*.dat_old";
            yield return "!*.BakaCoreInfo";
            yield return "!hmclversion.cfg";
            yield return "!log4j2.xml";
            yield return "";
        }
    }

    // ================ 杩藉姞鍐呭娈?================

    private List<string> extraFiles;

    /// <summary>
    ///     鑾峰彇褰撳墠瀹為檯鐢熸晥鐨勮拷鍔犲唴瀹广€?
    /// </summary>
    private IEnumerable<string> GetExtraFileLines()
    {
        if (extraFiles is not null)
        {
            // 杩斿洖瑕嗙洊鐨勫垪琛?
            foreach (var File in extraFiles)
                yield return File;
        }
        else
        {
            // 浠庡綋鍓嶅嬀閫夌殑鎵€鏈夐€夐」涓幏鍙栨墍鏈夎鍒欒
            yield return "";
            yield return "# " + Lang.Text("Instance.Export.Config.Comment.ExtraFiles");
            yield return "# " + Lang.Text("Instance.Export.Config.Comment.ExtraFiles2");
            yield return "";
        }
    }

    // ================ 閲嶇疆 ================

    /// <summary>
    ///     閲嶇疆閰嶇疆鏂囦欢鎵€甯︽潵鐨勫奖鍝嶃€?
    /// </summary>
    private void ResetConfigOverrides()
    {
        RulesOverrides = null;
        configPackPath = null;
        extraFiles = null;
        PanBack.ScrollToHome();
    }

    private void CardOptions_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (RulesOverrides is null)
            return;
        ResetConfigOverrides();
    }

    // ================ 淇濆瓨 / 璇诲彇 ================

    // 淇濆瓨閰嶇疆鏂囦欢
    private void ExportConfig(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var configPath = SystemDialogs.SelectSaveFile(Lang.Text("Instance.Export.SelectFileLocation"), "export_config.txt", Lang.Text("Instance.Export.Config.FileFilter"),
                (string?)States.System.ExportConfigPath);
            if (string.IsNullOrEmpty(configPath))
                return;
            States.System.ExportConfigPath = configPath;
            var configLines = new List<string>();
            // ini 娈?
            configLines.Add("Name:" + TextExportName.Text);
            configLines.Add("Version:" + TextExportVersion.Text);
            configLines.Add("");
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.IncludeLauncher"));
            configLines.Add("IncludeLauncher:" + CheckOptionsPcl.Checked);
            configLines.Add("");
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.IncludeLauncherCustom"));
            configLines.Add("IncludeLauncherCustom:" + CheckOptionsPclCustom.Checked);
            configLines.Add("");
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.BundleFiles"));
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.BundleFiles2"));
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.BundleFiles3"));
            configLines.Add("DontCheckHostedAssets:" + CheckAdvancedInclude.Checked);
            configLines.Add("");
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.Modrinth"));
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.Modrinth2"));
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.Modrinth3"));
            configLines.Add("ModrinthUploadMode:" + CheckAdvancedModrinth.Checked);
            configLines.Add("");
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.PackPath"));
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.PackPath2"));
            configLines.Add("# " + Lang.Text("Instance.Export.Config.Comment.PackPath3"));
            configLines.Add("PackPath:" + (configPackPath ?? ""));
            configLines.Add("");
            // 瀵煎嚭鍐呭娈?
            configLines.Add(sperator);
            configLines.AddRange(GetAllRules());
            // 杩藉姞鍐呭娈?
            configLines.Add(sperator);
            configLines.AddRange(GetExtraFileLines());
            // 缁撴潫
            ModBase.WriteFile(configPath, configLines.Join("\r\n"));
            HintService.Hint(Lang.Text("Instance.Export.SaveSuccess", configPath), HintType.Success);
            ModBase.OpenExplorer(configPath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "淇濆瓨閰嶇疆澶辫触",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Export.Error.OperationFailed"));
        }
    }

    #region 閰嶇疆鏂囦欢鏍稿績璇诲彇閫昏緫

    /// <summary>
    ///     浠庢寚瀹氳矾寰勮鍙栭厤缃枃浠讹紙渚涙寜閽拰鎷栨斁璋冪敤锛?
    /// </summary>
    /// <param name="configPath">閰嶇疆鏂囦欢璺緞</param>
    private void ReadConfigFile(string configPath)
    {
        try
        {
            // 淇濆瓨閰嶇疆鏂囦欢璺緞鍒扮紦瀛?
            States.System.ExportConfigPath = configPath;

            var fileContent = ModBase.ReadFile(configPath);
            var segments = fileContent.Split(sperator);

            if (segments.Length == 0)
            {
                HintService.Hint(Lang.Text("Instance.Export.Config.Invalid"), HintType.Error);
                return;
            }

            // === 瑙ｆ瀽INI娈?===
            var ini = new Dictionary<string, string>();
            foreach (var LineRaw in segments[0].Split("\r\n".ToCharArray()))
            {
                var line = LineRaw;
                line = line.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWithF("#") || line.StartsWithF("="))
                    continue;
                var index = line.IndexOfF(":");
                if (index > 0) ini[line.Substring(0, index)] = line.Substring(index + 1);
            }

            // 璧嬪€煎埌鐣岄潰鎺т欢
            TextExportName.Text = ini.GetOrDefault("Name", "");
            TextExportVersion.Text = ini.GetOrDefault("Version", "");
            CheckOptionsPcl.Checked =
                Convert.ToBoolean(ini.GetOrDefault("IncludeLauncher", false.ToString()));
            CheckOptionsPclCustom.Checked =
                Convert.ToBoolean(ini.GetOrDefault("IncludeLauncherCustom", true.ToString()));
            CheckAdvancedModrinth.Checked =
                Convert.ToBoolean(ini.GetOrDefault("ModrinthUploadMode", false.ToString()));
            CheckAdvancedInclude.Checked =
                Convert.ToBoolean(ini.GetOrDefault("DontCheckHostedAssets", false.ToString()));
            configPackPath = ini.GetOrDefault("PackPath");

            // === 瑙ｆ瀽瀵煎嚭鍐呭娈?===
            RulesOverrides = segments[1].Replace("\r", "\n")
                .Replace("\n" + "\n", "\n").Split("\n").ToList();

            // === 瑙ｆ瀽杩藉姞鍐呭娈?===
            if (segments.Length > 2)
                extraFiles = segments[2].Replace("\r", "\n")
                    .Replace("\n" + "\n", "\n").Split("\n").ToList();
            else
                extraFiles = null;

            // 鎻愮ず鎴愬姛
            HintService.Hint(Lang.Text("Instance.Export.ReadSuccess", configPath), HintType.Success);
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"璇诲彇閰嶇疆鏂囦欢澶辫触锛歿configPath}",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Export.Error.OperationFailed"));
        }
    }

    #endregion

    // 璇诲彇閰嶇疆鏂囦欢
    private void ImportConfig(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var configPath = SystemDialogs.SelectFile(Lang.Text("Instance.Export.Config.FileFilter"), Lang.Text("Instance.Export.SelectConfigFile"),
                (string?)States.System.ExportConfigPath);
            if (string.IsNullOrEmpty(configPath))
                return;

            // 璋冪敤鏍稿績璇诲彇閫昏緫
            ReadConfigFile(configPath);
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "閫夋嫨閰嶇疆鏂囦欢澶辫触",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Export.Error.OperationFailed"));
        }
    }

    #region 鎷栨斁浜嬩欢澶勭悊

    /// <summary>
    ///     鏂囦欢鎷栧叆鐣岄潰鏃惰Е鍙戯細楠岃瘉鏂囦欢绫诲瀷
    /// </summary>
    private void PanAllBack_DragEnter(object sender, DragEventArgs e)
    {
        // 妫€鏌ユ槸鍚﹀寘鍚枃浠舵嫋鏀炬暟鎹?
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // 鑾峰彇鎷栧叆鐨勬枃浠惰矾寰勬暟缁?
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // 楠岃瘉锛氫粎鍏佽鍗曚釜.txt鏂囦欢
            if (files.Length == 1 &&
                files[0].EndsWithF(".txt", true))
                e.Effects = DragDropEffects.Copy; // 璁剧疆鎷栨斁鏁堟灉涓衡€滃鍒垛€?
            else
                e.Effects = DragDropEffects.None; // 涓嶅厑璁告嫋鏀?
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    /// <summary>
    ///     鏂囦欢鏀句笅鏃惰Е鍙戯細璇诲彇閰嶇疆鏂囦欢
    /// </summary>
    private void PanAllBack_Drop(object sender, DragEventArgs e)
    {
        // 鑾峰彇鎷栧叆鐨勬枃浠惰矾寰?
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var configPath = files[0];

            // 璋冪敤鏍稿績璇诲彇閫昏緫
            ReadConfigFile(configPath);
        }

        e.Handled = true;
    }

    #endregion

    #endregion

    #region 瀵煎嚭

    /// <summary>
    ///     閰嶇疆鏂囦欢涓寚瀹氱殑瀵煎嚭浣嶇疆銆?
    /// </summary>
    private string configPackPath;

    /// <summary>
    ///     寮€濮嬪鍑恒€?
    /// </summary>
    private void StartExport(object sender, MouseButtonEventArgs e)
    {
        var packName = string.IsNullOrEmpty(TextExportName.Text) ? TextExportName.HintText : TextExportName.Text;
        var packVersion = string.IsNullOrEmpty(TextExportVersion.Text) ? "1.0.0" : TextExportVersion.Text;

        // 閲嶅浠诲姟妫€鏌?
        var loaderName = Lang.Text("Instance.Export.ExportTask.Prefix") + packName;
        foreach (var OngoingLoader in ModLoader.loaderTaskbar)
        {
            if ((OngoingLoader.name ?? "") != (loaderName ?? ""))
                continue;
            ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
            return;
        }

        // 纭瀵煎嚭浣嶇疆
        string packPath = null;
        if (!string.IsNullOrWhiteSpace(configPackPath) && !configPackPath.EndsWithF(@"\") &&
            !configPackPath.EndsWithF("/"))
            try
            {
                Directory.CreateDirectory(ModBase.GetPathFromFullPath(configPackPath));
                packPath = configPackPath;
                ModBase.Log($"[Export] 浣跨敤閰嶇疆鏂囦欢涓寚瀹氱殑瀵煎嚭璺緞锛歿configPackPath}");
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"鏃犳硶浣跨敤閰嶇疆鏂囦欢涓寚瀹氱殑瀵煎嚭璺緞锛坽configPackPath}锛?);
                if (ModMain.MyMsgBox(
                        Lang.Text("Instance.Export.PackPathInvalid.WithDetail", configPackPath, ex.ToString()),
                        Lang.Text("Instance.Export.PackPathInvalid.Title"), Lang.Text("Common.Action.Confirm"),
                        Lang.Text("Common.Action.Cancel")) == 2)
                    return;
            }

        if (packPath is null)
        {
            var extensions = new List<string>();
            if (CheckAdvancedModrinth.Checked == false)
                extensions.Add(Lang.Text("Instance.Export.ZipFilter"));
            if (CheckOptionsPcl.Checked == false)
                extensions.Add(Lang.Text("Instance.Export.MrpackFilter"));
            packPath = SystemDialogs.SelectSaveFile(Lang.Text("Instance.Export.SelectSaveLocation"),
                packName + (string.IsNullOrEmpty(TextExportVersion.Text) ? "" : " " + TextExportVersion.Text),
                extensions.Join("|"));
            ModBase.Log($"[Export] 鎵嬪姩鎸囧畾鐨勫鍑鸿矾寰勶細{packPath}");
        }

        if (string.IsNullOrEmpty(packPath))
            return;

        // 缂撳瓨鎵€闇€鍙傛暟
        var cacheFolder = ModMain.RequestTaskTempFolder();
        var overridesFolder = Path.Combine(cacheFolder, "modpack", "overrides");
        var mcInstance = PageInstanceLeft.McInstance;
        var pathIndie = mcInstance.PathIndie;
        var checkHostedAssets = (bool)!CheckAdvancedInclude.Checked;
        var modrinthUploadMode = (bool)CheckAdvancedModrinth.Checked;
        var includePCL = (bool)CheckOptionsPcl.Checked;
        var includePCLCustom = (bool)(includePCL ? CheckOptionsPclCustom.Checked : (bool?)false);
        var allRules = StandardizeLines(GetAllRules(), true).ToList();
        var allExtraFiles = StandardizeLines(GetExtraFileLines(), false).ToList();
        ModBase.Log($"[Export] 鍑嗗瀵煎嚭鏁村悎鍖咃紝鍏辨湁 {allRules.Count} 鏉¤鍒欙紝{allExtraFiles.Count} 鏉¤拷鍔犲唴瀹硅");

        // 鏋勯€犳楠ゅ姞杞藉櫒
        var loaders = new List<ModLoader.LoaderBase>();

        #region 鍑嗗 PCL 鏂囦欢
        
        #if !RELEASE
        if (includePCL)
            loaders.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Instance.Export.Task.DownloadPclRelease"),
                loader =>
                {
                    UpdateManager.DownloadLatestPCL(loader);
                    ModBase.CopyFile(Path.Combine(ModBase.pathTemp, "CE-Latest.exe"),
                        Path.Combine(cacheFolder, "Plain Craft Launcher.exe"));
                })
            {
                ProgressWeight = 0.5d,
                block = false
            });
        #endif

        #endregion

        #region 澶嶅埗鏂囦欢

        loaders.Add(new ModLoader.LoaderTask<int, List<ModLocalComp.LocalCompFile>>(
            Lang.Text("Instance.Export.Task.CopyContent"), loader =>
            {
                loader.output = [];
                // 澶嶅埗瀹炰緥鏂囦欢
                var progress = 0;
            Action<DirectoryInfo> searchFolder = null;
            searchFolder = folder =>
            {
                // 鏂囦欢澶癸細杩涗竴姝ユ悳绱?
                foreach (var SubFolder in folder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    // 璺宠繃閮ㄥ垎鍙堟病鐢ㄦ枃浠跺張澶氱殑鏂囦欢澶癸紝鍔犲揩鎼滅储
                    if ((folder.FullName ?? "") == (pathIndie ?? "") &&
                        new[] { "assets", "versions", "libraries" }.Contains(SubFolder.Name))
                        continue;
                    if (new[] { "structureCacheV1", ".fabric", ".git", "avatar-cache", "cosmetic-cache" }.Contains(
                            SubFolder.Name))
                        continue;
                    searchFolder(SubFolder);
                }

                // 鏂囦欢锛氭鏌ヨ鍒欏苟澶嶅埗
                foreach (var Entry in folder.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    var relativePath = Entry.FullName.AfterFirst(pathIndie);
                    // 妫€鏌ヨ鍒?
                    var shouldKeep = false;
                    foreach (var Rule in allRules)
                    {
                        var revert = Rule.StartsWith("!");
                        if (LikeString(relativePath, Rule.TrimStart('!')))
                            shouldKeep = !revert;
                    }

                    if (!shouldKeep)
                        continue;
                    var targetPath = Path.Combine(overridesFolder, relativePath);
                    ModBase.CopyFile(Entry.FullName, targetPath);
                    // 鑻ヤ负鍘嬬缉鍖咃紝鑰冭檻鑱旂綉鑾峰彇璺緞
                    if (checkHostedAssets &&
                        new[] { ".zip", ".rar", ".jar", ".disabled", ".old" }.Contains(Entry.Extension.ToLower()) &&
                        new[] { "mods", "packs", "openloader", "resource" }.Any(s => relativePath.Contains(s)))
                    {
                        var modFile = new ModLocalComp.LocalCompFile(targetPath);
                        var unused = modFile.ModrinthHash; // 鎻愬墠璁＄畻 Hash
                        unused = modFile.CurseForgeHash.ToString();
                        loader.output.Add(modFile);
                    }

                    // 鏇存柊杩涘害锛堣繘搴﹀苟涓嶅噯纭紝涓昏绐佸嚭涓€涓垜杩樻病浼硷級
                    progress += 1;
                    if (progress == 25)
                    {
                        loader.Progress += (0.94d - loader.Progress) * 0.012d;
                        progress = 0;
                    }
                }
            };
            searchFolder(new DirectoryInfo(pathIndie));
            ModBase.Log($"[Export] 澶嶅埗 overrides 鏂囦欢瀹屾垚锛屾湁 {loader.output.Count} 涓枃浠堕渶瑕佽仈缃戞鏌?);
            loader.Progress = 0.95d;
            // 澶嶅埗杩藉姞鍐呭鍒版牴鐩綍
            var baseFolder = includePCL ? cacheFolder : Path.Combine(cacheFolder, "modpack");
            foreach (var Line in allExtraFiles)
                if (Line.EndsWithF(@"\") || Line.EndsWithF("/"))
                {
                    if (Directory.Exists(Line))
                        ModBase.CopyDirectory(Line, Path.Combine(baseFolder, ModBase.GetFolderNameFromPath(Line)) + @"\");
                    else
                        HintService.Hint(Lang.Text("Instance.Export.Config.FolderNotFound", Line), HintType.Error);
                }
                else if (File.Exists(Line))
                {
                    ModBase.CopyFile(Line, Path.Combine(baseFolder, ModBase.GetFileNameFromPath(Line)));
                }
                else
                {
                    HintService.Hint(Lang.Text("Instance.Export.Config.FileNotFound", Line), HintType.Error);
                }

            loader.Progress = 0.97d;
            // 澶嶅埗 PCL 瀹炰緥璁剧疆
            ModBase.CopyDirectory(Path.Combine(mcInstance.PathInstance, "PCL"), Path.Combine(overridesFolder, "PCL"));
            #if RELEASE
                        // 澶嶅埗 PCL 鏈綋
                        if (includePCL) ModBase.CopyFile(Basics.ExecutablePath, Path.Combine(cacheFolder, Basics.ExecutableName));
            #endif
            // 澶嶅埗 PCL 涓€у寲鍐呭
            if (includePCLCustom)
            {
                if (Directory.Exists(Path.Combine(ModBase.exePath, "PCL", "Pictures")))
                    ModBase.CopyDirectory(Path.Combine(ModBase.exePath, "PCL", "Pictures"), Path.Combine(cacheFolder, "PCL", "Pictures"));
                if (Directory.Exists(Path.Combine(ModBase.exePath, "PCL", "Musics")))
                    ModBase.CopyDirectory(Path.Combine(ModBase.exePath, "PCL", "Musics"), Path.Combine(cacheFolder, "PCL", "Musics"));
                if (File.Exists(Path.Combine(ModBase.exePath, "PCL", "Custom.xaml")))
                    ModBase.CopyFile(Path.Combine(ModBase.exePath, "PCL", "Custom.xaml"), Path.Combine(cacheFolder, "PCL", "Custom.xaml"));
                if (File.Exists(Path.Combine(ModBase.exePath, "PCL", "Setup.ini")))
                    ModBase.CopyFile(Path.Combine(ModBase.exePath, "PCL", "Setup.ini"), Path.Combine(cacheFolder, "PCL", "Setup.ini"));
                if (File.Exists(Path.Combine(ModBase.exePath, "PCL", "hints.txt")))
                    ModBase.CopyFile(Path.Combine(ModBase.exePath, "PCL", "hints.txt"), Path.Combine(cacheFolder, "PCL", "hints.txt"));
                if (File.Exists(Path.Combine(ModBase.exePath, "PCL", "Logo.png")))
                    ModBase.CopyFile(Path.Combine(ModBase.exePath, "PCL", "Logo.png"), Path.Combine(cacheFolder, "PCL", "Logo.png"));
            }
        })
        {
            ProgressWeight = 5d
        });

        #endregion

        #region 鑱旂綉妫€鏌?

        loaders.Add(
            new ModLoader.LoaderTask<List<ModLocalComp.LocalCompFile>,
                Dictionary<ModLocalComp.LocalCompFile, List<string>>>(Lang.Text("Instance.Export.Task.FetchFileInfo"),
                loader =>
                {
                    loader.output = new Dictionary<ModLocalComp.LocalCompFile, List<string>>();
                    if (!checkHostedAssets)
                    {
                        ModBase.Log("[Export] 瑕佹眰璺宠繃鑱旂綉鑾峰彇姝ラ");
                        return;
                    }

                    if (!loader.input.Any())
                    {
                        ModBase.Log("[Export] 娌℃湁闇€瑕佽仈缃戞鏌ョ殑鏂囦欢锛岃烦杩囪仈缃戣幏鍙栨楠?);
                        return;
                    }

                    // 鍒嗗钩鍙拌幏鍙栦笅杞藉湴鍧€
                    var endedThreadCount = 0;
                    var failedExceptions = new List<Exception>();

                    // 浠?Modrinth 鑾峰彇淇℃伅
                    // 鏌ユ壘瀵瑰簲鐨勬枃浠?
                    // 鍐欏叆涓嬭浇鍦板潃
                    ModBase.RunInNewThread(() =>
                    {
                        try
                        {
                            var modrinthHashes = loader.input.Select(m => m.ModrinthHash);
                            var modrinthRaw = (JsonObject)ModBase.GetJson(ModDownload.DlModRequest(
                                "https://api.modrinth.com/v2/version_files", "POST",
                                $"{{\"hashes\": [\"{modrinthHashes.Join("\",\"")}\"], \"algorithm\": \"sha1\"}}",
                                "application/json"));
                            foreach (var ModFile in loader.input)
                            {
                                if (!modrinthRaw.ContainsKey(ModFile.ModrinthHash)) continue;
                                if ((string)modrinthRaw[ModFile.ModrinthHash]?["files"]?[0]["hashes"]?["sha1"] !=
                                    ModFile.ModrinthHash) continue;
                                loader.output.AddToList(ModFile,
                                    (string)modrinthRaw[ModFile.ModrinthHash]["files"][0]["url"]);
                            }

                            ModBase.Log($"[Export] 浠?Modrinth 鑾峰彇鍒?{modrinthRaw.Count} 涓湰鍦拌祫婧愰」鐨勫搴斾俊鎭?);
                        }
                        catch (Exception ex)
                        {
                            ModBase.Log(ex, "浠?Modrinth 鑾峰彇鏈湴 Mod 淇℃伅澶辫触");
                            failedExceptions.Add(ex);
                        }
                        finally
                        {
                            endedThreadCount += 1;
                            loader.Progress += 0.45d;
                        }
                    }, "Modrinth - " + loaderName);

                    // 浠?CurseForge 鑾峰彇淇℃伅
                    // 鏌ユ壘瀵瑰簲鐨勬枃浠?
                    // 鍐欏叆涓嬭浇鍦板潃
                    ModBase.RunInNewThread(() =>
                    {
                        try
                        {
                            if (modrinthUploadMode) return;
                            var curseForgeHashes = loader.input.Select(m => m.CurseForgeHash);
                            var curseForgeRaw = (JsonNode)((JsonObject)ModBase.GetJson(
                                ModDownload.DlModRequest("https://api.curseforge.com/v1/fingerprints/432/", "POST",
                                    $"{{\"fingerprints\": [{curseForgeHashes.Join(",")}]}}",
                                    "application/json")))["data"][
                                "exactMatches"];
                            foreach (JsonObject ResultJson in curseForgeRaw.AsArray())
                            {
                                if (!ResultJson.ContainsKey("file")) continue;
                                var file = (JsonObject)ResultJson["file"];
                                if (string.IsNullOrEmpty((string)file["downloadUrl"])) continue;
                                var modFile = loader.input.FirstOrDefault(m =>
                                    m.CurseForgeHash == file["fileFingerprint"].ToObject<uint>());
                                if (modFile is null) continue;
                                loader.output.AddToList(modFile,
                                    ModComp.CompFile.HandleCurseForgeDownloadUrls(file["downloadUrl"].ToString()));
                            }

                            ModBase.Log($"[Export] 浠?CurseForge 鑾峰彇鍒?{curseForgeRaw.AsArray().Count} 涓湰鍦拌祫婧愰」鐨勫搴斾俊鎭?);
                        }
                        catch (Exception ex)
                        {
                            ModBase.Log(ex, "浠?CurseForge 鑾峰彇鏈湴 Mod 淇℃伅澶辫触");
                            failedExceptions.Add(ex);
                        }
                        finally
                        {
                            endedThreadCount += 1;
                            loader.Progress += 0.45d;
                        }
                    }, "CurseForge - " + loaderName); // Modrinth 涓婁紶妯″紡涓嬶紝涓嶈兘浠?CurseForge 鑾峰彇淇℃伅

                    // 绛夊緟绾跨▼缁撴潫
                    while (endedThreadCount != 2)
                    {
                        if (loader.IsAborted)
                            return;
                        Thread.Sleep(10);
                    }

                    // 鑻ュけ璐ワ紝纭鏄惁缁х画
                    if (failedExceptions.Count == 1)
                    {
                        if (ModMain.MyMsgBox(
                                Lang.Text("Instance.Export.NetCheckPartialFailed.Message"),
                                Lang.Text("Instance.Export.NetCheckPartialFailed.Title"),
                                Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) == 2)
                            throw failedExceptions.First();
                    }
                    else if (failedExceptions.Count > 1)
                    {
                        if (ModMain.MyMsgBox(
                                Lang.Text("Instance.Export.NetCheckAllFailed.Message"),
                                Lang.Text("Instance.Export.NetCheckAllFailed.Title"),
                                Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) == 2)
                            throw failedExceptions.First();
                    }
                })
            {
                show = checkHostedAssets,
                ProgressWeight = checkHostedAssets ? 2d : 0.01d
            });

        #endregion

        #region 鐢熸垚鍘嬬缉鍖?

        loaders.Add(new ModLoader.LoaderTask<Dictionary<ModLocalComp.LocalCompFile, List<string>>, int>(
            Lang.Text("Instance.Export.Task.CreateArchive"),
            loader =>
            {
                // 鏁寸悊鏂囦欢鍒楄〃
                var files = new JsonArray();
                foreach (var Pair in loader.input)
                {
                    var modFile = Pair.Key;
                    files.Add(new JsonObject
                    {
                        { "path", Path.GetRelativePath(overridesFolder, modFile.path).Replace(@"\", "/") },
                        {
                            "hashes",
                            new JsonObject
                            {
                                { "sha1", modFile.ModrinthHash }, { "sha512", ModBase.GetFileSHA512(modFile.path) }
                            }
                        },
                        { "downloads", new JsonArray(Pair.Value.OrderByDescending(u => u.Contains("modrinth.com")).Select(s => (JsonNode)s).ToArray()) },
                        { "fileSize", new FileInfo(modFile.path).Length }
                    });
                    File.Delete(modFile.path);
                }

                loader.Progress = 0.2d;
                // 瀵煎嚭鏈€缁?JSON 鏂囦欢
                var dependencies = new JsonObject { { "minecraft", mcInstance.Info.VanillaName } };
                if (mcInstance.Info.HasForge)
                    dependencies.Add("forge", mcInstance.Info.Forge);
                if (mcInstance.Info.HasFabric)
                    dependencies.Add("fabric-loader", mcInstance.Info.Fabric);
                if (mcInstance.Info.HasNeoForge)
                    dependencies.Add("neoforge", mcInstance.Info.NeoForge);
                var resultJson = new JsonObject
                {
                    { "game", "minecraft" }, { "formatVersion", 1 }, { "versionId", packVersion }, { "name", packName },
                    { "summary", mcInstance.Desc }, { "files", files }, { "dependencies", dependencies }
                };
                File.WriteAllText(Path.Combine(cacheFolder, "modpack", "modrinth.index.json"),
                    resultJson.ToJsonString(new JsonSerializerOptions(JsonCompat.SerializerOptions) { WriteIndented = true }));
                // 鎵撳寘
                Directory.CreateDirectory(ModBase.GetPathFromFullPath(packPath));
                if (File.Exists(packPath))
                    File.Delete(packPath);
                if (includePCL)
                {
                    // 棣栨鍘嬬缉鏁村悎鍖?
                    ZipFile.CreateFromDirectory(Path.Combine(cacheFolder, "modpack"), Path.Combine(cacheFolder, "modpack.mrpack"));
                    loader.Progress = 0.5d;
                    Directory.Delete(Path.Combine(cacheFolder, "modpack"), true);
                    loader.Progress = 0.6d;
                    // 浜屾鍘嬬缉鏁村悎鍖?
                    ZipFile.CreateFromDirectory(cacheFolder, packPath);
                    loader.Progress = 0.9d;
                }
                else
                {
                    // 鐩存帴鍘嬬缉鏁村悎鍖?
                    ZipFile.CreateFromDirectory(Path.Combine(cacheFolder, "modpack"), packPath);
                    loader.Progress = 0.8d;
                }

                Directory.Delete(cacheFolder, true);
                ModBase.OpenExplorer(packPath);
            })
        {
            ProgressWeight = 6d
        });

        #endregion

        // 鍚姩
        var mainLoader = new ModLoader.LoaderCombo<string>(loaderName, loaders)
            { OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly };
        mainLoader.Start();
        ModLoader.LoaderTaskbarAdd(mainLoader);
        ModMain.frmMain.BtnExtraDownload.ShowRefresh();
        ModMain.frmMain.BtnExtraDownload.Ribble();
        ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
    }

    #endregion

    private static bool LikeString(string input, string pattern)
    {
        pattern = pattern.Replace("#", "[0-9]");
        var options = new GlobOptions { Evaluation = { CaseInsensitive = true } };
        var glob = Glob.Parse(pattern, options);
        return glob.IsMatch(input);
    }
}
