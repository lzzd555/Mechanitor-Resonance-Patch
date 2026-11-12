using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MechResonancePatch;

/// <summary>
/// 对上游 mjcg.mechresonance（internal 类 MechScanResearch）的访问门面。
/// 名称查询类方法按 defName 约定本地复刻（无漂移风险）；
/// DurationTicks/Complete 是行为复用，走反射并缓存，失败时回退本地实现。
/// </summary>
internal static class UpstreamApi
{
    public const string ProjectPrefix = "MR_MechAnalysis_";
    public const string ApocritonDefName = "Mech_Apocriton";

    private static MethodInfo _durationTicks;
    private static MethodInfo _complete;
    private static bool _resolved;

    public static bool IsApocriton(ThingDef def)
    {
        return def != null && def.defName == ApocritonDefName;
    }

    public static bool IsResearched(ThingDef mech)
    {
        var project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ProjectPrefix + mech.defName);
        return project != null && project.IsFinished;
    }

    public static bool HasAnalysisProject(ThingDef mech)
    {
        return DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ProjectPrefix + mech.defName) != null;
    }

    /// <summary>上游 MechScanResearch.IsMechManufacturingRecipe 的语义复刻：培育配方（排除复活配方）。</summary>
    public static bool HasManufacturingRecipe(ThingDef mech)
    {
        return DefDatabase<RecipeDef>.AllDefs.Any(recipe =>
            recipe.mechanitorOnlyRecipe && !recipe.mechResurrection &&
            recipe.products != null &&
            recipe.products.Any(product => product.thingDef == mech));
    }

    /// <summary>裂解扫描时长（小时×2500 tick）。优先反射上游实现以保持同步。</summary>
    public static int DurationTicks(ThingDef mech)
    {
        ResolveMethods();
        if (_durationTicks != null)
        {
            try
            {
                return (int)_durationTicks.Invoke(null, new object[] { mech });
            }
            catch (Exception e)
            {
                Log.WarningOnce($"[MechResonancePatch] upstream DurationTicks failed, using fallback: {e.Message}", 0x4D525001);
            }
        }
        return FallbackDurationHours(mech) * 2500;
    }

    /// <summary>完成解析研究（FinishProject + 材料回收 + 信件 + 配额信件），全部复用上游。</summary>
    public static void Complete(ThingDef mech, IntVec3 outputCell, Map map)
    {
        ResolveMethods();
        if (_complete != null)
        {
            _complete.Invoke(null, new object[] { mech, outputCell, map });
            return;
        }
        Log.Error("[MechResonancePatch] upstream MechScanResearch.Complete not found; cannot finish analysis");
    }

    private static void ResolveMethods()
    {
        if (_resolved)
        {
            return;
        }
        _resolved = true;
        var type = AccessTools.TypeByName("MechResonance.MechScanResearch");
        if (type == null)
        {
            Log.WarningOnce("[MechResonancePatch] MechResonance.MechScanResearch not found; falling back to local duration table", 0x4D525002);
            return;
        }
        _durationTicks = AccessTools.Method(type, "DurationTicks");
        _complete = AccessTools.Method(type, "Complete");
        if (_durationTicks == null || _complete == null)
        {
            Log.WarningOnce("[MechResonancePatch] MechScanResearch method signatures changed; some features may degrade", 0x4D525003);
        }
    }

    private static int FallbackDurationHours(ThingDef mech)
    {
        if (IsApocriton(mech))
        {
            return 24;
        }
        var weightClass = mech.race?.mechWeightClass;
        if (weightClass == MechWeightClassDefOf.Light)
        {
            return 4;
        }
        if (weightClass == MechWeightClassDefOf.Medium)
        {
            return 8;
        }
        return 12;
    }
}
