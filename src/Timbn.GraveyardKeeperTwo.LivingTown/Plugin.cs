namespace Timbn.GraveyardKeeperTwo.LivingTown;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.0.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    private const string _hermClosedKey = "timbn_living_town_herm_closed";
    private const string _petKey = "timbn_living_town_pet";

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        var herm = new HermDay();
        var linda = new LindaDay(this);
        var larry = new LarryDay();
        var astrologer = new AstrologerDay();
        var visits = new ChurchVisits(this);
        Settings.While(PluginConfig.DailyRoutine, () => Npcs.Schedule(TimbnGameIds.Npcs.Herm, herm.Build()));
        Settings.While(PluginConfig.DailyRoutine, () => Npcs.Schedule(TimbnGameIds.Npcs.TavernOwner, linda.Build()));
        Events.Update(linda.Tick);
        Events.SaveClosed(linda.Reset);
        Settings.Toggle(PluginConfig.DailyRoutine, () => { }, linda.Reset);
        Settings.While(PluginConfig.DailyRoutine, () => Npcs.Schedule(TimbnGameIds.Npcs.Larry, larry.Build()));
        Settings.While(PluginConfig.DailyRoutine, () => Npcs.Schedule(TimbnGameIds.Npcs.Astrologer, astrologer.Build()));
        Events.SaveClosed(herm.Reset);
        UI.AddObjectAction(new TimbnObjectAction("common_box_08", new Vector3(-104.36f, 0f, -338.20f), _petKey));
        Events.Update(visits.Tick);
        Events.SaveClosed(visits.Reset);
        Events.BeforeSave(visits.SendAway);
        Settings.Toggle(PluginConfig.ChurchVisits, () => { }, visits.Reset);
        Settings.Toggle(PluginConfig.Funerals, () => { }, visits.Reset);
        Settings.Changed(PluginConfig.GratitudePerVisitor, _ => visits.Replan());
        Settings.Changed(PluginConfig.MinimumChurchVisitors, _ => visits.Replan());
        Settings.Changed(PluginConfig.MaximumChurchVisitors, _ => visits.Replan());
        Settings.While(PluginConfig.OpeningHours, () => Npcs.CloseTradeWhen(TimbnGameIds.Npcs.Herm, VendorHours.IsClosed, _hermClosedKey));
        new Requests(this).Start();
        new Deliveries(this).Start();
    }
}
