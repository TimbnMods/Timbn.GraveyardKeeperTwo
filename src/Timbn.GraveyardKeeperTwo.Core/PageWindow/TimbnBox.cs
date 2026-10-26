namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The base of a container that can be given a size or a share of the room around it, such as a <see cref="TimbnRow"/>,
/// a <see cref="TimbnColumn"/> or a <see cref="TimbnCard"/>. A mod makes a container of its own the same way Core does.
/// Its <see cref="TimbnComponent.Build(Transform)"/> makes the box, hands it to <see cref="Place(RectTransform)"/> and
/// fills it through <see cref="TimbnComponent.BuilderFor(RectTransform)"/>.
/// </summary>
/// <example>
/// <code>
/// public sealed class Framed(TimbnComponent content) : TimbnBox
/// {
///     protected override RectTransform Build(Transform parent)
///     {
///         var box = new GameObject("Framed", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent&lt;RectTransform&gt;();
///         box.SetParent(parent, false);
///         Place(box);
///         BuilderFor(box).Add(content);
///         return box;
///     }
/// }
///
/// page.Add(new Framed(new TimbnColumn(column => column.Add(new TimbnText("Inside")))) { Fill = 1f });
/// </code>
/// </example>
public abstract class TimbnBox : TimbnComponent
{
    /// <summary>Makes the container. It builds nothing until it is put on a page.</summary>
    protected TimbnBox()
    {
    }

    /// <summary>
    /// A share of the room the box's parent has left along its direction, by weight, or null for none. Two boxes weighted
    /// 2 and 3 split the room 2 to 3. Set it before the box is put on a page.
    /// </summary>
    public float? Fill { get; set; }

    /// <summary>A fixed width, or null to size from what is in it. Set it before the box is put on a page.</summary>
    public float? Width { get; set; }

    /// <summary>
    /// A fixed height, or null to size from what is in it. A column taller than what it holds puts it in the middle. Set it
    /// before the box is put on a page.
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// Gives the box <see cref="Fill"/>, <see cref="Width"/> and <see cref="Height"/>. In a row a box with no fill keeps to
    /// its own width. Call it from <see cref="TimbnComponent.Build(Transform)"/> before filling the box.
    /// </summary>
    /// <param name="box">The box the container made.</param>
    protected void Place(RectTransform box)
    {
        if (Width != null || Height != null)
            TimbnUi.Fixed(box, Width ?? -1f, Height ?? -1f);

        if (Height != null && box.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() is { } layout)
            layout.childAlignment = TextAnchor.MiddleLeft;

        Share(box, Fill, InColumn);
    }

    // A share of the room the parent has left along its direction, or none in a row, for any component that takes a fill.
    internal static void Share(RectTransform box, float? fill, bool inColumn)
    {
        if (fill is { } weight)
        {
            if (inColumn)
                TimbnUi.FillHeight(box, weight);
            else
                TimbnUi.Fill(box, weight);
        }
        else if (!inColumn)
        {
            // A column stretches its own parts across, which Unity also reports as the column wanting a share of the row,
            // so a box in a row would take spare room it never asked for.
            TimbnUi.Element(box).flexibleWidth = 0f;
        }
    }
}
