namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(MovementComponent))]
internal static class MovementComponentPatch
{
    [HarmonyPatch(nameof(MovementComponent.StartPath), typeof(Vector3), typeof(string), typeof(string), typeof(MovementType), typeof(float), typeof(string), typeof(Action), typeof(Pathfinding.Seeker), typeof(MovementComponent.DestinationType))]
    [HarmonyPrefix]
    private static void StartPathPreFix(MovementComponent __instance) => TimbnGameMoves.OnWalk(__instance);

    [HarmonyPatch(nameof(MovementComponent.StartPath), typeof(Vector3), typeof(Pathfinding.GraphMask), typeof(string), typeof(float), typeof(string), typeof(Action), typeof(MovementComponent.DestinationType))]
    [HarmonyPrefix]
    private static void StartZonePathPreFix(MovementComponent __instance) => TimbnGameMoves.OnWalk(__instance);
}
