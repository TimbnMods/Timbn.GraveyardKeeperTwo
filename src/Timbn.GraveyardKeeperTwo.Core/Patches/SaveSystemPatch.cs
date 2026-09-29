namespace Timbn.GraveyardKeeperTwo.Core.Patches;

[HarmonyPatch(typeof(SaveSystem))]
internal static class SaveSystemPatch
{
    [HarmonyPatch(nameof(SaveSystem.Save))]
    [HarmonyPrefix]
    private static void SavePreFix(SaveSlotData slotData, GameSave gameSave, ref Action? callbackSuccessful, out IDisposable[] __state)
    {
        try
        {
            callbackSuccessful = TimbnSaves.WrapSaveCallback(slotData, gameSave, callbackSuccessful);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(SaveSystemPatch)}|Could not add mod save data to the save: {ex}");
        }

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

    [HarmonyPatch(nameof(SaveSystem.Save))]
    [HarmonyFinalizer]
    private static void SaveFinalizer(IDisposable[]? __state)
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

    [HarmonyPatch(nameof(SaveSystem.Remove))]
    [HarmonyPostfix]
    private static void RemovePostFix(SaveSlotData slotData, bool __result)
    {
        if (!__result || slotData.isDemoSave)
            return;

        try
        {
            TimbnSaves.DeleteSlot(slotData.slotName);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(SaveSystemPatch)}|Could not delete mod save data for slot {slotData.slotName}: {ex}");
        }
    }
}
