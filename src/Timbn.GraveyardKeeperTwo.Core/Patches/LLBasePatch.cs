using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the language load that writes the text plugins added into the loaded language.</summary>
[HarmonyPatch(typeof(LLBase))]
internal static class LLBasePatch
{
    [HarmonyPatch(nameof(LLBase.InitHashDictionary))]
    [HarmonyPostfix]
    private static void InitHashDictionaryPostFix(LLBase __instance) =>
        TimbnLocale.ApplyTo(__instance);
}
