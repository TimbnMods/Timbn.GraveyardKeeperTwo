namespace Timbn.GraveyardKeeperTwo.Core.Patches;

/// <summary>
/// Writes the mod save data next to the game's save and deletes it with the slot. While the game writes its own save,
/// Core takes out what mods added (plugin talk events come off NPCs, spawned people leave the world, claimed spots are
/// freed, played animations are cleared, what the player carries for a mod is taken from their hands, potions revert
/// their saved changes, and mod items, buffs, crafts, stations, hotbar pins and quests move into the Timbn save file)
/// and writes the game's own people it moved where the base game last had them, then puts it all back.
/// </summary>
[HarmonyPatch(typeof(SaveSystem))]
internal static class SaveSystemPatch
{
    [HarmonyPatch(nameof(SaveSystem.Save))]
    [HarmonyPrefix]
    private static void SavePreFix(SaveSlotData slotData, GameSave gameSave, ref Action? callbackSuccessful, out List<IDisposable> __state)
    {
        try
        {
            callbackSuccessful = TimbnSaves.WrapSaveCallback(slotData, gameSave, callbackSuccessful);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(SaveSystemPatch)}|Could not add mod save data to the save: {ex}");
        }

        __state = [];
        var isLiveSave = MainGame.Instance != null && gameSave == MainGame.Instance.GameSave;
        if (isLiveSave)
            TimbnSafe.Run(TimbnSaveEvents.RaiseBefore, TimbnCorePlugin.Logger, nameof(TimbnSaveEvents.Before));

        Strip(__state, () => TimbnQuests.HideForSave(gameSave));
        Strip(__state, TimbnDialog.RemoveForSave);
        Strip(__state, TimbnNpcs.RemoveSpawnedForSave);
        Strip(__state, TimbnNpcs.ReleaseClaimsForSave);
        Strip(__state, TimbnNpc.ClearAnimationsForSave);
        Strip(__state, TimbnOverhead.RemoveForSave);
        Strip(__state, TimbnBaseGamePlaces.PlaceAllForSave);
        Strip(__state, TimbnPotions.RevertForSave);
        if (isLiveSave)
        {
            Strip(__state, () => TimbnModContent.StripForSave(gameSave));
            // First in the list, so the reverse order below runs it last, once everything is back.
            __state.Insert(0, new TimbnUndo(() => TimbnSafe.Run(TimbnSaveEvents.RaiseAfter, TimbnCorePlugin.Logger, nameof(TimbnSaveEvents.After))));
        }
    }

    [HarmonyPatch(nameof(SaveSystem.Save))]
    [HarmonyFinalizer]
    private static void SaveFinalizer(List<IDisposable>? __state)
    {
        if (__state is null)
            return;

        // Newest first, so what a later strip took out of an earlier one's state goes back before that state does.
        for (var i = __state.Count - 1; i >= 0; i--)
        {
            try
            {
                __state[i].Dispose();
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

    private static void Strip(List<IDisposable> undos, Func<IDisposable> strip)
    {
        try
        {
            undos.Add(strip());
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(SaveSystemPatch)}|Could not strip mod state from the save: {ex}");
        }
    }
}
