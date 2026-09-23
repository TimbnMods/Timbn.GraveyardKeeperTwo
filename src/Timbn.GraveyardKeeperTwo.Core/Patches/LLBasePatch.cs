using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(LLBase), nameof(LLBase.InitHashDictionary))]
internal static class LLBasePatch
{
    private static void Postfix(LLBase __instance) => TimbnLocale.ApplyTo(__instance);
}
