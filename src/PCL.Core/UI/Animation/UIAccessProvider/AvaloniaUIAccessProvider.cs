using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace PCL.Core.UI.Animation.UIAccessProvider;

// [port] 原 WpfUIAccessProvider：WPF Dispatcher/CompositionTarget.Rendering → Avalonia Dispatcher.UIThread。
// FrameTick 改由 DispatcherTimer 提供（UI 线程按需启停），供需要渲染节拍的事件订阅者使用。

public sealed class AvaloniaUIAccessProvider(Dispatcher dispatcher) : IUIAccessProvider
{
    private readonly Dispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    private DispatcherTimer? _frameTimer;
    private int _frameSubscribers;

    public bool CheckAccess() => _dispatcher.CheckAccess();

    public void Invoke(Action action)
    {
        if (_dispatcher.CheckAccess())
            action();
        else
            _dispatcher.Invoke(action, DispatcherPriority.Send);
    }

    public Task InvokeAsync(Action action)
    {
        if (!_dispatcher.CheckAccess()) return _dispatcher.InvokeAsync(() => { action(); return Task.CompletedTask; }, DispatcherPriority.Send);
        action();
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        return _dispatcher.CheckAccess() ? Task.FromResult(func()) : _dispatcher.InvokeAsync(() => Task.FromResult(func()), DispatcherPriority.Send);
    }

    public Task InvokeAsync(Func<Task> func)
    {
        return _dispatcher.CheckAccess() ? func() : _dispatcher.InvokeAsync(func, DispatcherPriority.Send);
    }

    public Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        return _dispatcher.CheckAccess() ? func() : _dispatcher.InvokeAsync(func, DispatcherPriority.Send);
    }

    public event EventHandler? FrameTick
    {
        add
        {
            Invoke(() =>
            {
                _frameTimer ??= _CreateFrameTimer();
                _frameTimer.Tick += value;
                if (++_frameSubscribers == 1) _frameTimer.Start();
            });
        }
        remove
        {
            Invoke(() =>
            {
                if (_frameTimer is null) return;
                _frameTimer.Tick -= value;
                if (--_frameSubscribers <= 0)
                {
                    _frameSubscribers = 0;
                    _frameTimer.Stop();
                }
            });
        }
    }

    private DispatcherTimer _CreateFrameTimer()
    {
        var timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000d / 60d)
        };
        return timer;
    }
}
