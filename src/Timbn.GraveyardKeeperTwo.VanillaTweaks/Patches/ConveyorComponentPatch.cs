using System.Reflection;
using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch]
internal static class ConveyorComponentPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        typeof(ConveyorComponent).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ConveyorComponent)))
            .Select(type => AccessTools.DeclaredMethod(type, nameof(ConveyorComponent.HandleCycleDependency)))
            .Where(method => method != null);

    [HarmonyPrefix]
    private static bool HandleCycleDependencyPreFix(ConveyorComponent __instance, ConveyorComponent __0, out bool __state) =>
        ConveyorLoopCrash.Enter(__instance, __0, out __state);

    [HarmonyFinalizer]
    private static void HandleCycleDependencyFinalizer(bool __state) =>
        ConveyorLoopCrash.Exit(__state);
}
