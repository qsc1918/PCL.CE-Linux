# Linux 调试脚本集

配合 `docs/HANDOVER.md` §0.5 与 `docs/LINUX-BLANK-WINDOW.md` 使用。

## 真机（用户 VMware VM）

连接信息见 `docs/HANDOVER.md` §0.5：

```
H=172.22.192.1        # WSL 默认网关 = Windows 宿主
ssh abc@$H -p 62222   # 密码 abc；sudo 密码也是 abc（不是 root）
```

### `vm-run-and-shot.sh`

在真机上启动 PCL（可带参数）、截图、报告窗口内容与资源占用。

```bash
# 先传到真机
sshpass -e scp -P 62222 vm-run-and-shot.sh abc@$H:/tmp/
# 在真机上执行
sshpass -e ssh -p 62222 abc@$H "bash /tmp/vm-run-and-shot.sh --opacity1 --skip-eula"
```

关键输出：`颜色数`（**=1 就是全黑**）、`中心像素`、日志行数、CPU/内存。

### `vm-diag.sh`

同上的简化版，固定带 `--opacity1 --skip-eula --uidump`，并打印 `uidump.txt`（UI 线程看门狗输出）。

## 本地 WSL 快速复现

### `xvfb-shot.sh`

用 Xvfb 虚拟显示跑 PCL 并截图。**迭代比真机快得多**，适合快速验证改动。

> ⚠️ 脚本里会启动 `xcompmgr`。**不加合成器时窗口必然全黑**，会把"缺合成器"误判成程序 bug。

## 部署流程

```bash
# 1. 本地发布 + 打包
cd D:/a/j/PCL.CE-Linux
dotnet publish "src\Plain Craft Launcher 2\Plain Craft Launcher 2.csproj" \
  -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o dist/linux-x64-flat
chmod +x dist/linux-x64-flat/PCL
cd dist && tar czf /d/a/j/pcl-flat.tgz linux-x64-flat

# 2. 传到真机并解压
H=172.22.192.1; export SSHPASS=abc
sshpass -e scp -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -P 62222 /mnt/d/a/j/pcl-flat.tgz abc@$H:/tmp/
sshpass -e ssh -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -p 62222 abc@$H \
  "cd /tmp && tar xzf pcl-flat.tgz && chmod +x linux-x64-flat/PCL"
```

本地网络很快，55MB 的包约 1.5 秒传完。

## 踩坑（务必先看）

- `echo root | sudo -S` 与 `bash -s` 会**抢同一个 stdin** → 报 "no password was provided"。
  改用 `SUDO_ASKPASS`（写脚本输出密码，`SUDO_ASKPASS=... sudo -A ...`）。
- 截图要抓**内层窗口**：外层是 mutter 的装饰框架（928x616），内层才是 PCL（900x550）。
  `xwininfo -root -tree | grep '"PCL' | grep -v mutter`
- 运行前必须设 `XAUTHORITY=$(ls /run/user/1000/.mutter-Xwaylandauth.* | head -1)`，否则连不上 X。
- **诊断输出直接写文件**，不要靠 stdout：重定向到文件 + `SIGKILL` 会丢缓冲。
