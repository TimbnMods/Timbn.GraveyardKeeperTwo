using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A column whose content is built again when asked, for a part of the page that shows something chosen elsewhere, such
/// as the settings of the line picked in a <see cref="TimbnList"/>. The lambda builds the content, and
/// <see cref="Rebuild"/> runs it again on the same box after clearing everything it built.
/// <see cref="TimbnPageShorthands.Region"/> puts one on a page in a single call.
/// </summary>
/// <param name="build">Builds the region's content.</param>
public sealed class TimbnRegion(Action<TimbnPageBuilder> build) : TimbnBox
{
    private RectTransform? _box;
    private ManualLogSource? _logger;
    private TimbnPageBuilder? _builder;
    private bool _building;

    /// <summary>The space between what is in it, or null for the page's own.</summary>
    public float? Gap { get; set; }

    /// <summary>
    /// Removes what the region shows and builds it again. It does nothing before the region is on a page, or when asked
    /// while the region is building, such as from a watch inside it, which runs once as soon as it is made.
    /// </summary>
    public void Rebuild()
    {
        if (_box == null || _logger == null || _building)
            return;

        _building = true;
        try
        {
            BuildContent(_box, _logger);
        }
        finally
        {
            _building = false;
        }

        _box.GetComponentInParent<TimbnPageWindow>()?.ContentChanged();
    }

    private void BuildContent(RectTransform box, ManualLogSource logger)
    {
        TimbnKeyCapture.CancelAny();
        TimbnUi.Clear(box);
        var watcher = box.GetComponent<TimbnPageWatcher>();
        watcher?.Reset();
        var builder = _builder = TimbnPageBuilder.Create(box, logger);
        TimbnSafe.Run(() => build(builder), logger, $"{nameof(TimbnRegion)}|Building the region");
        UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(box);
        box.GetComponentInParent<GamepadNavigationController>()?.ReinitItems(focusOnFirstActive: false);
    }

    /// <summary>
    /// With a gamepad in use, puts the focus on the first control in the region, such as after the player picked something in
    /// a list that the region shows. With a mouse in use it is where the gamepad's focus starts once the player picks one up.
    /// </summary>
    public void FocusFirst()
    {
        if (_box != null)
            TimbnPageWindow.FocusOrResume(_box.GetComponentsInChildren<GamepadNavigationItem>(false).FirstOrDefault(item => item.isActiveAndEnabled && item.Active));
    }

    /// <summary>Reads every control in the region again from its get.</summary>
    public void Refresh() => _builder?.Refresh();

    protected internal override RectTransform Build(Transform parent)
    {
        _box = TimbnUi.Column(parent, "Region", TimbnPageBuilder.ColumnGap(Gap));
        Place(_box);
        _logger = Watcher!.Logger;
        Rebuild();
        Watcher.Widgets.Add(new TimbnRegionWidget(_box, this));
        return _box;
    }
}
