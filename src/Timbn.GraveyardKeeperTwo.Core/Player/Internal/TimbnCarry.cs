using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnCarry
{
    private const string _iconName = "TimbnCarriedIcon";
    private const float _iconHeight = 0.3f;

    private static readonly AccessTools.FieldRef<PlayerData, List<Item>>? _overheadItems =
        TimbnReflection.TryField<PlayerData, List<Item>>("overheadItems");

    private static readonly AccessTools.FieldRef<DropViewAtomMesh, List<DropViewAtomMeshElement>>? _elements =
        TimbnReflection.TryField<DropViewAtomMesh, List<DropViewAtomMeshElement>>("elements");

    private static readonly AccessTools.FieldRef<DropViewAtomMesh, DropViewAtomMeshElement>? _defaultElement =
        TimbnReflection.TryField<DropViewAtomMesh, DropViewAtomMeshElement>("defaultElement");

    private static readonly AccessTools.FieldRef<DropViewAtomSprite, SpriteRenderer>? _itemSprite =
        TimbnReflection.TryField<DropViewAtomSprite, SpriteRenderer>("itemSprite");

    private static readonly List<Carried> _carried = [];

    internal static Item? Current => TimbnGame.IsInGame ? MainGame.PlayerData.overheadItem : null;

    internal static bool IsReadingInput { get; set; }

    internal static bool BlocksDrop(Item item) =>
        _carried.Exists(carried => carried.Item == item && carried.Rule == TimbnCarryRule.OnlyByCode);

    /// <summary>Whether the player holds something exclusive, so nothing else may be picked up or handed to them.</summary>
    internal static bool HandsLocked =>
        TimbnGame.IsInGame && _carried.Exists(carried => carried.Rule != TimbnCarryRule.Vanilla && IsCarried(carried.Item));

    internal static Carried Carry(Item item, Action? putDown, TimbnCarryRule rule, ManualLogSource logger)
    {
        var carried = new Carried(item, putDown, rule, logger);
        if (!TimbnGame.IsInGame || item == null || item.IsEmpty)
        {
            carried.End();
            return carried;
        }

        if (IsCarried(item) || _carried.Exists(other => other.Item == item))
        {
            logger.LogWarning($"{nameof(TimbnPluginPlayer)}.{nameof(TimbnPluginPlayer.Carry)}|The player already carries this {item.id}, so it is not handed to them again.");
            carried.End();
            return carried;
        }

        if (HandsLocked)
        {
            logger.LogWarning($"{nameof(TimbnPluginPlayer)}.{nameof(TimbnPluginPlayer.Carry)}|The player holds something exclusive, so {item.id} is not handed to them.");
            carried.End();
            return carried;
        }

        if (rule != TimbnCarryRule.Vanilla)
        {
            foreach (var held in MainGame.PlayerData.OverheadItems.ToList())
                MainGame.PlayerData.DropOverheadItem(held);
        }

        _carried.Add(carried);
        MainGame.PlayerData.AddOverheadItem(item);
        return carried;
    }

    internal static IDisposable RemoveForSave()
    {
        if (!TimbnGame.IsInGame || _overheadItems is null || _carried.Count == 0)
            return TimbnUndo.None;

        var items = _overheadItems(MainGame.PlayerData);
        if (items is null)
            return TimbnUndo.None;

        List<(int Index, Item Item)> removed = [];
        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (!_carried.Exists(carried => carried.Item == items[i]))
                continue;

            removed.Add((i, items[i]));
            items.RemoveAt(i);
        }

        return new TimbnUndo(() =>
        {
            for (var i = removed.Count - 1; i >= 0; i--)
                items.Insert(Math.Min(removed[i].Index, items.Count), removed[i].Item);
        });
    }

    private static bool IsCarried(Item item) =>
        TimbnGame.IsInGame && MainGame.PlayerData.OverheadItems.Contains(item);

    internal static void GiveLook(DropViewAtomMesh view, string iconId)
    {
        if (!view.isOverhead || string.IsNullOrEmpty(iconId) || _elements is null || _defaultElement is null)
            return;

        var elements = _elements(view);
        if (elements.Exists(element => element != null && element.name == iconId))
            return;

        var sprites = LazySingletonSO<EasySpritesCollection>.Instance;
        if (sprites == null || !sprites.HasSprite(iconId))
            return;

        var placeholder = _defaultElement(view);
        var element = UnityEngine.Object.Instantiate(placeholder, placeholder.transform.parent);
        element.name = iconId;
        element.gameObject.SetActive(false);
        foreach (Transform child in element.transform)
        {
            if (child.GetComponentInChildren<Object3D>(true) != null)
                child.gameObject.SetActive(false);
        }

        var icon = new GameObject(_iconName);
        icon.transform.SetParent(element.transform, false);
        icon.transform.localPosition = Vector3.up * _iconHeight;
        var renderer = icon.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites.GetSprite(iconId);
        if (SmallDropSprite() is { } source)
        {
            renderer.sharedMaterial = source.sharedMaterial;
            icon.transform.localScale = source.transform.localScale;
        }

        elements.Add(element);
    }

    private static SpriteRenderer? SmallDropSprite()
    {
        if (_itemSprite is null)
            return null;

        foreach (var view in Resources.FindObjectsOfTypeAll<DropViewAtomSprite>())
        {
            if (_itemSprite(view) is { } sprite && sprite.sharedMaterial != null)
                return sprite;
        }

        return null;
    }

    internal sealed class Carried : IDisposable
    {
        private readonly Action? _putDown;
        private readonly ManualLogSource _logger;
        private bool _ended;

        internal Carried(Item item, Action? putDown, TimbnCarryRule rule, ManualLogSource logger)
        {
            Item = item;
            _putDown = putDown;
            Rule = rule;
            _logger = logger;
        }

        internal Item Item { get; }

        internal TimbnCarryRule Rule { get; }

        internal bool IsDone => _ended;

        internal Action? Ended { get; set; }

        internal void Check()
        {
            if (_ended || IsCarried(Item))
                return;

            End();
            if (_putDown != null)
                TimbnSafe.Run(_putDown, _logger, $"{nameof(TimbnPluginPlayer)}.{nameof(TimbnPluginPlayer.Carry)}|{TimbnSafe.Describe(_putDown)}");
        }

        public void Dispose()
        {
            if (_ended)
                return;

            End();
            if (IsCarried(Item))
                MainGame.PlayerData.RemoveOverheadItem(Item);
        }

        internal void End()
        {
            if (_ended)
                return;

            _ended = true;
            _carried.Remove(this);
            Ended?.Invoke();
        }
    }
}
