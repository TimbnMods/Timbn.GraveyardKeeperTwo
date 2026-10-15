namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class BedSaveWhenRested
{
    public static void Register(TimbnFrameworkPlugin plugin) =>
        plugin.Settings.While(PluginConfig.BedSaveWhenRested, () => plugin.Events.SleepRefused(Save));

    private static void Save(bool wouldSave)
    {
        if (!wouldSave)
            return;

        if (TimbnGame.SaveNow())
            Plugin.Logger.LogInfo("Saved from the bed without sleeping.");
    }
}
