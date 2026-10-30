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
            new WorkshopHandoff(_logger).Run();
        }
        catch (Exception exception)
        {
            _logger.LogError($"Could not hand Workshop mods to the game: {exception}");
        }
    }

    public static void Patch(AssemblyDefinition assembly)
    {
    }
}
