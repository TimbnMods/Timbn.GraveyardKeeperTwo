namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.5.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        var meditation = new Meditation(this);
        Events.Update(meditation.Tick);
        Events.SaveClosed(meditation.Stop);
        Logger.LogMessage("Campfire Meditation started.");
    }
}
