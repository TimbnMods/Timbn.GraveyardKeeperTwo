using LazyBearTechnology;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Shows an icon over a spot in the world, with no key or text. The game marks a workstation that needs something
/// the same way. Use it to show what someone wants, like a beer over a customer's head. By default the icon sits in
/// the game's small square frame. Set Framed to false for the bare icon, Scale to size it and Tint to color it. Get
/// one from UI.CreateBubble and move it with Show as often as you like. It is hidden when the mod unloads or the
/// player returns to the main menu. Dispose a bubble when the mod is done with it, for example one per customer.
/// </summary>
/// <example>
/// <code>
/// _wants = UI.CreateBubble();
/// _wants.Framed = false;
/// _wants.Scale = 0.6f;
/// // as their patience runs out
/// _wants.Tint = Color.Lerp(Color.white, Color.red, waited / patience);
/// // every frame the customer waits
/// _wants.Show(customer.Position + Vector3.up * 1.6f, "i_cup_beer");
/// // once they are served
/// _wants.Hide();
/// </code>
/// </example>
public sealed class TimbnBubble : IBubbleDrawable, IDisposable
{
    private static readonly ConditionalWeakTable<UIWorkbenchAdditionWorldIconWidgetData, TimbnBubble> _sized = [];

    private static readonly ConstructorInfo? _withBackground = AccessTools.Constructor(
        typeof(UIWorkbenchAdditionWorldIconWidgetData),
        [typeof(string), typeof(bool), typeof(bool), typeof(string)]);

    private static readonly ConditionalWeakTable<UIWorkbenchAdditionWorldIconWidget, StrongBox<Color>> _gameColors = [];

    private static readonly AccessTools.FieldRef<UIWorkbenchAdditionWorldIconWidget, UIWorkbenchAdditionWorldIconWidgetData>? _widgetData =
        TimbnReflection.TryField<UIWorkbenchAdditionWorldIconWidget, UIWorkbenchAdditionWorldIconWidgetData>("data");

    private static readonly AccessTools.FieldRef<UIWorkbenchAdditionWorldIconWidget, UnityEngine.UI.Image>? _icon =
        TimbnReflection.TryField<UIWorkbenchAdditionWorldIconWidget, UnityEngine.UI.Image>("icon");

    private readonly SGuid _id = new(Guid.NewGuid());
    private Vector3 _position;
    private string _iconId = "";
    private bool _crossedOut;
    private bool _shown;
    private bool _framed = true;
    private float _scale = 1f;
    private Color _tint = Color.white;
    private UIWorkbenchAdditionWorldIconWidget? _widget;

    internal TimbnBubble()
    {
    }

    internal IDisposable? Owned { get; set; }

    /// <summary>Whether the bubble is on screen.</summary>
    public bool IsVisible => _shown;

    /// <summary>Whether the icon sits in the game's small square frame. True by default.</summary>
    public bool Framed
    {
        get => _framed;
        set
        {
            _framed = value;
            Redisplay();
        }
    }

    /// <summary>How big the icon is drawn, where 1 is the game's own size. 1 by default.</summary>
    public float Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            Redisplay();
        }
    }

    /// <summary>
    /// The color the icon is multiplied by, white for its own colors. Its alpha fades the icon, so 0 hides it while the
    /// bubble keeps its place. Changing it every frame is cheap, so it suits a fade or a flash.
    /// </summary>
    public Color Tint
    {
        get => _tint;
        set
        {
            _tint = value;
            if (_widget != null && _widgetData?.Invoke(_widget) is { } data && _sized.TryGetValue(data, out var owner) && owner == this)
                Paint(_widget, value);
        }
    }

    SGuid IBubbleDrawable.BubbleDrawableUniqueId => _id;

    List<LazyWidgetDataBase> IBubbleDrawable.BubbleDrawableWidgets => _shown ? [WidgetData()] : [];

    Vector3 IBubbleDrawable.BubbleDrawablePosition => _position;

    /// <summary>
    /// Shows the bubble, or moves it and changes its icon if it is already showing. Calling it every frame with the
    /// same values costs nothing.
    /// </summary>
    /// <param name="position">The spot in the world to draw it over.</param>
    /// <param name="iconId">The icon, such as an item's i_cup_beer or one added with Sprites.AddPng.</param>
    /// <param name="crossedOut">Draws a red line through the icon, such as for a wish that went unmet.</param>
    public void Show(Vector3 position, string iconId, bool crossedOut = false)
    {
        var manager = UIObjectBubbleManager.Instance;
        if (manager == null)
            return;

        if (_shown && iconId == _iconId && crossedOut == _crossedOut && position == _position && manager.TryGetDisplayedBubble(_id, out _))
            return;

        _position = position;
        _iconId = iconId;
        _crossedOut = crossedOut;
        _shown = true;
        manager.Display(this);
    }

    /// <summary>Takes the bubble off the screen. It does nothing when the bubble is not showing.</summary>
    public void Hide()
    {
        if (!_shown)
            return;

        _shown = false;
        var manager = UIObjectBubbleManager.Instance;
        if (manager != null)
            manager.Hide(this);
    }

    /// <summary>Hides the bubble and lets the mod forget it. It can still be shown again afterwards, but nothing hides it for the mod any more.</summary>
    public void Dispose()
    {
        Hide();
        var owned = Owned;
        Owned = null;
        owned?.Dispose();
    }

    internal static void Style(UIWorkbenchAdditionWorldIconWidget widget)
    {
        if (_widgetData is null || _widgetData(widget) is not { } data)
            return;

        if (!_sized.TryGetValue(data, out var bubble))
        {
            widget.transform.localScale = Vector3.one;
            if (_gameColors.TryGetValue(widget, out var gameColor))
                Paint(widget, gameColor.Value);

            return;
        }

        bubble._widget = widget;
        widget.transform.localScale = Vector3.one * bubble._scale;
        Paint(widget, bubble._tint);
    }

    private static void Paint(UIWorkbenchAdditionWorldIconWidget widget, Color color)
    {
        if (_icon?.Invoke(widget) is not { } icon)
            return;

        if (!_gameColors.TryGetValue(widget, out _))
            _gameColors.Add(widget, new StrongBox<Color>(icon.color));

        icon.color = color;
    }

    private UIWorkbenchAdditionWorldIconWidgetData WidgetData()
    {
        var data = !_framed && _withBackground != null
            ? (UIWorkbenchAdditionWorldIconWidgetData)_withBackground.Invoke([_iconId, !_crossedOut, false, null])
            : new UIWorkbenchAdditionWorldIconWidgetData(_iconId, !_crossedOut);
        _sized.Add(data, this);
        return data;
    }

    private void Redisplay()
    {
        if (_shown && UIObjectBubbleManager.Instance is { } manager)
            manager.Display(this);
    }
}
