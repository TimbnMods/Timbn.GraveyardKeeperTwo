namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class FuneralData
{
    public bool Seeded { get; set; }

    public int CheckedDay { get; set; } = -1;

    public HashSet<string> KnownGraves { get; set; } = [];

    // Graves whose funeral began once but failed to reach the ceremony, capping subsequent attempts to two.
    public HashSet<string> TriedGraves { get; set; } = [];
}
