using UnityEngine.ResourceManagement.AsyncOperations;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Serves a plugin model's template where the pool would load a world object's addressable prefab.</summary>
[HarmonyPatch(typeof(WgoPartPool))]
internal static class WgoPartPoolPatch
{
    [HarmonyPatch("LoadPrefabAsync")]
    [HarmonyPrefix]
    private static bool LoadPrefabAsyncPreFix(string addressableKey, ref AsyncOperationHandle<GameObject> __result)
    {
        if (!TimbnModels.TryGetPart(addressableKey, out var part))
            return true;

        __result = part.CreateHandle();
        return false;
    }
}
