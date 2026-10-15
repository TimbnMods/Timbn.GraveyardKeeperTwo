using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(LLBase))]
internal static class LLBasePatch
{
    [HarmonyPatch(nameof(LLBase.InitHashDictionary))]
    [HarmonyPostfix]
    private static void InitHashDictionaryPostFix(LLBase __instance) =>
        TimbnLocale.ApplyTo(__instance);
}
