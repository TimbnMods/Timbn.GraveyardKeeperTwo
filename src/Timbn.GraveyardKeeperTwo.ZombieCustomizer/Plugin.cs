namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.5.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    internal ZombieCustomization Customization { get; private set; } = null!;

    internal RestyleButton RestyleButton { get; private set; } = null!;

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        Customization = new ZombieCustomization();
        RestyleButton = new RestyleButton(Customization);
        Text.Add(RestyleButton.TooltipKey, "Customize Appearance");
        Events.SaveReady(() => ZombieTint.PaintAll(reset: false));
        Events.SaveClosed(Customization.Stop);
        Events.SaveClosed(() => ZombieTint.PaintAll(reset: true));
        Events.ZombieViewReady((part, zombie) => ZombieTint.Paint(part, ZombieTint.Read(zombie)));
        Logger.LogMessage("Zombie Customizer started. Click a zombie's portrait in its inspect window to restyle it.");
    }

    protected override void OnDestroyed() => RestyleButton.Remove();
}
