namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>Paints a plugin's answer button in its own color, and keeps that color when the pointer moves on and off it.</summary>
[HarmonyPatch(typeof(UIMultiAnswerOption))]
internal static class UIMultiAnswerOptionPatch
{
    [HarmonyPatch(nameof(UIMultiAnswerOption.Show))]
    [HarmonyPostfix]
    private static void ShowPostFix(UIMultiAnswerOption __instance) =>
        TimbnSafe.Run(() => TimbnAnswerColors.Paint(__instance), $"{nameof(UIMultiAnswerOptionPatch)}|Painting an answer");

    [HarmonyPatch(nameof(UIMultiAnswerOption.ChangeColor))]
    [HarmonyPrefix]
    private static void ChangeColorPreFix(UIMultiAnswerOption __instance, ref Color color)
    {
        var shown = color;
        TimbnSafe.Run(() => shown = TimbnAnswerColors.Recolor(__instance, shown), $"{nameof(UIMultiAnswerOptionPatch)}|Recoloring an answer");
        color = shown;
    }
}
