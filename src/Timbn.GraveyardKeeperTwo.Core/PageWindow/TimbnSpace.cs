namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// An empty box of a fixed size, for keeping room in a <see cref="TimbnField"/> so its parts line up with another field's.
/// It also makes a gap in a row or a column that is different from the gap the container already puts between its
/// parts.
/// </summary>
/// <param name="width">The width.</param>
/// <param name="height">The height, or 0 for none.</param>
public sealed class TimbnSpace(float width = 0f, float height = 0f) : TimbnComponent
{
    protected internal override RectTransform Build(Transform parent) => TimbnUi.Space(parent, width, height);
}
