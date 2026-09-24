using LazyBearTechnology;
using System.Collections.Generic;
using System.Linq;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class HiddenKeyBindings
{
    private static readonly AccessTools.FieldRef<UIGameBindingSettingsWindow, List<GameKey>> _keysToBind =
        AccessTools.FieldRefAccess<UIGameBindingSettingsWindow, List<GameKey>>("keysToBind");

    public static Dictionary<KeyBinding, KeyCode> Snapshot(UIGameBindingSettingsWindow window) =>
        Hidden(window).ToDictionary(binding => binding, binding => binding.keyCode);

    public static void Restore(Dictionary<KeyBinding, KeyCode> saved)
    {
        List<string> restored = [];
        foreach (var pair in saved)
        {
            if (pair.Key.keyCode == pair.Value)
                continue;

            pair.Key.keyCode = pair.Value;
            restored.Add(NameOf(pair.Key));
        }

        Save(restored, "Kept");
    }

    public static void RepairCleared(UIGameBindingSettingsWindow window)
    {
        var defaults = GameSettings.Instance.defaultKeyboardKeybindings;
        List<string> repaired = [];
        foreach (var binding in Hidden(window).Where(binding => binding.keyCode == KeyCode.None))
        {
            var fallback = defaults.FirstOrDefault(saved => saved.gameKeyValue == binding.gameKey.value);
            if (fallback == null || fallback.keyCode == KeyCode.None)
                continue;

            binding.keyCode = fallback.keyCode;
            repaired.Add(NameOf(binding));
        }

        Save(repaired, "Put back");
    }

    private static IEnumerable<KeyBinding> Hidden(UIGameBindingSettingsWindow window)
    {
        var shown = new HashSet<int>(_keysToBind(window).Select(key => key.value));
        return LazyInput.GameBindings.keyBindings.Where(binding =>
            !shown.Contains(binding.gameKey.value) && binding.gameKey.value != GameKey.SpeechSkip2.value);
    }

    private static void Save(List<string> changed, string verb)
    {
        if (changed.Count == 0)
            return;

        GameSettings.Instance.SaveCurrentGameBindings();
        ControllerIconLibrary.UpdateStandaloneIcons();
        Plugin.Logger.LogInfo($"{verb} the key for {string.Join(", ", changed)}, which the controls menu does not list.");
    }

    private static string NameOf(KeyBinding binding) =>
        Enumeration.GetNameOfStaticField<GameKey>(binding.gameKey.value) ?? binding.gameKey.value.ToString();
}
