namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>What a craft's <see cref="TimbnCraftBuilder.OnStarted"/> and <see cref="TimbnCraftBuilder.OnFinished"/> handlers are told.</summary>
public sealed class TimbnCraftEvent
{
    internal TimbnCraftEvent(WgoData station, string craftId)
    {
        Station = station;
        CraftId = craftId;
    }

    /// <summary>The station the craft runs at, for reading its worker, its position or anything else about it.</summary>
    public WgoData Station { get; }

    /// <summary>The craft's id, as it was given to Crafts.Add.</summary>
    public string CraftId { get; }
}
