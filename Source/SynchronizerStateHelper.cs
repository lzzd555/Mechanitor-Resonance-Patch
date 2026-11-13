using System.Reflection;
using HarmonyLib;
using MechResonance;
using RimWorld;
using UnityEngine;
using Verse;

namespace MechResonancePatch;

/// <summary>缓存上游建筑私有状态的反射访问。</summary>
internal static class SynchronizerStateHelper
{
    private static readonly FieldInfo ExperimentField = AccessTools.Field(typeof(Building_NeuralSynchronizer), "selectedExperiment");
    private static readonly FieldInfo TicksField = AccessTools.Field(typeof(Building_NeuralSynchronizer), "ticksRemaining");
    private static readonly FieldInfo SelectedPawnField = AccessTools.Field(typeof(Building_Enterable), "selectedPawn");

    /// <summary>
    /// 把上游实验状态置为惰性：selectedExperiment=0（无每 tick 分支、完成时无副作用）、
    /// ticksRemaining=-1（永不触发上游 CompleteExperiment）。我们的机械体入舱前后都要调用。
    /// </summary>
    public static void Neutralize(Building_NeuralSynchronizer building)
    {
        ExperimentField?.SetValue(building, 0);
        TicksField?.SetValue(building, -1);
    }

    public static Pawn SelectedPawnOf(Building_NeuralSynchronizer building)
    {
        return (Pawn)SelectedPawnField?.GetValue(building);
    }

    /// <summary>等效于 protected 的 Building_Enterable.SelectPawn（EnterBuilding 作业驱动据此校验）。</summary>
    public static void SelectPawnOn(Building_NeuralSynchronizer building, Pawn pawn)
    {
        SelectedPawnField?.SetValue(building, pawn);
    }
}

/// <summary>本补丁的贴图缓存（解析图标使用本 mod 自带贴图，取消图标复用原版贴图）。</summary>
internal static class TexPool
{
    private static readonly CachedTexture AnalyzeMechPath = new("UI/Commands/NeuralSynchronizer/MRP_AnalyzeMech");
    private static readonly CachedTexture CancelPath = new("UI/Designators/Cancel");

    public static Texture2D AnalyzeMech => AnalyzeMechPath.Texture;
    public static Texture2D Cancel => CancelPath.Texture;
}
