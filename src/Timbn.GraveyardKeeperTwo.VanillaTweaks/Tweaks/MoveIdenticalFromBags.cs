namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class MoveIdenticalFromBags
{
    public static bool Applies(Inventory target, Inventory? source) =>
        PluginConfig.MoveIdenticalFromBags.Value
        && source != null
        && source == MainGame.PlayerData?.Inventory
        && target.Data != null
        && !target.Data.IsBag;
}
