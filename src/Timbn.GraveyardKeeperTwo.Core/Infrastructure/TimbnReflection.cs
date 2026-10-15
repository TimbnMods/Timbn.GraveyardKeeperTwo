using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reaches the game's private fields and methods, logging one warning and returning null when a game update renamed
/// or changed one, so the caller can skip what needs it instead of failing to load.
/// </summary>
internal static class TimbnReflection
{
    internal static AccessTools.FieldRef<TObject, TField>? TryField<TObject, TField>(string name)
        where TObject : class
    {
        try
        {
            return AccessTools.FieldRefAccess<TObject, TField>(name);
        }
        catch (Exception ex)
        {
            Warn($"{typeof(TObject).Name}.{name}", ex);
            return null;
        }
    }

    internal static TDelegate? TryDelegate<TDelegate>(Func<MethodInfo?> find, string what)
        where TDelegate : Delegate
    {
        try
        {
            if (find() is { } method)
                return AccessTools.MethodDelegate<TDelegate>(method);

            Warn(what, null);
            return null;
        }
        catch (Exception ex)
        {
            Warn(what, ex);
            return null;
        }
    }

    private static void Warn(string what, Exception? ex) =>
        TimbnCorePlugin.Logger?.LogWarning($"{nameof(TimbnReflection)}|The game's {what} was not found{(ex == null ? "" : $" ({ex.Message})")}, so what needs it is skipped.");
}
