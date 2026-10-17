namespace Timbn.GraveyardKeeperTwo.ChestLabels;

/// <summary>Keeps one label over each chest that has a saved name, shown while the chest is in the scene the keeper is in.</summary>
internal sealed class ChestLabelBoard
{
    private readonly Plugin _plugin;
    private readonly TimbnSaveData<ChestLabelData> _data;
    private readonly Dictionary<string, TimbnLabel> _labels = [];

    public ChestLabelBoard(Plugin plugin, TimbnSaveData<ChestLabelData> data)
    {
        _plugin = plugin;
        _data = data;
    }

    public static bool IsChest(WGOInteractionHandlerBase handler) => handler is ChestInteractionHandler;

    public string GetText(WgoData chest) =>
        _data.Current.Labels.TryGetValue(Key(chest), out var entry) ? entry.Text : "";

    public void SetText(WgoData chest, string text)
    {
        var key = Key(chest);
        text = text.Trim();
        if (text.Length == 0)
        {
            _data.Current.Labels.Remove(key);
            Drop(key);
        }
        else
        {
            _data.Current.Labels[key] = new ChestLabelEntry { Text = text };
        }

        Refresh();
    }

    public void ChestRemoved(SGuid id)
    {
        var key = id.ToString();
        if (_data.Current.Labels.Remove(key))
            Drop(key);
    }

    public void Refresh()
    {
        if (!TimbnGame.IsInGame || MainGame.WorldData == null)
            return;

        var zone = TimbnPlayer.Zone;
        foreach (var key in _data.Current.Labels.Keys.ToList())
        {
            var chest = MainGame.WorldData.GetWgoData(SGuid.Parse(key));
            if (chest == null)
            {
                Drop(key);
                continue;
            }

            if (chest.IsHidden || zone == null || chest.WorldZoneData != zone)
            {
                Hide(key);
                continue;
            }

            if (!_labels.TryGetValue(key, out var label))
            {
                label = _plugin.UI.CreateLabel();
                _labels[key] = label;
            }

            label.Show(chest.Position, _data.Current.Labels[key].Text);
        }

        foreach (var key in _labels.Keys.Where(key => !_data.Current.Labels.ContainsKey(key)).ToList())
            Drop(key);
    }

    public void Clear()
    {
        foreach (var label in _labels.Values)
            label.Dispose();

        _labels.Clear();
    }

    private static string Key(WgoData chest) => chest.UniqueId.ToString();

    private void Hide(string key)
    {
        if (_labels.TryGetValue(key, out var label))
            label.Hide();
    }

    private void Drop(string key)
    {
        if (_labels.Remove(key, out var label))
            label.Dispose();
    }
}
