namespace Timbn.GraveyardKeeperTwo.LivingTown;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.2.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    private const string _hermClosedKey = "timbn_living_town_herm_closed";

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override TimbnModInfo ModInfo => new()
    {
        ModGroupName = "Timbn Mods",
        ModGroupId = "338834e9-f5a5-4d7b-9773-17dfe3d5a954",
        Description = "Villagers keep daily routines, visit the graveyard, and ask for dishes and deliveries.",
        Author = "Timbn",
        NexusLink = "https://www.nexusmods.com/graveyardkeeper2/mods/394",
        SteamWorkshopLink = "https://steamcommunity.com/sharedfiles/filedetails/?id=3821083573",
        ThunderstoreLink = "https://thunderstore.io/c/graveyard-keeper-2/p/Timbn/TimbnLivingTown/",
    };

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
        var cat = new ClubCat(this);
        Events.Update(cat.Tick);
        Events.SaveClosed(cat.Stop);
        Events.SaveReady(visits.Prepare);
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
