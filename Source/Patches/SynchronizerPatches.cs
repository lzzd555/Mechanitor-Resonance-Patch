using HarmonyLib;
using MechResonance;
using RimWorld;
using Verse;

namespace MechResonancePatch;

/// <summary>
/// 解析模式布防时接管准入：只放行选中的机械体（原版入口要求"玩家方人形机械师"，
/// 机械体必被拒）；其余 Pawn（包括想做实验的机械师）在布防期间一律拒绝，防止状态交叠。
/// </summary>
[HarmonyPatch(typeof(Building_NeuralSynchronizer), "CanAcceptPawn")]
internal static class SynchronizerCanAcceptPatch
{
    private static bool Prefix(Building_NeuralSynchronizer __instance, Pawn selPawn, ref AcceptanceReport __result)
    {
        var comp = __instance.GetComp<CompNeuralAnalyzer>();
        if (comp == null || !comp.Armed)
        {
            return true; // 未布防：上游实验路径原样运行
        }
        if (selPawn == comp.SelectedMech && comp.IngredientsLoaded && comp.CanAnalyze(selPawn))
        {
            if (!__instance.PowerOn)
            {
                __result = "CannotUseNoPower".Translate();
                return false;
            }
            if (__instance.Occupant != null)
            {
                __result = "SubcoreScannerOccupied".Translate();
                return false;
            }
            // 先中和上游实验状态：TryAcceptPawn 在 selectedPawn!=null 时会沿用 selectedExperiment
            SynchronizerStateHelper.Neutralize(__instance);
            __result = AcceptanceReport.WasAccepted;
            return false;
        }
        __result = comp.Running ? "MRP_AnalysisInProgress".Translate() : "MRP_AnalysisArmed".Translate();
        return false;
    }
}

/// <summary>
/// 机械体入舱后接管计时：上游 TryAcceptPawn 在 num==0（我们预置的实验位）时无 hediff 副作用，
/// 但会把 ticksRemaining 置为 7500——Postfix 立即覆盖为 -1 并启动本补丁自己的倒计时。
/// </summary>
[HarmonyPatch(typeof(Building_NeuralSynchronizer), "TryAcceptPawn")]
internal static class SynchronizerTryAcceptPatch
{
    private static void Postfix(Building_NeuralSynchronizer __instance, Pawn pawn)
    {
        var comp = __instance.GetComp<CompNeuralAnalyzer>();
        if (comp == null || !comp.Armed || pawn != comp.SelectedMech)
        {
            return;
        }
        if (__instance.Occupant != pawn)
        {
            comp.SoftCancel();
            return;
        }
        comp.BeginAnalysis(pawn);
    }
}
