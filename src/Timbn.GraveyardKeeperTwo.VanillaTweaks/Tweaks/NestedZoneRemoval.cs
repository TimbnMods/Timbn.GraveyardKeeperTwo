using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class NestedZoneRemoval
{
    public static bool IsInsideBuildArea(IBuildRemovable removable)
    {
        var wgo = removable as Wgo;
        var zone = LazySingleton<BuildManager>.Instance.WorldZone?.Data;
        if (wgo == null || wgo.Data == null || zone == null || wgo.Data.WorldId != zone.gameSceneId)
            return false;

        return zone.wholeZoneRect.Contains(new Vector2(wgo.Data.Position.x, wgo.Data.Position.z));
    }
}
