using System;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch(typeof(EnergySystem), nameof(EnergySystem.StartSleeping))]
internal static class EnergySystemPatch
{
    [HarmonyPrefix]
    private static void SaveWhenNotSleepy(ref Action onSleepDidNotStartedCallback, bool sleepWithoutSavingGame)
    {
        if (sleepWithoutSavingGame || !PluginConfig.BedSaveWhenRested.Value)
            return;

        var notSleepy = onSleepDidNotStartedCallback;
        onSleepDidNotStartedCallback = () =>
        {
            SaveSystem.Save(MainGame.Instance.SaveSlotData, MainGame.Instance.GameSave);
            Plugin.Logger.LogInfo("Saved from the bed without sleeping.");
            notSleepy?.Invoke();
        };
    }
}
