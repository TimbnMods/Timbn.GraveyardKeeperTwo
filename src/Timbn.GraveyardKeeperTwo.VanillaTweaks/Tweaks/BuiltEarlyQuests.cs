namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class BuiltEarlyQuests
{
    private static readonly (string Quest, string Building, string Wgo)[] _quests =
    [
        ("74_crossroad_chore_build", "zmb_choir_unlock_s", "zmb_choir_unlock"),
        ("96_crossroad_organ_build", "zmb_organ_unlock_s", "zmb_organ_unlock"),
    ];

    private bool _pending;

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        var builtEarly = new BuiltEarlyQuests();
        plugin.Settings.Toggle(PluginConfig.BuiltEarlyQuests, builtEarly.Queue);
        plugin.Settings.While(PluginConfig.BuiltEarlyQuests, () => plugin.Events.QuestStarted(builtEarly.OnQuestStarted));
        plugin.Settings.While(PluginConfig.BuiltEarlyQuests, () => plugin.Events.Update(builtEarly.Tick));
    }

    private void Queue() => _pending = true;

    private void OnQuestStarted(QuestData quest)
    {
        foreach (var entry in _quests)
        {
            if (entry.Quest == quest.id)
                _pending = true;
        }
    }

    private void Tick()
    {
        if (!_pending)
            return;

        _pending = false;
        var locked = MainGame.Instance.GameSave.knowledgeSystem.lockedBuildings;
        foreach (var entry in _quests)
        {
            if (TimbnQuests.StatusOf(entry.Quest) != QuestStatus.InProgress || !locked.Contains(entry.Building))
                continue;

            Plugin.Logger.LogInfo($"{entry.Building} was built before {entry.Quest} started, finishing the quest.");
            GlobalEventsSystem.FireTrigger(GlobalEventsSystem.Event.Type.BuildBuilding, entry.Wgo);
        }
    }
}
