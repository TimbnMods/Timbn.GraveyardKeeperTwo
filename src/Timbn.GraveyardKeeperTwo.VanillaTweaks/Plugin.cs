using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.4.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    private PerkTalentBonus? _perkTalentBonus;
    private TechPointCap? _techPointCap;
    private StrayTechPoints? _strayTechPoints;
    private StuckCarriers? _stuckCarriers;
    private BuiltEarlyQuests? _builtEarly;
    private UnreachableDismantle? _unreachableDismantle;

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        if (PluginConfig.PerkTalentBonus.Value)
        {
            _perkTalentBonus = new PerkTalentBonus();
            Events.GameStarted(_perkTalentBonus.Apply);
            if (TimbnGame.IsInGame)
                _perkTalentBonus.Apply();
        }

        if (PluginConfig.TechPointCap.Value > 999)
        {
            _techPointCap = new TechPointCap(PluginConfig.TechPointCap.Value);
            Events.GameStarted(_techPointCap.Apply);
            if (TimbnGame.IsInGame)
                _techPointCap.Apply();
        }

        if (PluginConfig.CollectStrayTechPoints.Value)
            _strayTechPoints = new StrayTechPoints();

        Text.AddLanguageFiles();
        LostItems.Register(this);

        if (PluginConfig.BuiltEarlyQuests.Value)
        {
            _builtEarly = new BuiltEarlyQuests();
            Events.GameStarted(_builtEarly.Queue);
            Events.QuestStarted(_builtEarly.OnQuestStarted);
            _builtEarly.Queue();
        }

        if (PluginConfig.StuckCarriers.Value)
        {
            _stuckCarriers = new StuckCarriers();
            Events.GoToMainMenu(_stuckCarriers.Reset);
        }

        Events.GoToMainMenu(ConveyorLoopCrash.Restore);
        Events.GameStarted(StuckResourceWorkers.Repair);
        if (TimbnGame.IsInGame)
            StuckResourceWorkers.Repair();

        _unreachableDismantle = new UnreachableDismantle();
        _unreachableDismantle.Apply();

        Logger.LogMessage($"Vanilla Tweaks started. Unstuck on {PluginConfig.UnstuckKey.Value}.");
    }

    protected override void OnUpdate()
    {
        if (TimbnGame.IsInGame)
        {
            _strayTechPoints?.Tick();
            _stuckCarriers?.Tick();
            _builtEarly?.Tick();
            ConveyorLoopCrash.Tick();
        }

        if (PluginConfig.UnstuckKey.Value.IsDownWithControl())
            Unstuck.Run(PluginConfig.UnstuckRange.Value);
    }

    protected override void OnDestroyed()
    {
        ConveyorLoopCrash.Restore();
        _perkTalentBonus?.Revert();
        _techPointCap?.Revert();
        _unreachableDismantle?.Revert();
    }
}
