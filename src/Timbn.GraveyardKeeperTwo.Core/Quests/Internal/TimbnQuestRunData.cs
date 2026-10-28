namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>What Core saves of a quest run.</summary>
internal sealed class TimbnQuestRunData
{
    public int Day { get; set; }

    public Dictionary<string, string> Npcs { get; set; } = [];

    public Dictionary<string, string> Items { get; set; } = [];

    public Dictionary<string, int> Numbers { get; set; } = [];

    public Dictionary<string, string> Texts { get; set; } = [];
}
