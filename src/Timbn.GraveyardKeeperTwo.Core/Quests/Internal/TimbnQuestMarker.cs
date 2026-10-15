namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Shows the game's blue point of interest marker for every NPC a marked quest is waiting on. The HUD draws it at the
/// screen edge or on the door that leads to objects with a work event, and HUDPatch counts these NPCs in. Once they
/// are on screen the HUD hands over to the NPC's own talk bubble.
/// </summary>
internal static class TimbnQuestMarker
{
    private static readonly Action<HUD, WgoData>? _refresh = TimbnReflection.TryDelegate<Action<HUD, WgoData>>(
        () =>
            AccessTools.Method(typeof(HUD),
            "HandleInteractionEventChangedAndRedraw"),
            "HUD.HandleInteractionEventChangedAndRedraw");

    private static readonly AccessTools.FieldRef<UIMagnifyingGlassWidget, Sprite>? _icon = TimbnReflection.TryField<UIMagnifyingGlassWidget, Sprite>("originalIconSprite");
    private static readonly AccessTools.FieldRef<UIMagnifyingGlassWidget, Vector2>? _iconSize = TimbnReflection.TryField<UIMagnifyingGlassWidget, Vector2>("originalIconSize");

    private static readonly HashSet<SGuid> _marked = [];
    private static readonly HashSet<string> _npcsNow = [];
    private static readonly HashSet<SGuid> _markedNow = [];
    private static readonly List<SGuid> _changed = [];
    private static HUD? _hud;

    internal static bool IsMarked(WgoData? data) => data != null && IsMarked(data.UniqueId);

    internal static bool IsMarked(SGuid? id) => _marked.Count > 0 && id is not null && _marked.Contains(id);

    internal static void Tick()
    {
        if (!TimbnGame.IsInGame)
            return;

        _npcsNow.Clear();
        TimbnQuests.CollectMarkerNpcs(_npcsNow);
        _markedNow.Clear();
        foreach (var npcId in _npcsNow)
        {
            if (TimbnNpcs.FindData(npcId) is { } data)
                _markedNow.Add(data.UniqueId);
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

    // On a door the game swaps in its magnifying glass, which reads as a work spot rather than a quest.
    internal static void FixDoorIcon(UIMagnifyingGlassWidget widget, UIMagnifyingGlassWidgetData data)
    {
        if (data.IsOutOfScreen || !IsMarked(data.UniqueId) || widget.iconImage == null)
            return;

        if (_icon?.Invoke(widget) is not { } icon || icon == null)
            return;

        widget.iconImage.sprite = icon;
        if (_iconSize is not null)
            widget.iconImage.rectTransform.sizeDelta = _iconSize(widget);
    }

    internal static void Clear()
    {
        _marked.Clear();
        _hud = null;
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
