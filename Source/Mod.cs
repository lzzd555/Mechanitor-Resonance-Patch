using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MechResonancePatch;

public class MechResonancePatchMod : Mod
{
    public const string HarmonyId = "lzzd.mechresonance.universalanalysis";

    public static new ModContentPack Content;
    public static MechResonancePatchSettings Settings;

    public MechResonancePatchMod(ModContentPack content) : base(content)
    {
        Content = content;
        Settings = GetSettings<MechResonancePatchSettings>();
        var harmony = new Harmony(HarmonyId);
        harmony.PatchAll();
        Log.Message($"[MechResonancePatch {content.PackageIdPlayerFacing}] Harmony patches applied");
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("MRP_DiagLoggingLabel".Translate(), ref Settings.diagLogging, "MRP_DiagLoggingDesc".Translate());
        listing.Gap(6f);
        listing.End();
    }

    public override string SettingsCategory() => Content.Name;
}

public class MechResonancePatchSettings : ModSettings
{
    public bool diagLogging;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref diagLogging, "MRP_diagLogging", false);
        base.ExposeData();
    }
}

/// <summary>诊断日志门面：由 mod 设置控制是否输出 [MRP诊断] 行，避免日常游戏刷日志。</summary>
public static class MRPLog
{
    public static void Message(string text)
    {
        if (MechResonancePatchMod.Settings?.diagLogging == true)
        {
            Log.Message(text);
        }
    }
}
