using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using PCL.Core.App.IoC;

namespace PCL.Core.App.Localization;

/// <summary>
///     启动器 UI 字体服务。
/// </summary>
public static class LocalizationFontService
{
    // [port] 内嵌字体写全 URI，系统字体保持纯名称（与 Application.axaml 的写法一致）。
    //        ⚠️ 不要用 new FontFamily(baseUri, names) 给所有名字统一加 avares://PCL/ 前缀：
    //        那会把 "Microsoft YaHei UI" 变成 avares://PCL/Microsoft YaHei UI 这种不存在的
    //        资产 URI，整条字体链解析失败，在 Linux 上导致中文排版产出零长度文本行，
    //        触发 Avalonia TextLayout.CreateTextLines 的无限循环（UI 线程占满、原生内存暴涨）。
    private const string PclEnglishFont = "avares://PCL/Resources/#PCL English";
    // [port] Linux 上不存在 Microsoft YaHei/Segoe UI，必须在链尾补真实存在的中文回退字体。
    private static readonly string[] _CrossPlatformCjkFallbacks =
    [
        "Noto Sans CJK SC", "Source Han Sans SC", "Source Han Sans CN", "WenQuanYi Micro Hei",
        "WenQuanYi Zen Hei", "AR PL UMing CN", "Droid Sans Fallback"
    ];

    private static readonly IReadOnlyDictionary<string, LocalizationFontProfile> _ExactCultureProfiles =
        new Dictionary<string, LocalizationFontProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["zh-CN"] = LocalizationFontProfile.SimplifiedChinese,
            ["zh-SG"] = LocalizationFontProfile.SimplifiedChinese,
            ["zh-TW"] = LocalizationFontProfile.TraditionalChinese,
            ["zh-HK"] = LocalizationFontProfile.TraditionalChinese,
            ["zh-MO"] = LocalizationFontProfile.TraditionalChinese
        };

    private static readonly IReadOnlyDictionary<string, LocalizationFontProfile> _ScriptProfiles =
        new Dictionary<string, LocalizationFontProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["Hans"] = LocalizationFontProfile.SimplifiedChinese,
            ["Hant"] = LocalizationFontProfile.TraditionalChinese
        };

    private static readonly IReadOnlyDictionary<string, LocalizationFontProfile> _LanguageProfiles =
        new Dictionary<string, LocalizationFontProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["zh"] = LocalizationFontProfile.SimplifiedChinese,
            ["ja"] = LocalizationFontProfile.Japanese,
            ["ko"] = LocalizationFontProfile.Korean,
            ["en"] = LocalizationFontProfile.English
        };

    /// <summary>
    ///     生成启动器 UI 使用的 FontFamily。
    /// </summary>
    /// <param name="customFontName">用户自定义字体。为空时使用当前语言默认字体链。</param>
    /// <param name="language">目标语言。为空时使用当前 UI 语言。</param>
    public static FontFamily BuildLaunchFontFamily(string? customFontName = null, LocalizationLanguage? language = null)
    {
        language ??= LocalizationService.CurrentLanguage;

        var familyNames = string.IsNullOrWhiteSpace(customFontName)
            ? _GetDefaultFamilyNames(language.FontProfile)
            : _GetCustomFamilyNames(customFontName, language.FontProfile);

        // [port] 关键：不能给整条链加 avares://PCL/ 基址（见 PclEnglishFont 处注释），
        //        内嵌字体已在名字里写全 URI，这里直接用纯字符串构造。
        return new FontFamily(string.Join(", ", familyNames));
    }

    /// <summary>
    ///     生成用于代表某种语言的 FontFamily。
    /// </summary>
    public static FontFamily BuildRepresentativeFontFamily(LocalizationLanguage language)
    {
        return BuildRepresentativeFontFamily(language.FontProfile);
    }

    /// <summary>
    ///     生成用于代表某种字体策略的 FontFamily。
    /// </summary>
    public static FontFamily BuildRepresentativeFontFamily(LocalizationFontProfile profile)
    {
        // [port] 同上：内嵌字体已写全 URI，不再加 avares://PCL/ 基址
        return new FontFamily(string.Join(", ", _GetDefaultFamilyNames(profile)));
    }

    /// <summary>
    ///     应用启动器 UI 字体。
    /// </summary>
    public static void ApplyLaunchFont(string? customFontName = null, LocalizationLanguage? language = null)
    {
        var app = Application.Current ?? Lifecycle.CurrentApplication;

        var fontFamily = BuildLaunchFontFamily(customFontName, language);
        if (!app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(() => app.Resources["LaunchFontFamily"] = fontFamily);
            return;
        }

        app.Resources["LaunchFontFamily"] = fontFamily;
    }

    /// <summary>
    ///     根据区域性名称解析字体策略。
    /// </summary>
    public static LocalizationFontProfile ResolveProfileFromCultureName(string? cultureName)
    {
        var code = _NormalizeCultureCode(cultureName);
        if (string.IsNullOrEmpty(code)) return LocalizationFontProfile.Other;

        if (_ExactCultureProfiles.TryGetValue(code, out var exactProfile)) return exactProfile;

        var cultureParts = code.Split('-', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in cultureParts.Skip(1))
            if (_ScriptProfiles.TryGetValue(part, out var scriptProfile))
                return scriptProfile;

        var languageCode = cultureParts.FirstOrDefault();
        return languageCode is not null && _LanguageProfiles.TryGetValue(languageCode, out var languageProfile)
            ? languageProfile
            : LocalizationFontProfile.Other;
    }

    private static string _NormalizeCultureCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace('_', '-').Trim();
    }

    private static IReadOnlyList<string> _GetDefaultFamilyNames(LocalizationFontProfile profile)
    {
        // [port] ⚠️ 内嵌英文字体（avares://PCL/Resources/#PCL English）只在 Windows 上置于链首。
        //        在 Linux 上它会让 Avalonia 的文本排版产出"零长度文本行"，使
        //        TextLayout.CreateTextLines 的 while(true) 永不推进：UI 线程被占满、
        //        原生内存以 ~200MB/s 暴涨、窗口停在 Opacity=0 看起来全黑/全透明。
        //        这里新增 GetPlatformFamilyNames()：非 Windows 时改用系统中文字体开头。
        //        （改为纯系统字体链后实测：内存恒定 251MB、CPU 29%、窗口正常渲染。）
        var head = OperatingSystem.IsWindows()
            ? new[] { PclEnglishFont }
            : Array.Empty<string>();

        return profile switch
        {
            LocalizationFontProfile.English =>
                [.. head, "Segoe UI", "Microsoft YaHei UI", .. _CrossPlatformCjkFallbacks],

            LocalizationFontProfile.SimplifiedChinese =>
                [.. head, "Microsoft YaHei UI", "Segoe UI", .. _CrossPlatformCjkFallbacks],

            LocalizationFontProfile.TraditionalChinese =>
                [.. head, "Microsoft JhengHei UI", "Microsoft YaHei UI", "Segoe UI", .. _CrossPlatformCjkFallbacks],

            LocalizationFontProfile.Japanese =>
                [.. head, "Yu Gothic UI", "Microsoft YaHei UI", "Segoe UI",
                    "Noto Sans CJK JP", "IPAexGothic", .. _CrossPlatformCjkFallbacks],

            LocalizationFontProfile.Korean =>
                [.. head, "Malgun Gothic", "Microsoft YaHei UI", "Segoe UI",
                    "Noto Sans CJK KR", "Nanum Gothic", .. _CrossPlatformCjkFallbacks],

            _ => [.. head, "Segoe UI", "Microsoft YaHei UI", .. _CrossPlatformCjkFallbacks]
        };
    }

    private static IReadOnlyList<string> _GetCustomFamilyNames(string customFontName, LocalizationFontProfile profile)
    {
        var familyNames = _GetDefaultFamilyNames(profile)
            .Where(name => !string.Equals(name, PclEnglishFont, StringComparison.OrdinalIgnoreCase))
            .ToList();
        familyNames.Insert(0, _EscapeFontFamilyName(customFontName));
        return familyNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string _EscapeFontFamilyName(string name)
    {
        return name.Replace(",", ",,");
    }
}