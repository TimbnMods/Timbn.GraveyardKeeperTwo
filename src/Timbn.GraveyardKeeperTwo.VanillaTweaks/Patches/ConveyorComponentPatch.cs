using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[TimbnFeature(nameof(PluginConfig.ConveyorLoopCrash))]
[HarmonyPatch]
internal static class ConveyorComponentPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        typeof(ConveyorComponent).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ConveyorComponent)))
            .Select(type => type.GetMethod(nameof(ConveyorComponent.HandleCycleDependency), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => method != null);

    [HarmonyPrefix]
    private static bool HandleCycleDependencyPreFix(ConveyorComponent __instance, ConveyorComponent __0, out bool __state)
    {
        __state = false;
        return Plugin.Instance?.ConveyorLoops.Enter(__instance, __0, out __state) ?? true;
    }

    [HarmonyFinalizer]
    private static void HandleCycleDependencyFinalizer(bool __state) =>
        Plugin.Instance?.ConveyorLoops.Exit(__state);
}
