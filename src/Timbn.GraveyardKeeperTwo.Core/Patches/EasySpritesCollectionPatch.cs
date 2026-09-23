using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(EasySpritesCollection))]
internal static class EasySpritesCollectionPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(EasySpritesCollection.GetSprite))]
    private static bool GetSprite(string spriteName, ref Sprite __result)
    {
        if (!TimbnSprites.TryGet(spriteName, out var sprite))
            return true;

        __result = sprite;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EasySpritesCollection.HasSprite))]
    private static bool HasSprite(string spriteName, ref bool __result)
    {
        if (!TimbnSprites.TryGet(spriteName, out _))
            return true;

        __result = true;
        return false;
    }
}
