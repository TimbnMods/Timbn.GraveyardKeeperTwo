using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.ChestLabels;

/// <summary>
/// Keeps the saved name of each chest and adds it to the chest's own bubble, so the game stacks it with the Open and
/// Label hints the way it stacks a grave's quality label.
/// </summary>
internal sealed class ChestLabelBoard
{
    private readonly TimbnSaveData<ChestLabelData> _data;
    private readonly Dictionary<Guid, string> _texts = [];

    public ChestLabelBoard(TimbnSaveData<ChestLabelData> data)
    {
        _data = data;
        Rebuild();
    }

    // Only objects with chest in the id, since the chest interaction is also used by shelves, containers and storages.
    public static bool CanLabel(WGOInteractionHandlerBase handler, Wgo? wgo) =>
        handler is ChestInteractionHandler && wgo?.Id?.Contains("chest", StringComparison.OrdinalIgnoreCase) == true;

    public string GetText(WgoData chest) =>
        _data.Current.Labels.TryGetValue(Key(chest.UniqueId), out var entry) ? entry.Text : "";

    public void SetText(Wgo chest, string text)
    {
        var key = Key(chest.Data.UniqueId);
        text = text.Trim();
        if (text.Length == 0)
            _data.Current.Labels.Remove(key);
        else
            _data.Current.Labels[key] = new ChestLabelEntry { Text = text };

        Rebuild();
        UIObjectBubbleManager.Instance?.Display(chest);
    }

    public void ChestRemoved(SGuid id)
    {
        if (_data.Current.Labels.Remove(Key(id)))
            Rebuild();
    }

    // Runs for every object the game draws a bubble for, so it only looks up a Guid and never builds a string.
    public void AddLabel(Wgo chest, List<LazyWidgetDataBase> widgets)
    {
        if (_texts.Count == 0 || chest.Data == null || chest.Data.IsHidden)
            return;

        if (_texts.TryGetValue(chest.Data.UniqueId.Guid, out var text))
            widgets.Insert(0, new UIQualityTooltipWidgetData("", text));
    }

    public void Rebuild()
    {
        _texts.Clear();
        foreach (var (key, entry) in _data.Current.Labels)
        {
            if (Guid.TryParse(key, out var id))
                _texts[id] = entry.Text;
        }
    }

    private static string Key(SGuid id) => id.ToString();
}
