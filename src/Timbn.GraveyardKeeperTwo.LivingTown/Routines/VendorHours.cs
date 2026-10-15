namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal static class VendorHours
{
    public const float Working = 6 / 24f;

    // The game's town stalls close at the eveningTime of their t_b_character script (19:12), so Herm closes with them.
    public const float Evening = 0.8f;
    public const float Night = 22 / 24f;

    public static bool IsClosed() => TimbnClock.TimeOfDay is < Working or >= Evening;
}
