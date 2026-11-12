using System.Collections.Generic;
using System.Linq;
using System.Text;
using MechResonance;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MechResonancePatch;

public class CompProperties_NeuralAnalyzer : CompProperties
{
    public int plasteelCost = 200;
    public int advancedComponentCost = 3;
    public float durationFactor = 2f;

    public CompProperties_NeuralAnalyzer()
    {
        compClass = typeof(CompNeuralAnalyzer);
    }
}

/// <summary>
/// 神经连接器的机械体解析模式：
/// 布防并选机 → 搬入配料（200 玻璃钢 + 3 高级零部件）→ 机械体进入 → 倒计时为裂解扫描的
/// durationFactor 倍 → 完成时扣料、调用上游 MechScanResearch.Complete、弹出存活的机械体。
/// 倒计时在本 comp 的 CompTick 内进行；上游建筑 Tick 因 selectedExperiment=0、
/// ticksRemaining=-1（见 SynchronizerStateHelper.Neutralize）对本流程完全惰性。
/// </summary>
public class CompNeuralAnalyzer : ThingComp
{
    public bool Armed;
    public Pawn SelectedMech;
    public int TicksRemaining;
    public int TotalDuration;

    public CompProperties_NeuralAnalyzer Props => (CompProperties_NeuralAnalyzer)props;

    public Building_NeuralSynchronizer Building => (Building_NeuralSynchronizer)parent;

    public bool Running => Armed && Building.Occupant == SelectedMech && SelectedMech != null;

    private int InContainer(ThingDef def)
    {
        return Building.innerContainer.TotalStackCountOfDef(def);
    }

    public bool IngredientsLoaded =>
        InContainer(ThingDefOf.Plasteel) >= Props.plasteelCost &&
        InContainer(ThingDefOf.ComponentSpacer) >= Props.advancedComponentCost;

    public int AnalysisDurationFor(ThingDef mechDef)
    {
        return Mathf.RoundToInt(UpstreamApi.DurationTicks(mechDef) * Props.durationFactor);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref Armed, "MRP_armed", false);
        Scribe_References.Look(ref SelectedMech, "MRP_selectedMech");
        Scribe_Values.Look(ref TicksRemaining, "MRP_ticksRemaining", 0);
        Scribe_Values.Look(ref TotalDuration, "MRP_totalDuration", 0);
    }

    public bool CanAnalyze(Pawn pawn)
    {
        return pawn != null && pawn.IsColonyMech && !pawn.Dead &&
            MechanitorUtility.GetOverseer(pawn) != null &&
            !UpstreamApi.IsApocriton(pawn.def) &&
            UpstreamApi.HasAnalysisProject(pawn.def) &&
            !UpstreamApi.IsResearched(pawn.def);
    }

    public void ArmFor(Pawn mech)
    {
        Armed = true;
        SelectedMech = mech;
        SynchronizerStateHelper.Neutralize(Building);
    }

    /// <summary>取消/中止：弹出舱内一切（机械体与未消耗配料一并退还）。</summary>
    public void Disarm()
    {
        Armed = false;
        SelectedMech = null;
        TicksRemaining = 0;
        TotalDuration = 0;
        if (parent.Spawned)
        {
            Building.EjectContents();
        }
        SynchronizerStateHelper.Neutralize(Building);
    }

    /// <summary>状态异常时的静默回退（同 Disarm，但保证建筑状态干净）。</summary>
    public void SoftCancel()
    {
        Disarm();
    }

    /// <summary>机械体已被建筑接收（TryAcceptPawn Postfix 调用），接管计时并中和上游实验状态。</summary>
    public void BeginAnalysis(Pawn mech)
    {
        TotalDuration = AnalysisDurationFor(mech.def);
        TicksRemaining = TotalDuration;
        SynchronizerStateHelper.Neutralize(Building);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!Armed)
        {
            return;
        }
        var building = Building;
        var occupant = building.Occupant;
        if (occupant == null)
        {
            // 布防待入舱：选中的机械体失去资格则回退
            if (SelectedMech != null && (!SelectedMech.Spawned || SelectedMech.Dead || !CanAnalyze(SelectedMech)))
            {
                SoftCancel();
            }
            return;
        }
        if (occupant != SelectedMech)
        {
            // 不应发生（CanAcceptPawn 只放行 SelectedMech）
            SoftCancel();
            return;
        }
        if (!building.PowerOn)
        {
            return;
        }
        TicksRemaining--;
        if (TicksRemaining <= 0)
        {
            CompleteAnalysis(occupant);
        }
    }

    private void CompleteAnalysis(Pawn mech)
    {
        var def = mech.def;
        bool alreadyDone = UpstreamApi.IsResearched(def);
        if (!alreadyDone)
        {
            ConsumeIngredients();
            UpstreamApi.Complete(def, parent.InteractionCell, parent.Map);
        }
        else
        {
            Messages.Message("MRP_AlreadyAnalyzed".Translate(def.label), parent, MessageTypeDefOf.NeutralEvent, false);
        }
        Disarm(); // EjectContents：存活机械体 + 超额配料弹出
    }

    private void ConsumeIngredients()
    {
        Consume(ThingDefOf.Plasteel, Props.plasteelCost);
        Consume(ThingDefOf.ComponentSpacer, Props.advancedComponentCost);
    }

    private void Consume(ThingDef def, int count)
    {
        foreach (var item in Building.innerContainer.Where(t => t.def == def).ToList())
        {
            if (count <= 0)
            {
                break;
            }
            int take = Mathf.Min(item.stackCount, count);
            count -= take;
            item.SplitOff(take).Destroy();
        }
    }

    public void ChooseMech()
    {
        var candidates = parent.Map.mapPawns.AllPawnsSpawned.Where(CanAnalyze).ToList();
        if (candidates.Count == 0)
        {
            Messages.Message("MRP_NoAnalyzableMechs".Translate(), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }
        var options = candidates
            .OrderBy(p => p.def.label)
            .Select(p => new FloatMenuOption(p.LabelCap, () => ArmFor(p)))
            .ToList();
        Find.WindowStack.Add(new FloatMenu(options));
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        var building = Building;
        if (!Armed && building.Occupant == null && SynchronizerStateHelper.SelectedPawnOf(building) == null)
        {
            var command = new Command_Action
            {
                defaultLabel = "MRP_AnalyzeMechLabel".Translate(),
                defaultDesc = "MRP_AnalyzeMechDesc".Translate(Props.plasteelCost, Props.advancedComponentCost, Props.durationFactor),
                icon = TexPool.AnalyzeMech,
                activateSound = SoundDefOf.Tick_Tiny,
                action = ChooseMech,
            };
            if (!building.PowerOn)
            {
                command.Disable("NoPower".Translate().CapitalizeFirst());
            }
            yield return command;
        }
        else if (Armed)
        {
            yield return new Command_Action
            {
                defaultLabel = "MRP_CancelAnalysisLabel".Translate(),
                defaultDesc = "MRP_CancelAnalysisDesc".Translate(),
                icon = TexPool.Cancel,
                activateSound = SoundDefOf.Tick_Tiny,
                action = Disarm,
            };
            if (Running && Prefs.DevMode && DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Finish analysis",
                    icon = TexPool.Cancel,
                    action = () => TicksRemaining = 1,
                };
            }
        }
    }

    public override string CompInspectStringExtra()
    {
        if (!Armed)
        {
            return null;
        }
        var builder = new StringBuilder();
        if (Running)
        {
            builder.AppendLine("MRP_AnalysisProgress".Translate(SelectedMech.def.label, GenDate.ToStringTicksToPeriod(TicksRemaining)));
        }
        else
        {
            builder.AppendLine("MRP_AnalysisWaiting".Translate(SelectedMech?.def.label ?? "…"));
        }
        builder.Append("MRP_AnalysisIngredients".Translate(
            InContainer(ThingDefOf.Plasteel), Props.plasteelCost,
            InContainer(ThingDefOf.ComponentSpacer), Props.advancedComponentCost));
        return builder.ToString();
    }
}
