**简体中文**

<div align="center">

<img src="src/Plain Craft Launcher 2/Images/icon.png" alt="Logo" width="96" height="96">

# PCL CE Linux

**把 PCL CE 的界面与功能原样移植到 Avalonia / .NET 10，让它在 Linux 上也能跑。**

</div>

---

## ⚠️ 第三方声明（请先读这一段）

**PCL CE Linux** 是**第三方**基于 [PCL](https://github.com/Meloong-Git/PCL)（作者：**龙腾猫跃**）与
[PCL-CE](https://github.com/PCL-Community/PCL2-CE)（PCL-Community）**独立进行的二次创作移植版本**。

- **与龙腾猫跃、PCL-Community 及其成员均无从属关系**，他们不参与本项目的开发，也不为本项目的使用做任何担保。
- 使用中遇到问题**请在本仓库反馈**，**不要**向 PCL / PCL-CE 上游仓库提交 issue。
- 本项目**不是** Minecraft 官方产品，未经 Mojang 或 Microsoft 批准，也不与其关联。
- 如果你是意外来到这里的，建议直接使用原版 [PCL](https://github.com/Meloong-Git/PCL) 或 [PCL-CE](https://github.com/PCL-Community/PCL2-CE)。

## 这是什么

把 PCL CE（WPF）的界面与功能**原样移植**到 Avalonia 12 / .NET 10：

- **移植而非重写**：布局、配色、文案、交互逻辑均尽量与上游保持一致，目标是"就是 PCL CE，只是换了框架"。
- **跨平台**：在 Windows 与 Linux 上运行，使用 Avalonia 的 FreeDesktop 后端。
- **逻辑零改动**：业务逻辑沿用上游实现，改动集中在 UI 框架层。

> 移植过程中遇到的 Avalonia ↔ WPF 语义差异，都记录在 [`docs/HANDOVER.md`](docs/HANDOVER.md) §7 的「坑」清单里。

## 当前状态

| 项 | 状态 |
|---|---|
| 编译 | ✅ 0 错误 |
| Windows 运行 | ✅ 主界面、启动、下载、实例管理、设置可用 |
| Linux 运行 | ⚠️ 已产出 linux-x64 自包含包，**尚未在真机验证** |
| 暂缓功能 | 崩溃分析 / 音乐 / 视频背景 / 联机 / 换肤 / 工具页 / 剪贴板行为 / 懒加载 |

已知未完成项见 [`docs/HANDOVER.md`](docs/HANDOVER.md) §2 与 §9。

## 构建

需要 **.NET 10 SDK**。

```bash
# 构建整个解决方案
dotnet build PCL.CE-Linux.slnx

# 或只构建主工程
dotnet build "src/Plain Craft Launcher 2/Plain Craft Launcher 2.csproj"

# Linux 自包含发布
dotnet publish "src/Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" \
  -c Release -r linux-x64 --self-contained true -o dist/linux-x64
```

运行：

```bash
./PCL.exe          # Windows
./PCL              # Linux
```

## 微软正版登录说明

正版登录需要自备 Azure 应用（上游同样如此，仓库中**不含**任何客户端 ID）：

1. 在 [Azure 门户](https://portal.azure.com) 注册应用，账户类型选「仅限个人帐户」，重定向 URI 留空；
2. 在「身份验证」页底部把 **「允许公共客户端流」设为「是」**；
3. 按 Mojang 的 [Java 版 API 应用审批流程](https://aka.ms/mce-reviewappid) 提交审批，**通过后**才能访问 Minecraft API；
4. 通过环境变量提供客户端 ID 后启动：

```powershell
setx PCL_MS_CLIENT_ID "<你的客户端 ID>"   # Windows，重开终端生效
```

```bash
export PCL_MS_CLIENT_ID="<你的客户端 ID>"  # Linux
```

> Release 构建不读取环境变量，需要在构建期注入密钥。

## 许可证

本项目沿用**上游的分目录许可方案**，详见 [`LICENSE`](LICENSE)：

| 目录 | 许可证 |
|---|---|
| `src/Plain Craft Launcher 2/` | [PCL 自定义许可](src/Plain%20Craft%20Launcher%202/LICENCE)（《PCL 分发有限许可》+《存储库合理使用指南》） |
| 其余所有目录 | [Apache License 2.0](LICENSE) |

**重要**：`src/Plain Craft Launcher 2/` 中的代码**不是** Apache 2.0，而是 PCL 的自定义许可。
基于本仓库进行修改属于该指南中的「重度使用」，需遵守其中的署名、命名与开源要求 —— 请务必阅读 [`src/Plain Craft Launcher 2/LICENCE`](src/Plain%20Craft%20Launcher%202/LICENCE)。

本仓库对上游文件所做的修改说明见 [`NOTICE`](NOTICE)。

## 致谢

- **[龙腾猫跃](https://github.com/Meloong-Git)** —— PCL 原作者。[赞助支持](https://ifdian.net/a/LTCat)
- **[PCL-Community](https://github.com/PCL-Community)** —— PCL CE 维护者
- **[Avalonia](https://avaloniaui.net/)** —— 跨平台 UI 框架
- 以及所有上游依赖的开源项目
