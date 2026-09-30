using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Harmony patch on the game's sprite lookup that hands out sprites plugins added.</summary>
[HarmonyPatch(typeof(EasySpritesCollection))]
internal static class EasySpritesCollectionPatch
{
    [HarmonyPatch(nameof(EasySpritesCollection.GetSprite))]
    [HarmonyPrefix]
    private static bool GetSpritePreFix(string spriteName, ref Sprite __result)
    {
        if (!TimbnSprites.TryGet(spriteName, out var sprite))
            return true;

        __result = sprite;
        return false;
    }

    [HarmonyPatch(nameof(EasySpritesCollection.HasSprite))]
    [HarmonyPrefix]
    private static bool HasSpritePreFix(string spriteName, ref bool __result)
    {
        if (!TimbnSprites.TryGet(spriteName, out _))
            return true;

        __result = true;
        return false;
    }
}
