namespace Timbn.GraveyardKeeperTwo.Core;

/// <summary>Core's own data for a save, such as the one shot talks the player has already started.</summary>
internal sealed class TimbnCoreSaveData
{
    public HashSet<string> SeenTalks { get; set; } = [];
}
