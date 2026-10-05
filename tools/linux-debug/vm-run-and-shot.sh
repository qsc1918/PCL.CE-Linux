#!/bin/bash
# 在真机上运行 PCL（可带参数）并截图，报告窗口内容
export DISPLAY=:0
XA=$(ls /run/user/1000/.mutter-Xwaylandauth.* 2>/dev/null | head -1)
[ -n "$XA" ] && export XAUTHORITY="$XA"

ARGS="$@"
TAG="${ARGS:-default}"
TAG=$(echo "$TAG" | tr ' /' '__')

pkill -9 -x PCL 2>/dev/null
sleep 1
cd /tmp/linux-x64-flat || exit 1
rm -rf PCL-Data
setsid ./PCL $ARGS > /tmp/pcl_out.log 2>&1 < /dev/null &
sleep 18

echo "=== 参数: [$ARGS] ==="
WID=$(xwininfo -root -tree 2>/dev/null | grep -a '"PCL' | grep -av mutter | awk '{print $1}' | head -1)
echo "PCL 窗口ID=$WID"
if [ -n "$WID" ]; then
  OUT="/tmp/shot_${TAG}.png"
  import -window "$WID" "$OUT" 2>&1 | head -1
  ls -la "$OUT" 2>/dev/null
  convert "$OUT" -format "尺寸=%wx%h 颜色数=%[colors]" info: 2>/dev/null; echo ""
  convert "$OUT" -crop 1x1+450+275 -format "中心像素=%[pixel:p{0,0}]" info: 2>/dev/null; echo ""
  convert "$OUT" -crop 1x1+60+40 -format "左上像素=%[pixel:p{0,0}]" info: 2>/dev/null; echo ""
else
  echo "未找到 PCL 窗口"
fi
echo "=== 日志 ==="
f=$(find PCL-Data -name 'Launch-*.log' 2>/dev/null | head -1)
[ -n "$f" ] && echo "行数=$(wc -l < "$f") FTL=$(grep -ac 'FTL!' "$f") ERR=$(grep -ac 'ERR!' "$f")"
echo "=== 内存/CPU ==="
PID=$(pgrep -x PCL | head -1)
[ -n "$PID" ] && ps -p "$PID" -o pcpu,rss --no-headers
pkill -9 -x PCL 2>/dev/null
