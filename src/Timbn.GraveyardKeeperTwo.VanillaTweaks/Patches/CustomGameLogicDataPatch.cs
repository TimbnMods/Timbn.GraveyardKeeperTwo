namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(CustomGameLogicData), "UpdateTimer")]
internal static class CustomGameLogicDataPatch
{
    private const string _restoreSuffix = "_restore";

    [HarmonyPrefix]
    private static bool ScheduleNextRefill(CustomGameLogicData __instance)
    {
        if (!PluginConfig.FishingRefill.Value || !__instance.id.EndsWith(_restoreSuffix, StringComparison.Ordinal))
            return true;

        var fishingId = __instance.id[..^_restoreSuffix.Length];
        var fishing = GameBalance.Me.fishingDefs.Find(def => def.id == fishingId);
        var environment = MainGame.Instance.GameSave.environmentData;
        if (fishing == null || environment.EnvironmentEngine == null)
            return true;

        var next = MathF.Round(environment.Day + environment.TimeOfDay
            + fishing.regenTime / environment.EnvironmentEngine.gameplayDayInMinutes, 3);

        __instance.execDay = (int)next;
        __instance.execTime = next - __instance.execDay;
        return false;
    }
}
