namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(SaveSystem), nameof(SaveSystem.Save))]
internal static class SaveSystemPatch
{
    private static void Prefix(GameSave gameSave, out IDisposable[] __state)
    {
        try
        {
            __state = [TimbnQuests.HideForSave(gameSave), TimbnDialog.RemoveForSave()];
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(SaveSystemPatch)}|Could not strip mod state from the save: {ex}");
            __state = [];
        }
    }

    private static void Finalizer(IDisposable[]? __state)
    {
        if (__state is null)
            return;

        foreach (var undo in __state)
        {
            try
            {
                undo.Dispose();
            }
            catch (Exception ex)
            {
                TimbnCorePlugin.Logger.LogError($"{nameof(SaveSystemPatch)}|Could not restore mod state after the save: {ex}");
            }
        }
    }
}
