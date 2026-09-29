using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.5.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        PerkTalentBonus.Register(this);
        TechPointCap.Register(this);
        StrayTechPoints.Register(this);
        StuckCarriers.Register(this);
        StuckResourceWorkers.Register(this);
        BuiltEarlyQuests.Register(this);
        UnreachableDismantle.Register(this);
        LostItems.Register(this);
        LostBattleRewards.Register(this);

        Logger.LogMessage($"Vanilla Tweaks started. Unstuck on {PluginConfig.UnstuckKey.Value}.");
    }

    protected override void OnUpdate()
    {
        if (PluginConfig.UnstuckKey.Value.IsDownWithControl())
            Unstuck.Run(PluginConfig.UnstuckRange.Value);
    }
}
