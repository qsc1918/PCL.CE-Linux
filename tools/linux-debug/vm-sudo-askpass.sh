#!/bin/bash
# 在真机上用 SUDO_ASKPASS 安装截图工具
cat > /tmp/askpass.sh <<'EOF'
#!/bin/sh
echo root
EOF
chmod +x /tmp/askpass.sh
export SUDO_ASKPASS=/tmp/askpass.sh
sudo -A apt-get install -y imagemagick > /tmp/im.log 2>&1
echo "退出码=$?"
tail -3 /tmp/im.log
echo "--- 工具 ---"
which import convert || echo "未安装"
