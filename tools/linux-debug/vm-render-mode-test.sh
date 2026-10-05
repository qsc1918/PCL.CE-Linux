#!/bin/bash
# 在真机上测试不同渲染模式
export DISPLAY=:0
XA=$(ls /run/user/1000/.mutter-Xwaylandauth.* 2>/dev/null | head -1)
[ -n "$XA" ] && export XAUTHORITY="$XA"

pkill -9 -f 'PCL1' 2>/dev/null
pkill -9 -f 'x64-flat/PCL' 2>/dev/null
sleep 1

MODE="${1:-default}"
cd /tmp/linux-x64-flat || exit 1
if [ "$MODE" = "software" ]; then
  export PCL_FORCE_SOFTWARE_RENDER=1
fi

setsid ./PCL > /tmp/pcl_run.log 2>&1 < /dev/null &
sleep 18

echo "=== 模式: $MODE ==="
WID=$(xwininfo -root -tree 2>/dev/null | grep -a '"PCL' | grep -av 'mutter' | awk '{print $1}' | head -1)
echo "PCL 窗口ID=$WID"
if [ -n "$WID" ]; then
  import -window "$WID" "/tmp/shot_${MODE}.png" 2>&1 | head -1
  ls -la "/tmp/shot_${MODE}.png" 2>/dev/null
  convert "/tmp/shot_${MODE}.png" -format "尺寸=%wx%h 颜色数=%[colors]" info: 2>/dev/null
  echo ""
  convert "/tmp/shot_${MODE}.png" -crop 1x1+450+275 -format "中心像素=%[pixel:p{0,0}]" info: 2>/dev/null
  echo ""
fi
echo "=== 程序输出 ==="
tail -4 /tmp/pcl_run.log
pkill -9 -f 'x64-flat/PCL' 2>/dev/null
