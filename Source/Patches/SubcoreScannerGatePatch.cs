using HarmonyLib;
using MechResonance;
using RimWorld;
using Verse;

namespace MechResonancePatch;

/// <summary>
/// 上游 MechScanAcceptPawnPatch 以"存在解析研究项目"为扫描准入条件；本补丁为无配方机械体
/// 生成项目后，它们反而会被放行进入裂解/差分扫描仪——这不符合设计（无配方机械体的解析
/// 只应走神经连接器）。以更高优先级抢先拦截。
/// </summary>
[HarmonyPatch(typeof(Building_SubcoreScanner), "CanAcceptPawn", new[] { typeof(Pawn) })]
internal static class SubcoreScannerGatePatch
{
    [HarmonyPriority(Priority.High)]
    private static bool Prefix(Building_SubcoreScanner __instance, Pawn selPawn, ref AcceptanceReport __result)
    {
        if (!CompMechScan.IsActive(__instance))
        {
            return true;
        }
        if (selPawn == null || !selPawn.IsColonyMech || UpstreamApi.IsApocriton(selPawn.def) || UpstreamApi.HasManufacturingRecipe(selPawn.def))
        {
            return true;
        }
        __result = "MRP_ScannerRequiresRecipe".Translate();
        return false;
    }
}
