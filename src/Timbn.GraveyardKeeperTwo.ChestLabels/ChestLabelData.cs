namespace Timbn.GraveyardKeeperTwo.ChestLabels;

/// <summary>The labels of a save, by the unique id of the chest.</summary>
public sealed class ChestLabelData
{
    public Dictionary<string, ChestLabelEntry> Labels { get; set; } = [];
}

public sealed class ChestLabelEntry
{
    public string Text { get; set; } = "";

    public string? Color { get; set; }

    public int SizeModifier { get; set; }
}

public enum ChestLabelSize
{
    Small = -2,
    DefaultSize = 0,
    Large = 2,
    ExtraLarge = 4
}