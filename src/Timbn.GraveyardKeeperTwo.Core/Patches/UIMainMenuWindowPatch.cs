namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(UIMainMenuWindow), nameof(UIMainMenuWindow.Open))]
internal static class UIMainMenuWindowPatch
{
    private static void Postfix(UIMainMenuWindow __instance) => TimbnMainMenu.OnMenuOpened(__instance);
}
