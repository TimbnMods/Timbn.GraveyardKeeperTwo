using LazyBearTechnology;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds the Mod Settings button under Settings on the main menu and in the pause menu, and takes every copy out
/// again when the mod unloads. The main menu lays its buttons out once when it opens and then turns its layout
/// groups off, so a button another mod adds later (GK2 Mod Framework's Mods button) is placed by hand and can land on
/// ours. Running the game's own layout pass again after every mod's Open postfix puts them all in order.
/// </summary>
internal static class TimbnModSettingsButton
{
    internal const string LabelToken = "timbn_mod_settings";
    internal const string LabelText = "Mod Settings";

    private const string _name = "TimbnModSettingsButton";
    private const string _spacerName = "TimbnModSettingsSpacing";

    private static readonly List<GameObject> _created = [];

    private static readonly AccessTools.FieldRef<UIMainMenuWindow, LazyButton> _settingsButton =
        AccessTools.FieldRefAccess<UIMainMenuWindow, LazyButton>("gameSettingsButton");

    private static readonly AccessTools.FieldRef<UIGamePauseWindow, LazyButton> _pauseSettingsButton =
        AccessTools.FieldRefAccess<UIGamePauseWindow, LazyButton>("settingsBtn");

    // A reload comes after the menus were set up, so the buttons go into the menus that already exist.
    internal static void AddToOpenMenus()
    {
        foreach (var menu in Resources.FindObjectsOfTypeAll<UIMainMenuWindow>().Where(window => window.gameObject.scene.IsValid()))
        {
            Create(menu);
            Relayout(menu);
        }

        foreach (var pause in Resources.FindObjectsOfTypeAll<UIGamePauseWindow>().Where(window => window.gameObject.scene.IsValid()))
            CreateInPause(pause);
    }

    // Other mods insert their buttons next to Settings too, so this runs after every Open to put ours back right under it.
    internal static void MoveUnderSettings(UIMainMenuWindow menu) => MoveUnder(_settingsButton(menu));

    internal static void MoveUnderSettings(UIGamePauseWindow pause) => MoveUnder(_pauseSettingsButton(pause));

    private static void MoveUnder(LazyButton settings)
    {
        if (settings == null)
            return;

        var parent = settings.transform.parent;
        var spacer = parent.Find(_spacerName);
        var button = parent.Find(_name);
        var index = settings.transform.GetSiblingIndex();
        if (spacer != null)
            spacer.SetSiblingIndex(++index);

        if (button != null)
            button.SetSiblingIndex(index + 1);
    }

    internal static void RemoveAll()
    {
        foreach (var created in _created)
        {
            if (created != null)
                UnityEngine.Object.DestroyImmediate(created);
        }

        _created.Clear();
        foreach (var menu in Resources.FindObjectsOfTypeAll<UIMainMenuWindow>().Where(window => window.gameObject.scene.IsValid()))
            Relayout(menu);
    }

    internal static void Create(UIMainMenuWindow menu)
    {
        var settings = _settingsButton(menu);
        if (settings == null || settings.transform.parent.Find(_name) != null)
            return;

        var clone = UnityEngine.Object.Instantiate(settings.gameObject, settings.transform.parent);
        clone.name = _name;
        Track(clone);
        clone.transform.SetSiblingIndex(settings.transform.GetSiblingIndex() + 1);

        var button = clone.GetComponent<LazyButton>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Open);
        button.SetCallbacksIntoGamepadNavigationItem();
        Relabel(clone);
    }

    // A scene change destroys the pause menu with its copy, so the dead ones are dropped as new ones come.
    private static void Track(GameObject created)
    {
        _created.RemoveAll(old => old == null);
        _created.Add(created);
    }

    // The same pass the menu's own Open ends with, which does nothing while the menu is hidden.
    internal static void Relayout(UIMainMenuWindow menu) => ((RectTransform)menu.transform).RefreshContentFitterAndDisable();

    // Placed the way the game adds its own bug report button, with a copy of the spacing before Settings when there is one.
    internal static void CreateInPause(UIGamePauseWindow pause)
    {
        var settings = _pauseSettingsButton(pause);
        if (settings == null || settings.transform.parent.Find(_name) != null)
            return;

        var parent = settings.transform.parent;
        var index = settings.transform.GetSiblingIndex();
        var clone = UnityEngine.Object.Instantiate(settings.gameObject, parent);
        clone.name = _name;
        Track(clone);
        clone.transform.SetSiblingIndex(index + 1);
        if (index > 0 && parent.GetChild(index - 1) is var spacer && spacer.GetComponent<LazyButton>() == null)
        {
            var space = UnityEngine.Object.Instantiate(spacer.gameObject, parent);
            space.name = _spacerName;
            Track(space);
            space.transform.SetSiblingIndex(index + 1);
        }

        var button = clone.GetComponent<LazyButton>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            pause.Close();
            if (!TimbnModsScreen.Show(() => pause.Open(null)))
                pause.Open(null);
        });
        button.SetCallbacksIntoGamepadNavigationItem();
        Relabel(clone);
    }

    private static void Relabel(GameObject clone)
    {
        foreach (var label in clone.GetComponentsInChildren<LocalizedLabel>(includeInactive: true))
        {
            label.langToken = LabelToken;
            label.Localize();
        }
    }

    // Only the main menu's button calls this, but in a save the main menu window still exists, and closing and
    // reopening it there would put it over the game.
    private static void Open()
    {
        if (TimbnGame.IsInGame)
        {
            TimbnModsScreen.Show();
            return;
        }

        var menu = LazyUI.GetWindow<UIMainMenuWindow>();
        if (menu == null)
        {
            TimbnModsScreen.Show();
            return;
        }

        menu.Close();
        if (!TimbnModsScreen.Show(() => menu.Open(null)))
            menu.Open(null);
    }
}
