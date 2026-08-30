using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace PCL;

/// <summary>
/// [port] WPF System.Windows.SplashScreen（Windows 专属启动画面）→ 轻量占位实现。
/// 上游仅用 Show(false, true) 显示内嵌图标启动图；Avalonia 版暂以空实现保持调用点编译，
/// 真正的启动画面窗口可作为后续增强（属于视觉细节，不阻塞功能）。
/// </summary>
public class SplashScreen
{
    public SplashScreen(string resourcePath)
    {
        IconPath = resourcePath;
    }

    public string IconPath { get; }

    public void Show(bool autoClose, bool useFastExit)
    {
        // [port] 占位实现：Avalonia 启动流程在窗口创建前无法独立显示启动图
    }

    public void Close()
    {
    }
}
