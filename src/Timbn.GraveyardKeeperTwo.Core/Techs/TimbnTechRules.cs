namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Pure rules for where a tech may sit on the tree, kept free of game types so they can be tested.</summary>
internal static class TimbnTechRules
{
    // The tree draws a tech 160 by 80 units and puts a column 210 units and a row 80 units from the next.
    internal const float NodeWidth = 160f;
    internal const float NodeHeight = 80f;
    internal const float ColumnSpacing = 210f;
    internal const float RowSpacing = 80f;

    /// <summary>Whether two techs at these spots would be drawn over each other.</summary>
    internal static bool Overlaps(float columnA, float rowA, float columnB, float rowB) =>
        Math.Abs(columnA - columnB) * ColumnSpacing < NodeWidth && Math.Abs(rowA - rowB) * RowSpacing < NodeHeight;
}
