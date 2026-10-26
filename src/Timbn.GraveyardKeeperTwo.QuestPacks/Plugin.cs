namespace Timbn.GraveyardKeeperTwo.QuestPacks;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.2.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override TimbnModInfo ModInfo => new()
    {
        ModGroupName = "Timbn Mods",
        ModGroupId = "338834e9-f5a5-4d7b-9773-17dfe3d5a954",
        Description = "Adds quests and talks written as plain JSON files, no code needed.",
        Author = "Timbn",
    };

    protected override void OnAwake()
    {
        Events.MainMenuOpened(TimbnQuestPacks.LoadStandalone);
        Subscriptions.Add(TimbnQuestPacks.UnloadStandalone);
        TimbnSafe.Run(TimbnQuestPacks.LoadStandalone, Logger, $"{nameof(TimbnQuestPacks)}|Loading the quest packs");
        foreach (var plugin in Running.ToList())
            LoadModPack(plugin);

        PluginStarted += LoadModPack;
        Subscriptions.Add(() => PluginStarted -= LoadModPack);
    }

    private void LoadModPack(TimbnFrameworkPlugin plugin) =>
        Subscriptions.Add(plugin.Subscriptions.Add(TimbnQuestPacks.LoadModPack(plugin)));
}
