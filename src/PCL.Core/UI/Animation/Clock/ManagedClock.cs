using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.UI.Animation.Clock;

// [port] 原 WinMMClock：winmm 多媒体定时器 → 托管 PeriodicTimer（跨平台，帧率语义不变）。

public sealed class ManagedClock(int fps = 60) : IClock, IDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private long _frameIndex;

    public event EventHandler<long>? Tick;

    public int Fps { get; set; } = fps;

    public bool IsRunning => _loop is not null && !_loop.IsCompleted;

    ~ManagedClock()
    {
        Dispose();
    }

    public void Start()
    {
        if (IsRunning) return;

        _frameIndex = 0;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _loop = Task.Run(async () =>
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(1, 1000.0 / Fps)));
                while (await timer.WaitForNextTickAsync(token))
                {
                    _frameIndex++;
                    Tick?.Invoke(this, _frameIndex);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止
            }
        }, CancellationToken.None);
    }

    public void Stop()
    {
        if (_cts is null) return;

        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
