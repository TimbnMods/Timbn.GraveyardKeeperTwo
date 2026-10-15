namespace Timbn.GraveyardKeeperTwo.Core;

internal sealed class TimbnCoreSaveData
{
    public HashSet<string> SeenTalks { get; set; } = [];

    public HashSet<string> QuestsReady { get; set; } = [];

    public Dictionary<string, TimbnQuestRunData> QuestRuns { get; set; } = [];
}
