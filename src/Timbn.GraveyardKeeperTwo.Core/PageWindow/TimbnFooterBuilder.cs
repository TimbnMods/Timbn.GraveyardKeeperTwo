namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Fills the button footer of a popup page, which <see cref="TimbnPageBuilder.ButtonFooter"/> hands to its lambda. Each call
/// adds one button to the footer and ties it to one of the four keys of a gamepad's face, and the buttons go side by side in
/// the order they are added. Core does not choose keys for the page. A page with a button, text box or key row has no use
/// for A, since A presses the focused control, so it ties its actions to X with <see cref="AddPrimarySpecial"/> instead.
/// An action does not close a popup, so it calls the close it was given when it is done. A second call for the same key
/// takes the key from the first button.
/// </summary>
/// <example>
/// <code>
/// page.ButtonFooter(footer =>
/// {
///     footer.AddPrimarySpecial("Save", () => { Save(_name); close(); }, new(() => _name.Length == 0));
///     footer.AddSecondary("Cancel", close);
/// });
/// </code>
/// </example>
public sealed class TimbnFooterBuilder
{
    private readonly TimbnPageBuilder _row;

    internal TimbnFooterBuilder(TimbnPageBuilder row) => _row = row;

    /// <summary>Adds a button tied to A (Space on the keyboard), such as OK or Yes.</summary>
    /// <param name="label">The button's text, already in the player's language.</param>
    /// <param name="onPress">Called when the player presses the button or its key.</param>
    /// <param name="disabled">Grays the button out and stops it running. It can be a value or a callback that is read all the time, like a component's <see cref="TimbnComponent.Disabled"/>.</param>
    /// <param name="gamepad">Where the button goes while a gamepad is in use.</param>
    public void AddPrimary(string label, Action onPress, TimbnBinding<bool> disabled = default, TimbnGamepadPlacement gamepad = TimbnGamepadPlacement.Inline) =>
        _row.AddAction(TimbnFaceButton.Primary, label, onPress, disabled, gamepad);

    /// <summary>
    /// Adds a button tied to B (Esc on the keyboard), such as Cancel or No. B and Esc then run it in place of the page's
    /// dismissed callback, and the window shows no separate Back hint. A page that sets
    /// <see cref="TimbnPage.AcceptsBackButton"/> to false has no B, so the button is left out.
    /// </summary>
    /// <param name="label">The button's text, already in the player's language.</param>
    /// <param name="onPress">Called when the player presses the button or its key.</param>
    /// <param name="disabled">Grays the button out and stops it running. It can be a value or a callback that is read all the time, like a component's <see cref="TimbnComponent.Disabled"/>.</param>
    /// <param name="gamepad">Where the button goes while a gamepad is in use.</param>
    public void AddSecondary(string label, Action onPress, TimbnBinding<bool> disabled = default, TimbnGamepadPlacement gamepad = TimbnGamepadPlacement.Inline) =>
        _row.AddAction(TimbnFaceButton.Secondary, label, onPress, disabled, gamepad);

    /// <summary>Adds a button tied to X (F on the keyboard), such as Save on a page with a text box.</summary>
    /// <param name="label">The button's text, already in the player's language.</param>
    /// <param name="onPress">Called when the player presses the button or its key.</param>
    /// <param name="disabled">Grays the button out and stops it running. It can be a value or a callback that is read all the time, like a component's <see cref="TimbnComponent.Disabled"/>.</param>
    /// <param name="gamepad">Where the button goes while a gamepad is in use.</param>
    public void AddPrimarySpecial(string label, Action onPress, TimbnBinding<bool> disabled = default, TimbnGamepadPlacement gamepad = TimbnGamepadPlacement.Inline) =>
        _row.AddAction(TimbnFaceButton.PrimarySpecial, label, onPress, disabled, gamepad);

    /// <summary>Adds a button tied to Y, such as Discard.</summary>
    /// <param name="label">The button's text, already in the player's language.</param>
    /// <param name="onPress">Called when the player presses the button or its key.</param>
    /// <param name="disabled">Grays the button out and stops it running. It can be a value or a callback that is read all the time, like a component's <see cref="TimbnComponent.Disabled"/>.</param>
    /// <param name="gamepad">Where the button goes while a gamepad is in use.</param>
    public void AddSecondarySpecial(string label, Action onPress, TimbnBinding<bool> disabled = default, TimbnGamepadPlacement gamepad = TimbnGamepadPlacement.Inline) =>
        _row.AddAction(TimbnFaceButton.SecondarySpecial, label, onPress, disabled, gamepad);
}
