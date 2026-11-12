using System.Collections.Generic;
using HarmonyLib;
using MechResonance;
using RimWorld;
using Verse;

namespace MechResonancePatch;

/// <summary>
/// 解析模式布防时接管准入：只放行选中的机械体（原版入口要求"玩家方人形机械师"，
/// 机械体必被拒）；其余 Pawn（包括想做实验的机械师）在布防期间一律拒绝，防止状态交叠。
/// 必须指定参数类型：上游 1.1.0.0 存在 private CanAcceptPawn(Pawn, int) 重载，
/// 不指定会导致 AmbiguousMatchException 并中断整个 PatchAll。
/// </summary>
[HarmonyPatch(typeof(Building_NeuralSynchronizer), "CanAcceptPawn", new[] { typeof(Pawn) })]
internal static class SynchronizerCanAcceptPatch
{
    // 诊断：JobDriver_EnterBuilding 的 FailOn 会在作业全程轮询 CanAcceptPawn，
    // 只在判定结果变化时打一条，避免每 tick 刷屏。
    private static readonly Dictionary<int, string> LastVerdicts = new();

    private static void LogVerdict(Building_NeuralSynchronizer building, Pawn pawn, string verdict)
    {
        int key = building.thingIDNumber;
        if (LastVerdicts.TryGetValue(key, out string prev) && prev == verdict)
        {
            return;
        }
        LastVerdicts[key] = verdict;
        MRPLog.Message($"[MRP诊断] 准入 {building.LabelShort}#{key} ← {pawn?.LabelShort ?? "空"}：{verdict}");
    }

    // 注意：前缀载荷参数用 [HarmonyArgument(0)] 按位置绑定——上游 1.1.0.0 参数名为 pawn、
    // 旧版 1.1.3 为 selPawn，按名绑定会随上游更新漂移并中断 PatchAll。
    private static bool Prefix(Building_NeuralSynchronizer __instance, [HarmonyArgument(0)] Pawn selPawn, ref AcceptanceReport __result)
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
                LogVerdict(__instance, selPawn, "拒绝：未通电");
                __result = "CannotUseNoPower".Translate();
                return false;
            }
            if (__instance.Occupant != null)
            {
                LogVerdict(__instance, selPawn, $"拒绝：舱内已占用（{__instance.Occupant.LabelShort}）");
                __result = "SubcoreScannerOccupied".Translate();
                return false;
            }
            // 先中和上游实验状态：TryAcceptPawn 在 selectedPawn!=null 时会沿用 selectedExperiment
            SynchronizerStateHelper.Neutralize(__instance);
            LogVerdict(__instance, selPawn, "放行（布防目标+配料已齐）");
            __result = AcceptanceReport.WasAccepted;
            return false;
        }
        LogVerdict(__instance, selPawn, comp.Running ? "拒绝：解析进行中" : $"拒绝：布防中只接受布防目标（{comp.SelectedMech?.LabelShort ?? "无"}），配料齐={comp.IngredientsLoaded}，目标准入={comp.CanAnalyze(selPawn)}");
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
    private static void Postfix(Building_NeuralSynchronizer __instance, [HarmonyArgument(0)] Pawn pawn)
    {
        var comp = __instance.GetComp<CompNeuralAnalyzer>();
        if (comp == null || !comp.Armed || pawn != comp.SelectedMech)
        {
            return;
        }
        if (__instance.Occupant != pawn)
        {
            MRPLog.Message($"[MRP诊断] TryAcceptPawn 后舱内为 {__instance.Occupant?.LabelShort ?? "无"} ≠ {pawn.LabelShort}（上游拒绝入舱，检查准入日志）");
            comp.SoftCancel("TryAcceptPawn 未接收布防目标");
            return;
        }
        MRPLog.Message($"[MRP诊断] {pawn.LabelShort} 已被连接器接收，接管计时");
        comp.BeginAnalysis(pawn);
    }
}

/// <summary>
/// 布防解析期间隐藏上游的实验入口按钮与"取消感知回流"按钮——上游 GetGizmos 在
/// selectedPawn/Occupant 存在时按 selectedExperiment 挂取消按钮（0=取消感知回流），
/// 点击会把解析中的机械体弹出；待入舱阶段也会并列显示四个实验入口。
/// 过滤规则：action 委托声明在 Building_NeuralSynchronizer 上的 Command_Action
/// 与网络夺取按钮；建筑/comp 的其余 gizmo 全部保留。
/// </summary>
[HarmonyPatch(typeof(Building_NeuralSynchronizer), "GetGizmos")]
internal static class SynchronizerGizmoFilterPatch
{
    private static void Postfix(Building_NeuralSynchronizer __instance, ref IEnumerable<Gizmo> __result)
    {
        var comp = __instance.GetComp<CompNeuralAnalyzer>();
        if (comp == null || !comp.Armed)
        {
            return;
        }
        var filtered = new List<Gizmo>();
        foreach (var gizmo in __result)
        {
            if (gizmo is Command_NetworkSeizureFromSynchronizer ||
                (gizmo is Command_Action action && action.action?.Method?.DeclaringType == typeof(Building_NeuralSynchronizer)))
            {
                continue;
            }
            filtered.Add(gizmo);
        }
        __result = filtered;
    }
}
