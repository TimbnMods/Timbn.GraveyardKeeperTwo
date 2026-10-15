namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The two moments around Core's cleanup of a save write. Mod handlers for <see cref="Before"/> run ahead of Core's own
/// strip and the ones for <see cref="After"/> run once Core has put everything back, so Core's cleanup is always the
/// innermost layer.
/// </summary>
internal static class TimbnSaveEvents
{
    internal static event Action? Before;

    internal static event Action? After;

    internal static void RaiseBefore() => Before?.Invoke();

    internal static void RaiseAfter() => After?.Invoke();
}
