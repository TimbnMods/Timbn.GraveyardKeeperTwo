namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class LostItems
{
    private const string _npcId = "npc_larry";

    private static readonly string[] _boardMakerIds = ["sawhorse", "sawhorse_place", "circular_saw", "circular_saw_place"];

    private static readonly LostItem[] _items =
    [
        new(
            "timbn_larry_boards",
            "flitch",
            2,
            () => PluginConfig.SoftLockedBoards.Value,
            () => MainGame.Instance.GameSave.knowledgeSystem.IsTechUnlocked("wood_basic")
                && !_boardMakerIds.Any(id => MainGame.WorldData.GetWgoDataList(id).Count > 0)),
        new(
            "timbn_larry_medallion",
            "intro_prison_medallion",
            1,
            () => PluginConfig.LostQuestItems.Value,
            () => TimbnQuests.StatusOf("1_intro_prison_find_key") is QuestStatus.InProgress or QuestStatus.Completed
                && TimbnQuests.StatusOf("22_doctor_tower_door") is not QuestStatus.Completed),
    ];

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        plugin.Text.AddLanguageFiles();
        foreach (var item in _items)
        {
            plugin.Dialog.AddTalk(_npcId, item.Name + "_talk", item.IsLost, talk => talk
                .PlayerSay(item.Name + "_offer_1")
                .Say(item.Name + "_offer_2")
                .Ask(item.Name + "_accept", item.Name + "_decline")
                .If(item.Name + "_accept", then => then
                    .Do(() => Give(item, talk.Npc))
                    .Say(item.Name + "_given")
                    .PlayerSay(item.Name + "_accepted"))
                .If(item.Name + "_decline", then => then
                    .Say(item.Name + "_declined")));
        }
    }

    private static int CountAnywhere(string id, int enough)
    {
        var count = MainGame.PlayerData.inventory.Data.GetTotalCountInInventory(id);
        if (count >= enough)
            return count;

        foreach (var scene in MainGame.WorldData.gameSceneDataList)
        {
            foreach (var wgo in scene.wgoDataList)
            {
                if (wgo.Definition?.hasRefToOtherWgoInventory == true)
                    continue;

                count += wgo.Inventory?.Data?.GetTotalCountInInventory(id) ?? 0;
                count += wgo.CraftInventory?.Data?.GetTotalCountInInventory(id) ?? 0;
            }

            count += scene.droppedItems.Where(drop => drop.Id == id).Sum(drop => drop.Count);
            if (count >= enough)
                return count;
        }

        return count;
    }

    private static void Give(LostItem lost, WgoData npc)
    {
        var item = new Item(lost.Id, lost.Count);
        var moved = new MultiInventory(MainGame.PlayerData).AddItem(item);
        if (moved < item.Count)
        {
            var rest = Item.Copy(item);
            rest.Count = item.Count - moved;
            MainGame.Instance.dropSystem.DropItemAsDropView(rest, npc.WorldId, npc.Position);
        }

        Plugin.Logger.LogInfo($"Larry gave you {lost.Count} {lost.Id}, {moved} into your inventory.");
    }

    private sealed class LostItem(string name, string id, int count, Func<bool> isEnabled, Func<bool> isNeeded)
    {
        public string Name { get; } = name;

        public string Id { get; } = id;

        public int Count { get; } = count;

        public bool IsLost() => isEnabled() && isNeeded() && CountAnywhere(Id, Count) < Count;
    }
}
