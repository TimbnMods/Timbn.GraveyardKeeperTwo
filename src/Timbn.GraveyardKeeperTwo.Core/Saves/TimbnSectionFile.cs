using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSectionFile
{
    private const int _formatVersion = 1;

    internal static JObject Read(string path, string backupPath) => Read(path, backupPath, TimbnCorePlugin.Logger);

    internal static JObject Read(string path, string backupPath, ManualLogSource logger)
    {
        try
        {
            if (ReadFile(path) is { } sections)
                return sections;
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(TimbnSectionFile)}|Could not read {path}, trying its backup. {ex.Message}");
            MoveAside(path, path + ".bad", logger);
        }

        try
        {
            if (ReadFile(backupPath) is { } sections)
            {
                logger.LogWarning($"{nameof(TimbnSectionFile)}|Loaded mod data from the backup {backupPath}.");
                return sections;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(TimbnSectionFile)}|Could not read the backup {backupPath} either, so mod data starts empty. {ex.Message}");
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

    internal static void LoadEntry(JObject sections, ITimbnSaveEntry entry, string badPathPrefix, string what)
    {
        var section = sections[entry.Id];
        try
        {
            entry.Load(section);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{what}|Could not read the data of {entry.Id}, so it starts empty. {ex.Message}");
            if (section is not null)
                WriteAside(badPathPrefix + "." + entry.Id + ".bad", section);

            entry.Load(null);
        }
    }

    internal static void KeepEntry(JObject sections, ITimbnSaveEntry entry, string what)
    {
        try
        {
            sections[entry.Id] = entry.Serialize();
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{what}|Could not keep the data of {entry.Id}, so its last saved copy stays. {ex.Message}");
        }
    }

    private static void WriteAside(string badPath, JToken section)
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

    private static void MoveAside(string path, string badPath, ManualLogSource logger)
    {
        try
        {
            File.Delete(badPath);
            File.Move(path, badPath);
            logger.LogWarning($"{nameof(TimbnSectionFile)}|Kept the unreadable file as {badPath}.");
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(TimbnSectionFile)}|Could not move the unreadable file {path} aside: {ex.Message}");
        }
    }
}
