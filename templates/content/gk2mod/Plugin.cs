namespace MyMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.0.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void OnAwake()
    {
        Logger.LogMessage("MyMod started.");
    }
}
