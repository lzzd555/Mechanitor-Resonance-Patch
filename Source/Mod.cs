using HarmonyLib;
using Verse;

namespace MechResonancePatch;

public class MechResonancePatchMod : Mod
{
    public const string HarmonyId = "lzzd.mechresonance.universalanalysis";

    public static new ModContentPack Content;

    public MechResonancePatchMod(ModContentPack content) : base(content)
    {
        Content = content;
        var harmony = new Harmony(HarmonyId);
        harmony.PatchAll();
        Log.Message($"[MechResonancePatch {content.PackageIdPlayerFacing}] Harmony patches applied");
    }
}
