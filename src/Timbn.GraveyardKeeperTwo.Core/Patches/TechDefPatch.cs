namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(TechDef))]
internal static class TechDefPatch
{
    [HarmonyPatch(nameof(TechDef.InitTechs))]
    [HarmonyPostfix]
    private static void InitTechsPostFix() =>
        TimbnSafe.Run(TimbnTechRows.RefreshAll, $"{nameof(TechDefPatch)}|The rewards a mod's tech lists");
}
