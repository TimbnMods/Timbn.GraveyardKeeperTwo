namespace Timbn.GraveyardKeeperTwo.ChestLabels;

/// <summary>The "moved" mod event, sent when a mod replaces a building with a new one.</summary>
internal sealed class MovedBuilding
{
    public string? OldId { get; set; }

    public string? NewId { get; set; }
}
