namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>
/// Gives a small item the player carries over their head a look of its icon, where the game only has models for logs,
/// stones, bodies and crates and would show a blank box.
/// </summary>
[HarmonyPatch(typeof(DropViewAtomMesh))]
internal static class DropViewAtomMeshPatch
{
    [HarmonyPatch(nameof(DropViewAtomMesh.Activate))]
    [HarmonyPrefix]
    private static void ActivatePreFix(DropViewAtomMesh __instance, string iconId) =>
        TimbnSafe.Run(() => TimbnCarry.GiveLook(__instance, iconId), $"{nameof(DropViewAtomMeshPatch)}|The carried look of {iconId}");
}
