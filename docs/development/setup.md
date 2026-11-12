# 环境记录（rimworld-setup，2026-11-12）

> 2026-11-12 自 macOS 迁移至 Windows 机器后重验，本文件取代旧记录（macOS 版见 git 历史）。工程已实现完整补丁并作首次发布（commit 9874756）。

## 目标工程

- Mechanitor-Resonance-Patch：RimWorld **1.7** C# 补丁 mod（About.xml 仅声明 1.7）。
- 依赖：Harmony（brrainz.harmony）+ [DR] Mechanitor: Resonance（mjcg.mechresonance）。
- 制作目标与本机安装版本一致（1.7），非从安装推断。

## 已验证能力（Windows 本机，三层）

### 安装层

| 组件 | 版本/路径 | 状态 |
| --- | --- | --- |
| Python | 3.12.10（PATH） | 可用 |
| .NET SDK / runtime | 10.0.402 / NETCore 10.0.13 | 可用（csproj 目标 net10.0） |
| PowerShell | 5.1.22621.4391 | 可用（本工程无 preflight 脚本，备用） |
| RimWorld（Steam Win） | 1.7.5005 rev1082，`D:\Steam\steamapps\common\RimWorld` | 可用 |
| 主程序集 | Assembly-CSharp.dll **1.7.9810.32479**、UnityStubs.dll，均在游戏根 | 可用（1.7 根目录布局契约） |
| DLC 数据 | Core/Royalty/Ideology/Biotech/Anomaly/Odyssey 齐全 | 可用 |
| 0Harmony | workshop 2009463077 `1.7\Assemblies\0Harmony.dll`，文件版本 3.0.0.0 | 可用 |
| 上游 MechResonance | workshop 3807734021 `1.7\Assemblies\MechResonance.dll`，文件版本 1.1.0.0 | 可用（见复核点） |

Steam 库仅 `D:\Steam`（libraryfolders.vdf 单库）。

### 构建接线（csproj 本机路径）

- `Source/MechResonancePatch.csproj` 三个 HintPath 已改为 Windows 路径（游戏根 / 0Harmony 1.7 / 上游 MechResonance 1.7），2026-11-13 实跑 `dotnet build` 通过（0 警告 0 错误），DLL 自动落位 `1.7\Assemblies\`。

### 部署位

- 游戏根 `Mods\` 存在且已有本地 mod（本机实际使用的本地 mod 位）。
- `%USERPROFILE%\…\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Mods` 不存在。

## 缺口与复核点

- **上游版本**：工坊公开版即 DLL 1.1.0.0（2026-10-13，acf 与文件一致）；README 旧记的 1.1.3 为开发机另一份副本，注记已更正。补丁反射上游 internal 类型，上游更新后需复核：`CanAcceptPawn` 重载、页签生成时机、`UI/Commands/NeuralSynchronizer/` 贴图名。
- RimSearcher / DecompilerServer / RimSage MCP：本机均未接入（本次用 ilspycmd 11.2 反编译取证，装于 `%USERPROFILE%\.dotnet\tools`）；按需再备。
- 1.6：本机无安装；本工程仅面向 1.7，不需要。

## 画像注意

- 技能包 1.7 画像基线 1.7.4977 rev1248；本机 1.7.5005 rev1082，同大版本，不阻断实查（证据绑定本机文件）。
- 主程序集 1.7.9810.32479 与前机一致 → 前机建立的反射/元数据结论在本机仍适用。

## 检查点

- 环境就绪：XML/C# 制作、构建、部署全链路可用（2026-11-13 实跑验证）。
- 2026-11-13 修复会话：适配上游工坊公开版 1.1.0.0 的四处漂移（`CanAcceptPawn` 重载、Harmony 3 postfix 降序致页签缺失、`MR_SensoryConnection` 贴图、WorkGiver CanReserve）；证据为 Player.log 783/3373/42370/43934 行 + 反编译缓存 `%TEMP%\mr-decompile`。**待游戏内复验**。
