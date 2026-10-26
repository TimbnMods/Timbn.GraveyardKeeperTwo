namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Works out how big a page popup is from the width and height its page asks for.</summary>
internal static class TimbnPageSize
{
    internal const float MinShare = 10f;
    internal const float MaxShare = 100f;
    internal const float DefaultWidthShare = 50f;
    internal const float MinWidth = 120f;
    internal const float MinHeight = 40f;

    /// <summary>
    /// The size of the area inside the window's frame. A share is a percentage of the screen taken by the whole window,
    /// so the frame's padding comes off what the area gets, and pixels are the area's own size. A width left on auto
    /// takes the default share. A height left on auto comes back as zero, so the area is as tall as its content.
    /// Neither falls below a small minimum.
    /// </summary>
    internal static Vector2 Resolve(TimbnLength width, TimbnLength height, Vector2 screen, Vector2 padding) =>
        new(
            Mathf.Max(MinWidth, Side(width, screen.x, padding.x, DefaultWidthShare)),
            height.IsAuto ? 0f : Mathf.Max(MinHeight, Side(height, screen.y, padding.y, MaxShare)));

    private static float Side(TimbnLength length, float screen, float padding, float defaultShare) => length.Kind switch
    {
        TimbnLengthKind.Pixels => length.Value,
        TimbnLengthKind.Percent => screen * Mathf.Clamp(length.Value, MinShare, MaxShare) / 100f - padding,
        _ => screen * defaultShare / 100f - padding,
    };
}
