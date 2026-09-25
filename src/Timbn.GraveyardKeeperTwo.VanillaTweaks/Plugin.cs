using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.2.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    private SpecialStorageFilters? _storageFilters;
    private GreenThumbTalentBonus? _greenThumb;
    private TechPointCap? _techPointCap;
    private StrayTechPoints? _strayTechPoints;
    private StuckCarriers? _stuckCarriers;
    private OvenIngredientSlots? _ovenSlots;
    private BuiltEarlyQuests? _builtEarly;
    private UnreachableDismantle? _unreachableDismantle;
    private CollisionFixes? _collisionFixes;

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        if (PluginConfig.SpecialStorageFilters.Value)
        {
            _storageFilters = new SpecialStorageFilters();
            Events.GameStarted(_storageFilters.Apply);
            if (TimbnGame.IsInGame)
                _storageFilters.Apply();
        }

        if (PluginConfig.GreenThumbTalentBonus.Value)
        {
            _greenThumb = new GreenThumbTalentBonus();
            Events.GameStarted(_greenThumb.Apply);
            if (TimbnGame.IsInGame)
                _greenThumb.Apply();
        }

        if (PluginConfig.TechPointCap.Value > 999)
        {
            _techPointCap = new TechPointCap(PluginConfig.TechPointCap.Value);
            Events.GameStarted(_techPointCap.Apply);
            if (TimbnGame.IsInGame)
                _techPointCap.Apply();
        }

        if (PluginConfig.StudyTableNoStuckCrafts.Value)
        {
            Events.GameStarted(StudyTableStuckCraft.ClearStuckCrafts);
            if (TimbnGame.IsInGame)
                StudyTableStuckCraft.ClearStuckCrafts();
        }

        if (PluginConfig.OvenNoLostIngredients.Value)
        {
            _ovenSlots = new OvenIngredientSlots();
            Events.GameStarted(_ovenSlots.Apply);
            if (TimbnGame.IsInGame)
                _ovenSlots.Apply();
        }

        if (PluginConfig.CollectStrayTechPoints.Value)
            _strayTechPoints = new StrayTechPoints();

        if (PluginConfig.SoftLockedBoards.Value)
            SoftLockedBoards.Register(this);

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

        _unreachableDismantle = new UnreachableDismantle();
        _unreachableDismantle.Apply();

        if (PluginConfig.CollisionFixes.Value)
        {
            _collisionFixes = new CollisionFixes();
            _collisionFixes.Subscribe(Events);
            _collisionFixes.ApplyToLoadedScenes();
        }

        Logger.LogMessage($"Vanilla Tweaks started. Unstuck on {PluginConfig.UnstuckKey.Value}.");
    }

    protected override void OnUpdate()
    {
        if (TimbnGame.IsInGame)
        {
            _strayTechPoints?.Tick();
            _stuckCarriers?.Tick();
            _builtEarly?.Tick();
        }

        if (PluginConfig.UnstuckKey.Value.IsDownWithControl())
            Unstuck.Run(PluginConfig.UnstuckRange.Value);
    }

    protected override void OnDestroyed()
    {
        _storageFilters?.Revert();
        _greenThumb?.Revert();
        _techPointCap?.Revert();
        _ovenSlots?.Revert();
        _unreachableDismantle?.Revert();
        _collisionFixes?.Revert();
    }
}
