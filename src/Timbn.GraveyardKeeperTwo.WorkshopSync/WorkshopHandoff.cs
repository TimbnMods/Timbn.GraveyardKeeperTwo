namespace Timbn.GraveyardKeeperTwo.WorkshopSync;

/// <summary>
/// Hands Workshop mods to the game, which copies them itself from 1.011 on. Runs before the chainloader scans
/// plugins, so the moved mods load on the same start. The game only tracks files it copied and keeps its copying
/// off while a Timbn DLL is in patchers, so this has to empty plugins/Workshop and take this DLL out of the way.
/// </summary>
internal sealed class WorkshopHandoff
{
    private const string _retiredExtension = ".retired";

    private readonly ManualLogSource _logger;
    private readonly string _workshopFolder = Path.Combine(Paths.PluginPath, "Workshop");

    public WorkshopHandoff(ManualLogSource logger)
    {
        _logger = logger;
    }

    public void Run()
    {
        if (Directory.Exists(_workshopFolder))
        {
            foreach (var item in Directory.GetDirectories(_workshopFolder))
            {
                MoveToPlugins(item);
            }

            Directory.Delete(_workshopFolder, true);
        }

        Retire();
        _logger.LogInfo("The game now installs your Workshop mods, so Workshop Sync turned itself off. You can delete it.");
    }

    private void MoveToPlugins(string item)
    {
        foreach (var entry in Directory.GetFileSystemEntries(item))
        {
            var target = Path.Combine(Paths.PluginPath, Path.GetFileName(entry));
            if (File.Exists(target) || Directory.Exists(target))
            {
                _logger.LogInfo($"{Path.GetFileName(entry)} is already in plugins, so that copy is kept.");
                continue;
            }

            if (Directory.Exists(entry))
            {
                Directory.Move(entry, target);
            }
            else
            {
                File.Move(entry, target);
            }
        }
    }

    private static void Retire()
    {
        var location = typeof(WorkshopHandoff).Assembly.Location;
        var retired = location + _retiredExtension;
        if (File.Exists(retired))
        {
            File.Delete(retired);
        }

        File.Move(location, retired);
    }
}
