#!/bin/bash
# 在真机上跑 PCL 并把 UiDump 诊断输出打印出来
export DISPLAY=:0
XA=$(ls /run/user/1000/.mutter-Xwaylandauth.* 2>/dev/null | head -1)
[ -n "$XA" ] && export XAUTHORITY="$XA"

pkill -9 -x PCL 2>/dev/null
sleep 1
cd /tmp/linux-x64-flat || exit 1
rm -rf PCL-Data
setsid ./PCL --opacity1 --skip-eula --uidump > /tmp/pcl_out.log 2>&1 < /dev/null &
sleep 24

echo "----- UiDump 输出 -----"
grep -a -A9 'UiDump' /tmp/pcl_out.log | head -60

echo "----- 窗口内容 -----"
WID=$(xwininfo -root -tree 2>/dev/null | grep -a '"PCL' | grep -av mutter | awk '{print $1}' | head -1)
if [ -n "$WID" ]; then
  import -window "$WID" /tmp/shot_diag.png 2>&1 | head -1
  convert /tmp/shot_diag.png -format "尺寸=%wx%h 颜色数=%[colors]" info: 2>/dev/null; echo ""
fi
echo "----- 资源 -----"
PID=$(pgrep -x PCL | head -1)
[ -n "$PID" ] && ps -p "$PID" -o pcpu,rss,nlwp --no-headers
pkill -9 -x PCL 2>/dev/null
