# 环境记录（rimworld-setup，2026-11-13）

> 2026-11-13 切回 macOS 机器后按只读预检重验（前次 Windows 记录见 commit `96706dc`，最新代码为 `c1bd904`）。

## 目标工程

- **工程身份**：`Mechanitor-Resonance-Patch`（`lzzd.mechresonance.universalanalysis`），目标版本 **RimWorld 1.7**（[About/About.xml](../../About/About.xml) 仅声明 `1.7`）。
- **依赖**：`brrainz.harmony`（Harmony）+ `mjcg.mechresonance`（[DR] Mechanitor: Resonance）。
- **无 `tools/preflight.ps1`**：按技能规范直接检查本机组件、客户端与数据层。

## 已验证能力（macOS 本机，三层）

### 1. 安装层

| 组件 | 版本 / 路径 | 状态 |
| --- | --- | --- |
| Python | `3.14.7`（`/opt/homebrew/bin/python3`） | 可用 |
| .NET SDK / Runtime | SDK `10.0.404` / NETCore `10.0.15`（`/opt/homebrew/Cellar/dotnet@10/10.0.404/libexec`） | 可用（匹配 `net10.0`） |
| PowerShell (`pwsh`) | 未安装 | 缺失（本工程无 `preflight.ps1`，不阻塞） |
| RimWorld（Steam macOS） | `1.7.5005 rev1082`，`~/Library/Application Support/Steam/steamapps/common/RimWorld` | 可用 |
| 主程序集与桩 | `RimWorld.app/Contents/MacOS/Assembly-CSharp.dll`（`1.7.9810.32479`，SHA256 `82387f78…f125`）、`UnityStubs.dll` | 可用（经 `.local/game-root-view` 桥接 1.7 根目录契约） |
| DLC 数据 | `Core` / `Royalty` / `Ideology` / `Biotech` / `Anomaly` / `Odyssey` 齐全 | 可用 |
| 0Harmony（Workshop 2009463077） | `…/workshop/content/294100/2009463077/1.7/Assemblies/0Harmony.dll`（SHA256 `c8ef7856…7cb0`） | 可用 |
| 上游 MechResonance（Workshop 3807734021） | `…/workshop/content/294100/3807734021/1.7/Assemblies/MechResonance.dll`（`1.1.0.0`，SHA256 `e93e9ca7…a94`） | 可用 |
| 本地反编译工具 `ilspycmd` | `.local/tools/ilspycmd`（`11.2.0.10050`） | 可用（需指定 `DOTNET_ROOT=/opt/homebrew/opt/dotnet@10/libexec`） |
| 本地 Mods 部署位 | `~/Library/Application Support/RimWorld/Mods/MechResonancePatch` → 本仓库符号链接（另有 `MRP-TestMechNoRecipe`） | 已就位 |

### 2. 接入层

- **内置查证脚本**：`rimworld-mod/scripts/lookup.py`、`catalog.py`、`source_cache.py` 可直接由 `python3` 调用。
- **RimSearcher / DecompilerServer / RimSage / RiMCP**：当前客户端均未接入 MCP；本地已有 `.local/lookup` 索引与 `.local/decomp-1.7`（`MechResonance 1.1.0.0` 反编译源码缓存），不阻塞当前 XML/C# 研发与排错。

### 3. 数据层（实测查询验证）

- **Def 查询（`lookup.py def`）**：通过 `--game-root .local/game-root-view --target 1.7` + `--def-root` 指向工坊 `3807734021` 实测查询 `ThingDef:MR_NeuralSynchronizer`，命中定义并完整解析原版 `Biotech` 的 `SubcoreScannerBase` 与 `Core` 的 `BuildingBase` 继承链。
- **元数据查询（`lookup.py type`）**：复用 `.local/lookup` 缓存并加载本机 `MechResonance.dll`（`1.1.0.0`），实测查询 `Building_NeuralSynchronizer` 命中 `MechResonance.Building_NeuralSynchronizer : RimWorld.Building_Enterable`。
- **上游源码缓存**：`.local/decomp-1.7/` 已存有 `MechResonance 1.1.0.0` 反编译源码，与本机工坊 DLL 版本一致。

## 缺口与跨机注意事项

1. **`Source/MechResonancePatch.csproj` 默认路径仍为 Windows 路径**：
   - 当前 `csproj` 顶部 `<GameDir>`、`<HarmonyDll>`、`<ResonanceDll>` 在 commit `96706dc` 中改为了 `D:\Steam\...`。
   - 在 macOS 上直接裸跑 `dotnet build` 会找不到引用；可通过命令行 `-p:GameDir=...` 覆盖，或将 `csproj` 改为按操作系统条件分支（Windows / macOS 自动切换默认路径）。
2. **`ilspycmd` 运行环境变量**：
   - Homebrew 的 `dotnet@10` 未注册到 `/usr/local/share/dotnet`，调用 `.local/tools/ilspycmd` 时需带 `DOTNET_ROOT=/opt/homebrew/opt/dotnet@10/libexec`。
3. **画像版本差异**：
   - 技能包 1.7 静态画像基线为 `1.7.4977 rev1248`，本机为 `1.7.5005 rev1082`；同大版本内以 `lookup.py` 绑定本机文件哈希（`82387f78…f125`）的实查结果为准。
4. **可选工具缺口（按需准备，当前不阻塞）**：
   - `RimSearcher`（运行时合并后 Def 数据库）、`DecompilerServer`、`RimSage` 在线 MCP、RimWorld 1.6 安装（本工程仅 1.7）。

## 检查点

- **当前状态**：macOS 下 1.7 游戏本体、DLC、Harmony、上游 `MechResonance 1.1.0.0`、本地 XML/DLL 查证索引及 `~/Library/Application Support/RimWorld/Mods/` 软链部署位均已验证就绪。
- **待确认动作**：若要在 macOS 上继续编译 C# 或做双机无缝切换，可授权更新 [Source/MechResonancePatch.csproj](../../Source/MechResonancePatch.csproj)（支持 macOS/Windows 自动路径切换并实测 `dotnet build`），或直接说明下一步制作/排错任务。
