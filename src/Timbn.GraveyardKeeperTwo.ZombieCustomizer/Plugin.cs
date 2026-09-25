namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.3.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        Text.Add(RestyleButton.TooltipKey, "Customize Appearance");
        ZombieTint.PaintAll(reset: false);
        Logger.LogMessage("Zombie Customizer started. Click a zombie's portrait in its inspect window to restyle it.");
    }

    protected override void OnDestroyed()
    {
        ZombieCustomization.Stop();
        RestyleButton.Remove();
        ZombieTint.PaintAll(reset: true);
    }
}
