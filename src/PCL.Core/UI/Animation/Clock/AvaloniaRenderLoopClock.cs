using System;
using Avalonia.Threading;

namespace PCL.Core.UI.Animation.Clock;

// [port] 原 WpfCompositionTargetRenderingClock：WPF CompositionTarget.Rendering → Avalonia DispatcherTimer(Render 优先级)。
// 保留上游契约：所有 Tick 均在 UI 线程触发，帧号按 Fps 折算。

public class AvaloniaRenderLoopClock(int fps = 60) : IUIClock, IDisposable
{
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render)
    {
        Interval = TimeSpan.FromMilliseconds(Math.Max(1, 1000.0 / Math.Min(fps, 1000)))
    };
    private long _lastFrame;

    public event EventHandler<long>? Tick;

    public int Fps { get; set; } = fps;

    public bool IsRunning => _timer.IsEnabled;

    ~AvaloniaRenderLoopClock()
    {
        Dispose();
    }

    public void Start()
    {
        if (IsRunning) return;

        _timer.Tick += _OnTick;
        _timer.Start();
    }

    private void _OnTick(object? sender, EventArgs e)
    {
        _lastFrame++;
        Tick?.Invoke(this, _lastFrame);
    }

    public void Stop()
    {
        _timer.Tick -= _OnTick;
        _timer.Stop();
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
