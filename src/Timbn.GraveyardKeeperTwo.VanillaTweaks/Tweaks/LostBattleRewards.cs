namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class LostBattleRewards
{
    public static void Recover()
    {
        var player = MainGame.PlayerData;
        var world = MainGame.WorldData;
        if (player == null || world == null || string.IsNullOrEmpty(player.currentGameSceneId))
            return;

        var rewardIds = RewardIds();
        List<(GameSceneData Scene, DropData Drop)> lost = [];
        foreach (var scene in world.gameSceneDataList)
        {
            Find(scene, scene.droppedItems, rewardIds, lost);
            Find(scene, scene.queuedDrops, rewardIds, lost);
        }

        if (lost.Count == 0)
            return;

        var playerPosition = player.position.Value;
        if (!SpecialPhysicsCastUtils.GetPlayerDropPosition(playerPosition, player.Direction, out var position))
            position = playerPosition;

        foreach (var (scene, drop) in lost)
        {
            var item = new Item(drop.Id, drop.Count);
            scene.RemoveDrop(drop);
            MainGame.Instance.dropSystem.DropItemAsDropView(item, player.currentGameSceneId, position);
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

    private static void Find(
        GameSceneData scene,
        List<DropData>? drops,
        HashSet<string> rewardIds,
        List<(GameSceneData Scene, DropData Drop)> lost)
    {
        if (drops == null)
            return;

        foreach (var drop in drops)
        {
            if (drop?.Item != null && !drop.IsRemoving && rewardIds.Contains(drop.Id))
                lost.Add((scene, drop));
        }
    }
}
