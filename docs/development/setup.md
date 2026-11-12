# 环境记录（rimworld-setup，2026-11-12）

## 目标工程

- Mechanitor-Resonance-Patch，空工程（无任何内容/约定）。
- 制作目标版本**未确认**；本机仅 1.7 安装，环境按 1.7 准备。制作目标不得从安装推断，待任务确认。

## 已验证能力（三层）

### 安装层

| 组件 | 版本/路径 | 状态 |
| --- | --- | --- |
| Python | 3.14.7 `/opt/homebrew/bin/python3` | 可用 |
| .NET SDK + runtime | 10.0.404（SDK）；NETCore/AspNetCore 10.0.15 | 可用 |
| RimWorld（Steam macOS） | 1.7.5005 rev1082，2026-11-12 当日更新 | 可用 |
| DLC 数据 | Core/Royalty/Ideology/Biotech/Anomaly/Odyssey 齐全 | 可用 |
| pwsh/powershell | 未安装 | 缺失（本工程无 `tools/preflight.ps1`，当前不需要） |

游戏根：`~/Library/Application Support/Steam/steamapps/common/RimWorld`
引擎布局（1.7 新原生引擎，非 Unity）：程序集在 `RimWorld.app/Contents/MacOS/`（与 `MacOS/x64/` 两份，`Assembly-CSharp.dll` 哈希一致）。**无 0Harmony.dll**；有 `UnityStubs.dll`、`Waylith.UnityCompat.dll`、`RimWorldWaylith.PrepatcherCompat.dll`。

### 数据层（实测查询验证）

- `lookup.py def`：经 `--game-root .local/game-root-view` 实测，`ThingDef:Bed` 命中（源文件+哈希）。身份绑定 1.7.5005 rev1082 / gameSha256 `82387f78…f125`。
- `lookup.py type`：元数据索引已建（Assembly-CSharp 1.7.9810.32479），`RimWorld.Pawn_MechanitorTracker` 命中。
- 索引缓存：`.local/lookup`（同 DLL 指纹可复用）。

### macOS 布局适配

- `.local/game-root-view/`：符号链接视图（`Version.txt`、`Assembly-CSharp.dll`→`RimWorld.app/Contents/MacOS/`、`Data`→游戏根 `Data`）。lookup 脚本按 1.7 契约要求程序集在游戏根，macOS 实际在 bundle 内，此视图桥接。游戏本体未改动。

## 缺口（按需补，不阻塞 XML 制作）

- **0Harmony**：游戏与本地均无。若做 C# Harmony 补丁，需按实际上游引用获取对应 Harmony 包（catalog verify 对 harmony 项报 not_covered 属预期）。
- **RimSearcher**：未安装（运行时 Def 查询；其 .NET 10 runtime 前置已具备）。
- **DecompilerServer / ILSpy**：未安装（方法体/源码缓存；按需再备）。
- **RimSage MCP**：本客户端未接入。
- **1.6**：无安装。若需双版本制作需另装。
- **用户级 Mods 目录**：`~/Library/Application Support/RimWorld/Mods` 不存在；游戏根 `Mods/` 为空。部署测试副本时再建。

## 画像注意

技能包 1.7 画像基线为 1.7.4977 rev1248，落后本机 1.7.5005 rev1082（当日更新）。同大版本内不阻断 `lookup.py` 实查（证据绑定本机 DLL 哈希）；`catalog.py verify` 报 stale 属预期。

## 检查点

- 环境就绪：XML 查证/制作可直接开始；DLL 元数据查证可用。
- 下一动作：与用户确认制作目标（版本、XML 补丁或 C#、上游 mod"Resonance"具体指什么）后再进入需求/计划，不由 setup 代替。
