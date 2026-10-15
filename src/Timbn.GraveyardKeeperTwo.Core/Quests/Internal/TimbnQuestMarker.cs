namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Shows the game's blue point of interest marker for every NPC a marked quest is waiting on. The HUD draws it at the
/// screen edge or on the door that leads to objects with a work event, and HUDPatch counts these NPCs in. Once they
/// are on screen the HUD hands over to the NPC's own talk bubble. The marker takes the color of the quest owner's
/// talk bubbles.
/// </summary>
internal static class TimbnQuestMarker
{
    // Villagers in bed or the bathroom are teleported to the game's hidden home grid around x -1000 without being
    // flagged hidden, and a marker there points off the west edge of the map (which ends near x -101).
    private const float _hiddenGridX = -500f;

    private static readonly Action<HUD, WgoData>? _refresh = TimbnReflection.TryDelegate<Action<HUD, WgoData>>(
        () =>
            AccessTools.Method(typeof(HUD),
            "HandleInteractionEventChangedAndRedraw"),
            "HUD.HandleInteractionEventChangedAndRedraw");

    private static readonly AccessTools.FieldRef<UIMagnifyingGlassWidget, Sprite>? _icon = TimbnReflection.TryField<UIMagnifyingGlassWidget, Sprite>("originalIconSprite");
    private static readonly AccessTools.FieldRef<UIMagnifyingGlassWidget, Vector2>? _iconSize = TimbnReflection.TryField<UIMagnifyingGlassWidget, Vector2>("originalIconSize");

    private static readonly HashSet<SGuid> _marked = [];
    private static readonly Dictionary<SGuid, Color> _colors = [];
    private static readonly Dictionary<string, Color> _npcsNow = [];
    private static readonly HashSet<SGuid> _markedNow = [];
    private static readonly List<SGuid> _changed = [];
    private static readonly Dictionary<Sprite, Sprite> _sourceOf = [];
    private static readonly HashSet<string> _failed = [];
    private static HUD? _hud;

    // The HUD asks every frame, so an NPC teleported onto the hidden grid drops its marker at once instead of the marker
    // flying off toward x -1000 until the next tick.
    internal static bool IsMarked(WgoData? data) => data != null && IsMarked(data.UniqueId) && IsReachable(data);

    internal static bool IsMarked(SGuid? id) => _marked.Count > 0 && id is not null && _marked.Contains(id);

    internal static void Tick()
    {
        if (!TimbnGame.IsInGame)
            return;

        _npcsNow.Clear();
        TimbnQuests.CollectMarkerNpcs(_npcsNow);
        _markedNow.Clear();
        _colors.Clear();
        foreach (var npc in _npcsNow)
        {
            if (TimbnNpcs.FindData(npc.Key) is { } data && IsReachable(data))
            {
                _markedNow.Add(data.UniqueId);
                _colors[data.UniqueId] = npc.Value;
            }
        }

        if (_markedNow.SetEquals(_marked))
            return;

        _changed.Clear();
        _changed.AddRange(_marked.Where(id => !_markedNow.Contains(id)));
        _changed.AddRange(_markedNow.Where(id => !_marked.Contains(id)));
        _marked.Clear();
        _marked.UnionWith(_markedNow);
        foreach (var id in _changed)
        {
            if (MainGame.WorldData.GetWgoData(id) is { } data)
                Refresh(data);
        }
    }

    // Draws the marker in the quest's dialog color. On a door the game swaps in its magnifying glass, which reads as a
    // work spot rather than a quest, so the marker icon goes back there too.
    internal static void Recolor(UIMagnifyingGlassWidget widget, UIMagnifyingGlassWidgetData data)
    {
        if (!_colors.TryGetValue(data.UniqueId, out var color))
            return;

        if (widget.iconImage != null && _icon?.Invoke(widget) is { } icon && icon != null)
        {
            widget.iconImage.sprite = Recolored(icon, color);
            if (_iconSize is not null)
                widget.iconImage.rectTransform.sizeDelta = _iconSize(widget);
        }

        if (data.IsOutOfScreen && widget.pointerImage != null && widget.pointerImage.sprite is { } pointer && pointer != null)
            widget.pointerImage.sprite = Recolored(pointer, color);
    }

    private static bool IsReachable(WgoData data) => !data.IsHidden && data.Position.x > _hiddenGridX;

    internal static void Clear()
    {
        _marked.Clear();
        _colors.Clear();
        _hud = null;
    }

    // The pooled widget can still hold a sprite recolored for another quest, so always start from the game's own.
    private static Sprite Recolored(Sprite sprite, Color color)
    {
        var source = _sourceOf.TryGetValue(sprite, out var original) ? original : sprite;
        var name = $"timbn_marker_{source.name}_{ColorUtility.ToHtmlStringRGB(color)}";
        if (!TimbnSprites.TryGet(name, out var recolored))
        {
            if (_failed.Contains(name))
                return source;

            TimbnSafe.Run(() => TimbnSprites.AddRecolored(name, source, color), $"{nameof(TimbnQuestMarker)}|Recoloring {source.name}");
            if (!TimbnSprites.TryGet(name, out recolored))
            {
                _failed.Add(name);
                return source;
            }
        }

        _sourceOf[recolored] = source;
        return recolored;
    }

    // The HUD only rescans an object when the game says its events changed, so Core says so too.
    private static void Refresh(WgoData data)
    {
        if (_hud == null)
            _hud = UnityEngine.Object.FindAnyObjectByType<HUD>();

        if (_hud != null)
            _refresh?.Invoke(_hud, data);
    }
}
