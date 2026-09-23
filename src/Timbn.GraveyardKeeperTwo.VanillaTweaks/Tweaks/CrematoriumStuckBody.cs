namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class CrematoriumStuckBody
{
    private static readonly string[] _crematoriumIds = ["crematorium_1", "crematorium_2"];

    public static void BurnStuckBodies()
    {
        var burned = 0;
        foreach (var id in _crematoriumIds)
        {
            foreach (var crematorium in MainGame.Instance.GameSave.WorldData.GetWgoDataList(id))
            {
                if (TryBurnStuckBody(crematorium))
                    burned++;
            }
        }

        if (burned > 0)
            Plugin.Logger.LogInfo($"Started the burn for {burned} crematorium body that the game had left stuck.");
    }

    public static bool LacksMastery(WgoData crematorium)
    {
        var craft = crematorium.CraftComponent;
        if (craft == null || craft.AvailableCrafts.Count == 0)
            return false;

        var burn = craft.AvailableCrafts[0];
        var element = new CraftElement(craftParamsData: new CraftParamsData(burn.id, crematorium), craftId: burn.id, count: 1);
        return craft.GetStartCraftStatus(element) == CraftStatus.NotEnoughMastery;
    }

    private static bool TryBurnStuckBody(WgoData crematorium)
    {
        var craft = crematorium.CraftComponent;
        if (craft == null
            || craft.HasCraftsInQueue
            || craft.IsStarted
            || craft.Status == CraftComponentStatus.ReadyToFinishAutoCraft
            || !crematorium.Inventory.Data.TryGetItemInInventoryByGroupId("corpse", out _)
            || craft.AvailableCrafts.Count == 0)
            return false;

        var burn = craft.AvailableCrafts[0];
        var paramsData = new CraftParamsData(burn.id, crematorium, customMasteryLock: 0);
        craft.AddCraftNoStart(new CraftElement(craftParamsData: paramsData, craftId: burn.id, count: 1));
        craft.TryContinueFromQueue();
        return true;
    }
}
