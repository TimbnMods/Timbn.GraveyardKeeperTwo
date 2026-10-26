namespace Timbn.GraveyardKeeperTwo.ChestLabels;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.2.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    internal ChestLabelBoard Board { get; private set; } = null!;

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override TimbnModInfo ModInfo => new()
    {
        ModGroupName = "Timbn Mods",
        ModGroupId = "338834e9-f5a5-4d7b-9773-17dfe3d5a954",
        Description = "Name your chests, and the name floats over the chest.",
        Author = "Timbn",
        NexusLink = "https://www.nexusmods.com/graveyardkeeper2/mods/398",
        SteamWorkshopLink = "https://steamcommunity.com/sharedfiles/filedetails/?id=3821251395",
        ThunderstoreLink = "https://thunderstore.io/c/graveyard-keeper-2/p/Timbn/TimbnChestLabels/",
    };

    protected override void OnAwake()
    {
        var data = Saves.Register<ChestLabelData>(_ => Board?.Rebuild());
        Board = new ChestLabelBoard(data);
        Settings.Changed(PluginConfig.DefaultSize, _ => Board.Rebuild());
        Interactions.Register(ChestLabelBoard.CanLabel).AddSecondary(Text.Key("timbn_chest_labels_label"), NameChest);
        ModEvents.Listen<MovedBuilding>("moved", moved => Board.Moved(moved.OldId, moved.NewId));
        Events.WorldObjectRemoved(data => Board.ChestRemoved(data.UniqueId));
    }

    private void NameChest(Wgo chest)
    {
        var name = Board.GetText(chest.Data);
        var color = Board.GetColor(chest.Data);
        var labelSize = (ChestLabelSize)Board.GetSize(chest.Data);
        UI.ShowPage(
            Text.Get("timbn_chest_labels_title"),
            (page, close) =>
            {
                page.Add(new TimbnTextBox(() => name, value => name = value) { FocusWhenShown = true });
                page.Add(new TimbnColorPicker(PluginConfig.ColorList, () => color, value => color = value));
                page.Add(new TimbnChoice<ChestLabelSize>(() => labelSize, value => labelSize = value));
                page.ButtonFooter(footer => footer.AddPrimarySpecial(Text.Get(TimbnGameIds.Texts.UiSave), () => { Board.SetLabel(chest, name, color, (int)labelSize); close(); }));
            },
            width: TimbnLength.Pixels(180));
    }
}
