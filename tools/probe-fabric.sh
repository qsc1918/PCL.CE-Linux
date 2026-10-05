#!/bin/bash
# 提取第一个 stable:true 的 loader 版本（version 行位于 stable 行之前，用 -B 窗口）
loader=$(curl -s --max-time 12 'https://meta.fabricmc.net/v2/versions/loader/1.21.1' \
  | grep -B4 '"stable": true' | grep -o '"version": "[^"]*"' | head -1 | sed 's/.*: "//; s/"//')
installer=$(curl -s --max-time 12 'https://meta.fabricmc.net/v2/versions/installer' \
  | grep -B2 '"stable": true' | grep -o '"version": "[^"]*"' | head -1 | sed 's/.*: "//; s/"//')
echo "picked loader=[$loader] installer=[$installer]"
for u in \
  "https://meta.fabricmc.net/v2/versions/loader/1.21.1/$loader/$installer/profile/json" \
  "https://meta.fabricmc.net/v2/versions/loader/1.21.1/$installer/$loader/profile/json"; do
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 12 "$u")
  echo "$code  $u"
done
