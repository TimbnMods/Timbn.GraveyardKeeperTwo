namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Runs a handler and logs a throw instead of letting it reach the game, in one wording for all of Core and the
/// mods. Use it around any code a game event or a callback runs, since a throw that reaches the game can drop the
/// handler for the rest of the session or stop a frame half done.
/// </summary>
public static class TimbnSafe
{
    /// <summary>Runs the code and logs a throw as an error naming what threw.</summary>
    /// <example>
    /// <code>
    /// TimbnSafe.Run(LoadPacks, Logger, $"{nameof(QuestPacks)}|Loading the quest packs");
    /// </code>
    /// </example>
    /// <param name="action">The code to run. Null counts as having run.</param>
    /// <param name="logger">The mod's Logger, so the line shows under the mod's name.</param>
    /// <param name="what">What the code is, for the log line, such as a class and what it was doing.</param>
    /// <returns>True when the code ran through, false when it threw.</returns>
    public static bool Run(Action? action, ManualLogSource logger, string what)
    {
        if (action is null)
            return true;

        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"{what} threw: {ex}");
            return false;
        }
    }

    internal static bool Run(Action? action, string what) => Run(action, TimbnCorePlugin.Logger, what);

    // Asks a yes or no handler and counts a throw as no, for a check made every frame, where Run would build a closure to carry the answer out.
    internal static bool Check(Func<bool> condition, ManualLogSource logger, string what)
    {
        try
        {
            return condition();
        }
        catch (Exception ex)
        {
            logger.LogError($"{what} threw: {ex}");
            return false;
        }
    }

    internal static string Describe(Delegate handler) => $"{handler.Method.DeclaringType?.FullName}.{handler.Method.Name}";
}
