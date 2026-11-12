using System.Linq;
using MechResonance;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MechResonancePatch;

/// <summary>
/// 解析模式布防后，把 200 玻璃钢 + 3 高级零部件搬入神经连接器
/// （HaulToContainer 作业会存入 Building_Enterable.innerContainer，
/// 镜像原版 WorkGiver_HaulToSubcoreScanner 与上游 Apocriton 尸体搬运的模式）。
/// </summary>
public class WorkGiver_AnalyzerIngredients : WorkGiver_Scanner
{
    private static ThingDef _synchronizerDef;

    private static ThingDef SynchronizerDef => _synchronizerDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("MR_NeuralSynchronizer");

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(SynchronizerDef);

    // 与上游 WorkGiver_HaulApocritonCorpse 相同的路径终点
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_NeuralSynchronizer building || !building.Spawned || !building.PowerOn)
        {
            return false;
        }
        var comp = building.GetComp<CompNeuralAnalyzer>();
        return comp != null && comp.Armed && !comp.IngredientsLoaded && IngredientJobFor(pawn, building) != null;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_NeuralSynchronizer building || !building.Spawned || !building.PowerOn)
        {
            return null;
        }
        var comp = building.GetComp<CompNeuralAnalyzer>();
        if (comp == null || !comp.Armed || comp.IngredientsLoaded)
        {
            return null;
        }
        return IngredientJobFor(pawn, building);
    }

    private static Job IngredientJobFor(Pawn pawn, Building_NeuralSynchronizer building)
    {
        var comp = building.GetComp<CompNeuralAnalyzer>();
        var needs = new (ThingDef def, int count)[]
        {
            (ThingDefOf.Plasteel, comp.Props.plasteelCost),
            (ThingDefOf.ComponentSpacer, comp.Props.advancedComponentCost),
        };
        foreach (var (def, needed) in needs)
        {
            int remaining = needed - building.innerContainer.TotalStackCountOfDef(def);
            if (remaining <= 0)
            {
                continue;
            }
            var thing = FindReachableStack(pawn, def);
            if (thing == null)
            {
                continue;
            }
            var job = JobMaker.MakeJob(JobDefOf.HaulToContainer, thing, building);
            job.count = Mathf.Min(thing.stackCount, remaining);
            job.haulMode = HaulMode.ToContainer;
            return job;
        }
        return null;
    }

    private static Thing FindReachableStack(Pawn pawn, ThingDef def)
    {
        Thing best = null;
        int bestDistance = int.MaxValue;
        foreach (var thing in pawn.Map.listerThings.ThingsOfDef(def))
        {
            if (thing.stackCount <= 0 || thing.IsForbidden(pawn) ||
                !pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                continue;
            }
            int distance = pawn.Position.DistanceToSquared(thing.Position);
            if (distance < bestDistance)
            {
                best = thing;
                bestDistance = distance;
            }
        }
        return best;
    }
}
