namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The game's save slot background with its padding around a row or a column, the card every setting sits on in the Mods
/// window. <see cref="TimbnPageShorthands.Card"/> puts one holding a column on a page in a single call.
/// </summary>
/// <example>
/// <code>
/// page.Add(new TimbnCard(new TimbnRow(row => row.Add(new TimbnText("Side by side")))));
/// </code>
/// </example>
/// <param name="content">What the card holds, usually a <see cref="TimbnRow"/> or a <see cref="TimbnColumn"/>.</param>
public sealed class TimbnCard(TimbnComponent content) : TimbnBox
{
    /// <summary>What the card holds.</summary>
    public TimbnComponent Content { get; } = content;

    protected internal override RectTransform Build(Transform parent)
    {
        var box = TimbnUi.Card(parent, "Card");
        Place(box);
        BuilderFor(box).Add(Content);
        return box;
    }
}
