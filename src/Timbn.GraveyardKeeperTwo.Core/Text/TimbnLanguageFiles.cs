namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnLanguageFiles
{
    private const string _folderName = "lang";

    internal static Dictionary<string, Dictionary<string, string>> Read(string folder, ManualLogSource logger)
    {
        Dictionary<string, Dictionary<string, string>> textsByKey = [];
        var langFolder = Path.Combine(folder, _folderName);
        if (!Directory.Exists(langFolder))
        {
            logger.LogWarning($"No {_folderName} folder at {langFolder}, so its text is missing.");
            return textsByKey;
        }

        foreach (var file in Directory.GetFiles(langFolder, "*.txt"))
        {
            var language = Path.GetFileNameWithoutExtension(file);
            foreach (var (key, text) in ReadFile(file, logger))
            {
                if (!textsByKey.TryGetValue(key, out var texts))
                    textsByKey[key] = texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                texts[language] = text;
            }
        }

        var missing = textsByKey.Where(pair => !pair.Value.ContainsKey(TimbnLocale.DefaultLanguage)).Select(pair => pair.Key).ToList();
        if (missing.Count > 0)
            logger.LogWarning($"{missing.Count} key(s) in {langFolder} have no {TimbnLocale.DefaultLanguage}.txt line: {string.Join(", ", missing)}");

        logger.LogInfo($"Loaded {textsByKey.Count} text key(s) from {langFolder}.");
        return textsByKey;
    }

    private static IEnumerable<(string Key, string Text)> ReadFile(string file, ManualLogSource logger)
    {
        var lines = File.ReadAllLines(file);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            var split = line.IndexOf('=');
            var key = split > 0 ? line.Substring(0, split).Trim() : "";
            if (key.Length == 0)
            {
                logger.LogWarning($"{Path.GetFileName(file)} line {i + 1} is not 'key = text', skipped.");
                continue;
            }

            yield return (key, line.Substring(split + 1).Trim().Replace("\\n", "\n"));
        }
    }
}
