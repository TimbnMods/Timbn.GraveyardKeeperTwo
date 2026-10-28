using System.Text;
using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.QuestPacks;

/// <summary>
/// Loads quest packs, quests and talks written as JSON files. A standalone pack is a folder anywhere under
/// BepInEx/plugins with a pack.json and no DLL, owned by this plugin and read again each time the main menu opens when
/// its files changed. A mod's pack is a quests folder next to the mod's DLL, owned by the mod so it unloads and reloads
/// with it. The format is described in the Quest Packs documentation.
/// </summary>
internal static class TimbnQuestPacks
{
    private const string _questsFolder = "quests";
    private const string _packFile = "pack.json";
    private const string _spritesFolder = "sprites";
    private const string _langFolder = "lang";
    private const int _maxPopupLines = 6;

    private static readonly Regex _notId = new("[^a-z0-9_]");
    private static IDisposable? _standalone;
    private static string _signature = "";

    internal static void LoadStandalone()
    {
        List<string> packs;
        string signature;
        try
        {
            packs = FindStandalone();
            signature = Signature(packs);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"{nameof(TimbnQuestPacks)}|Could not look for quest packs: {ex}");
            return;
        }

        if (_standalone is not null && signature == _signature)
            return;

        UnloadStandalone();
        _signature = signature;
        TimbnSubscriptions loaded = new(Plugin.Logger);
        _standalone = loaded;
        List<string> problems = [];
        HashSet<string> ids = [];
        foreach (var folder in packs)
        {
            var shown = Shown(folder);
            TimbnPackInfo info;
            try
            {
                info = TimbnPackReader.ReadInfo(File.ReadAllText(Path.Combine(folder, _packFile)));
            }
            catch (Exception ex)
            {
                problems.Add($"{shown}/{_packFile}: {ex.Message} The pack was skipped.");
                continue;
            }

            if (!ids.Add(info.Id))
            {
                problems.Add($"{shown}: another pack already has the id \"{info.Id}\", so this one was skipped.");
                continue;
            }

            var color = info.BubbleColor ?? TimbnPluginDialog.DefaultBubbleColor;
            loaded.Add(Load(folder, shown, info.Id, $"pack {info.Id}", () => color, loadLanguage: true, problems));
        }

        loaded.Add(Report(problems));
    }

    internal static void UnloadStandalone()
    {
        _standalone?.Dispose();
        _standalone = null;
    }

    internal static IDisposable LoadModPack(TimbnFrameworkPlugin plugin)
    {
        TimbnSubscriptions loaded = new(plugin.PluginLogger);
        try
        {
            var folder = plugin.Folder;
            if (!Directory.Exists(Path.Combine(folder, _questsFolder)))
                return TimbnUndo.None;

            var shown = Shown(folder);
            var id = _notId.Replace(plugin.Metadata.GUID.ToLowerInvariant(), "_");
            Func<Color> color = () => plugin.Dialog.BubbleColor;
            List<string> problems = [];
            var packFile = Path.Combine(folder, _packFile);
            if (File.Exists(packFile))
            {
                try
                {
                    var info = TimbnPackReader.ReadInfo(File.ReadAllText(packFile));
                    id = info.Id;
                    if (info.BubbleColor is { } own)
                        color = () => own;
                }
                catch (Exception ex)
                {
                    problems.Add($"{shown}/{_packFile}: {ex.Message} The mod's own id was used.");
                }
            }

            loaded.Add(Load(folder, shown, id, $"{plugin.Metadata.GUID} pack", color, loadLanguage: false, problems));
            loaded.Add(Report(problems));
            return loaded;
        }
        catch (Exception ex)
        {
            loaded.Dispose();
            plugin.PluginLogger.LogError($"{nameof(TimbnQuestPacks)}|Could not load the mod's quest pack: {ex}");
            return TimbnUndo.None;
        }
    }

    private static IDisposable Load(string folder, string shown, string packId, string owner, Func<Color> color, bool loadLanguage, List<string> problems)
    {
        TimbnSubscriptions parts = new(Plugin.Logger);
        try
        {
            Load(folder, shown, packId, owner, color, loadLanguage, problems, parts);
            return parts;
        }
        catch (Exception ex)
        {
            parts.Dispose();
            Plugin.Logger.LogError($"{nameof(TimbnQuestPacks)}|Pack '{packId}' at {shown} threw while loading: {ex}");
            problems.Add($"{shown}: {ex.Message} The pack was skipped.");
            return TimbnUndo.None;
        }
    }

    private static void Load(string folder, string shown, string packId, string owner, Func<Color> color, bool loadLanguage, List<string> problems, TimbnSubscriptions parts)
    {
        var sprites = LoadSprites(folder, packId, parts, problems, shown);
        if (loadLanguage && Directory.Exists(Path.Combine(folder, _langFolder)))
            parts.Add(TimbnLocale.AddFromFiles(TimbnLanguageFiles.Read(folder, Plugin.Logger)));

        List<(string Name, string Text)> files = [];
        foreach (var file in Directory.GetFiles(Path.Combine(folder, _questsFolder), "*.json").OrderBy(file => file, StringComparer.Ordinal))
        {
            var name = $"{shown}/{_questsFolder}/{Path.GetFileName(file)}";
            try
            {
                files.Add((name, File.ReadAllText(file)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                problems.Add($"{name}: {ex.Message}");
            }
        }

        var content = new TimbnPackReader(packId, sprites).Read(files);
        problems.AddRange(content.Problems);
        foreach (var step in content.Quests.SelectMany(quest => quest.Steps))
        {
            try
            {
                var runtime = new TimbnQuestRuntime(step, color, owner);
                parts.Add(runtime.Register());
            }
            catch (Exception ex)
            {
                problems.Add($"{shown}, quest '{step.Id}': {ex.Message} It was skipped.");
            }
        }

        foreach (var talk in content.Talks)
        {
            try
            {
                var bubble = talk.BubbleColor is { } own ? () => own : color;
                parts.Add(TimbnPluginDialog.AddRecorded(talk.Npc, talk.Id, talk.When, talk.Lines, talk.Texts, bubble, talk.Once ? $"{packId}/{talk.Id}" : null));
            }
            catch (Exception ex)
            {
                problems.Add($"{shown}, talk '{talk.Id}': {ex.Message} It was skipped.");
            }
        }

        Plugin.Logger.LogInfo($"{nameof(TimbnQuestPacks)}|Pack '{packId}' at {shown} added {content.Quests.Count} quest(s) and {content.Talks.Count} talk(s), with {content.Problems.Count} problem(s).");
    }

    private static List<string> LoadSprites(string folder, string packId, TimbnSubscriptions parts, List<string> problems, string shown)
    {
        List<string> names = [];
        var spritesFolder = Path.Combine(folder, _spritesFolder);
        if (!Directory.Exists(spritesFolder))
            return names;

        foreach (var file in Directory.GetFiles(spritesFolder, "*.png"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            try
            {
                parts.Add(TimbnSprites.Add($"{packId}_{name}", File.ReadAllBytes(file), TimbnSpriteKind.Image));
                names.Add(name);
            }
            catch (Exception ex)
            {
                problems.Add($"{shown}/{_spritesFolder}/{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return names;
    }

    private static IDisposable Report(List<string> problems)
    {
        foreach (var problem in problems)
            Plugin.Logger.LogWarning($"{nameof(TimbnQuestPacks)}|{problem}");

        if (problems.Count == 0 || !PluginConfig.ShowProblemsOnMainMenu.Value)
            return TimbnUndo.None;

        var shown = problems.Take(_maxPopupLines).ToList();
        if (problems.Count > _maxPopupLines)
            shown.Add($"And {problems.Count - _maxPopupLines} more.");

        shown.Add("BepInEx/LogOutput.log has the details.");
        return TimbnMainMenu.AddPopup("Quest pack problems", string.Join("\n\n", shown));
    }

    private static List<string> FindStandalone()
    {
        if (!Directory.Exists(Paths.PluginPath))
            return [];

        return [.. Directory.GetFiles(Paths.PluginPath, _packFile, SearchOption.AllDirectories)
            .Select(file => Path.GetDirectoryName(file)!)
            .Where(folder => Directory.Exists(Path.Combine(folder, _questsFolder)) && Directory.GetFiles(folder, "*.dll").Length == 0)
            .OrderBy(folder => folder, StringComparer.Ordinal)];
    }

    private static string Signature(List<string> packs)
    {
        var signature = new StringBuilder();
        foreach (var folder in packs)
        {
            foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories).OrderBy(file => file, StringComparer.Ordinal))
            {
                var info = new FileInfo(file);
                signature.Append(file).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append('\n');
            }
        }

        return signature.ToString();
    }

    private static string Shown(string folder) =>
        folder.StartsWith(Paths.PluginPath, StringComparison.OrdinalIgnoreCase)
            ? folder[Paths.PluginPath.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/')
            : Path.GetFileName(folder);
}
