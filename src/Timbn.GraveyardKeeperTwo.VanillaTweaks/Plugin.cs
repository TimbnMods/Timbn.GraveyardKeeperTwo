using LazyBearTechnology;
using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.0.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    private StudyTableScienceOnly? _studyTable;
    private GreenThumbTalentBonus? _greenThumb;
    private TechPointCap? _techPointCap;
    private StrayTechPoints? _strayTechPoints;
    private StuckCarriers? _stuckCarriers;
    private OvenIngredientSlots? _ovenSlots;
    private BuiltEarlyQuests? _builtEarly;

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        if (PluginConfig.StudyTableScienceOnly.Value)
        {
            _studyTable = new StudyTableScienceOnly();
            Events.GameStarted(_studyTable.Apply);
            if (TimbnGame.IsInGame)
                _studyTable.Apply();
        }
        else
        {
            Events.GameStarted(StudyTableScienceOnly.RemoveSavedFilters);
            if (TimbnGame.IsInGame)
                StudyTableScienceOnly.RemoveSavedFilters();
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

        if (PluginConfig.CrematoriumNoStuckBodies.Value)
        {
            Events.GameStarted(CrematoriumStuckBody.BurnStuckBodies);
            if (TimbnGame.IsInGame)
                CrematoriumStuckBody.BurnStuckBodies();
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

        if (TimbnGame.IsInGame
            && LazyWindowsStackController.ActiveWindow == null
            && PluginConfig.UnstuckKey.Value.IsDown())
        {
            Unstuck.Run(PluginConfig.UnstuckRange.Value);
        }
    }

    protected override void OnDestroyed()
    {
        _studyTable?.Revert();
        _greenThumb?.Revert();
        _techPointCap?.Revert();
        _ovenSlots?.Revert();
    }
}
