namespace Timbn.GraveyardKeeperTwo.WorkshopSync;

internal sealed class WorkshopMirror
{
    private const string _appId = "4358690";

    private readonly ManualLogSource _logger;
    private readonly HashSet<string> _skippedItemIds;
    private readonly string _steamFolder;
    private readonly string _pluginsTarget = Path.Combine(Paths.PluginPath, "Workshop");

    public WorkshopMirror(ManualLogSource logger, HashSet<string> skippedItemIds, string steamFolder)
    {
        _logger = logger;
        _skippedItemIds = skippedItemIds;
        _steamFolder = steamFolder;
    }

    public void Run()
    {
        var steamApps = FindSteamApps();
        if (steamApps == null)
        {
            return;
        }

        if (Directory.Exists(_pluginsTarget))
        {
            Directory.Delete(_pluginsTarget, true);
        }

        var content = Path.Combine(steamApps, "workshop", "content", _appId);
        var installed = 0;
        foreach (var item in Directory.Exists(content) ? Directory.GetDirectories(content) : [])
        {
            var id = Path.GetFileName(item);
            if (!id.All(char.IsDigit) || _skippedItemIds.Contains(id))
            {
                continue;
            }

            try
            {
                if (CopyFolders(id, item))
                {
                    installed++;
                }
                else
                {
                    _logger.LogInfo($"Workshop item {id} has no plugins or config folder, so it is left alone.");
                }
            }
            catch (Exception exception)
            {
                _logger.LogError($"Could not install Workshop item {id}, so it is left out: {exception.Message}");
                RemovePartialInstall(id);
            }
        }

        _logger.LogInfo($"Installed {installed} Workshop mod(s).");
    }

    private string? FindSteamApps()
    {
        if (_steamFolder.Length > 0)
        {
            var configured = Path.GetFileName(_steamFolder.TrimEnd('\\', '/')).Equals("steamapps", StringComparison.OrdinalIgnoreCase)
                ? _steamFolder
                : Path.Combine(_steamFolder, "steamapps");
            if (Directory.Exists(configured))
            {
                return configured;
            }

            _logger.LogWarning($"SteamFolder {_steamFolder} has no steamapps folder, so Workshop mods are left as they are.");
            return null;
        }

        var detected = Path.GetFullPath(Path.Combine(Paths.GameRootPath, "..", ".."));
        if (File.Exists(Path.Combine(detected, $"appmanifest_{_appId}.acf")))
        {
            return detected;
        }

        _logger.LogWarning($"No Steam library found around {Paths.GameRootPath}, so Workshop mods are left as they are. Set SteamFolder in the config to the Steam folder that holds your Workshop downloads.");
        return null;
    }

    private void RemovePartialInstall(string id)
    {
        var target = Path.Combine(_pluginsTarget, id);
        try
        {
            if (Directory.Exists(target))
            {
                Directory.Delete(target, true);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($"Could not remove the partly installed Workshop item {id} from {target}: {exception.Message}");
        }
    }

    private bool CopyFolders(string id, string folder)
    {
        var found = false;
        foreach (var child in Directory.GetDirectories(folder))
        {
            var name = Path.GetFileName(child).ToLowerInvariant();
            if (name == "plugins")
            {
                CopyFolder(child, Path.Combine(_pluginsTarget, id), true);
                found = true;
            }
            else if (name == "config")
            {
                CopyFolder(child, Paths.ConfigPath, false);
                found = true;
            }
            else
            {
                found |= CopyFolders(id, child);
            }
        }

        return found;
    }

    private static void CopyFolder(string source, string target, bool overwrite)
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, file.Substring(source.Length + 1));
            if (!overwrite && File.Exists(destination))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, true);
        }
    }
}
