using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MechResonancePatch;

/// <summary>
/// 把解析模式 ThingComp 运行时挂到上游 MR_NeuralSynchronizer 上（镜像上游
/// MechScanRegistration 给子核扫描仪挂 CompMechScan 的做法，无需改上游 XML）。
/// </summary>
[StaticConstructorOnStartup]
internal static class SynchronizerRegistration
{
    private const string SynchronizerDefName = "MR_NeuralSynchronizer";

    static SynchronizerRegistration()
    {
        if (!ModsConfig.BiotechActive)
        {
            return;
        }
        var def = DefDatabase<ThingDef>.GetNamedSilentFail(SynchronizerDefName);
        if (def == null)
        {
            Log.WarningOnce("[MechResonancePatch] MR_NeuralSynchronizer def not found; analyzer mode disabled", 0x4D525020);
            return;
        }
        def.comps ??= new List<CompProperties>();
        if (def.comps.OfType<CompProperties_NeuralAnalyzer>().Any())
        {
            return;
        }
        def.comps.Add(new CompProperties_NeuralAnalyzer());
        Log.Message("[MechResonancePatch] neural analyzer mode registered on MR_NeuralSynchronizer");
    }
}
