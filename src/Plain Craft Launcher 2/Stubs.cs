using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

// [port] BlurBorder / MotdRenderer 轻量桩已迁移到 PCL.Core 程序集（src\PCL.Core\UI\Controls\Stubs.cs），
// 以匹配 XAML 引用 clr-namespace:PCL.Core.UI.Controls;assembly=PCL.Core。此处仅保留 PCL 命名空间下的桩。

namespace PCL
{
    /// <summary>Stub for ModVideoBack (video background feature, deferred).</summary>
    public class ModVideoBack
    {
        public static bool isMinimized = false; // 窗口是否被最小化

        public static bool IsGaming { get; set; }
        public static bool ForcePlay { get; set; }

        public static event EventHandler<BooleanEventArgs> GamingStateChanged;
        public static event EventHandler<BooleanEventArgs> ForcePlayChanged;

        public static void OnGamingStateChanged(object sender, BooleanEventArgs e) { }
        public static void OnForcePlayChanged(object sender, BooleanEventArgs e) { }

        // [port] 视频背景暂缓移植，空实现
        public static void VideoPlay() { }
        public static void VideoStop() { }
        public static void VideoPause() { }
        public static void Stop() { }
        public static void Play(string path, bool isLoop = true) { }
        public static bool IsPlaying => false;

        public class BooleanEventArgs : EventArgs
        {
            public BooleanEventArgs(bool value) { Value = value; }
            public bool Value { get; set; }
        }
    }

    /// <summary>Stub for ModMusic (music player, deferred).</summary>
    public class ModMusic
    {
        // [port] 音乐功能暂缓移植，空实现
        public static object musicNAudio;
        public static List<string> musicWaitingList = new();
        public static List<string> musicAllList = new();

        // [port] 音乐功能暂缓移植，空实现；以下成员仅用于保持调用点编译。
        public static void MusicRefreshPlay(bool showHint, bool isFirstLoad = false) { }
        public static void MusicRefreshUI() { }
        public static void MusicControlPause() { }
        public static void MusicControlNext() { }
        public static void Stop() { }
        public static bool IsPlaying => false;
    }

    /// <summary>Stub for ModSkin (Minecraft skin download, deferred).</summary>
    public class ModSkin
    {
        // [port] 皮肤功能暂缓移植，空实现
        public static string McSkinGetAddress(string uuid, string type) => "";
        public static string McSkinDownload(string address) => "";
        public static string McSkinSex(string uuid) => "";
    }

    /// <summary>
    /// [port] WPF MyColor ↔ Avalonia IBrush 转换辅助。
    /// WPF 中 MyColor→Brush 是隐式转换；Avalonia 中 MyColor 只隐式到 Brush/SolidColorBrush，
    /// 无法隐式链接到 IBrush（C# 不允许链式用户定义转换），故需要显式辅助方法完成映射。
    /// </summary>
    public static class MyColorExtensions
    {
        public static Brush ToBrush(this ModBase.MyColor color) => new SolidColorBrush(
            Avalonia.Media.Color.FromArgb(
                ClampByte(color.a),
                ClampByte(color.r),
                ClampByte(color.g),
                ClampByte(color.b)));

        public static ModBase.MyColor ToColor(this IBrush brush) => new ModBase.MyColor(brush);

        private static byte ClampByte(double d) => (byte)System.Math.Clamp((int)System.Math.Round(d), 0, 255);
    }

    /// <summary>
    /// [port] WPF Template.FindName(name, this) → Avalonia 通过控件名称作用域查找模板内命名元素。
    /// Avalonia 12 的 IControlTemplate 无 FindName 扩展（Avalonia 11 曾提供），改用 NameScope 查找等价元素。
    /// </summary>
    public static class ControlTemplateExtensions
    {
        public static object FindName(this IControlTemplate template, string name, Control control)
            => control.FindNameScope()?.Find(name);
    }
}
