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
    public static readonly AvaloniaProperty TitleProperty = AvaloniaProperty.Register<ExportOption, string>(nameof(Title));

    public static readonly AvaloniaProperty DescriptionProperty = AvaloniaProperty.Register<ExportOption, string>(nameof(Description));

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
    ///     婵″倹鐏?Rules 娑撹櫣鈹栭敍灞藉灟閺嶈宓?ShowRules 閻ㄥ嫬鍞寸€圭懓鍨介弬顓熸Ц閸氾箑绨茬拠銉︽▔缁€楦跨箹娑擃亜顦查柅澶嬵攱閵?
    ///     婵″倹鐏?ShowRules 娑旂喍璐熺粚鐚寸礉閸掓瑥顫愮紒鍫熸▔缁€鎭掆偓?
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
            RefreshAll(); // 閸掑洦宕查崚棰佺啊閸欙缚绔存稉顏勭杽娓氬绱濋柌宥囩枂妞ょ敻娼?
        ModAnimation.AniControlEnabled -= 1;
    }

    public void RefreshAll()
    {
        ModBase.Log("[Export] 閸掗攱鏌婄€电厧鍤い鐢告桨");
        HintOptiFine.IsVisible =
            PageInstanceLeft.McInstance.Info.HasOptiFine ? true : false;
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

    // 閼奉亜濮╂繅顐㈠晸閺佹潙鎮庨崠鍛倳缁?
    private void TextExportName_GotFocus(object sender, RoutedEventArgs routedEventArgs)
    {
        if (string.IsNullOrEmpty(TextExportName.Text))
        {
            TextExportName.Text = TextExportName.HintText;
            TextExportName.SelectionStart = TextExportName.Text.Length;
        }
    }

    // 閸曢箖鈧?Modrinth 娑撳﹣绱跺Ο鈥崇础閺冭绱濈粋浣诡剾閹垫挸瀵?PCL
    private void CheckAdvancedModrinth_Change(object sender, bool user)
    {
        if (CheckAdvancedModrinth.Checked == true)
            CheckOptionsPcl.Checked = false;
        CheckOptionsPcl.IsEnabled = (bool)!CheckAdvancedModrinth.Checked;
    }

    // 閸曢箖鈧?閸忔湹绮弬鍥︽婢?閺冭绱濋崥灞绢劄閸曢箖鈧?閸欐牗绉烽幍鈧張澶婄摍闁銆?
    private void CheckOptionsOtherFolders_Change(object sender, bool user)
    {
        if (!user) return;
        foreach (var child in PanOptionsOtherFolders.Children)
            if (child is MyCheckBox childBox)
                childBox.Checked = CheckOptionsOtherFolders.Checked;
    }

    // 閸曢箖鈧澧﹂崠鍛扮カ濠ф劖鏋冩禒鑸垫閿涘瞼顩﹀銏犵磻閸?Modrinth 娑撳﹣绱跺Ο鈥崇础
    private void CheckAdvancedInclude_Change(object sender, bool user)
    {
        if (CheckAdvancedInclude.Checked == true)
            CheckAdvancedModrinth.Checked = false;
        CheckAdvancedModrinth.IsEnabled = (bool)!CheckAdvancedInclude.Checked;
    }

    #region 鐎涙劙鈧銆?

    private readonly string[] subOptionBlackList = new[] { "Quark Programmer Art.zip", "+ EuphoriaPatches_" };

    /// <summary>
    ///     閸斻劍鈧胶鏁撻幋鎰摍閺傚洣娆㈡径閫涚瑓閻ㄥ嫰鈧銆嶉敍灞肩伐婵″倽绁┃鎰瘶閵嗕礁鐡ㄥ锝囩搼閵?
    /// </summary>
    private void ReloadAllSubOptions()
    {
        ReloadSubOptions(PanOptionsResourcePacks, true, true, "resourcepacks", "texturepacks");
        ReloadSubOptions(PanOptionsSaves, false, true, "saves");
        ReloadSubOptions(PanOptionsShaderPacks, true, true, "shaderpacks");
        ReloadOtherFolders();
    }

    /// <summary>
    ///     閹殿偅寮跨€圭偘绶ラ弽鍦窗瑜版洑绗呴張顏囶潶瀹稿弶婀侀柅澶愩€嶇憰鍡欐磰閻ㄥ嫭鏋冩禒璺恒仚閿涘瞼鏁撻幋鎰缁斿娈戞径宥夆偓澶嬵攱閵?
    /// </summary>
    private void ReloadOtherFolders()
    {
        PanOptionsOtherFolders.Children.Clear();

        var pathIndie = PageInstanceLeft.McInstance.PathIndie;
        var rootDir = new DirectoryInfo(pathIndie);
        if (!rootDir.Exists)
        {
            CheckOptionsOtherFolders.IsVisible = false;
            return;
        }

        var coveredFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // 濡紕绮?
            "mods", "coremods", "lib",
            // 閺佹潙鎮庨崠鍛村櫢鐟曚焦鏆熼幑?
            "addons", "multiblocked", "modpack-update-checker", "global_packs",
            "global_resource_packs", "global_data_packs", "optional_data_packs", "maps",
            "mods-resourcepacks", "matmos", "resource_assorts",
            "patchouli_books", "datapacks",
            "openloader", "worldshape", "resources", "scripts", "structures",
            "fontfiles", "oresources", "packmenu", "craftpresence", "pointblanks",
            // 濡紕绮嶇拋鍓х枂
            "config", "defaultconfigs", "journeymap", "local", "essential", "gg.essential.mod",
            "CustomSkinLoader",
            // 閸︽澘娴?
            "xaero", "XaeroWaypoints", "XaeroWorldMap",
            // 鐠у嫭绨崠?
            "resourcepacks", "texturepacks",
            // 閸忓濂?
            "shaderpacks",
            // 閹搭亜娴?/ 缂佹挻鐎?/ 瑜版洖鍎?
            "screenshots", "schematics",
            "replay_recordings", "replay_videos",
            // 鐎涙ɑ銆?/ 鐠佸墽鐤嗛弬鍥︽婢?
            "saves", "configureddefaults",
            // 婵绮撶捄瀹犵箖閿涘牆銇囬柌蹇旀瀮娴犺埖鍨ㄩ弮鐘垫暏缂傛挸鐡ㄩ敍?
            "assets", "versions", "libraries", "structureCacheV1",
            ".fabric", ".git", "avatar-cache", "cosmetic-cache",
            // PCL 閸楁洜瀚径鍕倞
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

        CheckOptionsOtherFolders.IsVisible = PanOptionsOtherFolders.Children.Count > 0
            ? true
            : false;
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
            // 閺屻儲澹橀弬鍥︽婢堕€涚瑓閻ㄥ嫬顕惔鏃堛€?
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
                    if (Folder == "shaderpacks") // 婢跺嫮鎮婇崗澶婂閸栧懐娈戦柊宥囩枂閺傚洣娆?
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
                    if (Folder == "shaderpacks") // 婢跺嫮鎮婇弬鍥︽婢剁懓鑸板蹇撳帨瑜板崬瀵橀惃鍕帳缂冾喗鏋冩禒?
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

    #region 闁銆?

    /// <summary>
    ///     闁插秵鏌婄涵顔款吇閺勵垰鎯佹惔鏃囶嚉閺勫墽銇氬В蹇庨嚋闁銆嶉敍灞借嫙鐏?ExportOption 閸氬本顒為崚?UI閵?
    /// </summary>
    private void RefreshAllOptionsUI()
    {
        // 妫板嫬鍘涜ぐ鎺旀捈閹碘偓閺堝鍤︽径姘癌缁狙呮畱閺傚洣娆?閺傚洣娆㈡径?
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

        ; // 濡偓閺屻儲鏋冩禒璺恒仚娑撳秳璐熺粚?
        // 娑撯偓閼割剚妲搁悽鍙樼艾閺冪姵纭剁拋鍧楁６閿涘本鍨ㄩ弰顖欑娑擃亝瀵氶崥鎴濆嚒娑撳秴鐡ㄩ崷銊ф畱閺傚洣娆㈡径鍦畱闁剧偓甯撮敍鍫滅伐婵″倷濞囬悽?mklink 閸掓盯鈧姷娈?resource 閺傚洣娆㈡径褰掓懠閹恒儻绱?
        var pathInfo = new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie);
        allEntries.AddRange(pathInfo.EnumerateFiles().Select(f => f.Name));
        foreach (var SubFolder in pathInfo.EnumerateDirectories().Where(IsValidDirectory))
        {
            allEntries.Add($@"{SubFolder.Name}\");
            allEntries.AddRange(SubFolder.EnumerateFiles().Select(f => $@"{SubFolder.Name}\{f.Name}"));
            allEntries.AddRange(SubFolder.EnumerateDirectories().Where(IsValidDirectory)
                .Select(d => $@"{SubFolder.Name}\{d.Name}\"));
        }

        ModBase.Log($"[Export] 閸忓崬褰傞悳?{allEntries.Count} 娑擃亜褰茬悰宀€娈戞禍宀€楠囬弬鍥︽/閺傚洣娆㈡径?);

        // 绾喛顓婚柅澶愩€嶉弰顖氭儊鎼存棁顕氱悮顐ｆ▔缁€?
        bool IsVisible(ExportOption targetOption)
        {
            // 濡偓閺屻儵娓剁憰?OptiFine 閹?Mod 閸旂姾娴囬崳?
            if (targetOption.RequireOptiFine && !PageInstanceLeft.McInstance.Info.HasOptiFine)
                return false;
            if (targetOption.RequireModLoader && !PageInstanceLeft.McInstance.Modable)
                return false;
            if (targetOption.RequireModLoaderOrOptiFine && !PageInstanceLeft.McInstance.Info.HasOptiFine &&
                !PageInstanceLeft.McInstance.Modable)
                return false;
            // 缁鏆愬Λ鈧弻銉︽Ц閸氾箑褰查懗鑺ユ箒缁楋箑鎮庣憴鍕灟閻ㄥ嫭鏋冩禒?閺傚洣娆㈡径?
            return StandardizeLines((targetOption.Rules ?? targetOption.ShowRules).Split('|'), true).Any(rule =>
            {
                if (rule.StartsWithF("!"))
                    return false; // 閸欘亞婀呭锝呮倻鐟欏嫬鍨?
                // 濡偓閺屻儱澧犳稉銈囬獓
                try
                {
                    if (allEntries.Any(entry => LikeString(entry, rule)))
                        return true;
                }
                catch (Exception ex)
                {
                    ModBase.Log(
                        ex,
                        $"闁挎瑨顕ら惃鍕潐閸掓瑱绱皗rule}",
                        ModBase.LogLevel.Hint,
                        userSummary: Lang.Text("Instance.Export.Error.OperationFailed"));
                    return false;
                }

                // 缁鏆愬Λ鈧弻銉﹀閺堝楠?
                rule = rule.Trim("*?".ToCharArray());
                if (rule.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Count() >= 3)
                {
                    if (rule.EndsWithF(@"\"))
                        return IsValidDirectory(new DirectoryInfo(PageInstanceLeft.McInstance.PathIndie + rule)); // 閺傚洣娆㈡径瑙勬箒閺?

                    return File.Exists(PageInstanceLeft.McInstance.PathIndie + rule);
                    // 閺傚洣娆㈤張澶嬫櫏
                }

                return false;
            });
        }

        ;
        // 闁劒閲滃Λ鈧弻銉┾偓澶愩€?
        foreach (var CheckBox in GetAllOptions(true))
        {
            var targetOption = GetExportOption(CheckBox);
            // 閸氬秶袨娑撳海鐣濇禒?
            CheckBox.Inlines.Clear();
            CheckBox.Inlines.Add(new Run(targetOption.Title));
            if (!string.IsNullOrEmpty(targetOption.Description))
                CheckBox.Inlines.Add(new Run("   " + targetOption.Description) { Foreground = ThemeManager.colorGray5 });
            // 閸欘垵顫嗛幀褋鈧線绮拋銈呭瑎闁?
            if (string.IsNullOrEmpty(targetOption.Rules) && string.IsNullOrEmpty(targetOption.ShowRules))
            {
                CheckBox.IsVisible = true;
                CheckBox.Checked = targetOption.DefaultChecked;
            }
            else
            {
                var pass = IsVisible(targetOption);
                CheckBox.IsVisible = pass ? true : false;
                CheckBox.Checked = targetOption.DefaultChecked && pass;
            }
        }
    }

    /// <summary>
    ///     鐎佃鏋冮張顒冾攽鏉╂稖顢戦弽鍥у櫙閸栨牕顦╅悶鍡礉娴犮儰绌舵担璺ㄦ暏 Like 鏉╂稖顢戦崠褰掑帳閵?
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
    ///     閼惧嘲褰囬幍鈧張澶婂讲娴ｆ粈璐熼柅澶愩€嶉惃?CheckBox閵?
    /// </summary>
    private IEnumerable<MyCheckBox> GetAllOptions(bool includeHidden)
    {
        foreach (var Element in PanOptions.Children)
        {
            if (!includeHidden &&
                ((Control)Element).IsVisible != true)
                continue;
            if (Element is MyCheckBox)
                yield return (MyCheckBox)Element;
            else if (Element is StackPanel)
                foreach (var SubElement in ((StackPanel)Element).Children)
                {
                    if (!includeHidden && ((Control)SubElement).IsVisible != true)
                        continue;
                    if (SubElement is MyCheckBox)
                        yield return (MyCheckBox)SubElement;
                }
        }
    }

    /// <summary>
    ///     閼惧嘲褰囩拠?CheckBox 鐎电懓绨查惃?ExportOption閵?
    /// </summary>
    private ExportOption GetExportOption(MyCheckBox checkBox)
    {
        return (ExportOption)checkBox.Tag;
    }

    #endregion

    #region 闁板秶鐤嗛弬鍥︽

    private const string sperator = "==============================================================";

    // ================ 鐎电厧鍤崘鍛啇濞?================

    /// <summary>
    ///     娴犲酣鍘ょ純顔芥瀮娴犳湹鑵戠拠璇插絿閻ㄥ嫯顫夐崚娆嶁偓?
    ///     婵″倹鐏夋稉宥勮礋 Nothing閿涘苯鍨导姘愁洬閸愭瑥缍嬮崜宥呭瑎闁娈戠憴鍕灟楠炲墎顩﹂悽銊ヮ嚠鎼?UI閵?
    /// </summary>
    private List<string> RulesOverrides
    {
        get => field;
        set
        {
            field = value;
            if (value is null)
            {
                BtnOverrideCancel.IsVisible = false;
                PanOptions.IsVisible = true;
                CardOptions.Inlines.Clear();
                CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionListTitle")) { FontWeight = FontWeights.Bold });
            }
            else
            {
                BtnOverrideCancel.IsVisible = true;
                PanOptions.IsVisible = false;
                CardOptions.Inlines.Clear();
                CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionListTitle") + ":閳ュň鈧ň鈧ň鈧?) { FontWeight = FontWeights.Bold });
                CardOptions.Inlines.Add(new Run(Lang.Text("Instance.Export.OptionList.FromConfig")) { FontWeight = FontWeights.Normal });
            }
        }
    }

    /// <summary>
    ///     閼惧嘲褰囪ぐ鎾冲鐎圭偤妾悽鐔告櫏閻ㄥ嫭澧嶉張澶庮潐閸掓瑣鈧?
    /// </summary>
    private IEnumerable<string> GetAllRules()
    {
        if (RulesOverrides is not null)
        {
            // 鏉╂柨娲栫憰鍡欐磰閻ㄥ嫬鍨悰?
            foreach (var Rule in RulesOverrides)
                yield return Rule;
        }
        else
        {
            // 娴犲骸缍嬮崜宥呭瑎闁娈戦幍鈧張澶愨偓澶愩€嶆稉顓″箯閸欐牗澧嶉張澶庮潐閸掓瑨顢?
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

    // ================ 鏉╄棄濮為崘鍛啇濞?================

    private List<string> extraFiles;

    /// <summary>
    ///     閼惧嘲褰囪ぐ鎾冲鐎圭偤妾悽鐔告櫏閻ㄥ嫯鎷烽崝鐘插敶鐎瑰箍鈧?
    /// </summary>
    private IEnumerable<string> GetExtraFileLines()
    {
        if (extraFiles is not null)
        {
            // 鏉╂柨娲栫憰鍡欐磰閻ㄥ嫬鍨悰?
            foreach (var File in extraFiles)
                yield return File;
        }
        else
        {
            // 娴犲骸缍嬮崜宥呭瑎闁娈戦幍鈧張澶愨偓澶愩€嶆稉顓″箯閸欐牗澧嶉張澶庮潐閸掓瑨顢?
            yield return "";
            yield return "# " + Lang.Text("Instance.Export.Config.Comment.ExtraFiles");
            yield return "# " + Lang.Text("Instance.Export.Config.Comment.ExtraFiles2");
            yield return "";
        }
    }

    // ================ 闁插秶鐤?================

    /// <summary>
    ///     闁插秶鐤嗛柊宥囩枂閺傚洣娆㈤幍鈧敮锔芥降閻ㄥ嫬濂栭崫宥冣偓?
    /// </summary>
    private void ResetConfigOverrides()
    {
        RulesOverrides = null;
        configPackPath = null;
        extraFiles = null;
        PanBack.ScrollToHome();
    }

    private void CardOptions_MouseLeftButtonDown(object sender, PointerReleasedEventArgs e)
    {
        if (RulesOverrides is null)
            return;
        ResetConfigOverrides();
    }

    // ================ 娣囨繂鐡?/ 鐠囪褰?================

    // 娣囨繂鐡ㄩ柊宥囩枂閺傚洣娆?
    private void ExportConfig(object sender, PointerReleasedEventArgs e)
    {
        try
        {
            var configPath = SystemDialogs.SelectSaveFile(Lang.Text("Instance.Export.SelectFileLocation"), "export_config.txt", Lang.Text("Instance.Export.Config.FileFilter"),
                (string?)States.System.ExportConfigPath);
            if (string.IsNullOrEmpty(configPath))
                return;
            States.System.ExportConfigPath = configPath;
            var configLines = new List<string>();
            // ini 濞?
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
            // 鐎电厧鍤崘鍛啇濞?
            configLines.Add(sperator);
            configLines.AddRange(GetAllRules());
            // 鏉╄棄濮為崘鍛啇濞?
            configLines.Add(sperator);
            configLines.AddRange(GetExtraFileLines());
            // 缂佹挻娼?
            ModBase.WriteFile(configPath, configLines.Join("\r\n"));
            HintService.Hint(Lang.Text("Instance.Export.SaveSuccess", configPath), HintType.Success);
            ModBase.OpenExplorer(configPath);
        }
        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "娣囨繂鐡ㄩ柊宥囩枂婢惰精瑙?,
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Export.Error.OperationFailed"));
        }
    }

    #region 闁板秶鐤嗛弬鍥︽閺嶇绺剧拠璇插絿闁槒绶?

    /// <summary>
    ///     娴犲孩瀵氱€规俺鐭惧鍕嚢閸欐牠鍘ょ純顔芥瀮娴犺绱欐笟娑欏瘻闁筋喖鎷伴幏鏍ㄦ杹鐠嬪啰鏁ら敍?
    /// </summary>
    /// <param name="configPath">闁板秶鐤嗛弬鍥︽鐠侯垰绶?/param>
    private void ReadConfigFile(string configPath)
    {
        try
        {
            // 娣囨繂鐡ㄩ柊宥囩枂閺傚洣娆㈢捄顖氱窞閸掓壆绱︾€?
            States.System.ExportConfigPath = configPath;

            var fileContent = ModBase.ReadFile(configPath);
            var segments = fileContent.Split(sperator);

            if (segments.Length == 0)
            {
                HintService.Hint(Lang.Text("Instance.Export.Config.Invalid"), HintType.Error);
                return;
            }

            // === 鐟欙絾鐎絀NI濞?===
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

            // 鐠у鈧厧鍩岄悾宀勬桨閹貉傛
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

            // === 鐟欙絾鐎界€电厧鍤崘鍛啇濞?===
            RulesOverrides = segments[1].Replace("\r", "\n")
                .Replace("\n" + "\n", "\n").Split("\n").ToList();

            // === 鐟欙絾鐎芥潻钘夊閸愬懎顔愬▓?===
            if (segments.Length > 2)
                extraFiles = segments[2].Replace("\r", "\n")
                    .Replace("\n" + "\n", "\n").Split("\n").ToList();
            else
                extraFiles = null;

            // 閹绘劗銇氶幋鎰
            HintService.Hint(Lang.Text("Instance.Export.ReadSuccess", configPath), HintType.Success);
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                $"鐠囪褰囬柊宥囩枂閺傚洣娆㈡径杈Е閿涙configPath}",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Export.Error.OperationFailed"));
        }
    }

    #endregion

    // 鐠囪褰囬柊宥囩枂閺傚洣娆?
    private void ImportConfig(object sender, PointerReleasedEventArgs e)
    {
        try
        {
            var configPath = SystemDialogs.SelectFile(Lang.Text("Instance.Export.Config.FileFilter"), Lang.Text("Instance.Export.SelectConfigFile"),
                (string?)States.System.ExportConfigPath);
            if (string.IsNullOrEmpty(configPath))
                return;

            // 鐠嬪啰鏁ら弽绋跨妇鐠囪褰囬柅鏄忕帆
            ReadConfigFile(configPath);
        }

        catch (Exception ex)
        {
            ModBase.Log(
                ex,
                "闁瀚ㄩ柊宥囩枂閺傚洣娆㈡径杈Е",
                ModBase.LogLevel.Msgbox,
                userSummary: Lang.Text("Instance.Export.Error.OperationFailed"));
        }
    }

    #region 閹锋牗鏂佹禍瀣╂婢跺嫮鎮?

    /// <summary>
    ///     閺傚洣娆㈤幏鏍у弳閻ｅ矂娼伴弮鎯靶曢崣鎴窗妤犲矁鐦夐弬鍥︽缁鐎?
    /// </summary>
    private void PanAllBack_DragEnter(object sender, DragEventArgs e)
    {
        // 濡偓閺屻儲妲搁崥锕€瀵橀崥顐ｆ瀮娴犺埖瀚嬮弨鐐殶閹?
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // 閼惧嘲褰囬幏鏍у弳閻ㄥ嫭鏋冩禒鎯扮熅瀵板嫭鏆熺紒?
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // 妤犲矁鐦夐敍姘矌閸忎浇顔忛崡鏇氶嚋.txt閺傚洣娆?
            if (files.Length == 1 &&
                files[0].EndsWithF(".txt", true))
                e.Effects = DragDropEffects.Copy; // 鐠佸墽鐤嗛幏鏍ㄦ杹閺佸牊鐏夋稉琛♀偓婊冾槻閸掑灈鈧?
            else
                e.Effects = DragDropEffects.None; // 娑撳秴鍘戠拋鍛婂珛閺€?
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    /// <summary>
    ///     閺傚洣娆㈤弨鍙ョ瑓閺冩儼袝閸欐埊绱扮拠璇插絿闁板秶鐤嗛弬鍥︽
    /// </summary>
    private void PanAllBack_Drop(object sender, DragEventArgs e)
    {
        // 閼惧嘲褰囬幏鏍у弳閻ㄥ嫭鏋冩禒鎯扮熅瀵?
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var configPath = files[0];

            // 鐠嬪啰鏁ら弽绋跨妇鐠囪褰囬柅鏄忕帆
            ReadConfigFile(configPath);
        }

        e.Handled = true;
    }

    #endregion

    #endregion

    #region 鐎电厧鍤?

    /// <summary>
    ///     闁板秶鐤嗛弬鍥︽娑擃厽瀵氱€规氨娈戠€电厧鍤担宥囩枂閵?
    /// </summary>
    private string configPackPath;

    /// <summary>
    ///     瀵偓婵顕遍崙鎭掆偓?
    /// </summary>
    private void StartExport(object sender, PointerReleasedEventArgs e)
    {
        var packName = string.IsNullOrEmpty(TextExportName.Text) ? TextExportName.HintText : TextExportName.Text;
        var packVersion = string.IsNullOrEmpty(TextExportVersion.Text) ? "1.0.0" : TextExportVersion.Text;

        // 闁插秴顦叉禒璇插濡偓閺?
        var loaderName = Lang.Text("Instance.Export.ExportTask.Prefix") + packName;
        foreach (var OngoingLoader in ModLoader.loaderTaskbar)
        {
            if ((OngoingLoader.name ?? "") != (loaderName ?? ""))
                continue;
            ModMain.frmMain.PageChange(FormMain.PageType.TaskManager);
            return;
        }

        // 绾喛顓荤€电厧鍤担宥囩枂
        string packPath = null;
        if (!string.IsNullOrWhiteSpace(configPackPath) && !configPackPath.EndsWithF(@"\") &&
            !configPackPath.EndsWithF("/"))
            try
            {
                Directory.CreateDirectory(ModBase.GetPathFromFullPath(configPackPath));
                packPath = configPackPath;
                ModBase.Log($"[Export] 娴ｈ法鏁ら柊宥囩枂閺傚洣娆㈡稉顓熷瘹鐎规氨娈戠€电厧鍤捄顖氱窞閿涙configPackPath}");
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"閺冪姵纭舵担璺ㄦ暏闁板秶鐤嗛弬鍥︽娑擃厽瀵氱€规氨娈戠€电厧鍤捄顖氱窞閿涘澖configPackPath}閿?);
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
            ModBase.Log($"[Export] 閹靛濮╅幐鍥х暰閻ㄥ嫬顕遍崙楦跨熅瀵板嫸绱皗packPath}");
        }

        if (string.IsNullOrEmpty(packPath))
            return;

        // 缂傛挸鐡ㄩ幍鈧棁鈧崣鍌涙殶
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
        ModBase.Log($"[Export] 閸戝棗顦€电厧鍤弫鏉戞値閸栧拑绱濋崗杈ㄦ箒 {allRules.Count} 閺壜ゎ潐閸掓瑱绱漿allExtraFiles.Count} 閺壜ゆ嫹閸旂姴鍞寸€圭顢?);

        // 閺嬪嫰鈧姵顒炴銈呭鏉炶棄娅?
        var loaders = new List<ModLoader.LoaderBase>();

        #region 閸戝棗顦?PCL 閺傚洣娆?
        
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

        #region 婢跺秴鍩楅弬鍥︽

        loaders.Add(new ModLoader.LoaderTask<int, List<ModLocalComp.LocalCompFile>>(
            Lang.Text("Instance.Export.Task.CopyContent"), loader =>
            {
                loader.output = [];
                // 婢跺秴鍩楃€圭偘绶ラ弬鍥︽
                var progress = 0;
            Action<DirectoryInfo> searchFolder = null;
            searchFolder = folder =>
            {
                // 閺傚洣娆㈡径鐧哥窗鏉╂稐绔村銉︽偝缁?
                foreach (var SubFolder in folder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    // 鐠哄疇绻冮柈銊ュ瀻閸欏牊鐥呴悽銊︽瀮娴犺泛寮垫径姘辨畱閺傚洣娆㈡径鐧哥礉閸旂姴鎻╅幖婊呭偍
                    if ((folder.FullName ?? "") == (pathIndie ?? "") &&
                        new[] { "assets", "versions", "libraries" }.Contains(SubFolder.Name))
                        continue;
                    if (new[] { "structureCacheV1", ".fabric", ".git", "avatar-cache", "cosmetic-cache" }.Contains(
                            SubFolder.Name))
                        continue;
                    searchFolder(SubFolder);
                }

                // 閺傚洣娆㈤敍姘梾閺屻儴顫夐崚娆忚嫙婢跺秴鍩?
                foreach (var Entry in folder.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    var relativePath = Entry.FullName.AfterFirst(pathIndie);
                    // 濡偓閺屻儴顫夐崚?
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
                    // 閼汇儰璐熼崢瀣級閸栧拑绱濋懓鍐閼辨梻缍夐懢宄板絿鐠侯垰绶?
                    if (checkHostedAssets &&
                        new[] { ".zip", ".rar", ".jar", ".disabled", ".old" }.Contains(Entry.Extension.ToLower()) &&
                        new[] { "mods", "packs", "openloader", "resource" }.Any(s => relativePath.Contains(s)))
                    {
                        var modFile = new ModLocalComp.LocalCompFile(targetPath);
                        var unused = modFile.ModrinthHash; // 閹绘劕澧犵拋锛勭暬 Hash
                        unused = modFile.CurseForgeHash.ToString();
                        loader.output.Add(modFile);
                    }

                    // 閺囧瓨鏌婃潻娑樺閿涘牐绻樻惔锕€鑻熸稉宥呭櫙绾噯绱濇稉鏄忣洣缁愪礁鍤稉鈧稉顏呭灉鏉╂ɑ鐥呮导纭风礆
                    progress += 1;
                    if (progress == 25)
                    {
                        loader.Progress += (0.94d - loader.Progress) * 0.012d;
                        progress = 0;
                    }
                }
            };
            searchFolder(new DirectoryInfo(pathIndie));
            ModBase.Log($"[Export] 婢跺秴鍩?overrides 閺傚洣娆㈢€瑰本鍨氶敍灞炬箒 {loader.output.Count} 娑擃亝鏋冩禒鍫曟付鐟曚浇浠堢純鎴烆梾閺?);
            loader.Progress = 0.95d;
            // 婢跺秴鍩楁潻钘夊閸愬懎顔愰崚鐗堢壌閻╊喖缍?
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
            // 婢跺秴鍩?PCL 鐎圭偘绶ョ拋鍓х枂
            ModBase.CopyDirectory(Path.Combine(mcInstance.PathInstance, "PCL"), Path.Combine(overridesFolder, "PCL"));
            #if RELEASE
                        // 婢跺秴鍩?PCL 閺堫兛缍?
                        if (includePCL) ModBase.CopyFile(Basics.ExecutablePath, Path.Combine(cacheFolder, Basics.ExecutableName));
            #endif
            // 婢跺秴鍩?PCL 娑擃亝鈧冨閸愬懎顔?
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

        #region 閼辨梻缍夊Λ鈧弻?

        loaders.Add(
            new ModLoader.LoaderTask<List<ModLocalComp.LocalCompFile>,
                Dictionary<ModLocalComp.LocalCompFile, List<string>>>(Lang.Text("Instance.Export.Task.FetchFileInfo"),
                loader =>
                {
                    loader.output = new Dictionary<ModLocalComp.LocalCompFile, List<string>>();
                    if (!checkHostedAssets)
                    {
                        ModBase.Log("[Export] 鐟曚焦鐪扮捄瀹犵箖閼辨梻缍夐懢宄板絿濮濄儵顎?);
                        return;
                    }

                    if (!loader.input.Any())
                    {
                        ModBase.Log("[Export] 濞屸剝婀侀棁鈧憰浣戒粓缂冩垶顥呴弻銉ф畱閺傚洣娆㈤敍宀冪儲鏉╁洩浠堢純鎴ｅ箯閸欐牗顒炴?);
                        return;
                    }

                    // 閸掑棗閽╅崣鎷屽箯閸欐牔绗呮潪钘夋勾閸р偓
                    var endedThreadCount = 0;
                    var failedExceptions = new List<Exception>();

                    // 娴?Modrinth 閼惧嘲褰囨穱鈩冧紖
                    // 閺屻儲澹樼€电懓绨查惃鍕瀮娴?
                    // 閸愭瑥鍙嗘稉瀣祰閸︽澘娼?
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

                            ModBase.Log($"[Export] 娴?Modrinth 閼惧嘲褰囬崚?{modrinthRaw.Count} 娑擃亝婀伴崷鎷岀カ濠ф劙銆嶉惃鍕嚠鎼存柧淇婇幁?);
                        }
                        catch (Exception ex)
                        {
                            ModBase.Log(ex, "娴?Modrinth 閼惧嘲褰囬張顒€婀?Mod 娣団剝浼呮径杈Е");
                            failedExceptions.Add(ex);
                        }
                        finally
                        {
                            endedThreadCount += 1;
                            loader.Progress += 0.45d;
                        }
                    }, "Modrinth - " + loaderName);

                    // 娴?CurseForge 閼惧嘲褰囨穱鈩冧紖
                    // 閺屻儲澹樼€电懓绨查惃鍕瀮娴?
                    // 閸愭瑥鍙嗘稉瀣祰閸︽澘娼?
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

                            ModBase.Log($"[Export] 娴?CurseForge 閼惧嘲褰囬崚?{curseForgeRaw.AsArray().Count} 娑擃亝婀伴崷鎷岀カ濠ф劙銆嶉惃鍕嚠鎼存柧淇婇幁?);
                        }
                        catch (Exception ex)
                        {
                            ModBase.Log(ex, "娴?CurseForge 閼惧嘲褰囬張顒€婀?Mod 娣団剝浼呮径杈Е");
                            failedExceptions.Add(ex);
                        }
                        finally
                        {
                            endedThreadCount += 1;
                            loader.Progress += 0.45d;
                        }
                    }, "CurseForge - " + loaderName); // Modrinth 娑撳﹣绱跺Ο鈥崇础娑撳绱濇稉宥堝厴娴?CurseForge 閼惧嘲褰囨穱鈩冧紖

                    // 缁涘绶熺痪璺ㄢ柤缂佹挻娼?
                    while (endedThreadCount != 2)
                    {
                        if (loader.IsAborted)
                            return;
                        Thread.Sleep(10);
                    }

                    // 閼汇儱銇戠拹銉礉绾喛顓婚弰顖氭儊缂佈呯敾
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

        #region 閻㈢喐鍨氶崢瀣級閸?

        loaders.Add(new ModLoader.LoaderTask<Dictionary<ModLocalComp.LocalCompFile, List<string>>, int>(
            Lang.Text("Instance.Export.Task.CreateArchive"),
            loader =>
            {
                // 閺佸鎮婇弬鍥︽閸掓銆?
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
                // 鐎电厧鍤張鈧紒?JSON 閺傚洣娆?
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
                // 閹垫挸瀵?
                Directory.CreateDirectory(ModBase.GetPathFromFullPath(packPath));
                if (File.Exists(packPath))
                    File.Delete(packPath);
                if (includePCL)
                {
                    // 妫ｆ牗顐奸崢瀣級閺佹潙鎮庨崠?
                    ZipFile.CreateFromDirectory(Path.Combine(cacheFolder, "modpack"), Path.Combine(cacheFolder, "modpack.mrpack"));
                    loader.Progress = 0.5d;
                    Directory.Delete(Path.Combine(cacheFolder, "modpack"), true);
                    loader.Progress = 0.6d;
                    // 娴滃本顐奸崢瀣級閺佹潙鎮庨崠?
                    ZipFile.CreateFromDirectory(cacheFolder, packPath);
                    loader.Progress = 0.9d;
                }
                else
                {
                    // 閻╁瓨甯撮崢瀣級閺佹潙鎮庨崠?
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

        // 閸氼垰濮?
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
