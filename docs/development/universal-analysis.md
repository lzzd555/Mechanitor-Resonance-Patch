# 功能记录：通用机械体解析（2026-11-12 实现首版）

## 概览

- 本工程 = 补丁 mod `[LZZD] Mechanitor Resonance: Universal Mech Analysis`（packageId `lzzd.mechresonance.universalanalysis`），仅 1.7。
- 上游：mjcg.mechresonance v1.1.3（workshop 3807734021，1.7 DLL 反编译于 `.local/decomp-1.7`）+ brrainz.harmony。
- 构建入口：`Source/MechResonancePatch.csproj`（`dotnet build` 自动落位 `1.7/Assemblies/MechResonancePatch.dll`）。
- 部署：`~/Library/Application Support/RimWorld/Mods/MechResonancePatch` → 本项目根（符号链接）；测试 mod `MRP-TestMechNoRecipe`（`/Users/lzzd/project/MRP-TestMechNoRecipe`，含无配方测试机械体 Mech_TestNoRecipe）同法链接，**不随补丁分发**。

## 需求与验收（用户确认）

1. 所有机械体都有 `MR_MechAnalysis_<defName>` 解析研究项目。
2. 无培育配方机械体仍不可进裂解/差分扫描仪（拒绝理由指向神经连接器）。
3. 神经连接器（建筑可用=已过 MR_MechAnalysis_Mech_Apocriton 前置）新增解析模式：任意机械体，200 玻璃钢 + 3 高级零部件，耗时 = 裂解扫描 × 2，完成后解锁研究，**机械体存活弹出**。
4. buff 档位、7/6/7 配额对补齐型号自动生效（复用上游按 defName 查询的逻辑）。

## 实现契约（文件 → 机制）

| 文件 | 机制 |
| --- | --- |
| `Source/Patches/AnalysisProjectGenerationPatch.cs` | Postfix 于 vanilla `DefGenerator.GenerateImpliedDefs_PreResolve`（Normal，上游为 0；双方幂等）。为无项目机械体生成占位物 + 研究项目（镜像上游 MakePlaceholder/FNV-1a analysisID），并统一重排研究页布局 |
| `Source/Patches/SubcoreScannerGatePatch.cs` | Prefix(High) 于 `Building_SubcoreScanner.CanAcceptPawn`：扫描模式下拒绝无配方机械体，先于上游 Prefix |
| `Source/CompNeuralAnalyzer.cs` | 解析模式 ThingComp：布防/配料/倒计时(CompTick)/完成。完成时扣料→反射上游 `MechScanResearch.Complete`→`EjectContents`（存活+退超配料） |
| `Source/Patches/SynchronizerPatches.cs` | Prefix 于同步器 `CanAcceptPawn`（布防时只放行选中机械体）+ Postfix 于 `TryAcceptPawn`（接管计时） |
| `Source/SynchronizerStateHelper.cs` | 反射访问上游私有 `selectedExperiment`/`ticksRemaining`/`selectedPawn`。**关键时序**：入舱前 `Neutralize`(exp=0, ticks=-1)，使上游 TryAcceptPawn 走 num==0 无副作用分支、上游 Tick 永不触发 CompleteExperiment |
| `Source/FloatMenuOptionProvider_Analyzer.cs` | 1.7 浮选菜单 Provider：选中机械体右击连接器 → SelectPawnOn(反射) + EnterBuilding 作业 |
| `Source/WorkGiver_AnalyzerIngredients.cs` + `Defs/AnalyzerJobs.xml` | 配料搬运（HaulToContainer → innerContainer），镜像上游 Apocriton 尸体搬运模式 |
| `Source/UpstreamApi.cs` | 反射门面：DurationTicks/Complete 走反射（绑 mjcg.mechresonance 1.1.3）；IsResearched/HasAnalysisProject/HasManufacturingRecipe 按 defName/配方语义本地复刻 |

状态存档：comp 自 scribe（MRP_armed/selectedMech/ticksRemaining/totalDuration）；舱内内容物随 vanilla Building_Enterable 序列化。

## 已验证（静态）

- `dotnet build` 零告警零错误；DLL 33.8KB 落位 `1.7/Assemblies/`。
- 符号链接部署到游戏 Mods 目录（两个 mod）。

## 未验证（需游戏内实测，步骤）

前置：mod 列表启用 Harmony + Resonance + 本补丁 +（可选）TestMech；开发者模式。当前 ModsConfig 尚未启用上游 mod。

1. 启动无红字；Player.log 出现 `[MechResonancePatch]` 三条初始化日志 + 生成项目计数日志。
2. 研究栏"机械族解析"：出现 TestMech 的项目（轻量级位置）。
3. TestMech 右击裂解仪/差分扫描仪 → 拒绝理由为中文指引文案；Lifter 等有配方机械体照常可破坏性扫描。
4. 建神经连接器（dev 完成 Apocriton 解析研究）→ gizmo"解析机械体" → 选 TestMech → 搬入 200 玻璃钢+3 高级零部件 → 右击连接器进入 → inspect 显示 8 小时倒计时（轻量 4h×2）→ 完成后：研究完成+信件、机械体存活弹出、材料扣除。
5. 共鸣机械师监督 TestMech → MR_ResonanceMechBuff ≥"完成研究"档（WorkSpeedGlobal ×1.1）。
6. 回归：同步器五种上游实验未布防时照常；解析中取消 → 机械体+配料退还；存档重载解析状态保持。
7. 并行兜底：同型号已被完成时再跑一舱 → 不扣料、提示已解析。

## 风险与维护

- 反射目标绑定 mjcg.mechresonance 1.1.0.0（工坊公开版）；上游更新若改名/改签名 → `UpstreamApi` 降级（DurationTicks 回退本地表；Complete 缺失则报错日志），需复核。
- Harmony 补丁参数名（selPawn/pawn/hotReload）与上游/vanilla 一致性为运行时绑定，已在 Mod ctor PatchAll 时即时报错。

## 增量记录：专属「解析机械体」图标与补丁封面（2026-11-13）

### 1. 已查证上游参考规格（来源：工坊 `3807734021`）

| 参考文件 | 实测规格 | 视觉特征 |
| --- | --- | --- |
| `Textures/UI/Commands/NeuralSynchronizer/MR_SensoryConnection.png` | `256 × 256`，8-bit RGBA，透明背景 | 粗深褐黑描边（`#26251c`），右侧冷灰蓝机械体头部（`#8d9ea1`），左侧橄榄绿人像与顶部连接管（`#8e8c60`），中间两条黄绿信号波纹（`#b6b576`） |
| `About/preview.png` | `1280 × 720`，8-bit RGB | 暗红背景机械师冥想主视觉，左侧机械体、右侧三只大型机械体，左下脑机徽标 + 银白黑边标题 `MECHANITOR: RESONANCE` |

### 2. 产物落地与验证记录（资源模式：混合）

| 资源项 | 消费者 / 加载位置 | 实际文件与 SHA256 | 实测规格 | 状态 |
| --- | --- | --- | --- | --- |
| 神经连接器「解析机械体」Gizmo 图标 | `CompNeuralAnalyzer.CompGetGizmosExtra` → `TexPool.AnalyzeMech`（`SynchronizerStateHelper.cs` 绑定 `UI/Commands/NeuralSynchronizer/MRP_AnalyzeMech`） | `Textures/UI/Commands/NeuralSynchronizer/MRP_AnalyzeMech.png`<br>SHA256：`b19ca50f843493f64ebb9c2bc07759fec5c762c861efb49f426c3405b6b93429` | `256 × 256`，8-bit RGBA，真实透明背景（`38053/65536` 透明像素） | 已生成、已导入且容器已检查、已视觉检查、**已绑定 C# 并重编落位 `1.7/Assemblies/MechResonancePatch.dll`**（0 警告 0 错误，DLL SHA256 `5c925094…5e3a`；未游戏验证） |
| 模组预览封面 | RimWorld Mod 列表与工坊封面 | `About/Preview.png`<br>SHA256：`d21142e9809e5c1d8ffb8084928e00869e277ddca4ce898ef683408592c10c10` | `1280 × 720`，8-bit RGB PNG | 已生成、已导入且容器已检查、已视觉检查、已落位加载路径（未游戏验证） |


