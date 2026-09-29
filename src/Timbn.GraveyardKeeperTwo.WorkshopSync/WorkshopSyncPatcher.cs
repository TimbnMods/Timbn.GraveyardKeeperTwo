using Mono.Cecil;

namespace Timbn.GraveyardKeeperTwo.WorkshopSync;

public static class WorkshopSyncPatcher
{
    private static readonly ManualLogSource _logger = Logger.CreateLogSource(MyPluginInfo.PLUGIN_NAME);

    public static IEnumerable<string> TargetDLLs => [];

    public static void Initialize()
    {
        try
        {
            PatcherConfig.Bind(new ConfigFile(Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID + ".cfg"), true));
            if (!PatcherConfig.Enabled.Value)
            {
                _logger.LogInfo("Turned off in the config, so Workshop mods are left as they are.");
                return;
            }

            new WorkshopMirror(_logger, PatcherConfig.SkippedItemIds(), PatcherConfig.SteamFolderPath()).Run();
        }
        catch (Exception exception)
        {
            _logger.LogError($"Workshop sync failed: {exception}");
        }
    }

    public static void Patch(AssemblyDefinition assembly)
    {
    }
}
