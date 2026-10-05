#!/usr/bin/env bash
# 在 Xvfb 虚拟显示里运行 PCL 并截图，用于确认界面是否真的渲染
set -u
export DISPLAY=:99
DIR=/mnt/d/a/j/PCL.CE-Linux/dist/linux-x64-flat

pkill -9 -f "x64-flat/PCL" 2>/dev/null
pkill -9 Xvfb 2>/dev/null
sleep 1

setsid Xvfb :99 -screen 0 1280x800x24 > /tmp/xvfb.log 2>&1 < /dev/null &
sleep 3

echo "=== Xvfb 状态 ==="
xwininfo -root 2>&1 | head -4 || echo "无法连接 :99"

cd "$DIR" || exit 1
rm -rf PCL-Data
setsid ./PCL --skip-eula > /mnt/d/a/j/_xvfb_run.txt 2>&1 < /dev/null &
sleep 14

echo "=== 窗口树 ==="
xwininfo -root -tree 2>&1 | head -14

echo "=== 截图 ==="
import -window root /mnt/d/a/j/shot_xvfb.png 2>&1 | head -2
ls -la /mnt/d/a/j/shot_xvfb.png 2>/dev/null || echo "(截图失败)"

echo "=== 日志 ==="
f=$(find PCL-Data -name "Launch-*.log" 2>/dev/null | head -1)
[ -n "$f" ] && echo "$(wc -l < "$f") 行 | FTL=$(grep -ac 'FTL!' "$f") | ERR=$(grep -ac 'ERR!' "$f")"

pkill -9 -f "x64-flat/PCL" 2>/dev/null
echo done
