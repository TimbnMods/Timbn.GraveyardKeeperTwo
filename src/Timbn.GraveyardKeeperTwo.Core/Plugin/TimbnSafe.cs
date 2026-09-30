namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnSafe
{
    internal static bool Run(Action? action, ManualLogSource logger, string what)
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

    internal static string Describe(Delegate handler) => $"{handler.Method.DeclaringType?.FullName}.{handler.Method.Name}";
}
