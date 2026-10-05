#!/bin/bash
# 采样 PCL 进程的 CPU / 内存 / 线程数，用于确认"内存泄漏 + CPU 暴涨"
#
# 用法（在真机上）：
#   bash measure-cpu-mem.sh            # 默认跑 32 秒，每 4 秒采样一次
#   bash measure-cpu-mem.sh 60 5       # 跑 60 秒，每 5 秒采样一次
#
# 判据：正常情况下内存应该稳定；如果每秒涨 100MB+ 就是命中了那个泄漏问题。

export DISPLAY=:0
XA=$(ls /run/user/1000/.mutter-Xwaylandauth.* 2>/dev/null | head -1)
[ -n "$XA" ] && export XAUTHORITY="$XA"

DURATION=${1:-32}
INTERVAL=${2:-4}
DIR=/tmp/linux-x64-flat

pkill -9 -x PCL 2>/dev/null
sleep 1
cd "$DIR" || { echo "找不到 $DIR"; exit 1; }

setsid ./PCL "$@" > /tmp/pcl_measure.log 2>&1 < /dev/null &
sleep 3

PID=$(pgrep -x PCL | head -1)
[ -z "$PID" ] && { echo "未找到 PCL 进程"; exit 1; }
echo "PID=$PID"
printf "%-8s %8s %10s %8s %10s\n" "时间" "CPU%" "内存MB" "线程数" "累计CPU"

TICKS=$(getconf CLK_TCK)
START=$(date +%s)
while [ $(( $(date +%s) - START )) -lt "$DURATION" ]; do
    [ -d "/proc/$PID" ] || { echo "进程已退出"; break; }
    read -r _ _ _ _ _ _ _ _ _ _ _ _ _ U S _ < "/proc/$PID/stat" 2>/dev/null
    RSS_KB=$(awk '/VmRSS/{print $2}' "/proc/$PID/status" 2>/dev/null)
    NLWP=$(awk '/Threads/{print $2}' "/proc/$PID/status" 2>/dev/null)
    CPU=$(ps -p "$PID" -o pcpu= 2>/dev/null | tr -d ' ')
    T=$(( $(date +%s) - START ))
    printf "t=%-6s %8s %10s %8s %10s\n" "${T}s" "$CPU" "$(( ${RSS_KB:-0} / 1024 ))" "$NLWP" \
        "$(awk -v t="${U:-0}" -v s="${S:-0}" -v c="$TICKS" 'BEGIN{printf "%.1fs", (t+s)/c}')"
    sleep "$INTERVAL"
done

echo "--- 日志 ---"
f=$(find PCL-Data -name 'Launch-*.log' 2>/dev/null | head -1)
[ -n "$f" ] && echo "行数=$(wc -l < "$f") FTL=$(grep -ac 'FTL!' "$f") ERR=$(grep -ac 'ERR!' "$f")"
pkill -9 -x PCL 2>/dev/null
