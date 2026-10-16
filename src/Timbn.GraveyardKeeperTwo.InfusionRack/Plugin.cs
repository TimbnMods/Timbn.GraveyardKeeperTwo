namespace Timbn.GraveyardKeeperTwo.InfusionRack;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.0.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        InfusionTech.Register(this);
        SpiritSolution.Register(this);
        SpiritRecipes.Register(this);
        InfusionStand.Register(this);
        InfusionMachine.Register(this);
    }
}
