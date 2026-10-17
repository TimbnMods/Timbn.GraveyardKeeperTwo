namespace Timbn.GraveyardKeeperTwo.ChestLabels;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.0.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    internal ChestLabelBoard Board { get; private set; } = null!;

    internal LabelWindow Window { get; private set; } = null!;

    protected override void OnAwake()
    {
        var data = Saves.Register<ChestLabelData>();
        Board = new ChestLabelBoard(this, data);
        Window = new LabelWindow(this);
        Events.SaveReady(Board.Refresh);
        Events.SaveClosed(Board.Clear);
        Events.SaveClosed(Window.Close);
        Events.EveryRealSeconds(0.5f, Board.Refresh);
    }
}
