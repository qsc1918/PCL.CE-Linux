#!/bin/bash
# 在真机上分析 PCL 的线程状态：UI 线程是否在空转
export DISPLAY=:0
XA=$(ls /run/user/1000/.mutter-Xwaylandauth.* 2>/dev/null | head -1)
[ -n "$XA" ] && export XAUTHORITY="$XA"

pkill -9 -f 'x64-flat/PCL' 2>/dev/null
sleep 1
cd /tmp/linux-x64-flat || exit 1
setsid ./PCL > /tmp/pcl_thread.log 2>&1 < /dev/null &
sleep 14

PID=$(pgrep -x PCL | head -1)
echo "PID=$PID"
[ -z "$PID" ] && { echo "未找到进程"; exit 1; }

echo "=== 进程总体 ==="
ps -p "$PID" -o pcpu,pmem,rss,nlwp,etimes --no-headers

echo "=== 各线程累计 CPU（前 8）==="
TICKS=$(getconf CLK_TCK)
for t in /proc/$PID/task/*/; do
  tid=$(basename "$t")
  read -r _ comm _ _ _ _ _ _ _ _ _ _ _ U S _ < "$t/stat" 2>/dev/null || continue
  st=$(awk '{print $3}' "$t/stat" 2>/dev/null)
  echo "$(( U + S )) $tid $comm $st"
done | sort -rn | head -8 | while read -r tot tid comm st; do
  printf "  %-10s %-18s 累计=%6.2fs 状态=%s\n" "$tid" "$comm" "$(awk -v t="$tot" -v c="$TICKS" 'BEGIN{print t/c}')" "$st"
done

echo "=== 日志末尾 ==="
f=$(find /tmp/linux-x64-flat/PCL-Data -name 'Launch-*.log' 2>/dev/null | head -1)
[ -n "$f" ] && { echo "行数=$(wc -l < "$f")"; tail -2 "$f"; }

pkill -9 -f 'x64-flat/PCL' 2>/dev/null
