namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class FuneralData
{
    public bool Seeded { get; set; }

    public int CheckedDay { get; set; } = -1;

    public HashSet<string> KnownGraves { get; set; } = [];

    // Graves whose funeral was started once and never got as far as the ceremony, so the second try is the last.
    public HashSet<string> TriedGraves { get; set; } = [];
}
