using HarmonyLib;
using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public class TimbnGameScriptTests
{
    // Core writes these into the game's switch and slider scripts by name, so a game update that renames one or changes
    // its type would only fail in game. The types are named, since the tests do not load Unity's UI or TextMesh Pro.
    [Theory]
    [InlineData(typeof(UISwitchButton), "increaseButton", "LazyButton")]
    [InlineData(typeof(UISwitchButton), "decreaseButton", "LazyButton")]
    [InlineData(typeof(UISwitchButton), "amountLabel", "TextMeshProUGUI")]
    [InlineData(typeof(UISwitchButton), "gamepadNavigationItem", "GamepadNavigationItem")]
    [InlineData(typeof(UISwitchButton), "decreaseKeys", "IReadOnlyList`1")]
    [InlineData(typeof(UISwitchButton), "increaseKeys", "IReadOnlyList`1")]
    [InlineData(typeof(UISlider), "slider", "Slider")]
    [InlineData(typeof(UISlider), "increaseButton", "Button")]
    [InlineData(typeof(UISlider), "decreaseButton", "Button")]
    [InlineData(typeof(UISlider), "amountLabel", "TextMeshProUGUI")]
    [InlineData(typeof(UISlider), "gamepadNavigationItem", "GamepadNavigationItem")]
    [InlineData(typeof(UISlider), "decreaseKeys", "List`1")]
    [InlineData(typeof(UISlider), "increaseKeys", "List`1")]
    public void TheGameScriptTakesWhatCoreSets(Type script, string field, string type)
    {
        var found = AccessTools.Field(script, field);
        Assert.NotNull(found);
        Assert.Equal(type, found.FieldType.Name);
    }

    // A text box that holds the game's input reads the gamepad through these by name, since the game stops reading it then.
    // A rename would only show as the gamepad never taking over again while a box has the caret.
    [Theory]
    [InlineData(typeof(LazyInput), "gamepad", "GamepadController")]
    [InlineData(typeof(GamepadController), "rewiredPlayer", "Player")]
    [InlineData(typeof(GamepadController), "stick", "Stick")]
    [InlineData(typeof(GamepadController), "rightStick", "Stick")]
    public void TheGameInputHasWhatTheTextBoxReads(Type script, string field, string type)
    {
        var found = AccessTools.Field(script, field);
        Assert.NotNull(found);
        Assert.Equal(type, found.FieldType.Name);
    }
}
