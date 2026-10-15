namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class LostItems
{
    private const string _npcId = TimbnGameIds.Npcs.Larry;

    private static readonly string[] _boardMakerIds = [TimbnGameIds.Workstations.Yard.Sawhorse, "sawhorse_place", TimbnGameIds.Workstations.Yard.CircularSaw, "circular_saw_place"];

    private static readonly LostItem[] _items =
    [
        new(
            "timbn_larry_boards_talk",
            TimbnGameIds.Items.Flitch,
            2,
            () => PluginConfig.SoftLockedBoards.Value,
            () => TimbnPlayer.HasTech(TimbnGameIds.Techs.WoodBasic)
                && !_boardMakerIds.Any(id => TimbnWorld.FindAll(id).Count > 0),
            new Lines(
                "I may have run out of boards with no way to get more.",
                "No sawhorse, no boards. No boards, no sawhorse...",
                "Got any boards lying around?",
                "Lucky for you, there are a few spare boards in my box. Here you go.",
                "Thank you, I will be more careful next time.",
                "I'll figure something out.",
                "Suit yourself... I'll be right here.")),
        new(
            "timbn_larry_medallion_talk",
            TimbnGameIds.Items.IntroPrisonMedallion,
            1,
            () => PluginConfig.LostQuestItems.Value,
            () => TimbnQuests.StatusOf("1_intro_prison_find_key") is QuestStatus.InProgress or QuestStatus.Completed
                && TimbnQuests.StatusOf("22_doctor_tower_door") is not QuestStatus.Completed,
            new Lines(
                "Have you seen an Inquisitor's medallion anywhere?",
                "Funny thing, one rolled up against my box this morning.",
                "That's the one! Can I have it back?",
                "Here you go.",
                "Thank you, Larry. I'll hold on to it this time.",
                "No, that can't be mine.",
                "Suit yourself...")),
    ];

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        foreach (var item in _items)
        {
            var lines = item.Lines;
            plugin.Dialog.AddTalk(_npcId, item.TalkId, item.IsLost, talk => talk
                .PlayerSay(lines.Asked)
                .Say(lines.Found)
                .Ask(ask => ask
                    .Answer(lines.Accept, then => then
                        .Give(item.Id, item.Count)
                        .Say(lines.Given)
                        .PlayerSay(lines.Thanks))
                    .Answer(lines.Decline, then => then
                        .Say(lines.Declined))));
        }
    }

    private sealed class LostItem(string talkId, string id, int count, Func<bool> isEnabled, Func<bool> isNeeded, Lines lines)
    {
        public string TalkId { get; } = talkId;

        public string Id { get; } = id;

        public int Count { get; } = count;

        public Lines Lines { get; } = lines;

        public bool IsLost() => isEnabled() && isNeeded() && TimbnItems.CountAnywhere(Id, Count) < Count;
    }

    private sealed class Lines(string asked, string found, string accept, string given, string thanks, string decline, string declined)
    {
        public string Asked { get; } = asked;

        public string Found { get; } = found;

        public string Accept { get; } = accept;

        public string Given { get; } = given;

        public string Thanks { get; } = thanks;

        public string Decline { get; } = decline;

        public string Declined { get; } = declined;
    }
}
