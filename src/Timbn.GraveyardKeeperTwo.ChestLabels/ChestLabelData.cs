namespace Timbn.GraveyardKeeperTwo.ChestLabels;

/// <summary>The labels of a save, by the unique id of the chest.</summary>
public sealed class ChestLabelData
{
    public Dictionary<string, ChestLabelEntry> Labels { get; set; } = [];
}

public sealed class ChestLabelEntry
{
    public string Text { get; set; } = "";
}
