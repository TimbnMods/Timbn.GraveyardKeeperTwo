namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.4.1")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    private readonly Meditation _meditation = new();

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        Text.AddLanguageFiles();
        Events.GoToMainMenu(_meditation.Stop);
        Logger.LogMessage("Campfire Meditation started.");
    }

    protected override void OnDestroyed() => _meditation.Dispose();

    protected override void OnUpdate() => _meditation.Tick();
}
