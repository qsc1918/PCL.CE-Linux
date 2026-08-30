using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace PCL.Core.UI;

/// <summary>
/// [port] WPF System.Windows.MessageBox（PCL.Core 崩溃兜底弹窗用）→ 轻量 Avalonia 对话框。
/// 崩溃路径下不阻塞调用线程（fire-and-forget），无可用窗口时降级为控制台输出。
/// </summary>
public static class CrashMessageBox
{
    public static void Show(string message, string caption)
    {
        try
        {
            Dispatcher.UIThread.Post(() => _ShowCore(message, caption));
        }
        catch
        {
            Console.Error.WriteLine($"[{caption}] {message}");
        }
    }

    private static void _ShowCore(string message, string caption)
    {
        try
        {
            var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var dialog = new Window
            {
                Title = caption,
                MinWidth = 420,
                MaxWidth = 640,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 14,
                    Children =
                    {
                        new ScrollViewer
                        {
                            MaxHeight = 320,
                            Content = new SelectableTextBlock
                            {
                                Text = message,
                                TextWrapping = TextWrapping.Wrap
                            }
                        },
                        new Button
                        {
                            Content = "确定",
                            HorizontalAlignment = HorizontalAlignment.Right,
                            MinWidth = 88
                        }
                    }
                }
            };

            if (dialog.Content is StackPanel panel && panel.Children[^1] is Button ok)
            {
                ok.Click += (_, _) => dialog.Close();
            }

            if (owner is not null && owner.IsVisible)
                dialog.ShowDialog(owner);
            else
                dialog.Show();
        }
        catch
        {
            Console.Error.WriteLine($"[{caption}] {message}");
        }
    }
}
