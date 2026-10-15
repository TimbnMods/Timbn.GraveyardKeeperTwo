namespace Timbn.GraveyardKeeperTwo.Core.Patches;

// The DemystifyExceptions patcher replaces Exception.ToString and returns an empty string whenever its stack trace
// builder throws, which leaves log lines like ScriptEngine's "because of exception: " blank. This only fills in an
// empty result, so a working ToString is left alone.
[HarmonyPatch(typeof(Exception))]
internal static class ExceptionPatch
{
    [HarmonyPatch(nameof(Exception.ToString))]
    [HarmonyPostfix]
    private static void ToStringPostFix(Exception __instance, ref string __result)
    {
        if (!string.IsNullOrWhiteSpace(__result))
            return;

        try
        {
            var text = __instance.GetType().FullName;
            if (!string.IsNullOrEmpty(__instance.Message))
                text += $": {__instance.Message}";

            if (__instance.InnerException is { } inner)
                text += $" ---> {inner}";

            __result = $"{text}{Environment.NewLine}{__instance.StackTrace}";
        }
        catch
        {
            __result = __instance.GetType().FullName ?? "Exception";
        }
    }
}
