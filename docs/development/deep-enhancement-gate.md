# 调查：深度加强是否仅限"有制造配方"的机械体（2026-11-12）

## 问题

上游 mod `[DR] Mechanitor: Resonance`（mjcg.mechresonance v1.1.3，workshop 3807734021）：
是否只有拥有制造配方（培育配方）的机械体才能获得基础共鸣以上的加强档位？

## 结论：属实（唯一特例为憎恶毒蜂 Apocriton 硬编码特判）

机械体加强 = hediff `MR_ResonanceMechBuff`，4 档按严重度：

| 严重度 | 档位（中文） | 数值（工作/移动 ×；冷却 ×） |
| --- | --- | --- |
| 0.5 | 基础共鸣 | 1.05 / 0.95 |
| ≥1.0 | 完成研究 | 1.1 / 0.9 |
| ≥1.5 | 感知连接 | 1.2 / 0.8 |
| ≥2.5 | 深度融合 | 1.3 / 0.7 |

## 证据链（1.7 DLL 反编译，.local/decomp-1.7）

1. **严重度判定** `MechResonance.ResonanceMechlinkUtility.RefreshControlledMechBuffs`（ResonanceMechlinkUtility.cs:66-86）：
   `MechScanResearch.IsResearched(mech.def)` 为 false → 严重度恒 0.5（基础共鸣）；
   为 true → 1（完成研究）/ 2（机械师有稳定感知连接）/ 3（机械师有 MR_DeepIntegration）。
   由 `Hediff_ResonanceController` 每 300 tick 刷新。
2. **研究项目生成** `MechScanResearch.GenerateDefs`（MechScanResearch.cs:197-207）：
   只为"产品中包含该机械体且 `mechanitorOnlyRecipe=true && mechResurrection=false` 的配方"生成 `MR_MechAnalysis_<defName>` 研究项目；`Mech_Apocriton` 硬编码特判加入（其项目需 7 轻+6 中+7 重/超重解析后扫描尸体解锁）。
   配方判定：`IsMechManufacturingRecipe`（同文件 142-150 行）。
3. **IsResearched**（MechScanResearch.cs:131-135）：项目不存在（GetNamedSilentFail=null）→ 永远 false。**没有配方 → 项目不存在 → 无论玩家做什么都无法超过基础档。**
4. **扫描入口拦截** `MechScanAcceptPawnPatch.CanAccept`（MechScanAcceptPawnPatch.cs:79-82）：
   `!HasResearchProject(def)` → 拒绝（"MR_MechCannotBeResearched"）。没有项目的机械体连扫描仪都进不去，无手动绕过途径。
   扫描完成经 `MechScanResearch.Complete` → `Find.ResearchManager.FinishProject`。

## 原版对照（本机 1.7.5005 游戏数据）

- 原版 `BaseMechanoidRecipe`（Data/Biotech/Defs/RecipeDefs/Recipes_MechGestator_Light.xml）设 `mechanitorOnlyRecipe=true`，即该 mod 筛选器实际含义 = 培育机配方。
- 原版 20 种机械体均有培育配方（Agrihand…Diabolus、Warqueen、Centurion），Apocriton 无配方但被特判。**纯原版下此限制不可见；它只影响"无培育配方的 mod 机械体"**（如其他 mod 的 Boss/袭击专属机械体）。

## 对本补丁工程的含义（已实施，见 universal-analysis.md）

改动点已落地（2026-11-12）：
- 研究项目生成：Postfix vanilla `DefGenerator.GenerateImpliedDefs_PreResolve` 补齐无配方机械体；
- 扫描时长回退：无 `mechWeightClass` 时上游默认 12 小时（DurationTicks，MechScanResearch.cs:116-129），补丁按 ×2 使用；
- 材料回收 `SpawnRecoveredMaterials` 对无配方机械体不掉材料（上游既有行为，保留）；
- 无配方机械体在扫描仪入口被补丁 Prefix 拦截（高优先级，先于上游放行逻辑）。

实现详情与验证步骤：[universal-analysis.md](universal-analysis.md)。
