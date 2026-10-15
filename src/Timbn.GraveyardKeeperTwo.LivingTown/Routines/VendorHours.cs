namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal static class VendorHours
{
    public const float Working = 0.25f;
    public const float Evening = 0.70f;
    public const float Night = 0.85f;

    public static bool IsClosed() => TimbnClock.TimeOfDay is < Working or >= Evening;
}
