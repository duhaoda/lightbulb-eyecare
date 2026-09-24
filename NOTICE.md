# NOTICE / 开源归属与合规声明

本仓库是 **Tyrrrz/LightBulb** 的一个修改版（fork），不是上游官方版本。
本文件说明：我们基于什么做的、改了什么、涉及哪些第三方开源项目及其许可证。

This repository is a fork of [Tyrrrz/LightBulb](https://github.com/Tyrrrz/LightBulb).
It is **not** an official release of the upstream project.

---

## 1. 上游项目（本项目的基础代码）

| 项目 | 版本基线 | 许可证 | 版权 |
|---|---|---|---|
| [Tyrrrz/LightBulb](https://github.com/Tyrrrz/LightBulb) | v2.7.2 | MIT | Copyright (c) 2017-2026 Oleksii Holub |

本仓库根目录的 `License.txt` 是上游 MIT 许可证**原文，逐字保留，未做任何修改**。
MIT 许可证要求在所有副本中保留原始版权声明与许可声明——这是保留该文件的原因，也是使用上游代码的法定前提。

我们自己的修改部分同样以 MIT 许可证发布，版权归 duhaoda（2026）。

> 关于上游 README 中那段带有政治立场的 “Terms of use” 声明：该声明**不是 MIT 许可证的组成部分**。
> 本项目作为独立 fork，不代表、不转述上游作者的任何政治立场，仅保留 MIT 许可证要求的版权与许可声明。

## 2. 我们对上游代码做了哪些修改

修改集中在三块功能，共 14 个文件（2026-09 完成）：

### 2.1 一键色彩预设

| 文件 | 说明 |
|---|---|
| `LightBulb.Core/ColorPreset.cs` | 新增 `ColorPreset` 类型与 8 个预设：阅读 / 办公 / 夜间 / 电影 / 编码 / 游戏 / 护眼 / 自定义（含色温、亮度、饱和度三参数） |
| `LightBulb/ViewModels/Components/DashboardViewModel.cs` | 预设的切换与展示逻辑 |
| `LightBulb/Views/MainView.axaml(.cs)` | 预设选择界面 |
| `LightBulb/Views/Components/Settings/GeneralSettingsTabView.axaml` | 设置页中的预设参数入口 |
| `LightBulb/Services/SettingsService.cs` | 预设的持久化与广播 |

### 2.2 休息提醒（20-20-20）

| 文件 | 说明 |
|---|---|
| `LightBulb/Services/BreakReminderService.cs` | 连续工作计时、到点触发提醒、推迟逻辑 |
| `LightBulb/Services/BreakMessages.cs` | 16 条护眼提示文案池，弹出时随机取一条 |
| `LightBulb/Views/Dialogs/BreakOverlayView.axaml(.cs)` | 全屏休息遮罩（倒计时 + 文案） |
| `LightBulb/ViewModels/Dialogs/BreakOverlayViewModel.cs` | 遮罩的倒计时与跳过逻辑 |

### 2.3 真·灰度阅读模式（Windows Magnification API）

| 文件 | 说明 |
|---|---|
| `LightBulb.PlatformInterop/MagnificationInterop.cs` | 封装 `MagInitialize` / `MagSetFullscreenColorEffect` / `MagUninitialize`，用 5×5 颜色变换矩阵做真正的全屏去饱和 |
| `LightBulb/Services/GammaService.cs` | 把饱和度参数接到 gamma/颜色链路上 |

**为什么不用 gamma 实现灰度**：显示器的 gamma 查找表是逐通道独立映射的，三条曲线再怎么调整也无法把红色通道的像素变成灰色。真正的去饱和需要通道间加权混合，所以改用微软官方的 Magnification API 全屏颜色矩阵。

**关于此处的代码来源**：`MagnificationInterop.cs` 是**独立实现**——P/Invoke 声明对应的是微软公开文档化的 Win32 API（`Magnification.dll`），灰度矩阵用的是公开的 Rec.709 亮度权重（0.2126 / 0.7152 / 0.0722），属于通用数学常数。文件注释中引用的
[Windows-classic-samples](https://github.com/microsoft/Windows-classic-samples)（MIT）与
[mlaily/NegativeScreen](https://github.com/mlaily/NegativeScreen)（**GPL-3.0**）**仅为 API 用法与现象的资料出处**，
我们**没有复制这两个项目中的任何代码**；文件中记录的两个“坑”（必须用 `Marshal.AllocHGlobal` 传参、矩阵按列而非按行排列）是本机实测得出的结论，不是从别处抄来的实现。因此本项目**不受 GPL 传染**，无需以 GPL 发布。

> 其余未列出的文件均保持上游原样。源码注释中出现的 `github.com/Tyrrrz/LightBulb/issues/NNN` 链接，是上游 issue 编号的引用（用于说明某段代码为何这么写），属于合理的出处标注，未改动。

## 3. 灵感来源声明：CareUEyes

本项目的**部分功能灵感**来自 [CareUEyes](https://care-eyes.com/)（一款商业闭源软件），具体为两个**功能思路**：

1. **20-20-20 休息提醒**——每 20 分钟提醒看 6 米外 20 秒；
2. **一键预设模式**——把色温/亮度打包成“阅读/办公/夜间”等情景预设。

需要明确的是：

- 我们**没有使用 CareUEyes 的任何代码**（它是闭源软件，不存在可供复制的源码）；
- 我们**没有使用它的任何图标、界面素材、文案或资源文件**；
- 我们与 CareUEyes 的开发者**不存在任何关联、赞助、合作或背书关系**；
- 相关的两处源码注释（`BreakReminderService.cs`、`ColorPreset.cs`）中主动写明了“灵感来自 CareUEyes”，这是独立实现的自我说明，而非代码来源。

**法律依据**：著作权保护的是“表达”，不保护“思想、功能与操作方法”。模仿一款软件的功能与交互思路、自行独立编写全部代码，不构成著作权侵权。

**但如果你（或你代表的权利人）认为本项目仍有任何不妥**——包括但不限于商标、外观、名称使用等问题——请直接在本仓库提 issue，我们会在核实后**立即修改或删除相关内容**，不做纠缠。

## 4. 本项目使用的第三方开源依赖及许可证

### 4.1 运行时依赖（会进入发布产物）

| 包 | 版本 | 许可证 |
|---|---|---|
| Avalonia / Avalonia.Desktop | 12.1.1 | MIT |
| Cogwheel | 2.1.1 | MIT |
| CommunityToolkit.Mvvm | 8.4.2 | MIT |
| Deorcify | 2.0.1 | MIT |
| DialogHost.Avalonia | 0.12.3 | MIT |
| JsonExtensions | 1.2.3 | MIT |
| Markdig | 1.3.2 | BSD-2-Clause |
| Material.Avalonia | 3.16.1 | MIT |
| Material.Icons.Avalonia | 3.0.2 | MIT |
| Microsoft.Extensions.DependencyInjection | 10.0.11 | MIT |
| Onova | 2.6.13 | MIT |
| PowerKit | 2.3.1 | MIT |

### 4.2 构建与测试依赖（不进入发布产物）

| 包 | 版本 | 许可证 |
|---|---|---|
| CSharpier.MsBuild | 1.3.0 | MIT |
| coverlet.collector | 10.0.1 | MIT |
| GitHubActionsTestLogger | 3.0.5 | MIT |
| Microsoft.NET.Test.Sdk | 18.9.0 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 4.0.0 | Apache-2.0 |
| **FluentAssertions** | **8.10.0** | **Xceed Community License（仅限非商业用途）** |

> ⚠️ 关于 FluentAssertions：从 8.0 版本起，它由 Xceed Software 以**专有社区许可**发布，仅允许非商业用途（明确包含“用于开发或测试开源项目”）。本项目属于开源非商业项目，符合其许可范围；且它**仅用于测试项目，不会打包进任何发布产物**。若你打算基于本仓库做商业用途，请自行将其替换为 MIT 许可的断言库（如 Shouldly）或购买 Xceed 商业许可。

## 5. 仓库内的非代码资源

| 资源 | 来源 | 许可 |
|---|---|---|
| `favicon.ico` / `favicon.png` | 上游 LightBulb | MIT（随上游代码库一并授权） |
| `.assets/dashboard.png` / `.assets/settings.png` | 上游 LightBulb 的 v2.4.5 界面截图 | MIT |

## 6. 免责声明

本项目按 MIT 许可证“按原样（AS IS）”提供，不提供任何明示或暗示的担保。屏幕色彩、gamma 与亮度调整涉及硬件显示输出，使用时请自行判断。本项目与上游 LightBulb 的作者、CareUEyes 的开发者均无隶属关系。
