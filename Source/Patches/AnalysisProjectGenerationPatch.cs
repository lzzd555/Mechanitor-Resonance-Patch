using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MechResonancePatch;

/// <summary>
/// 上游只为"有培育配方"的机械体生成 MR_MechAnalysis_* 解析研究项目（外加特判的憎恶毒蜂），
/// 导致无配方机械体永远无法超过"基础共鸣"档。本补丁为其余所有机械体补齐同名项目，
/// 使上游的 IsResearched/加强档位/配额逻辑自动纳入它们。
/// </summary>
[HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
internal static class AnalysisProjectGenerationPatch
{
    private const string TabDefName = "MR_MechAnalysis";
    private const string PlaceholderTexture = "Things/Item/MechAnalysisPlaceholder";

    // 上游 Postfix 的优先级是 0；postfix 按优先级升序执行，默认 Normal(400) 晚于其运行。
    // 双方生成均按 defName 幂等，任何顺序都会收敛。
    [HarmonyPriority(Priority.Normal)]
    private static void Postfix(bool hotReload)
    {
        try
        {
            GenerateMissingProjects(hotReload);
        }
        catch (Exception e)
        {
            Log.Error($"[MechResonancePatch] analysis project generation failed: {e}");
        }
    }

    private static void GenerateMissingProjects(bool hotReload)
    {
        if (!ModsConfig.BiotechActive)
        {
            return;
        }
        var tab = DefDatabase<ResearchTabDef>.GetNamedSilentFail(TabDefName);
        if (tab == null)
        {
            Log.WarningOnce("[MechResonancePatch] upstream MR_MechAnalysis research tab missing; project generation skipped", 0x4D525010);
            return;
        }
        var missing = DefDatabase<ThingDef>.AllDefs
            .Where(def => def.race != null && def.race.IsMechanoid
                && !UpstreamApi.IsApocriton(def)
                && !UpstreamApi.HasAnalysisProject(def))
            .OrderBy(WeightOrder)
            .ThenBy(def => def.race.baseBodySize)
            .ThenBy(def => def.defName, StringComparer.Ordinal)
            .ToList();
        if (missing.Count > 0)
        {
            var content = MechResonancePatchMod.Content;
            var usedIds = new HashSet<int>(DefDatabase<ThingDef>.AllDefs
                .SelectMany(def => def.comps?.OfType<CompProperties_CompAnalyzableUnlockResearch>() ?? Enumerable.Empty<CompProperties_CompAnalyzableUnlockResearch>())
                .Select(comp => comp.analysisID));
            foreach (var mech in missing)
            {
                string defName = UpstreamApi.ProjectPrefix + mech.defName;
                var placeholder = DefDatabase<ThingDef>.GetNamedSilentFail(defName + "_Placeholder");
                if (placeholder == null)
                {
                    placeholder = MakePlaceholder(mech, defName + "_Placeholder", AllocateAnalysisId(defName, usedIds), content);
                    DefGenerator.AddImpliedDef(placeholder, hotReload);
                }
                var project = new ResearchProjectDef
                {
                    defName = defName,
                    label = mech.label,
                    description = "MRP_AnalysisProjectDesc".Translate(mech.label, UpstreamApi.DurationTicks(mech) * 2 / 2500),
                    descriptionHyperlinks = new List<DefHyperlink> { new DefHyperlink(mech) },
                    modContentPack = content,
                    tab = tab,
                    baseCost = 1f,
                    techLevel = TechLevel.Industrial,
                    requiredAnalyzed = new List<ThingDef> { placeholder },
                };
                DefGenerator.AddImpliedDef(project, hotReload);
            }
            Log.Message($"[MechResonancePatch] generated {missing.Count} analysis research project(s) for recipe-less mechanoids");
        }
        RelayoutAnalysisTab();
    }

    /// <summary>镜像上游 MakePlaceholder：隐藏的分析解锁占位物品。</summary>
    private static ThingDef MakePlaceholder(ThingDef mech, string defName, int analysisId, ModContentPack content)
    {
        var def = new ThingDef
        {
            defName = defName,
            label = "MRP_AnalysisPlaceholderLabel".Translate(mech.label),
            description = "MRP_AnalysisPlaceholderDesc".Translate(),
            modContentPack = content,
        };
        def.descriptionHyperlinks = new List<DefHyperlink> { new DefHyperlink(mech) };
        def.thingClass = typeof(ThingWithComps);
        def.category = ThingCategory.Item;
        def.altitudeLayer = AltitudeLayer.Item;
        def.graphicData = new GraphicData
        {
            texPath = PlaceholderTexture,
            graphicClass = typeof(Graphic_Single),
        };
        def.tradeability = Tradeability.None;
        def.generateCommonality = 0f;
        def.scatterableOnMapGen = false;
        def.selectable = false;
        def.useHitPoints = false;
        def.statBases = new List<StatModifier>
        {
            new StatModifier { stat = StatDefOf.MarketValue, value = 0f },
            new StatModifier { stat = StatDefOf.Flammability, value = 0f },
            new StatModifier { stat = StatDefOf.DeteriorationRate, value = 0f },
        };
        def.comps = new List<CompProperties>
        {
            new CompProperties_CompAnalyzableUnlockResearch
            {
                analysisID = analysisId,
                activateTexPath = PlaceholderTexture,
            },
        };
        return def;
    }

    /// <summary>镜像上游的 FNV-1a 分析 ID 分配（含碰撞递增）。</summary>
    private static int AllocateAnalysisId(string defName, HashSet<int> usedIds)
    {
        uint hash = 2166136261u;
        foreach (char c in defName)
        {
            hash = (hash ^ c) * 16777619;
        }
        int id = (int)(hash & 0x7FFFFFFF);
        while (!usedIds.Add(id))
        {
            id = id != int.MaxValue ? id + 1 : 0;
        }
        return id;
    }

    /// <summary>重量级排序键，镜像上游 WeightOrder：未知重量级按重型（2）处理。</summary>
    private static int WeightOrder(ThingDef def)
    {
        var weightClass = def.race?.mechWeightClass;
        if (weightClass == MechWeightClassDefOf.Light)
        {
            return 0;
        }
        if (weightClass == MechWeightClassDefOf.Medium)
        {
            return 1;
        }
        if (weightClass == MechWeightClassDefOf.UltraHeavy)
        {
            return 3;
        }
        return 2;
    }

    /// <summary>
    /// 对上游+本补丁的全部 MR_MechAnalysis_* 项目做统一研究页布局（每列 7 项，按重量级分块，
    /// 憎恶毒蜂单独收尾），镜像上游 GenerateDefs 的排布算法。
    /// </summary>
    private static void RelayoutAnalysisTab()
    {
        var entries = DefDatabase<ResearchProjectDef>.AllDefs
            .Where(project => project.defName.StartsWith(UpstreamApi.ProjectPrefix))
            .Select(project => new
            {
                project,
                mech = DefDatabase<ThingDef>.GetNamedSilentFail(project.defName.Substring(UpstreamApi.ProjectPrefix.Length)),
            })
            .Where(entry => entry.mech != null && entry.mech.race != null)
            .OrderBy(entry => UpstreamApi.IsApocriton(entry.mech) ? 1 : 0)
            .ThenBy(entry => WeightOrder(entry.mech))
            .ThenBy(entry => entry.mech.race.baseBodySize)
            .ThenBy(entry => entry.mech.defName, StringComparer.Ordinal)
            .ToList();
        int column = 0;
        foreach (var group in entries.Where(e => !UpstreamApi.IsApocriton(e.mech)).GroupBy(e => WeightOrder(e.mech)))
        {
            int index = 0;
            foreach (var entry in group)
            {
                entry.project.researchViewX = (column + index / 7) * 1.2f;
                entry.project.researchViewY = index % 7;
                index++;
            }
            column += (index + 6) / 7;
        }
        foreach (var entry in entries.Where(e => UpstreamApi.IsApocriton(e.mech)))
        {
            entry.project.researchViewX = column * 1.2f;
            entry.project.researchViewY = 3f;
        }
    }
}
