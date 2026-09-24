namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class SoftLockedBoards
{
    private const string _npcId = "npc_larry";
    private const string _talkId = "timbn_larry_boards_talk";
    private const string _boardId = "flitch";
    private const string _sawhorseTechId = "wood_basic";
    private const int _sawhorseBoards = 2;
    private const int _boardsGiven = 2;

    private static readonly string[] _boardMakerIds = ["sawhorse", "sawhorse_place", "circular_saw", "circular_saw_place"];

    private static readonly Dictionary<string, string> _texts = new()
    {
        ["timbn_larry_boards_offer_1"] = "I may have run out of boards with no way to get more.",
        ["timbn_larry_boards_offer_2"] = "No sawhorse, no boards. No boards, no sawhorse... Even I can see the problem, and I don't have a brain.",
        ["timbn_larry_boards_accept"] = "Got any boards lying around?",
        ["timbn_larry_boards_decline"] = "I'll figure something out.",
        ["timbn_larry_boards_given"] = "Lucky for you, the Inquisitors left a couple behind my box. Here... Try not to lose these too. He he.",
        ["timbn_larry_boards_declined"] = "Suit yourself... I'll be right here on my box. As always.",
        ["timbn_larry_boards_accepted"] = "Thank you, I will be more careful next time.",
    };

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        plugin.Text.Add(_texts);
        plugin.Dialog.AddTalk(_npcId, _talkId, IsStuck, talk => talk
            .PlayerSay("timbn_larry_boards_offer_1")
            .Say("timbn_larry_boards_offer_2")
            .Ask("timbn_larry_boards_accept", "timbn_larry_boards_decline")
            .If("timbn_larry_boards_accept", then => then
                .Do(() => GiveBoards(talk.Npc))
                .Say("timbn_larry_boards_given")
                .PlayerSay("timbn_larry_boards_accepted"))
            .If("timbn_larry_boards_decline", then => then
                .Say("timbn_larry_boards_declined")));
    }

    private static bool IsStuck()
    {
        if (!MainGame.Instance.GameSave.knowledgeSystem.IsTechUnlocked(_sawhorseTechId))
            return false;

        var world = MainGame.WorldData;
        if (_boardMakerIds.Any(id => world.GetWgoDataList(id).Count > 0))
            return false;

        return !HasBoards();
    }

    private static bool HasBoards()
    {
        var count = MainGame.PlayerData.inventory.Data.GetTotalCountInInventory(_boardId);
        foreach (var scene in MainGame.WorldData.gameSceneDataList)
        {
            foreach (var wgo in scene.wgoDataList)
            {
                if (wgo.Definition?.hasRefToOtherWgoInventory == true)
                    continue;

                count += wgo.Inventory?.Data?.GetTotalCountInInventory(_boardId) ?? 0;
                count += wgo.CraftInventory?.Data?.GetTotalCountInInventory(_boardId) ?? 0;
            }

            count += scene.droppedItems.Where(drop => drop.Id == _boardId).Sum(drop => drop.Count);
            if (count >= _sawhorseBoards)
                return true;
        }

        return false;
    }

    private static void GiveBoards(WgoData larry)
    {
        var boards = new Item(_boardId, _boardsGiven);
        var moved = new MultiInventory(MainGame.PlayerData).AddItem(boards);
        if (moved < boards.Count)
        {
            var rest = Item.Copy(boards);
            rest.Count = boards.Count - moved;
            MainGame.Instance.dropSystem.DropItemAsDropView(rest, larry.WorldId, larry.Position);
        }

        Plugin.Logger.LogInfo($"Larry gave you {_boardsGiven} boards, {moved} into your inventory.");
    }
}
