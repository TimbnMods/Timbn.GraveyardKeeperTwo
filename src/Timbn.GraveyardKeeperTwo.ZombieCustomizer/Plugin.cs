namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.2.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    internal ZombieCustomization Customization { get; private set; } = null!;

    internal RestyleButton RestyleButton { get; private set; } = null!;

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override TimbnModInfo ModInfo => new()
    {
        ModGroupName = "Timbn Mods",
        ModGroupId = "338834e9-f5a5-4d7b-9773-17dfe3d5a954",
        Description = "Restyle your zombies' hair and clothes in the character customization window.",
        Author = "Timbn",
        NexusLink = "https://www.nexusmods.com/graveyardkeeper2/mods/126",
        SteamWorkshopLink = "https://steamcommunity.com/sharedfiles/filedetails/?id=3810071742",
        ThunderstoreLink = "https://thunderstore.io/c/graveyard-keeper-2/p/Timbn/TimbnZombieCustomizer/",
    };

    protected override void OnAwake()
    {
        Customization = new ZombieCustomization();
        RestyleButton = new RestyleButton(Customization);
        Text.Add(RestyleButton.TooltipKey, "Customize Appearance");
        Events.SaveReady(() => ZombieTint.PaintAll(reset: false));
        Events.SaveClosed(Customization.Stop);
        Events.SaveClosed(() => ZombieTint.PaintAll(reset: true));
        Events.ZombieViewReady((part, zombie) => ZombieTint.Paint(part, ZombieTint.Read(zombie)));
    }

    protected override void OnUnload() => RestyleButton.Remove();
}
