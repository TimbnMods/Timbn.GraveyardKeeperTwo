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
            () => TimbnPlayer.HasTech("wood_basic")
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
        foreach (var item in _items)
        {
            plugin.Dialog.AddTalk(_npcId, item.Name + "_talk", item.IsLost, talk => talk
                .PlayerSay(item.Name + "_offer_1")
                .Say(item.Name + "_offer_2")
                .Ask(item.Name + "_accept", item.Name + "_decline")
                .If(item.Name + "_accept", then => then
                    .Give(item.Id, item.Count)
                    .Say(item.Name + "_given")
                    .PlayerSay(item.Name + "_accepted"))
                .If(item.Name + "_decline", then => then
                    .Say(item.Name + "_declined")));
        }
    }

    private sealed class LostItem(string name, string id, int count, Func<bool> isEnabled, Func<bool> isNeeded)
    {
        public string Name { get; } = name;

        public string Id { get; } = id;

        public int Count { get; } = count;

        public bool IsLost() => isEnabled() && isNeeded() && TimbnItems.CountAnywhere(Id, Count) < Count;
    }
}
