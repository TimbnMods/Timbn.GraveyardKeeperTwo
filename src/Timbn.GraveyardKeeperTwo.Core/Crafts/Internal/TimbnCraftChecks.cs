namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Holds the checks mods add for when a craft may start at a station, kept free of game statics so it can be tested.
/// A craft may start when every check added for its id passes. A check that throws counts as passing, so a broken mod
/// can't lock a craft away, and it is reported once rather than on every queue update.
/// </summary>
internal sealed class TimbnCraftChecks<TStation>
{
    private readonly Dictionary<string, List<Func<TStation, bool>>> _checks = [];
    private readonly HashSet<Func<TStation, bool>> _reported = [];

    internal bool IsEmpty => _checks.Count == 0;

    internal IDisposable Add(string craftId, Func<TStation, bool> check)
    {
        if (!_checks.TryGetValue(craftId, out var list))
        {
            list = [];
            _checks[craftId] = list;
        }

        list.Add(check);
        return new TimbnUndo(() =>
        {
            list.Remove(check);
            _reported.Remove(check);
            if (list.Count == 0)
                _checks.Remove(craftId);
        });
    }

    internal bool Allows(string craftId, TStation station, Action<Func<TStation, bool>, Exception> report)
    {
        if (!_checks.TryGetValue(craftId, out var list))
            return true;

        foreach (var check in list.ToList())
        {
            try
            {
                if (!check(station))
                    return false;
            }
            catch (Exception exception)
            {
                if (_reported.Add(check))
                    report(check, exception);
            }
        }

        return true;
    }
}

/// <summary>The checks for every craft, asked by the game's own start check through CraftComponentPatch.</summary>
internal static class TimbnCraftRequirements
{
    private static readonly TimbnCraftChecks<WgoData> _checks = new();

    internal static IDisposable Add(string craftId, Func<WgoData, bool> check) => _checks.Add(craftId, check);

    internal static bool Allows(string craftId, WgoData station) =>
        _checks.IsEmpty || _checks.Allows(craftId, station, (check, exception) =>
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnCraftRequirements)}|The check {TimbnSafe.Describe(check)} for craft {craftId} threw, so the craft is allowed. It is only reported once. {exception}"));
}
