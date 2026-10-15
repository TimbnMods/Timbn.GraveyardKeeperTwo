namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(GameSave))]
internal static class GameSavePatch
{
    [HarmonyPatch(nameof(GameSave.PrepareForGame))]
    [HarmonyPrefix]
    private static void PrepareForGamePreFix(GameSave __instance) =>
        TimbnSafe.Run(() => TimbnModContent.OnPrepareForGame(__instance), $"{nameof(GameSavePatch)}|Putting mod items, buffs, crafts and stations back into the save");
}
