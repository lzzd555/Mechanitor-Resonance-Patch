using System.Collections.Generic;
using MechResonance;
using RimWorld;
using Verse;
using Verse.AI;

namespace MechResonancePatch;

/// <summary>
/// 选中殖民地机械体右击神经连接器：配料齐备时提供"进入连接器开始解析"入口
/// （镜像上游 FloatMenuOptionProvider_MechScan 的机械体入舱方式：SelectPawn + EnterBuilding 作业）。
/// </summary>
public class FloatMenuOptionProvider_Analyzer : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool MechanoidCanDo => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
    {
        return base.SelectedPawnValid(pawn, context) && pawn.IsColonyMech;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing is not Building_NeuralSynchronizer building)
        {
            yield break;
        }
        var comp = building.GetComp<CompNeuralAnalyzer>();
        if (comp == null || !comp.Armed)
        {
            yield break;
        }
        var pawn = context.FirstSelectedPawn;
        if (pawn == null || pawn != comp.SelectedMech)
        {
            yield break;
        }
        if (!comp.IngredientsLoaded)
        {
            yield return new FloatMenuOption("MRP_WaitingIngredients".Translate(), null);
            yield break;
        }
        if (!comp.CanAnalyze(pawn) || building.Occupant != null)
        {
            yield break;
        }
        string label = "MRP_EnterAnalyzerLabel".Translate(GenDate.ToStringTicksToPeriod(comp.AnalysisDurationFor(pawn.def)));
        yield return new FloatMenuOption(label, () =>
        {
            if (!comp.CanAnalyze(pawn) || building.Occupant != null || !building.Spawned)
            {
                return;
            }
            SynchronizerStateHelper.Neutralize(building);
            SynchronizerStateHelper.SelectPawnOn(building, pawn);
            Job job = JobMaker.MakeJob(JobDefOf.EnterBuilding, building);
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        });
    }
}
