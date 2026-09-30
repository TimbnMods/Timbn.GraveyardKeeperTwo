namespace Timbn.GraveyardKeeperTwo.Core;

/// <summary>Core's own data that belongs to no save, such as which game build the version warning was shown for.</summary>
internal sealed class TimbnCoreData
{
    public string WarnedGameVersion { get; set; } = "";
}
