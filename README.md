# [LZZD] Mechanitor Resonance: Universal Mech Analysis

RimWorld 1.7 补丁 mod，修复 **[DR] Mechanitor: Resonance**（mjcg.mechresonance）中"没有培育配方的机械体永远无法获得基础共鸣以上加强"的问题。

## 问题

上游 mod 中，机械体的深度加强（`MR_ResonanceMechBuff` 高于"基础共鸣"的档位）要求完成该型号的解析研究，而解析研究项目**只为拥有培育配方（`mechanitorOnlyRecipe` 且非复活配方）的机械体生成**。无培育配方的机械体（其他 mod 添加的 Boss / 袭击专属机械体）永远停留在基础共鸣档，且被扫描仪入口直接拒绝，没有任何补救途径。

## 本补丁的行为

1. **所有机械体**都会生成对应的 `MR_MechAnalysis_*` 解析研究项目。
2. 无培育配方的机械体**仍然不能**进入裂解扫描仪 / 差分扫描仪（保持上游行为，拒绝理由会指向神经连接器）。
3. 神经连接器（Neural Synchronizer）新增**机械体解析模式**：任意机械体（含无配方者）花费 **200 玻璃钢 + 3 高级零部件**，耗时为破坏性扫描的 **2 倍**，完成后解锁该型号的解析研究。解析是**非破坏性**的——机械体会存活并弹出连接器。
4. 解析完成后，上游的共鸣加强档位与憎恶毒蜂（Apocriton）的 7/6/7 解析配额对新补齐的型号自动生效。

## 需求

- RimWorld 1.7 + Biotech
- [Harmony（brrainz.harmony）](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- [[DR] Mechanitor: Resonance（mjcg.mechresonance）](https://steamcommunity.com/sharedfiles/filedetails/?id=3807734021)

加载顺序：Harmony → Resonance → 本补丁。

## 安装

将本仓库放入 RimWorld 的 `Mods` 目录（可 git clone 或下载 ZIP 后解压）：

```
macOS:   ~/Library/Application Support/RimWorld/Mods/
Windows: %USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Mods\
Linux:   ~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Mods/
```

`1.7/Assemblies/` 内已包含编译好的 DLL，克隆即用。

## 从源码构建

```
cd Source
dotnet build
```

`MechResonancePatch.csproj` 顶部有三个本机路径（游戏程序集、Harmony、上游 DLL），换机器时按注释修改。构建成功后自动把 DLL 拷贝到 `../1.7/Assemblies/`。

- 上游依赖版本：mjcg.mechresonance 工坊公开版（DLL 1.1.0.0，2026-10-13）已适配。反射访问其 internal 类型；上游更新后需复核：`CanAcceptPawn` 重载结构、`MR_MechAnalysis` 页签生成时机、`UI/Commands/NeuralSynchronizer/` 贴图名。

## 许可

MIT。
