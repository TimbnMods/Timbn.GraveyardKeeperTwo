using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSectionFile
{
    private const int _formatVersion = 1;

    internal static JObject Read(string path, string backupPath)
    {
        try
        {
            if (ReadFile(path) is { } sections)
                return sections;
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnSectionFile)}|Could not read {path}, trying its backup. {ex.Message}");
            MoveAside(path, path + ".bad");
        }

        try
        {
            if (ReadFile(backupPath) is { } sections)
            {
                TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnSectionFile)}|Loaded mod data from the backup {backupPath}.");
                return sections;
            }
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnSectionFile)}|Could not read the backup {backupPath} either, so mod data starts empty. {ex.Message}");
        }

        return [];
    }

    internal static void Write(string path, string backupPath, JObject sections)
    {
        var root = new JObject
        {
            ["formatVersion"] = _formatVersion,
            ["mods"] = sections,
        };

        var temporaryPath = path + ".new";
        File.WriteAllText(temporaryPath, root.ToString(Formatting.Indented));
        if (File.Exists(path))
            File.Replace(temporaryPath, path, backupPath);
        else
            File.Move(temporaryPath, path);
    }

    internal static void WriteAside(string badPath, JToken section)
    {
        try
        {
            File.WriteAllText(badPath, section.ToString(Formatting.Indented));
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnSectionFile)}|Kept the unreadable data as {badPath}.");
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnSectionFile)}|Could not keep the unreadable data as {badPath}: {ex.Message}");
        }
    }

    private static JObject? ReadFile(string path)
    {
        if (!File.Exists(path))
            return null;

        var root = JObject.Parse(File.ReadAllText(path));
        return root["mods"] as JObject ?? [];
    }

    private static void MoveAside(string path, string badPath)
    {
        try
        {
            File.Delete(badPath);
            File.Move(path, badPath);
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnSectionFile)}|Kept the unreadable file as {badPath}.");
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnSectionFile)}|Could not move the unreadable file {path} aside: {ex.Message}");
        }
    }
}
