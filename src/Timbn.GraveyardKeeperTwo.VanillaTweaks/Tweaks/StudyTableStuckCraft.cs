namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class StudyTableStuckCraft
{
    private const string _tableId = "survey_wgo";

    public static void ClearStuckCrafts()
    {
        var cleared = 0;
        foreach (var table in MainGame.Instance.GameSave.WorldData.GetWgoDataList(_tableId))
        {
            var craft = table.CraftComponent;
            if (craft == null || !craft.IsStarted || craft.CurrentCraftElement is { IsStarted: true })
                continue;

            craft.Clear();
            cleared++;
        }

        if (cleared > 0)
            Plugin.Logger.LogInfo($"Freed {cleared} study table(s) stuck on a craft that had already finished.");
    }
}
