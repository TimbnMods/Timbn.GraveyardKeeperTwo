using BepInEx.Bootstrap;
using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnLoadErrors
{
    private const int _maxLines = 6;

    private static readonly Regex _missing = new(@"^Could not load \[(.+)\] because it has missing dependencies: (.+)$");
    private static readonly Regex _skipped = new(@"^Skipping \[(.+)\] because it has a dependency that was not loaded");
    private static readonly Regex _incompatible = new(@"^Could not load \[(.+)\] because it is incompatible with: (.+)$");
    private static readonly Regex _wrongBepInEx = new(@"^Plugin \[.+\] targets a wrong version of BepInEx");
    private static readonly Regex _dependency = new(@"^(\S+)(?: \(v(.+) or newer\))?$");

    private static readonly List<string> _startFailures = [];

    internal static void AddStartFailure(string plugin) => _startFailures.Add(plugin);

    internal static string? Describe()
    {
        List<string> lines = [];
        foreach (var error in Chainloader.DependencyErrors)
        {
            if (Describe(error) is { } line)
                lines.Add(line);
        }

        foreach (var plugin in _startFailures)
            lines.Add($"{plugin} is off because a game update changed something it needs.");

        if (lines.Count == 0)
            return null;

        var shown = lines.Take(_maxLines).ToList();
        if (lines.Count > _maxLines)
            shown.Add($"And {lines.Count - _maxLines} more.");
        shown.Add("BepInEx/LogOutput.log has the details.");
        return string.Join("\n\n", shown);
    }

    private static string? Describe(string error)
    {
        if (_wrongBepInEx.IsMatch(error))
            return null;

        var match = _missing.Match(error);
        if (match.Success)
        {
            var needs = match.Groups[2].Value.Split([", "], StringSplitOptions.RemoveEmptyEntries).Select(DescribeDependency);
            return $"{match.Groups[1].Value} needs {string.Join(" and ", needs)}.";
        }

        match = _skipped.Match(error);
        if (match.Success)
            return $"{match.Groups[1].Value} is off because a mod it needs did not load.";

        match = _incompatible.Match(error);
        if (match.Success)
        {
            var others = match.Groups[2].Value.Split([", "], StringSplitOptions.RemoveEmptyEntries).Select(NameOf);
            return $"{match.Groups[1].Value} can't run alongside {string.Join(" and ", others)}.";
        }

        return error;
    }

    private static string DescribeDependency(string entry)
    {
        var match = _dependency.Match(entry);
        if (!match.Success)
            return entry;

        var guid = match.Groups[1].Value;
        var minimum = match.Groups[2].Success ? match.Groups[2].Value : null;
        if (Chainloader.PluginInfos.TryGetValue(guid, out var info))
        {
            var installed = info.Metadata.Version;
            return minimum is null ? info.Metadata.Name : $"{info.Metadata.Name} {minimum} or newer (you have {installed})";
        }

        return minimum is null ? $"{guid}, which is missing" : $"{guid} {minimum} or newer, which is missing";
    }

    private static string NameOf(string guid) =>
        Chainloader.PluginInfos.TryGetValue(guid, out var info) ? info.Metadata.Name : guid;
}
