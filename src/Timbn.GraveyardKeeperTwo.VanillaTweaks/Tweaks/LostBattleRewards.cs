namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class LostBattleRewards
{
    public static void Register(TimbnFrameworkPlugin plugin) =>
        plugin.Settings.While(PluginConfig.RecoverBattleRewards, () => plugin.Events.SleepStarted(Recover));

    private static void Recover()
    {
        if (TimbnPlayer.SceneId is not { Length: > 0 } sceneId)
            return;

        var rewardIds = RewardIds();
        var lost = TimbnItems.FindDrops(drop => rewardIds.Contains(drop.Id));
        if (lost.Count == 0)
            return;

        var position = TimbnItems.PlayerDropPosition();
        foreach (var (scene, drop) in lost)
        {
            var item = new Item(drop.Id, drop.Count);
            scene.RemoveDrop(drop);
            TimbnItems.DropAt(item, sceneId, position);
            Plugin.Logger.LogInfo($"Brought back {item.Count}x {item.id} from {scene.id}.");
        }
    }

    private static HashSet<string> RewardIds()
    {
        HashSet<string> ids = [];
        foreach (var fight in GameBalance.Me.fightDefinitions)
        {
            if (fight?.rewards == null)
                continue;

            foreach (var reward in fight.rewards)
            {
                if (!string.IsNullOrEmpty(reward?.id))
                    ids.Add(reward!.id);
            }
        }

        return ids;
    }
}
