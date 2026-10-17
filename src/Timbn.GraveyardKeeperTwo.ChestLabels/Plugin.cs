namespace Timbn.GraveyardKeeperTwo.ChestLabels;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.1.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    internal ChestLabelBoard Board { get; private set; } = null!;

    protected override void OnAwake()
    {
        var data = Saves.Register<ChestLabelData>(_ => Board?.Rebuild());
        Board = new ChestLabelBoard(data);
    }
}
