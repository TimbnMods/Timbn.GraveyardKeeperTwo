using LazyBearTechnology;
using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.ChestLabels;

/// <summary>
/// Keeps the saved name of each chest and adds it to the chest's own bubble, so the game stacks it with the Open and
/// Label hints the way it stacks a grave's quality label.
/// </summary>
internal sealed class ChestLabelBoard
{
    private const float _moveGraceSeconds = 1f;
    private const int _maxLength = 200;

    private readonly TimbnSaveData<ChestLabelData> _data;
    private readonly Dictionary<Guid, string> _texts = [];
    private readonly Dictionary<string, (ChestLabelEntry Entry, float Until)> _removed = [];

    public ChestLabelBoard(TimbnSaveData<ChestLabelData> data)
    {
        _data = data;
        Rebuild();
    }

    // Only objects with chest in the id, since the chest interaction is also used by shelves, containers and storages.
    public static bool CanLabel(Wgo wgo) =>
        wgo.InteractionHandler is ChestInteractionHandler && wgo.Id?.Contains("chest", StringComparison.OrdinalIgnoreCase) == true;

    public string GetText(WgoData chest) =>
        _data.Current.Labels.TryGetValue(Key(chest.UniqueId), out var entry) ? entry.Text : "";

    public string? GetColor(WgoData chest) =>
        _data.Current.Labels.TryGetValue(Key(chest.UniqueId), out var entry) ? entry.Color : null;

    public int GetSize(WgoData chest) =>
        _data.Current.Labels.TryGetValue(Key(chest.UniqueId), out var entry) ? entry.SizeModifier : 0;

    public void SetLabel(Wgo chest, string text, string? color, int sizeModifier)
    {
        var key = Key(chest.Data.UniqueId);
        text = text.Trim();
        if (text.Length > _maxLength)
            text = text[.._maxLength];

        if (text.Length == 0)
            _data.Current.Labels.Remove(key);
        else
            _data.Current.Labels[key] = new ChestLabelEntry { Text = text, Color = color, SizeModifier = sizeModifier };

        Rebuild();
        chest.DrawWidgets();
    }

    // A mod that moves a chest removes the old one and places a new one in the same call, then publishes the move, so the label waits a moment for that before it is dropped.
    public void ChestRemoved(SGuid id)
    {
        var key = Key(id);
        if (!_data.Current.Labels.Remove(key, out var entry))
            return;

        ForgetExpired();
        _removed[key] = (entry, Time.unscaledTime + _moveGraceSeconds);
        Rebuild();
    }

    public void Moved(string? oldId, string? newId)
    {
        if (!Guid.TryParse(oldId, out var oldGuid) || !Guid.TryParse(newId, out var guid) || !_removed.Remove(Key(new SGuid(oldGuid)), out var parked) || parked.Until < Time.unscaledTime)
            return;

        _data.Current.Labels[Key(new SGuid(guid))] = parked.Entry;
        Rebuild();
        if (TimbnWorld.Find(new SGuid(guid)) is { } data)
            TimbnWorld.ViewOf(data)?.DrawWidgets();
    }

    private void ForgetExpired()
    {
        foreach (var key in _removed.Where(pair => pair.Value.Until < Time.unscaledTime).Select(pair => pair.Key).ToArray())
            _removed.Remove(key);
    }

    // Runs for every object the game draws a bubble for, so it only looks up a Guid and never builds a string.
    public void AddLabel(Wgo chest, List<LazyWidgetDataBase> widgets)
    {
        if (_texts.Count == 0 || chest.Data == null || chest.Data.IsHidden)
            return;

        if (!_texts.TryGetValue(chest.Data.UniqueId.Guid, out var text))
            return;

        var widget = new UIQualityTooltipWidgetData("", text);
        if (PluginConfig.LabelBelow.Value)
            widgets.Add(widget);
        else
            widgets.Insert(0, widget);
    }

    public void Rebuild()
    {
        _texts.Clear();
        foreach (var (key, entry) in _data.Current.Labels)
        {
            if (!Guid.TryParse(key, out var id))
                continue;

            var size = PluginConfig.FontSize(entry.SizeModifier).ToString("0.##", CultureInfo.InvariantCulture);
            var text = entry.Color == null ? entry.Text : $"<color={entry.Color}>{entry.Text}</color>";
            _texts[id] = $"<size={size}>{text}</size>";
        }
    }

    private static string Key(SGuid id) => id.ToString();
}
