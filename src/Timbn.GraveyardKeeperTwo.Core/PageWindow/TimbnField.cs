namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A row with a title and an optional line of description on the left and other components on the right, such as a
/// button or an icon button. The title and description are one object with the game's own spacing, the parts sit level
/// with the middle of the title and description, and the row is never shorter than a setting row.
/// </summary>
/// <example>
/// <code>
/// page.Add(new TimbnField("Rename", "Gives the chest a new name.", new TimbnButton("Open", Open)));
/// </code>
/// </example>
public sealed class TimbnField : TimbnComponent
{
    private const float _gap = 6f;
    private const float _minControlWidth = 120f;

    private readonly string _title;
    private readonly string _subtitle;
    private readonly TimbnComponent[] _parts;

    /// <summary>
    /// The share of the row the parts take after the title, from 0 to 1, or null for Core's default of 0.20. A long control
    /// such as a text box can ask for more. It is read when the field is put on a page.
    /// </summary>
    public float? ControlShare { get; set; }

    /// <summary>
    /// Short marks shown under the title in the description's small gray text, joined in parentheses, such as
    /// "(Read Only, Advanced)". Empty for none. It is read when the field is put on a page.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Makes a field.</summary>
    /// <param name="title">The name on the left, already in the player's language, or empty for none.</param>
    /// <param name="subtitle">The description under the name, or empty for none.</param>
    /// <param name="parts">What goes on the right, left to right.</param>
    public TimbnField(string title, string subtitle, params TimbnComponent[] parts)
    {
        _title = title;
        _subtitle = subtitle;
        _parts = parts;
    }

    protected internal override RectTransform Build(Transform parent)
    {
        var row = TimbnUi.Row(parent, "Field", _gap);

        var share = ControlShare ?? TimbnUi.ControlShare;
        if (_title.Length > 0)
            TimbnUi.Fill(TimbnUi.Entry(row, _title, _subtitle, marks: Tags.Count > 0 ? $"({string.Join(", ", Tags)})" : ""), 1f - share);

        var parts = TimbnUi.Row(row, "Parts", _gap, align: TextAnchor.MiddleRight);
        TimbnUi.Fill(parts, _title.Length > 0 ? share : 1f);

        // A control keeps room to show its value, and the title gives way first in a narrow row.
        if (_parts.Any(part => part is TimbnControl))
            TimbnUi.Element(parts).minWidth = _minControlWidth;

        foreach (var part in _parts)
        {
            if (part is TimbnControl control)
            {
                control.InField = true;
                if (control.Title.Length == 0)
                    control.Title = _title;
            }

            part.BuildInto(parts, Watcher);
        }

        // A part wider than its share, such as a button with a long label, keeps its room instead of spilling past the row.
        parts.gameObject.AddComponent<TimbnKeepsRoom>();

        return row;
    }
}
