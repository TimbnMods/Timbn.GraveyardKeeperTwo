using System.Collections;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Keeps one script that fails to load from stopping ScriptEngine's F6 reload. Each script that throws is logged and
/// skipped, the rest still load, and a summary line after the reload names every script that was skipped. Does
/// nothing when ScriptEngine is not installed.
/// </summary>
internal static class TimbnScriptReloads
{
    private const string _scriptEngineType = "ScriptEngine.ScriptEngine";
    private const string _harmonyId = "Timbn.GraveyardKeeperTwo.Core.ScriptReloads";

    private static readonly List<string> _skipped = [];
    private static ManualLogSource? _logger;

    internal static IDisposable Install(ManualLogSource logger)
    {
        var engine = AccessTools.TypeByName(_scriptEngineType);
        var reload = engine == null ? null : AccessTools.Method(engine, "ReloadPlugins");
        var load = engine == null ? null : AccessTools.Method(engine, "LoadDLL");
        if (reload == null || load == null)
        {
            if (engine != null)
                logger.LogWarning($"{nameof(TimbnScriptReloads)}|ScriptEngine changed, so one script failing to load still stops the F6 reload.");

            return TimbnUndo.None;
        }

        _logger = logger;
        var harmony = new Harmony(_harmonyId);
        harmony.Patch(reload, prefix: Hook(nameof(ReloadPluginsPreFix)), postfix: Hook(nameof(ReloadPluginsPostFix)));
        harmony.Patch(load, finalizer: Hook(nameof(LoadDLLFinalizer)));
        return new TimbnUndo(() =>
        {
            harmony.UnpatchSelf();
            _logger = null;
        });
    }

    private static HarmonyMethod Hook(string name) => new(typeof(TimbnScriptReloads).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static));

    private static void ReloadPluginsPreFix()
    {
        _skipped.Clear();
        if (TimbnCorePlugin.Instance is not { } core)
            return;

        // Every page closes, the Mods window's questions included, since their buttons would act on the mods being swapped out.
        TimbnPageWindow.CloseAll();
        TimbnModsScreen.CloseForReload();
        TimbnBalance.HoldRemovals();
        TimbnSprites.HoldRemovals();
        core.StartCoroutine(ReleaseAfterReload());
    }

    // ScriptEngine adds each new plugin one frame later from a coroutine of its own, so the end of the next frame is
    // after all of them have registered.
    private static IEnumerator ReleaseAfterReload()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        TimbnSafe.Run(TimbnBalance.ReleaseHeldRemovals, $"{nameof(TimbnScriptReloads)}|Removing what the reloaded plugins left out");
        TimbnSafe.Run(TimbnSprites.ReleaseHeldRemovals, $"{nameof(TimbnScriptReloads)}|Removing the sprites the reloaded plugins left out");
    }

    private static void ReloadPluginsPostFix()
    {
        if (_skipped.Count > 0)
            _logger?.LogError($"F6 reload skipped {_skipped.Count} script(s) that failed to load, the rest loaded: {string.Join(", ", _skipped)}.");
    }

    private static Exception? LoadDLLFinalizer(Exception? __exception, string path)
    {
        if (__exception == null)
            return null;

        var name = Path.GetFileNameWithoutExtension(path);
        _skipped.Add(name);
        var hint = __exception is TypeLoadException or MissingMemberException or ReflectionTypeLoadException or BadImageFormatException
            ? " It was likely built against a newer Timbn Core than the one running, so close the game and deploy Core."
            : "";
        _logger?.LogError($"Could not reload {name}, skipped it and went on with the other scripts.{hint} {__exception.GetType().Name}: {__exception.Message}");
        return null;
    }
}
