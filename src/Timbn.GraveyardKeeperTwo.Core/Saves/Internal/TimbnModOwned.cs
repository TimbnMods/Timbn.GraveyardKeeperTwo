namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Decides whether a saved id belongs to a mod and has to stay out of the game's save. An id is a mod's when a mod
/// added its definition through Core, or when no definition exists for it at all, which is what a save holds once
/// its mod is gone. The game's own placeholder ids never count.
/// </summary>
internal sealed class TimbnModOwned
{
    private readonly HashSet<string> _added;
    private readonly Func<string, bool> _exists;
    private readonly HashSet<string> _ignored;

    public TimbnModOwned(HashSet<string> added, Func<string, bool> exists, params string[] ignored)
    {
        _added = added;
        _exists = exists;
        _ignored = new HashSet<string>(ignored, StringComparer.Ordinal);
    }

    /// <summary>Only ids with no definition, which is the check a load runs over a save made before its mod went missing.</summary>
    public static TimbnModOwned Missing(Func<string, bool> exists, params string[] ignored) => new([], exists, ignored);

    public bool Takes(string? id)
    {
        if (string.IsNullOrEmpty(id) || _ignored.Contains(id!))
            return false;

        return _added.Contains(id!) || !_exists(id!);
    }
}
